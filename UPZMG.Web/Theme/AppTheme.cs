using MudBlazor;

namespace UPZMG.Web.Theme;

/// <summary>
/// Institutional MudBlazor theme for SIGIE / UPZMG.
///
/// Design language: a restrained institutional palette — deep navy as the brand
/// anchor, a muted teal as the interactive accent, a subdued indigo as the
/// analytical accent and a soft ochre for highlights. Colours are deliberately
/// desaturated: this is a reporting tool for a university, read for hours at a
/// time, not a marketing dashboard.
///
/// Only the palette + layout metrics live here. Typography scale, surfaces,
/// motion and chart styling are centralised in wwwroot/app.css.
/// </summary>
public static class AppTheme
{
    // Shared accent ramp — kept in sync with the CSS tokens in app.css.
    public const string Teal   = "#1B7F73";
    public const string TealUp = "#4FA396";
    public const string Indigo = "#54539B";
    public const string Navy   = "#1F3D6B";
    public const string Ochre  = "#B0862F";
    public const string Clay   = "#B4443F";

    public static readonly MudTheme Institutional = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary          = Navy,       // institutional navy
            Secondary        = Teal,       // interactive accent
            Tertiary         = Indigo,     // analytical accent
            Info             = "#2C6099",
            Success          = "#2F7D5B",
            Warning          = Ochre,
            Error            = Clay,

            // Header is a light sheet (app.css blurs it); text stays ink.
            AppbarBackground = "#FFFFFF",
            AppbarText       = "#1B2635",

            // The rail follows the theme: airy in light mode, ink in dark.
            DrawerBackground = "#FFFFFF",
            DrawerText       = "#44505F",
            DrawerIcon       = "#6B7686",

            Background       = "#F4F6F9",
            BackgroundGray   = "#E9EDF2",
            Surface          = "#FFFFFF",
            TextPrimary      = "#1B2635",
            TextSecondary    = "#5A6675",
            TextDisabled     = "rgba(27,38,53,0.38)",
            ActionDefault    = "#5A6675",
            ActionDisabled   = "rgba(27,38,53,0.26)",
            Divider          = "#DFE4EB",
            DividerLight     = "#EAEDF2",
            LinesDefault     = "#DFE4EB",
            LinesInputs      = "#CFD6E0",
            TableLines       = "#E7EBF0",
            TableHover       = "#F1F4F8",
            GrayLight        = "#F1F4F8",
        },
        PaletteDark = new PaletteDark
        {
            Primary          = "#7EA3D6",  // navy reads as muted sky on ink
            Secondary        = TealUp,
            Tertiary         = "#8C8ACB",
            Info             = "#7BA6D9",
            Success          = "#5CA37E",
            Warning          = "#C9A253",
            Error            = "#CF706B",

            AppbarBackground = "#161B23",
            AppbarText       = "#E2E6EC",

            DrawerBackground = "#12161D",
            DrawerText       = "rgba(226,230,236,0.82)",
            DrawerIcon       = "rgba(226,230,236,0.66)",

            Background       = "#10141B",
            BackgroundGray   = "#171C24",
            Surface          = "#181D26",
            TextPrimary      = "#E2E6EC",
            TextSecondary    = "#98A2B0",
            TextDisabled     = "rgba(226,230,236,0.36)",
            ActionDefault    = "#98A2B0",
            ActionDisabled   = "rgba(226,230,236,0.26)",
            Divider          = "rgba(255,255,255,0.09)",
            DividerLight     = "rgba(255,255,255,0.05)",
            LinesDefault     = "rgba(255,255,255,0.11)",
            LinesInputs      = "rgba(255,255,255,0.17)",
            TableLines       = "rgba(255,255,255,0.07)",
            TableHover       = "rgba(255,255,255,0.04)",
            GrayLight        = "#1E242E",
        },
        LayoutProperties = new LayoutProperties
        {
            // Squarer corners read as more institutional and, at this density,
            // keep tables and cards visually aligned with each other.
            DefaultBorderRadius = "8px",
            DrawerWidthLeft     = "268px",
            DrawerMiniWidthLeft = "78px",
            AppbarHeight        = "68px",
        },
    };
}
