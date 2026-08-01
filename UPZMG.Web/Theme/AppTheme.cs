using MudBlazor;

namespace UPZMG.Web.Theme;

/// <summary>
/// Institutional MudBlazor theme for SIGIE / UPZMG.
/// Navy chrome, teal interactive accent, gold highlights, red for danger.
/// Light and dark palettes; the font is applied via app.css (Inter).
/// </summary>
public static class AppTheme
{
    public static readonly MudTheme Institutional = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary          = "#0B2350", // navy (slightly richer)
            Secondary        = "#12A594", // teal (brighter, more interactive)
            Tertiary         = "#C69528", // gold
            Info             = "#2563EB",
            Success          = "#0F9D6E",
            Warning          = "#D9922B",
            Error            = "#DC3B2E",
            AppbarBackground = "#0A1F44",
            AppbarText       = "#FFFFFF",
            DrawerBackground = "#0A1F44",
            DrawerText       = "rgba(255,255,255,0.90)",
            DrawerIcon       = "rgba(255,255,255,0.75)",
            Background       = "#F3F6FB",
            BackgroundGray   = "#E9EEF6",
            Surface          = "#FFFFFF",
            TextPrimary      = "#152238",
            TextSecondary    = "#5A6678",
            ActionDefault    = "#5A6678",
            Divider          = "#E6EBF2",
            LinesDefault     = "#E6EBF2",
        },
        PaletteDark = new PaletteDark
        {
            Primary          = "#5B8DD1", // lighter navy for dark-mode contrast
            Secondary        = "#1DB0AB",
            Tertiary         = "#E0B44A",
            Info             = "#5A9BF5",
            Success          = "#4CAF7D",
            Warning          = "#E0B44A",
            Error            = "#E06557",
            AppbarBackground = "#07172F",
            AppbarText       = "#FFFFFF",
            DrawerBackground = "#07172F",
            DrawerText       = "rgba(255,255,255,0.82)",
            DrawerIcon       = "rgba(255,255,255,0.82)",
            Background       = "#0E1621",
            Surface          = "#152230",
            TextPrimary      = "#E6EAF0",
            TextSecondary    = "#9AA5B3",
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "12px",
            DrawerWidthLeft     = "264px",
            AppbarHeight        = "64px",
        },
    };
}
