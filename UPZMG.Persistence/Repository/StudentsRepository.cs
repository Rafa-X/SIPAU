using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.Charts;
using UPZMG.Implementations.DataCapture;
using UPZMG.Persistence.Models;

namespace UPZMG.Persistence.Repository;

public class StudentsRepository(AppDBContext db)
{
    public async Task<Student> CreateStudentAsync(CreateStudent request)
    {
        RequireCaptureRules(request);
        await RequireCatalogueAsync(request);

        var existEnrollment = await db.Students.AnyAsync(s => s.EnrollmentNumber == request.EnrollmentNumber);
        if (existEnrollment)
            throw new InvalidOperationException($"Ya existe un alumno con la matrícula '{request.EnrollmentNumber}'.");

        // A student with no CURP is allowed, and there can be many of them: the unique index is
        // filtered on [CURP] <> '', so checking a blank value here rejected everyone after the first.
        if (!string.IsNullOrWhiteSpace(request.CURP)
            && await db.Students.AnyAsync(s => s.CURP == request.CURP))
        {
            throw new InvalidOperationException($"Ya existe un alumno con la CURP '{request.CURP}'.");
        }

        var matchedLanguageIds = await LanguageCatalogue.ResolveAsync(db, request.IndigenousLanguages);
        
        var student = new Student
        {
            // Assigned here rather than by the database: the language links below copy this id,
            // and before the entity is saved any other source for it would be an empty Guid.
            Id = Guid.NewGuid(),
            EnrollmentNumber = request.EnrollmentNumber,
            CURP = request.CURP,
            Name = request.Name,
            MiddleName = request.MiddleName,
            LastName = request.LastName,
            BirthDate = request.BirthDate,
            Gender = request.Gender,
            PostalCode = request.PostalCode,
            EnglishLevel = request.EnglishLevel,
            MaritalStatus = request.MaritalStatus,
            NumberOfChildren = request.NumberOfChildren,
            Works = request.Works,
            BirthCountry = request.BirthCountry,
            BirthState = request.BirthState,
            Address = request.Address,
            CareerId = request.CareerId,
            PeriodIdStart = request.PeriodIdStart,
            InscriptionType = request.InscriptionType,
            Status = request.Status,
        };

        foreach (var languageId in matchedLanguageIds)
        {
            db.IndigenousLanguagesSpeakers.Add(new IndigenousLanguagesSpeakers
            {
                Id = Guid.NewGuid(),
                SpeakerId = student.Id,
                LanguageId = languageId
            });
        }

        student.IndigenousLanguages = matchedLanguageIds;
        db.Students.Add(student);
        await db.SaveGuardingUniquenessAsync(
            $"Ya existe un alumno con la matrícula '{request.EnrollmentNumber}' o esa CURP.");

        return student;
    }

    /// <summary>
    /// The career and the entry period must exist. The database refuses them too, but as a
    /// foreign-key violation the person would see a server error instead of the reason.
    /// </summary>
    private async Task RequireCatalogueAsync(CreateStudent request)
    {
        if (!await db.Careers.AnyAsync(c => c.Id == request.CareerId))
            throw new InvalidOperationException("La carrera seleccionada no existe en el catálogo.");

        if (!await db.Periods.AnyAsync(p => p.Id == request.PeriodIdStart))
            throw new InvalidOperationException("El periodo de inicio seleccionado no existe en el catálogo.");
    }

    /// <summary>
    /// The capture rules, here as well as in the form: the form is only one client of the API, and
    /// what it never sends — a blank name, a status outside the list, text longer than its column —
    /// was stored as it came, or came back as a server error.
    /// </summary>
    private static void RequireCaptureRules(CreateStudent request)
    {
        var problems = ImportRules.Check(request, DateOnly.FromDateTime(DateTime.Today));
        if (problems.Count > 0) throw new InvalidOperationException(string.Join(" ", problems));
    }

