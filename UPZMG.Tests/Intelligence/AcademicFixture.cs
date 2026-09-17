using UPZMG.Implementations.Intelligence;
using UPZMG.Shared.Enums;

namespace UPZMG.Tests.Intelligence;

/// <summary>Builds small academic snapshots by hand, one period and one student at a time.</summary>
internal sealed class AcademicFixture
{
    private readonly List<PeriodRow> _periods = [];
    private readonly List<StudentRow> _students = [];
    private readonly List<RegistrationRow> _registrations = [];
    private readonly List<GradeRow> _grades = [];

    /// <summary>Adds "2020 Ene-Abr", "2020 May-Ago", … in order and returns their ids.</summary>
    public Guid[] Periods(int count, int firstYear = 2020)
    {
        string[] terms = ["Ene-Abr", "May-Ago", "Sep-Dic"];
        var ids = new Guid[count];
        for (int i = 0; i < count; i++)
        {
            ids[i] = Guid.NewGuid();
            var year = firstYear + i / 3;
            _periods.Add(new PeriodRow(ids[i], $"{year} {terms[i % 3]}", new DateOnly(year, 1 + 4 * (i % 3), 1)));
        }
        return ids;
    }

    public Guid Student(
        Guid firstPeriod,
        StudentStatus status = StudentStatus.Activo,
        DateOnly? birthDate = null,
        bool works = false,
        string career = "Ingeniería Industrial")
    {
        var id = Guid.NewGuid();
        _students.Add(new StudentRow(
            id, $"M{_students.Count + 1:000}", $"Alumno {_students.Count + 1}", career,
            firstPeriod, birthDate ?? new DateOnly(2000, 6, 15), works, status));
        return id;
    }

    /// <summary>One registration row per subject, as the real table stores them.</summary>
    public void Enrol(Guid student, Guid period, FinalStatus status = FinalStatus.Activo, int subjects = 3)
    {
        for (int i = 0; i < subjects; i++)
            _registrations.Add(new RegistrationRow(student, period, status));
    }

    public void Grade(Guid student, Guid period, double grade, bool passed = true,
        Opportunity opportunity = Opportunity.Ordinario, int times = 1)
    {
        for (int i = 0; i < times; i++)
            _grades.Add(new GradeRow(student, period, grade, passed, opportunity));
    }

    public AcademicSnapshot Build() => new(_periods, _students, _registrations, _grades);
}
