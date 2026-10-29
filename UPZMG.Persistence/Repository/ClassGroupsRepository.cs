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
        EnsureReadableSchedule(request.Schedule);
        await RequireReferencesAsync(request);

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
        EnsureReadableSchedule(request.Schedule);

        var group = await db.ClassGroups.FirstOrDefaultAsync(g => g.Id == request.Id)
            ?? throw new InvalidOperationException("El grupo ya no existe.");

        await RequireReferencesAsync(request);

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

    /// <summary>
    /// What the group points at must exist, and the course must be one of the career's. The
    /// database refuses a missing one too, but as a foreign-key violation the person saw a server
    /// error; a course of another career was stored and the group listed under the wrong career.
    /// </summary>
    private async Task RequireReferencesAsync(CreateClassGroup request)
    {
        if (!await db.Periods.AnyAsync(p => p.Id == request.PeriodId))
            throw new InvalidOperationException("El periodo seleccionado no existe.");

        if (!await db.Careers.AnyAsync(c => c.Id == request.CareerId))
            throw new InvalidOperationException("La carrera seleccionada no existe.");

        var courseCareer = await db.Courses.AsNoTracking()
            .Where(c => c.Id == request.CourseId)
            .Select(c => (Guid?)c.CareerId)
            .FirstOrDefaultAsync();
        if (courseCareer is null)
            throw new InvalidOperationException("La materia seleccionada no existe.");
        if (courseCareer != request.CareerId)
            throw new InvalidOperationException("La materia seleccionada no pertenece a esa carrera.");

        var isTeacher = await db.Employees.AsNoTracking()
            .Where(e => e.Id == request.TeacherId)
            .Select(e => (bool?)e.IsTeacher)
            .FirstOrDefaultAsync();
        if (isTeacher is null)
            throw new InvalidOperationException("El docente seleccionado no existe.");
        if (isTeacher == false)
            throw new InvalidOperationException("El empleado seleccionado no está registrado como docente.");

        if (!await db.Set<SchoolFacility>().AnyAsync(f => f.Id == request.FacilityId))
            throw new InvalidOperationException("El espacio seleccionado no existe.");
    }

    /// <summary>
    /// Refuses a schedule the overlap check cannot read. An entry whose day or time range does
    /// not parse was skipped silently, so a group written as "Lunes 8 a 12" went into a room
    /// that was already taken: the clash check had nothing to compare and let it through.
    /// </summary>
    private static void EnsureReadableSchedule(IReadOnlyList<Schedule> schedule)
    {
        if (schedule.Count == 0)
            throw new InvalidOperationException("El grupo necesita al menos un día y horario.");

        foreach (var entry in schedule)
        {
            // Checked token by token: "Lunes y lo que sea" used to pass because one of the two
            // words was readable, and the unreadable half silently stopped being checked.
            var days = SplitDays(entry.Day);
            if (days.Count == 0 || days.Any(day => CanonicalDay(day) is null))
            {
                throw new InvalidOperationException(
                    $"No se entiende el día «{entry.Day}». Escribe por ejemplo «Lunes» o «Lunes y miércoles».");
            }

            if (entry.Day.Length > MaxScheduleText || entry.Time.Length > MaxScheduleText)
                throw new InvalidOperationException("Cada día y cada horario admiten hasta 60 caracteres.");

            if (ParseTimeRange(entry.Time) is not { } range)
            {
                throw new InvalidOperationException(
                    $"No se entiende el horario «{entry.Time}» del {entry.Day}. Escríbelo como «08:00 - 12:00».");
            }

            // "12:00 - 08:00" was stored, and as an interval it overlaps nothing: the room looked free.
            if (range.End <= range.Start)
            {
                throw new InvalidOperationException(
                    $"El horario «{entry.Time}» del {entry.Day} termina antes de empezar.");
            }
        }
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

    // Compared by canonical day, not by the text as written: "Lun", "lunes" and "Miércoles"
    // used to be three different days, so a group written in short form never collided with
    // one written in full and both ended up in the same room at the same hour.
    private static readonly string[] WeekDays =
        ["lunes", "martes", "miercoles", "jueves", "viernes", "sabado", "domingo"];

    private const int MaxScheduleText = 60;

    private static List<string> SplitDays(string day) =>
        [.. DaySeparatorRegex().Split(day).Where(token => token.Trim().Length > 0)];

    /// <summary>The week day a token names, or null when it names none.</summary>
    private static string? CanonicalDay(string token)
    {
        var normalized = Normalize(token).TrimEnd('.', ':', ';');
        if (normalized.Length < 3) return null;

        // A short form names its day ("Lun", "Mié"); a longer text that merely begins with one
        // does not. "Lunes a viernes" used to read as Monday alone, so the clash check missed
        // Tuesday to Friday, and "LunesLunes…" was accepted as a day.
        return WeekDays.FirstOrDefault(day => day.StartsWith(normalized, StringComparison.Ordinal));
    }

    private static HashSet<string> ParseDays(string day) =>
        [.. SplitDays(day).Select(CanonicalDay).OfType<string>()];

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
