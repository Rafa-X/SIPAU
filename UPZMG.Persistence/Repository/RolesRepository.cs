using Microsoft.EntityFrameworkCore;
using UPZMG.Persistence.Models;
using UPZMG.Implementations;

namespace UPZMG.Persistence.Repository;

public class RolesRepository(AppDBContext _db)
{
    private readonly AppDBContext db = _db;

    public async Task<List<Role>> GetAllRolesAsync(GetAllRoles request)
    {
        return await db.Roles
            .AsNoTracking()
            .Select(role => new Role
            {
                Id = role.Id,
                Name = role.Name
            })
            .ToListAsync();
    }
}
