using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.DataCapture;
using UPZMG.Persistence.Models;

namespace UPZMG.Persistence.Repository;

public sealed class ClassGroupsRepository(AppDBContext db)
{
    public async Task<ClassGroups> CreateAsync(CreateClassGroup request)
    {
        var group = new ClassGroups
        {
            Id = Guid.NewGuid(),
            FacilityId = request.FacilityId,
            Schedule = request.Schedule.Select(s => new Schedule { Day = s.Day.Trim(), Time = s.Time.Trim() }).ToList(),
            PeriodId = request.PeriodId,
            CareerId = request.CareerId,
            AsignatureId = request.AsignatureId,
            Weekhours = request.Weekhours,
            TeacherId = request.TeacherId,
            Capacity = request.Capacity,
            IsTheory = request.IsTheory,
            Enrolled = 0,
        };

        db.ClassGroups.Add(group);
        await db.SaveChangesAsync();
        return group;
    }
}
