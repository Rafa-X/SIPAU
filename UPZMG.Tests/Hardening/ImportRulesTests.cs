using UPZMG.Implementations.DataCapture;
using UPZMG.Shared.Enums;
using Xunit;

namespace UPZMG.Tests.Hardening;

/// <summary>
/// The checks a CSV row passes before anything is written. They are the rules of the capture
/// forms plus the one a form cannot have: a file that repeats its own keys.
/// </summary>
public class ImportRulesTests
{
    private static readonly DateOnly Today = new(2026, 10, 18);

    private static CreateStudent Student(string enrollment = "M001", string curp = "") => new()
    {
        RequesterKey = Guid.NewGuid(),
        EnrollmentNumber = enrollment,
        CURP = curp,
        Name = "Ana",
        MiddleName = "",
        LastName = "López Ruiz",
        BirthDate = new DateOnly(2005, 3, 1),
        Address = "Calle 1",
        CareerId = Guid.NewGuid(),
        PeriodIdStart = Guid.NewGuid(),
        BirthCountry = "México",
        BirthState = "Jalisco",
    };

    private static CreateEmployee Employee() => new()
    {
        RequesterKey = Guid.NewGuid(),
        EmployeeNumber = 10,
        FullName = "Laura Mendoza",
        Email = "laura@upzmg.local",
        Rfc = "MERL850202AB1",
        Curp = "MERL850202MJCNZR01",
        Gender = Gender.M,
        BirthDate = new DateOnly(1985, 2, 2),
        FirstHiredDate = new DateOnly(2015, 8, 1),
    };

    private static List<ImportRow<CreateStudent>> Rows(params CreateStudent[] students) =>
        [.. students.Select((s, i) => new ImportRow<CreateStudent> { Line = i + 2, Request = s })];

    [Fact]
    public void ACompleteStudentPasses()
    {
        Assert.Empty(ImportRules.Check(Student(), Today));
        Assert.Empty(ImportRules.Check(Student(curp: "LORA050301MJCPZN09"), Today));
    }

    [Fact]
    public void EveryMissingOrWrongFieldOfAStudentIsNamed()
    {
        var student = Student(enrollment: " ", curp: "ABC");
        student.Name = "";
        student.CareerId = Guid.Empty;
        student.BirthDate = Today.AddDays(1);
        student.NumberOfChildren = -1;

        var problems = ImportRules.Check(student, Today);

        Assert.Contains("Falta la matrícula.", problems);
        Assert.Contains("Falta el nombre.", problems);
        Assert.Contains("La CURP debe tener 18 caracteres.", problems);
        Assert.Contains("Falta la carrera.", problems);
        Assert.Contains("La fecha de nacimiento está en el futuro.", problems);
        Assert.Contains("El número de hijos no puede ser negativo.", problems);
    }

    [Fact]
    public void AnEmployeeNeedsValidIdentifiersAndDates()
    {
        Assert.Empty(ImportRules.Check(Employee(), Today));

        var employee = Employee();
        employee.EmployeeNumber = 0;
        employee.Email = "sin-arroba";
        employee.Rfc = "CORTO";
        employee.FirstHiredDate = default;

        var problems = ImportRules.Check(employee, Today);

        Assert.Contains("El número de empleado debe ser mayor que cero.", problems);
        Assert.Contains("El correo electrónico no es válido.", problems);
        Assert.Contains("El RFC debe tener 12 o 13 caracteres.", problems);
        Assert.Contains("Falta la fecha de primera contratación.", problems);
    }

    /// <summary>
    /// Text longer than its column used to pass every rule and come back from the database as a
    /// server error; the form said "Request failed with status code: InternalServerError".
    /// </summary>
    [Fact]
    public void TextLongerThanItsColumnIsNamedBeforeTheDatabaseSeesIt()
    {
        var student = Student(enrollment: new string('M', 21));
        student.Name = new string('N', 101);
        student.Address = new string('D', 100);

        var problems = ImportRules.Check(student, Today);

        Assert.Contains("Matrícula: máximo 20 caracteres (tiene 21).", problems);
        Assert.Contains("Nombre: máximo 100 caracteres (tiene 101).", problems);
        Assert.DoesNotContain(problems, p => p.StartsWith("Domicilio"));

        var employee = Employee();
        employee.FullName = new string('F', 201);
        employee.Email = new string('a', 95) + "@upzmg.local";

        var staff = ImportRules.Check(employee, Today);
        Assert.Contains("Nombre completo: máximo 200 caracteres (tiene 201).", staff);
        Assert.Contains(staff, p => p.StartsWith("Correo electrónico: máximo 100 caracteres"));
    }

    /// <summary>A number outside a catalogue was stored as it came and read back as nothing.</summary>
    [Fact]
    public void ValuesOutsideTheCataloguesAreRefused()
    {
        var student = Student();
        student.Gender = (Gender)99;
        student.Status = (StudentStatus)42;
        student.InscriptionType = (InscriptionType)(-1);

        var problems = ImportRules.Check(student, Today);

        Assert.Contains("El sexo no es un valor del catálogo.", problems);
        Assert.Contains("El estatus del alumno no es un valor del catálogo.", problems);
        Assert.Contains("El tipo de inscripción no es un valor del catálogo.", problems);

        var employee = Employee();
        employee.TitleStatus = (TitleStatus)9;
        Assert.Contains("El estatus de titulación no es un valor del catálogo.", ImportRules.Check(employee, Today));
    }

    [Fact]
    public void ImplausibleDatesAreRefused()
    {
        var student = Student();
        student.BirthDate = new DateOnly(1890, 1, 1);
        Assert.Contains("La fecha de nacimiento es anterior a 1900.", ImportRules.Check(student, Today));

        var employee = Employee();
        employee.FirstHiredDate = employee.BirthDate.AddYears(-1);
        Assert.Contains("La fecha de primera contratación es anterior al nacimiento.", ImportRules.Check(employee, Today));
    }

    [Fact]
    public void ARepeatedKeyIsRefusedOnTheLaterRowAndPointsAtTheFirst()
    {
        var rows = Rows(Student("M001"), Student("M002"), Student(" m001 "));

        var repeated = ImportRules.RepeatedInFile(rows, ("matrícula", s => s.EnrollmentNumber));

        var reasons = Assert.Single(repeated);
        Assert.Equal(4, reasons.Key);
        Assert.Contains("ya aparece en la línea 2", Assert.Single(reasons.Value));
    }

    [Fact]
    public void BlankKeysRepeatNothing()
    {
        // Students without a CURP are allowed, many of them.
        var rows = Rows(Student("M001"), Student("M002"), Student("M003"));

        Assert.Empty(ImportRules.RepeatedInFile(rows, ("CURP", s => s.CURP)));
    }

    [Theory]
    [InlineData("  Náhuatl ", "NAHUATL")]
    [InlineData("loRa050301", "LORA050301")]
    [InlineData(null, "")]
    public void KeysAreComparedWithoutCaseAccentsOrSpaces(string? value, string expected) =>
        Assert.Equal(expected, ImportRules.Normalize(value));
}
