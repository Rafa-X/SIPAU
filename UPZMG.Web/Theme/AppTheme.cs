using MudBlazor;

namespace UPZMG.Web.Theme;

/// <summary>
/// Institutional MudBlazor theme for SIGIE / UPZMG — "Cantera y Cal".
/// </summary>
public static class AppTheme
{
    // ── The five inks ────────────────────────────────────────────────────────
    public const string Cal = "#EAEDE7";

    public const string Recinto = "#1E1C1A";

    public const string Anil = "#1B3A6B";

    public const string AnilLight = "#2A5590";

    public const string Rosa = "#C4325F";
    public const string Cempasuchil = "#D99A0B";
    public const string Nopal = "#3F6B4A";

    public static readonly MudTheme Institutional = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary   = Anil,
            Secondary = Anil,
            Tertiary  = AnilLight,
            Info      = AnilLight,
            Success   = Nopal,
            Warning   = Cempasuchil,
            Error     = Rosa,

            AppbarBackground = Anil,
            AppbarText       = "#F2F4F0",
            DrawerBackground = Anil,
            DrawerText       = "rgba(242,244,240,0.86)",
            DrawerIcon       = "rgba(242,244,240,0.64)",

            Background     = Cal,
            BackgroundGray = "#DFE3DB",
            Surface        = "#F5F7F3",

            TextPrimary   = "#1C201B",
            TextSecondary = "#585F55",
            TextDisabled  = "rgba(28,32,27,0.38)",

            ActionDefault  = "#585F55",
            ActionDisabled = "rgba(28,32,27,0.26)",

            Divider      = "#CBD1C6",
            DividerLight = "#DCE1D6",
            LinesDefault = "#CBD1C6",
            LinesInputs  = "#B4BCAE",
            TableLines   = "#D5DACE",
            TableHover   = "#E4E8DF",
            GrayLight    = "#E4E8DF",
        },
        PaletteDark = new PaletteDark
        {
            Primary   = "#8FB0DE",
            Secondary = "#8FB0DE",
            Tertiary  = "#8FB0DE",
            Info      = "#8FB0DE",
            Success   = "#7FB48C",
            Warning   = "#E0B255",
            Error     = "#E4738F",

            AppbarBackground = "#16223A",
            AppbarText       = "#E9EDE6",

            DrawerBackground = "#16223A",
            DrawerText       = "rgba(233,237,230,0.86)",
            DrawerIcon       = "rgba(233,237,230,0.62)",

            Background     = Recinto,
            BackgroundGray = "#262320",
            Surface        = "#272421",

            TextPrimary   = "#E9EDE6",
            TextSecondary = "#A3A79D",
            TextDisabled  = "rgba(233,237,230,0.36)",

            ActionDefault  = "#A3A79D",
            ActionDisabled = "rgba(233,237,230,0.26)",

            Divider      = "rgba(233,237,230,0.12)",
            DividerLight = "rgba(233,237,230,0.07)",
            LinesDefault = "rgba(233,237,230,0.14)",
            LinesInputs  = "rgba(233,237,230,0.22)",
            TableLines   = "rgba(233,237,230,0.09)",
            TableHover   = "rgba(233,237,230,0.05)",
            GrayLight    = "#2E2A27",
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "2px",
            DrawerWidthLeft     = "264px",
            DrawerMiniWidthLeft = "76px",
            AppbarHeight        = "64px",
        },
    };
}
