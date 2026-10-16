using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.Security;
using UPZMG.Implementations.Users;
using UPZMG.Persistence.Models;

namespace UPZMG.Persistence.Repository;

public class UsersRepository(AppDBContext db)
{
    private static readonly PasswordHasher<SystemUser> PasswordHasher = new();
    public async Task<List<SystemUser>> GetAllUsersAsync()
    {
        return await (
            from su in db.SystemUsers.AsNoTracking()
            join ur in db.UserRoles.AsNoTracking() on su.Id equals ur.UserId
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            join e in db.Employees.AsNoTracking() on su.EmployeeId equals e.Id
            // Suspended accounts stay in the listing: hiding them is what makes a suspension
            // irreversible, and the administrator has to be able to bring one back.
            select new SystemUser
            {
                Id = su.Id,
                EmployeeId = su.EmployeeId,
                Email = su.Email,
                IsActive = su.IsActive,
                LastLogin = su.LastLogin,
                Role = new Role
                {
                    Id = r.Id,
                    Name = r.Name
                },
                UserName = su.UserName
            })
            .ToListAsync();
    }

    public async Task<SystemUser> GetSystemUserByIdAsync(Guid userId)
    {
        return await (
            from su in db.SystemUsers.AsNoTracking()
            join ur in db.UserRoles.AsNoTracking() on su.Id equals ur.UserId
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where su.Id == userId
            select new SystemUser
            {
                Id = su.Id,
                EmployeeId = su.EmployeeId,
                UserName = su.UserName,
                Email = su.Email,
                // No password here: only the sign-in lookup needs the hash.
                IsActive = su.IsActive,
                LastLogin = su.LastLogin,
                Role = new Role
                {
                    Id = r.Id,
                    Name = r.Name
                }
            })
            .FirstOrDefaultAsync() ?? new SystemUser();
    }

    public async Task<SystemUser> CreateUserAsync(CreateUser request)
    {
        var employee = await db.Employees.AsNoTracking().Select(e => new { e.Id, e.Email })
            .FirstOrDefaultAsync(e => e.Email == request.Email)
            ?? throw new InvalidOperationException(
                $"No hay ningún empleado con el correo '{request.Email}': el usuario se crea sobre su expediente.");
        
        var user = new SystemUser
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            UserName = request.UserName,
            Email = request.Email,
            IsActive = true,
            LastLogin = null
        };
        user.Password = PasswordHasher.HashPassword(user, request.Password);

        var userrole = new UserRole
        {
            UserId = user.Id,
            RoleId = Guid.Parse(request.RoleId)
        };

        db.SystemUsers.Add(user);
        db.UserRoles.Add(userrole);
        await db.SaveGuardingUniquenessAsync("Ya existe un usuario con ese correo.");

