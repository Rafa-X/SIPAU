using UPZMG.Implementations.DataCapture;
using UPZMG.Shared.Enums;

namespace UPZMG.IntegrationTests.Infrastructure;

/// <summary>Valid capture requests, built against the catalogues of the run's database.</summary>
public static class Samples
{
    public static async Task<CreateStudent> StudentAsync(TestDatabase db, string enrollment, string curp = "") => new()
    {
        RequesterKey = Guid.NewGuid(),
        EnrollmentNumber = enrollment,
        CURP = curp,
        Name = "Prueba",
        MiddleName = "",
        LastName = "Integración Sistema",
        BirthDate = new DateOnly(2005, 5, 20),
        Gender = Gender.M,
        PostalCode = "45640",
        EnglishLevel = EnglishLevel.A2,
        MaritalStatus = MaritalStatus.Soltero,
        NumberOfChildren = 0,
        Works = false,
        Address = "Calle de prueba 1",
        CareerId = await db.ScalarAsync<Guid>("SELECT TOP 1 Id FROM careers ORDER BY Name"),
        PeriodIdStart = await db.ScalarAsync<Guid>("SELECT TOP 1 Id FROM periods ORDER BY StartDate DESC"),
        InscriptionType = InscriptionType.NuevoIngreso,
        Status = StudentStatus.Activo,
        BirthCountry = "México",
        BirthState = "Jalisco",
    };

    public static CreateEmployee Employee(int number, string suffix) => new()
    {
        RequesterKey = Guid.NewGuid(),
        EmployeeNumber = number,
        FullName = $"Empleado de prueba {suffix}",
        Email = $"empleado.{suffix}@prueba.local",
        Rfc = $"PRUE{number:000000}AB1",
        Curp = $"PRUE{number:000000}HJCRRS01",
        Gender = Gender.H,
        BirthDate = new DateOnly(1985, 2, 2),
        HighestEducationLevel = EducationLevel.Licenciatura,
        TitleStatus = TitleStatus.Titulado,
        HasDisability = false,
        ExternalXPYears = 3,
        FirstHiredDate = new DateOnly(2015, 8, 1),
        IsTeacher = true,
    };

    /// <summary>How many students carry this enrolment number, read straight from the database.</summary>
    public static Task<int> StudentsWithAsync(TestDatabase db, string enrollment) =>
        db.ScalarAsync<int>("SELECT COUNT(*) FROM students WHERE EnrollmentNumber = @e",
            new Microsoft.Data.SqlClient.SqlParameter("@e", enrollment));
}
