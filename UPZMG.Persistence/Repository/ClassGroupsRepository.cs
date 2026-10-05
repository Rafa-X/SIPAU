using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.DataCapture;
using UPZMG.Persistence.Models;

namespace UPZMG.Persistence.Repository;

public sealed partial class ClassGroupsRepository(AppDBContext db)
{
    public async Task<ClassGroups> CreateAsync(CreateClassGroup request)
    {
        var sameSpace = await db.ClassGroups
            .AsNoTracking()
            .Where(g => g.FacilityId == request.FacilityId && g.PeriodId == request.PeriodId)
            .ToListAsync();

        foreach (var existing in sameSpace)
        {
            var conflict = FindScheduleConflict(request.Schedule, existing.Schedule);
            if (conflict is { } day)
                throw new InvalidOperationException(
                    $"El espacio ya está ocupado el {day} en un horario que se traslapa con otro grupo de este periodo.");
        }

        var group = new ClassGroups
        {
            Id = Guid.NewGuid(),
            FacilityId = request.FacilityId,
            Schedule = request.Schedule.Select(s => new Schedule { Day = s.Day.Trim(), Time = s.Time.Trim() }).ToList(),
            PeriodId = request.PeriodId,
            CareerId = request.CareerId,
            CourseId = request.CourseId,
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

    /// <summary>The whole group, to fill the correction form.</summary>
    public async Task<UpdateClassGroup?> GetAsync(Guid id)
    {
        var group = await db.ClassGroups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);
        if (group is null) return null;

        return new UpdateClassGroup
        {
            Id = group.Id,
            RequesterKey = Guid.Empty,
            Schedule = group.Schedule,
            CourseId = group.CourseId,
            PeriodId = group.PeriodId,
            CareerId = group.CareerId,
            TeacherId = group.TeacherId,
            FacilityId = group.FacilityId,
            IsTheory = group.IsTheory,
            Capacity = group.Capacity,
            Weekhours = group.Weekhours,
        };
    }

    /// <summary>
    /// Corrects a captured group. The room clash is checked again against the other groups of
    /// the period — excluding this one, so a group never collides with its own former schedule.
    /// </summary>
    public async Task<ClassGroups> UpdateAsync(UpdateClassGroup request)
    {
        var group = await db.ClassGroups.FirstOrDefaultAsync(g => g.Id == request.Id)
            ?? throw new InvalidOperationException("El grupo ya no existe.");

        if (request.Capacity < group.Enrolled)
            throw new InvalidOperationException(
                $"El grupo ya tiene {group.Enrolled} inscritos: la capacidad no puede quedar por debajo.");

        // Only what the correction actually moves is re-checked. Refusing to fix a capacity
        // because of a clash the group did not create would leave old records uncorrectable.
        bool moved = group.FacilityId != request.FacilityId
            || group.PeriodId != request.PeriodId
            || !SameSchedule(group.Schedule, request.Schedule);

        if (moved)
        {
            var sameSpace = await db.ClassGroups
                .AsNoTracking()
                .Where(g => g.Id != request.Id && g.FacilityId == request.FacilityId && g.PeriodId == request.PeriodId)
                .ToListAsync();

            foreach (var existing in sameSpace)
            {
                var conflict = FindScheduleConflict(request.Schedule, existing.Schedule);
                if (conflict is { } day)
                    throw new InvalidOperationException(
                        $"El espacio ya está ocupado el {day} en un horario que se traslapa con otro grupo de este periodo.");
            }
        }

        group.FacilityId = request.FacilityId;
        group.Schedule = request.Schedule.Select(s => new Schedule { Day = s.Day.Trim(), Time = s.Time.Trim() }).ToList();
        group.PeriodId = request.PeriodId;
        group.CareerId = request.CareerId;
        group.CourseId = request.CourseId;
        group.Weekhours = request.Weekhours;
        group.TeacherId = request.TeacherId;
        group.Capacity = request.Capacity;
        group.IsTheory = request.IsTheory;

        await db.SaveChangesAsync();
        return group;
    }

    // Captured class groups with display-friendly names instead of raw foreign keys.
    public async Task<List<ClassGroupListItem>> GetClassGroupsListAsync()
    {
        var query =
            from g in db.ClassGroups.AsNoTracking()
            join c in db.Careers.AsNoTracking() on g.CareerId equals c.Id
            join a in db.Courses.AsNoTracking() on g.CourseId equals a.Id
            join t in db.Employees.AsNoTracking() on g.TeacherId equals t.Id
            join f in db.Set<SchoolFacility>().AsNoTracking() on g.FacilityId equals f.Id
            join p in db.Periods.AsNoTracking() on g.PeriodId equals p.Id
            select new ClassGroupListItem
            {
                Id = g.Id,
                PeriodId = g.PeriodId,
                FacilityId = g.FacilityId,
                CareerId = g.CareerId,
                Career = c.Name ?? "Sin nombre",
                Course = a.CourseName,
                Teacher = t.FullName,
                Facility = f.Name,
                Period = p.SchoolCycle + " " + p.SEP_Period,
                Schedule = g.Schedule,
                IsTheory = g.IsTheory,
                Capacity = g.Capacity,
                Enrolled = g.Enrolled,
            };

        return await query.ToListAsync();
    }

    // Same days and same hours, in any order: the group did not move.
    private static bool SameSchedule(IReadOnlyList<Schedule> stored, IReadOnlyList<Schedule> incoming) =>
        stored.Count == incoming.Count &&
        stored.Select(s => $"{Normalize(s.Day)}|{Normalize(s.Time)}").OrderBy(x => x)
            .SequenceEqual(incoming.Select(s => $"{Normalize(s.Day)}|{Normalize(s.Time)}").OrderBy(x => x));

    // Returns the conflicting day (as written) when a new schedule shares a day and overlapping time range with an existing one.
    private static string? FindScheduleConflict(IReadOnlyList<Schedule> newSchedule, IReadOnlyList<Schedule> existingSchedule)
    {
        foreach (var newItem in newSchedule)
        {
            var newDays = ParseDays(newItem.Day);
            var newRange = ParseTimeRange(newItem.Time);
            if (newDays.Count == 0 || newRange is null) continue;

            foreach (var existingItem in existingSchedule)
            {
                var existingDays = ParseDays(existingItem.Day);
                var existingRange = ParseTimeRange(existingItem.Time);
                if (existingDays.Count == 0 || existingRange is null) continue;
                if (!newDays.Overlaps(existingDays)) continue;

                var (newStart, newEnd) = newRange.Value;
                var (existingStart, existingEnd) = existingRange.Value;

                // Interval overlap covers both partial and total overlaps (e.g. 8-12 vs 10-12, or 8-12 vs 8-12).
                if (newStart < existingEnd && existingStart < newEnd)
                    return existingItem.Day;
            }
        }
        return null;
    }

    private static HashSet<string> ParseDays(string day) =>
        DaySeparatorRegex().Split(day)
            .Select(Normalize)
            .Where(token => token.Length > 0)
            .ToHashSet();

    private static (TimeOnly Start, TimeOnly End)? ParseTimeRange(string time)
    {
        var cleaned = HoursSuffixRegex().Replace(time, string.Empty).Trim();
        var parts = cleaned.Split('-', StringSplitOptions.TrimEntries);
        if (parts.Length != 2) return null;
        if (!TimeOnly.TryParse(parts[0], CultureInfo.InvariantCulture, out var start)) return null;
        if (!TimeOnly.TryParse(parts[1], CultureInfo.InvariantCulture, out var end)) return null;
        return (start, end);
    }

    private static string Normalize(string value)
    {
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var withoutAccents = decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(withoutAccents.ToArray()).ToLowerInvariant();
    }

    // Splits combined day cells like "Lunes y miércoles" or "Lunes, Miercoles" into individual days.
    [GeneratedRegex(@"[,/&]|\by\b|\be\b", RegexOptions.IgnoreCase)]
    private static partial Regex DaySeparatorRegex();

    [GeneratedRegex(@"\s*hrs\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex HoursSuffixRegex();
}
