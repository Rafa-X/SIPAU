using System.Text.RegularExpressions;

namespace UPZMG.Shared.Enums;

/// <summary>
/// How an enum value reads on screen. The members are written as identifiers — no spaces, no
/// accents — and printing them as they are put "UnionLibre", "ExperienciaProfesional" or a bare
/// "H" in front of the person capturing.
/// </summary>
public static partial class EnumLabels
{
    // Words the identifiers spell without their accent.
    private static readonly Dictionary<string, string> Accents = new(StringComparer.OrdinalIgnoreCase)
    {
        ["academica"] = "académica", ["academico"] = "académico", ["areas"] = "áreas", ["artisticas"] = "artísticas",
        ["cafeteria"] = "cafetería", ["canceleria"] = "cancelería", ["capacitacion"] = "capacitación",
        ["circulacion"] = "circulación", ["consolidacion"] = "consolidación", ["cubiculo"] = "cubículo",
        ["direccion"] = "dirección", ["distincion"] = "distinción", ["electrica"] = "eléctrica",
        ["formacion"] = "formación", ["genero"] = "género", ["hidraulica"] = "hidráulica",
        ["ingenieria"] = "ingeniería", ["instalacion"] = "instalación", ["lider"] = "líder",
        ["maestria"] = "maestría", ["medica"] = "médica", ["medico"] = "médico", ["miercoles"] = "miércoles",
        ["operacion"] = "operación", ["reinscripcion"] = "reinscripción",
        ["reparacion"] = "reparación", ["sabado"] = "sábado", ["separacion"] = "separación",
        ["tecnico"] = "técnico", ["tecnologico"] = "tecnológico", ["union"] = "unión", ["vibracion"] = "vibración",
    };

    /// <summary>The value as a person reads it: separate words, accents, sentence case.</summary>
    public static string Label(this Enum value)
    {
        if (value is Gender gender) return gender == Gender.M ? "Mujer" : "Hombre";

        var words = WordBreak().Split(value.ToString().Trim('_'))
            .Where(word => word.Length > 0)
            .Select((word, index) =>
            {
                // Acronyms stay as they are: SNI, PRODEP, TSU.
                if (word.Length > 1 && word.All(char.IsUpper) && word != "NO") return word;

                var lower = Accents.TryGetValue(word, out var accented) ? accented : word.ToLowerInvariant();
                return index == 0 ? char.ToUpperInvariant(lower[0]) + lower[1..] : lower;
            });

        return string.Join(' ', words);
    }

    // Between a lower-case letter and a capital, and before the last capital of a run that
    // starts a word ("NOEscolarizada" → "NO", "Escolarizada").
    [GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
    private static partial Regex WordBreak();
}