        return user;
    }

    /// <summary>
    /// Suspends or restores an account. Signing in already refuses an inactive user, so this is
    /// the reversible alternative to deleting: the person stops entering, their captures stay
    /// attributed to them. An administrator cannot suspend their own account, which is the one
    /// mistake that would lock everyone out of user management.
    /// </summary>
    public async Task<SystemUser> SetUserActiveAsync(Guid userId, bool isActive, Guid requestedBy)
    {
        var user = await db.SystemUsers.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new InvalidOperationException("El usuario ya no existe.");

        var roleId = await db.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => (Guid?)ur.RoleId)
            .FirstOrDefaultAsync();

        await EnsureAccessSurvivesAsync(userId, requestedBy, isActive, roleId);

        user.IsActive = isActive;
        await db.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Corrects an account. It goes through the same guards as the suspension switch: the edit
    /// dialog can also deactivate an account and change its role, which locks everyone out the
    /// same way, only reached from another screen.
    /// </summary>
    public async Task<SystemUser> EditUserAsync(EditUser request, Guid requestedBy)
    {
        var user = await db.SystemUsers.FirstOrDefaultAsync(u => u.Id == request.UserId)
            ?? throw new InvalidOperationException("El usuario ya no existe.");
        var userRole = await db.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == request.UserId)
            ?? throw new InvalidOperationException("El usuario no tiene un rol asignado.");

        // A missing or unparseable role used to be stored as an empty Guid, which left the
        // account pointing at a role that does not exist: no permissions, and no way back.
        var roleId = userRole.RoleId;
        if (!string.IsNullOrWhiteSpace(request.RoleId))
        {
            if (!Guid.TryParse(request.RoleId, out roleId))
                throw new InvalidOperationException("El rol indicado no es válido.");

            if (!await db.Roles.AnyAsync(r => r.Id == roleId))
                throw new InvalidOperationException("El rol indicado ya no existe.");
        }

        await EnsureAccessSurvivesAsync(request.UserId, requestedBy, request.IsActive, roleId);

        user.UserName = string.IsNullOrWhiteSpace(request.UserName) ? user.UserName : request.UserName;
        user.Password = string.IsNullOrWhiteSpace(request.Password) ? user.Password : PasswordHasher.HashPassword(user, request.Password);
        user.IsActive = request.IsActive;
        userRole.RoleId = roleId;

        await db.SaveChangesAsync();

        return user;
    }

    /// <summary>
    /// Refuses a change that would leave nobody able to sign in, or nobody able to administer
    /// users. The account being changed is judged by the state it is about to have; everybody
    /// else by the state stored now, which this change does not touch.
    /// </summary>
    private async Task EnsureAccessSurvivesAsync(Guid userId, Guid requestedBy, bool isActive, Guid? roleId)
    {
        if (!isActive && userId == requestedBy)
            throw new InvalidOperationException("No puedes darte de baja a ti mismo.");

        if (!isActive && !await db.SystemUsers.AnyAsync(u => u.Id != userId && u.IsActive))
            throw new InvalidOperationException("Es la única cuenta activa: dar de baja dejaría el sistema sin acceso.");

        var keepsAdministering = isActive
            && roleId is { } role
            && await (
                from rp in db.RolePermissions.AsNoTracking()
                join p in db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
                where rp.RoleId == role && p.Name == AppPermissions.ManageUsers
                select rp.Id)
                .AnyAsync();

        if (keepsAdministering) return;

        var othersAdminister = await (
            from u in db.SystemUsers.AsNoTracking()
            join ur in db.UserRoles.AsNoTracking() on u.Id equals ur.UserId
            join rp in db.RolePermissions.AsNoTracking() on ur.RoleId equals rp.RoleId
            join p in db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
            where u.Id != userId && u.IsActive && p.Name == AppPermissions.ManageUsers
            select u.Id)
            .AnyAsync();

        if (!othersAdminister)
            throw new InvalidOperationException(
                "Es la única cuenta activa que puede administrar usuarios: el cambio dejaría el sistema sin administración.");
    }


    // Authentication methods
    /// <summary>
    /// Stamps a successful sign-in. The column existed and the user list showed it, but nothing
    /// wrote it: every account carried the date it was seeded with.
    /// </summary>
    public Task RegisterSignInAsync(Guid userId) =>
        db.SystemUsers
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(set => set.SetProperty(u => u.LastLogin, DateTime.UtcNow));

    /// <summary>The user as the API returns it: everything except the hash.</summary>
    public static UserView ToResponse(SystemUser user) =>
        new(user.Id, user.EmployeeId, user.UserName, user.Email, user.IsActive, user.LastLogin,
            user.Role?.Id, user.Role?.Name);

    public async Task<SystemUser> GetSystemUserByEmailAsync(string email)
    {
        return await (
            from su in db.SystemUsers.AsNoTracking()
            join ur in db.UserRoles.AsNoTracking() on su.Id equals ur.UserId
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where su.Email == email && su.IsActive
            select new SystemUser
            {
                Id = su.Id,
                EmployeeId = su.EmployeeId,
                UserName = su.UserName,
                Email = su.Email,
                Password = su.Password,
                IsActive = su.IsActive,
                LastLogin = su.LastLogin,
                Role = new Role
                {
                    Id = r.Id,
                    Name = r.Name
                }
            })
            .FirstOrDefaultAsync() ?? new SystemUser();
    }
}