    /// <summary>The whole student, to fill the correction form.</summary>
    public async Task<UpdateStudent?> GetStudentAsync(Guid id)
    {
        var student = await db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (student is null) return null;

        var languages = await db.IndigenousLanguagesSpeakers.AsNoTracking()
            .Where(s => s.SpeakerId == id)
            .Join(db.IndigenousLanguages.AsNoTracking(), s => s.LanguageId, l => l.Id, (s, l) => l.Language)
            .Where(language => language != null)
            .ToListAsync();

        return new UpdateStudent
        {
            Id = student.Id,
            RequesterKey = Guid.Empty,
            EnrollmentNumber = student.EnrollmentNumber,
            CURP = student.CURP,
            Name = student.Name,
            MiddleName = student.MiddleName,
            LastName = student.LastName,
            BirthDate = student.BirthDate,
            Gender = student.Gender,
            IndigenousLanguages = [.. languages!],
            PostalCode = student.PostalCode,
            EnglishLevel = student.EnglishLevel,
            MaritalStatus = student.MaritalStatus,
            NumberOfChildren = student.NumberOfChildren,
            Works = student.Works,
            Address = student.Address ?? string.Empty,
            CareerId = student.CareerId,
            PeriodIdStart = student.PeriodIdStart,
            InscriptionType = student.InscriptionType,
            Status = student.Status,
            BirthCountry = student.BirthCountry,
            BirthState = student.BirthState,
        };
    }

    /// <summary>
    /// Corrects a captured student. The enrolment number and the CURP stay unique, but the
    /// student's own row is excluded from that check: saving it unchanged must not be a conflict.
    /// </summary>
    public async Task<Student> UpdateStudentAsync(UpdateStudent request)
    {
        var student = await db.Students.FirstOrDefaultAsync(s => s.Id == request.Id)
            ?? throw new InvalidOperationException("El alumno ya no existe.");

        RequireCaptureRules(request);
        await RequireCatalogueAsync(request);

        if (await db.Students.AnyAsync(s => s.Id != request.Id && s.EnrollmentNumber == request.EnrollmentNumber))
            throw new InvalidOperationException($"Ya existe otro alumno con la matrícula '{request.EnrollmentNumber}'.");

        // Only a CURP that was given has to be unique: a blank one is allowed for many students,
        // as on capture. Checking the blank too refused every correction once two students
        // had no CURP.
        if (!string.IsNullOrWhiteSpace(request.CURP)
            && await db.Students.AnyAsync(s => s.Id != request.Id && s.CURP == request.CURP))
            throw new InvalidOperationException($"Ya existe otro alumno con la CURP '{request.CURP}'.");

        student.EnrollmentNumber = request.EnrollmentNumber;
        student.CURP = request.CURP;
        student.Name = request.Name;
        student.MiddleName = request.MiddleName;
        student.LastName = request.LastName;
        student.BirthDate = request.BirthDate;
        student.Gender = request.Gender;
        student.PostalCode = request.PostalCode;
        student.EnglishLevel = request.EnglishLevel;
        student.MaritalStatus = request.MaritalStatus;
        student.NumberOfChildren = request.NumberOfChildren;
        student.Works = request.Works;
        student.BirthCountry = request.BirthCountry;
        student.BirthState = request.BirthState;
        student.Address = request.Address;
        student.CareerId = request.CareerId;
        student.PeriodIdStart = request.PeriodIdStart;
        student.InscriptionType = request.InscriptionType;
        student.Status = request.Status;

        // Spoken languages are a set: rebuild it rather than trying to diff it.
        var previous = db.IndigenousLanguagesSpeakers.Where(s => s.SpeakerId == request.Id);
        db.IndigenousLanguagesSpeakers.RemoveRange(previous);

        var languageIds = await LanguageCatalogue.ResolveAsync(db, request.IndigenousLanguages);

        foreach (var languageId in languageIds)
        {
            db.IndigenousLanguagesSpeakers.Add(new IndigenousLanguagesSpeakers
            {
                Id = Guid.NewGuid(),
                SpeakerId = student.Id,
                LanguageId = languageId,
            });
        }

        student.IndigenousLanguages = languageIds;
        await db.SaveGuardingUniquenessAsync(
            $"Ya existe otro alumno con la matrícula '{request.EnrollmentNumber}' o esa CURP.");
        return student;
    }

