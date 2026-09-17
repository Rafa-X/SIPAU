using MudBlazor;

namespace UPZMG.Web.Theme;

/// <summary>
/// Institutional MudBlazor theme for SIPAU / UPZMG.
/// </summary>
public static class AppTheme
{
    // ── The five inks ────────────────────────────────────────────────────────
    public const string Cal = "#F4F7FB";
    public const string Recinto = "#081E33";
    public const string Anil = "#073B68";
    public const string AnilLight = "#0B6DB8";
    public const string Rosa = "#B93854";
    public const string Cempasuchil = "#C98211";
    public const string Nopal = "#167451";

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
            AppbarText       = "#F8FBFF",
            DrawerBackground = Anil,
            DrawerText       = "rgba(248,251,255,0.86)",
            DrawerIcon       = "rgba(248,251,255,0.64)",

            Background     = Cal,
            BackgroundGray = "#E8EEF5",
            Surface        = "#FFFFFF",

            TextPrimary   = "#142B42",
            TextSecondary = "#5D6D7E",
            TextDisabled  = "rgba(20,43,66,0.38)",

            ActionDefault  = "#5D6D7E",
            ActionDisabled = "rgba(20,43,66,0.26)",

            Divider      = "#D7E1EC",
            DividerLight = "#E8EEF5",
            LinesDefault = "#D7E1EC",
            LinesInputs  = "#AEBED0",
            TableLines   = "#E2E9F1",
            TableHover   = "#F0F6FB",
            GrayLight    = "#EEF4F9",
        },
        PaletteDark = new PaletteDark
        {
            Primary   = "#67B9F4",
            Secondary = "#67B9F4",
            Tertiary  = "#67B9F4",
            Info      = "#67B9F4",
            Success   = "#63C49A",
            Warning   = "#F2B95D",
            Error     = "#F08398",

            AppbarBackground = "#071B2F",
            AppbarText       = "#F4F8FC",

            DrawerBackground = "#071B2F",
            DrawerText       = "rgba(244,248,252,0.86)",
            DrawerIcon       = "rgba(244,248,252,0.62)",

            Background     = Recinto,
            BackgroundGray = "#102B45",
            Surface        = "#112D48",

            TextPrimary   = "#F4F8FC",
            TextSecondary = "#AABACA",
            TextDisabled  = "rgba(244,248,252,0.36)",

            ActionDefault  = "#AABACA",
            ActionDisabled = "rgba(244,248,252,0.26)",

            Divider      = "rgba(244,248,252,0.12)",
            DividerLight = "rgba(244,248,252,0.07)",
            LinesDefault = "rgba(244,248,252,0.14)",
            LinesInputs  = "rgba(244,248,252,0.22)",
            TableLines   = "rgba(244,248,252,0.09)",
            TableHover   = "rgba(244,248,252,0.05)",
            GrayLight    = "#173754",
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "6px",
            DrawerWidthLeft     = "264px",
            DrawerMiniWidthLeft = "76px",
            AppbarHeight        = "64px",
        },
    };
}
