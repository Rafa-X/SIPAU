namespace UPZMG.Web.Services;

/// <summary>
/// How a role reads on screen. The catalogue keeps the name the seed wrote, which the default
/// permissions look up by, and that name lost its accents.
/// </summary>
public static class RoleLabels
{
    public static string Of(string? name) => name switch
    {
        null or "" => "—",
        "Direccion Academica" => "Dirección Académica",
        _ => name,
    };
}
