using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
            ?? throw new Exception($"No employee found with email {request.Email}");
        
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
        if (!isActive && userId == requestedBy)
            throw new InvalidOperationException("No puedes darte de baja a ti mismo.");

        var user = await db.SystemUsers.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new InvalidOperationException("El usuario ya no existe.");

        if (!isActive && !await db.SystemUsers.AnyAsync(u => u.Id != userId && u.IsActive))
            throw new InvalidOperationException("Es la única cuenta activa: dar de baja dejaría el sistema sin acceso.");

        user.IsActive = isActive;
        await db.SaveChangesAsync();
        return user;
    }

    public async Task<SystemUser> EditUserAsync(EditUser request)
    {
        var user = await db.SystemUsers.FirstOrDefaultAsync(u => u.Id == request.UserId) 
            ?? throw new Exception($"No user found with ID {request.UserId}");
        var userRole = await db.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == request.UserId)
            ?? throw new Exception($"No user role found for user ID {request.UserId}");
        
        user.UserName = string.IsNullOrWhiteSpace(request.UserName) ? user.UserName : request.UserName;
        user.Password = string.IsNullOrWhiteSpace(request.Password) ? user.Password : PasswordHasher.HashPassword(user, request.Password);
        user.IsActive = request.IsActive;
        userRole.RoleId = Guid.Parse(request.RoleId ?? Guid.Empty.ToString());

        await db.SaveChangesAsync();

        return user;
    }


    // Authentication methods
    /// <summary>The user as the API returns it: everything except the hash.</summary>
    public static UserView ToResponse(SystemUser user) =>
        new(user.Id, user.EmployeeId, user.UserName, user.Email, user.IsActive, user.LastLogin,
            user.Role?.Id, user.Role?.Name);

    public async Task<UserView> GetResponseAsync(Guid userId)
    {
        var user = await GetSystemUserByIdAsync(userId);
        return ToResponse(user);
    }

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