namespace UPZMG.Shared.Enums;


/// <summary>
/// Represents marital status options.
/// </summary>
public enum MaritalStatus
{
    Soltero,
    Casado,
    UnionLibre,
    Divorciado,
    Viudo,
}

/// <summary>
/// Represents English proficiency levels.
/// </summary>
public enum EnglishLevel
{
    A1,
    A2,
    B1,
    B2,
    C1,
    C2
}

/// <summary>
/// Represents types of inscriptions for students.
/// </summary>
public enum InscriptionType
{
    NuevoIngreso,
    Reingreso,
    Reinscripcion,
    Egreso,
    Baja
}

/// <summary>
/// Represents the current academic status of a student.
/// </summary>
public enum StudentStatus
{
    Activo,
    BajaTemporal,
    BajaDefinitiva,
    Egresado
}

/// <summary>
/// Represents types of final status for students.
/// </summary>
public enum FinalStatus
{
    Activo,
    BajaTemporal,
    BajaDefinitiva,
    Egresado
}

/// <summary>
/// Represents types of course opportunities for students.
/// </summary>
public enum Opportunity
{
    Ordinario,
    Extraordinario,
    Titulo
}

/// <summary>
/// Represents types of titulation modalities for students.
/// </summary>
public enum TitulationModality
{
    Promedio,
    Tesis,
    Ceneval,
    ExperienciaProfesional
}

public enum Days
{
    Lunes,
    Martes,
    Miercoles,
    Jueves,
    Viernes,
    Sabado
}

public static class DayLabels
{
    /// <summary>Reads a stored day back, with or without its accent.</summary>
    public static Days? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var plain = text.Trim().Normalize(System.Text.NormalizationForm.FormD);
        plain = new string([.. plain.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
            != System.Globalization.UnicodeCategory.NonSpacingMark)]);
        return Enum.TryParse<Days>(plain, ignoreCase: true, out var day) ? day : null;
    }
}

public enum ScheduleTime
{
    _07_00,
    _07_30,
    _08_00,
    _08_30,
    _09_00,
    _09_30,
    _10_00,
    _10_30,
    _11_00,
    _11_30,
    _12_00,
    _12_30,
    _13_00,
    _13_30,
    _14_00,
    _14_30,
    _15_00,
    _15_30,
    _16_00,
    _16_30,
    _17_00,
    _17_30,
    _18_00,
    _18_30,
    _19_00,
    _19_30,
    _20_00,
    _20_30,
    _21_00,
    _21_30,
    _22_00
}