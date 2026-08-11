using MudBlazor;

namespace UPZMG.Web.Theme;

/// <summary>
/// Institutional MudBlazor theme for SIGIE / UPZMG.
///
/// Design language: "ink &amp; aurora" — a deep institutional navy as the brand
/// anchor, a bright cyan-teal as the interactive accent, indigo as the
/// technological counterpoint and gold for highlights. Light mode is a cool,
/// airy canvas; dark mode is a near-black ink navy so the accents glow.
///
/// Only the palette + layout metrics live here. Typography scale, glass
/// surfaces, motion and chart styling are centralised in wwwroot/app.css.
/// </summary>
public static class AppTheme
{
    // Shared accent ramp — kept in sync with the CSS tokens in app.css.
    public const string Cyan   = "#0FB5A0";
    public const string CyanUp = "#22D3BF";
    public const string Indigo = "#6D5CE7";
    public const string Navy   = "#173A6D";
    public const string Gold   = "#D4A02C";
    public const string Rose   = "#E5484D";

    public static readonly MudTheme Institutional = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary          = Navy,       // institutional navy
            Secondary        = Cyan,       // interactive accent
            Tertiary         = Indigo,     // technological accent
            Info             = "#2F6FED",
            Success          = "#0E9F6E",
            Warning          = Gold,
            Error            = Rose,

            // Header is a light glass sheet (app.css blurs it); text stays ink.
            AppbarBackground = "#FFFFFF",
            AppbarText       = "#0E1B33",

            // The navy rail is the brand signature and stays dark in both modes.
            DrawerBackground = "#0B1B3A",
            DrawerText       = "rgba(255,255,255,0.88)",
            DrawerIcon       = "rgba(255,255,255,0.70)",

            Background       = "#F4F7FC",
            BackgroundGray   = "#E8EDF7",
            Surface          = "#FFFFFF",
            TextPrimary      = "#0E1B33",
            TextSecondary    = "#61708A",
            TextDisabled     = "rgba(14,27,51,0.38)",
            ActionDefault    = "#61708A",
            ActionDisabled   = "rgba(14,27,51,0.26)",
            Divider          = "#E3E9F4",
            DividerLight     = "#EDF1F8",
            LinesDefault     = "#E3E9F4",
            LinesInputs      = "#D5DEED",
            TableLines       = "#EAEFF8",
            TableHover       = "#F2F6FD",
            GrayLight        = "#F2F6FD",
        },
        PaletteDark = new PaletteDark
        {
            Primary          = "#6D9BFF",  // navy reads as sky-blue on ink
            Secondary        = CyanUp,
            Tertiary         = "#8C7CF7",
            Info             = "#6BA1FF",
            Success          = "#33C48D",
            Warning          = "#E8BC55",
            Error            = "#F2686C",

            AppbarBackground = "#0B1220",
            AppbarText       = "#E9EEF8",

            DrawerBackground = "#080D18",
            DrawerText       = "rgba(255,255,255,0.84)",
            DrawerIcon       = "rgba(255,255,255,0.70)",

            Background       = "#070B14",
            BackgroundGray   = "#0D1422",
            Surface          = "#111A2B",
            TextPrimary      = "#E9EEF8",
            TextSecondary    = "#93A1B8",
            TextDisabled     = "rgba(233,238,248,0.36)",
            ActionDefault    = "#93A1B8",
            ActionDisabled   = "rgba(233,238,248,0.26)",
            Divider          = "rgba(255,255,255,0.09)",
            DividerLight     = "rgba(255,255,255,0.05)",
            LinesDefault     = "rgba(255,255,255,0.10)",
            LinesInputs      = "rgba(255,255,255,0.16)",
            TableLines       = "rgba(255,255,255,0.07)",
            TableHover       = "rgba(255,255,255,0.04)",
            GrayLight        = "#162034",
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "16px",
            DrawerWidthLeft     = "268px",
            DrawerMiniWidthLeft = "78px",
            AppbarHeight        = "68px",
        },
    };
}