    public async Task<StudentsListResponse> GetStudentsListAsync(StudentsListRequest request)
    {
        // Longer than the columns it searches, it matches nobody, and as a parameter it was a server error.
        if (request.Name?.Trim().Length > 100 || request.EnrollmentNumber?.Trim().Length > 20)
            return new StudentsListResponse();

        var query =
            from s in db.Students.AsNoTracking()
            join c in db.Careers.AsNoTracking() on s.CareerId equals c.Id
            join p in db.Periods.AsNoTracking() on s.PeriodIdStart equals p.Id
            where 
                (request.CareerId == null || s.CareerId == request.CareerId) &&
                (request.PeriodId == null || s.PeriodIdStart == request.PeriodId)
            select new { Student = s, Career = c, Period = p };

        // A search term is text: "%", "_" and "[" in it used to act as wildcards and matched everyone.
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var term = $"%{LikeLiteral(request.Name.Trim())}%";
            query = query.Where(x =>
                EF.Functions.Like(x.Student.Name, term, @"\") ||
                EF.Functions.Like(x.Student.LastName, term, @"\") ||
                EF.Functions.Like(x.Student.MiddleName, term, @"\"));
        }

        if (!string.IsNullOrWhiteSpace(request.EnrollmentNumber))
        {
            var term = $"%{LikeLiteral(request.EnrollmentNumber.Trim())}%";
            query = query.Where(x => EF.Functions.Like(x.Student.EnrollmentNumber, term, @"\"));
        }

        // Cap page size to keep listing queries cheap regardless of client input.
        var pageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 100);
        var page = request.Page <= 0 ? 1 : request.Page;

        var items = await query
            .OrderBy(x => x.Student.LastName).ThenBy(x => x.Student.Name)
            // In long: page 2147483647 overflowed into a negative offset and a server error.
            .Skip((int)Math.Min((page - 1L) * pageSize, int.MaxValue))
            .Take(pageSize)
            .Select(x => new StudentListItem
            {
                Id = x.Student.Id,
                EnrollmentNumber = x.Student.EnrollmentNumber,
                // Without a second given name the plain concatenation left a double space.
                Name = string.IsNullOrEmpty(x.Student.MiddleName)
                    ? x.Student.Name + " " + x.Student.LastName
                    : x.Student.Name + " " + x.Student.MiddleName + " " + x.Student.LastName,
                Career = x.Career.Name,
                // With the year: every cycle has an "Ene-Abr", and alone it named nine periods.
                Period = x.Period.SchoolCycle + " " + x.Period.SEP_Period,
                Status = x.Student.Status,
            }).ToListAsync();

        return new StudentsListResponse { Items = items, TotalCount = await query.CountAsync() };
    }

    private static string LikeLiteral(string term) =>
        term.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_").Replace("[", @"\[");

    public Task<List<StudentsAges>> GetStudentsAgesNew() => GetStudentsAgesAsync(newOnly: true);

    public Task<List<StudentsAges>> GetStudentsAgesAll() => GetStudentsAgesAsync(newOnly: false);

    private async Task<List<StudentsAges>> GetStudentsAgesAsync(bool newOnly)
    {
        // "New" is who entered in the active period. Filtering by inscription type alone
        // returned every student ever registered, since that is how all of them came in, and
        // the two charts were the same picture.
        var activePeriodId = newOnly
            ? await db.Periods.AsNoTracking().Where(p => p.IsActive).Select(p => (Guid?)p.Id).FirstOrDefaultAsync()
            : null;

        if (newOnly && activePeriodId is null) return [];

        var students = await db.Students
            .AsNoTracking()
            .Where(s => !newOnly
                || (s.InscriptionType == UPZMG.Shared.Enums.InscriptionType.NuevoIngreso && s.PeriodIdStart == activePeriodId))
            .Select(s => s.BirthDate)
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return students
            .Select(birthDate => CalculateAge(birthDate, today))
            .GroupBy(age => age)
            .OrderBy(group => group.Key)
            .Select(group => new StudentsAges
            {
                Name = group.Key.ToString(),
                Values = group.Select(age => (double)age).ToList(),
                Color = "var(--mud-palette-secondary)"
            })
            .ToList();
    }

    private static int CalculateAge(DateOnly birthDate, DateOnly today)
    {
        var age = today.Year - birthDate.Year;
        if (birthDate > today.AddYears(-age)) age--;
        return age;
    }
}