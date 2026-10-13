namespace UPZMG.Shared.Enums;

/// <summary>
/// Represents the gender type of a student or academic.
/// </summary>
public enum Gender
{
    H,
    M
}


/// <summary>
/// Represents the education levels for students, academics, or infraestructure.
/// </summary>
public enum EducationLevel
{
    MediaSuperiorOMenor,
    Bachillerato,
    TecnicoSuperior,
    Licenciatura,
    Ingenieria,
    Posgrado,
    Especialidad,
    Maestria,
    EspecialidadMedica,
    Doctorado,
    SubespecialidadMedica,
}

public static class EducationLevelLabels
{
    /// <summary>
    /// The level as a person reads it. The member names carry no accents or spaces, and
    /// printing them as they are put "MediaSuperiorOMenor" on the screens.
    /// </summary>
    public static string Label(this EducationLevel level) => level switch
    {
        EducationLevel.MediaSuperiorOMenor => "Media superior o menor",
        EducationLevel.Bachillerato => "Bachillerato",
        EducationLevel.TecnicoSuperior => "Técnico superior",
        EducationLevel.Licenciatura => "Licenciatura",
        EducationLevel.Ingenieria => "Ingeniería",
        EducationLevel.Posgrado => "Posgrado",
        EducationLevel.Especialidad => "Especialidad",
        EducationLevel.Maestria => "Maestría",
        EducationLevel.EspecialidadMedica => "Especialidad médica",
        EducationLevel.Doctorado => "Doctorado",
        EducationLevel.SubespecialidadMedica => "Subespecialidad médica",
        _ => level.ToString(),
    };
}

/// <summary>
/// Represents the degree/title process status for a student or academic.
/// </summary>
public enum TitleStatus
{
    Titulado,
    Pasante,
    Trunco
}

/// <summary>
/// Represents the place of study for an employee's education.
/// </summary>
public enum StudyPlace
{
    Nacional,
    Extranjero
}

/// <summary>
/// Represents the type of internal area within the organization.
/// </summary>

public enum InternalAreaType
{
    AcademicaOperativa,
    AdministrativaCentral,
}

/// <summary>
/// Represents the SEP 911 category of an employee.
/// </summary>
public enum SEP_911
{
    Directivo,
    Docente,
    DocenteInvestigador,
    AuxiliarInvestigador,
    Administrativo,
    Servicios
}

/// <summary>
/// Represents the type of workday for an employee's contract.
/// </summary>
public enum WorkdayShift
{
    TiempoCompleto,
    TresCuartos,
    MedioTiempo,
    Asignatura,
}

/// <summary>
/// Represents the academic tab level of an employee.
/// </summary>

public enum TabLevel
{
    TitularA,
    TitularB,
    TitularC,
    AsociadoA,
    AsociadoB,
    AsociadoC,
    TecnicoAcademico
}

/// <summary>
/// Represents the type of academic goal for employees.
/// </summary>

public enum GoalType
{
    SNI,
    PRODEP,
    Distincion,
}

/// <summary>
/// Represents the role of an employee in a AS (Académic Staff).
/// </summary>
public enum RoleAS
{
    Lider,
    Integrante,
    Colaborador
}

/// <summary>
/// Represents the status of an employee in a AS (Académic Staff).
/// </summary>
public enum StatusAS
{
    EnFormacion,
    EnConsolidacion,
    Consolidado
}

public enum IndigenousLanguage
{
    Nahuatl,
    Maya,
    Zapoteco,
    Mixteco,
    Otomi,
    Tzotzil,
    Tzeltal,
    Huichol,
    Otro
}