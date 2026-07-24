using Microsoft.AspNetCore.Razor.TagHelpers;

namespace UPZMG.Web.TagHelpers;

/// <summary>
/// Renders a small inline SVG icon by name (Lucide-style line icons,
/// stroke = currentColor), replacing decorative emojis. Extend <see cref="Paths"/>
/// to add icons. Unknown names render nothing (fail-safe).
///
/// Usage:  &lt;icon name="upload" /&gt;
/// </summary>
[HtmlTargetElement("icon", TagStructure = TagStructure.WithoutEndTag)]
public class IconTagHelper : TagHelper
{
    public string Name { get; set; } = string.Empty;

    private static readonly Dictionary<string, string> Paths = new(StringComparer.OrdinalIgnoreCase)
    {
        // Navigation
        ["dashboard"] = "<rect x='3' y='3' width='7' height='9' rx='1'/><rect x='14' y='3' width='7' height='5' rx='1'/><rect x='14' y='12' width='7' height='9' rx='1'/><rect x='3' y='16' width='7' height='5' rx='1'/>",
        ["capture"]   = "<rect x='8' y='2' width='8' height='4' rx='1'/><path d='M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2'/><path d='M9 12h6'/><path d='M9 16h6'/>",
        ["chart"]     = "<path d='M23 6l-9.5 9.5-5-5L1 18'/><path d='M17 6h6v6'/>",
        ["report"]    = "<path d='M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z'/><path d='M14 2v6h6'/><path d='M9 13h6'/><path d='M9 17h6'/>",
        ["board"]     = "<path d='M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z'/><path d='M14 2v6h6'/><path d='M8 18v-2'/><path d='M12 18v-4'/><path d='M16 18v-6'/>",
        ["minutes"]   = "<path d='M12 20h9'/><path d='M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4 12.5-12.5z'/>",
        ["settings"]  = "<circle cx='12' cy='12' r='3'/><path d='M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z'/>",
        ["users"]     = "<path d='M17 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2'/><circle cx='9.5' cy='7' r='4'/><path d='M22 21v-2a4 4 0 0 0-3-3.87'/><path d='M16 3.13a4 4 0 0 1 0 7.75'/>",
        ["shield"]    = "<path d='M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z'/>",

        // Actions
        ["menu"]        = "<path d='M4 6h16'/><path d='M4 12h16'/><path d='M4 18h16'/>",
        ["arrow-left"]  = "<path d='M19 12H5'/><path d='M12 19l-7-7 7-7'/>",
        ["upload"]      = "<path d='M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4'/><path d='M17 8l-5-5-5 5'/><path d='M12 3v12'/>",
        ["plus"]        = "<path d='M12 5v14'/><path d='M5 12h14'/>",
        ["edit"]        = "<path d='M12 20h9'/><path d='M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4 12.5-12.5z'/>",
        ["logout"]      = "<path d='M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4'/><path d='M16 17l5-5-5-5'/><path d='M21 12H9'/>",
        ["chevron-down"]= "<path d='M6 9l6 6 6-6'/>",

        // Sections / areas
        ["student"]   = "<circle cx='12' cy='8' r='4'/><path d='M4 21v-1a6 6 0 0 1 6-6h4a6 6 0 0 1 6 6v1'/>",
        ["teacher"]   = "<rect x='3' y='4' width='18' height='12' rx='1'/><path d='M8 20h8'/><path d='M12 16v4'/>",
        ["building"]  = "<rect x='4' y='3' width='16' height='18' rx='1'/><path d='M9 8h.5M14 8h.5M9 12h.5M14 12h.5M9 16h.5M14 16h.5'/>",
        ["link"]      = "<path d='M10 13a5 5 0 0 0 7 0l3-3a5 5 0 0 0-7-7l-1 1'/><path d='M14 11a5 5 0 0 0-7 0l-3 3a5 5 0 0 0 7 7l1-1'/>",
        ["money"]     = "<circle cx='12' cy='12' r='9'/><path d='M12 7v10'/><path d='M15 9.5a3 3 0 0 0-3-2 2.5 2.5 0 0 0 0 5 2.5 2.5 0 0 1 0 5 3 3 0 0 1-3-2'/>",
        ["trophy"]    = "<path d='M6 9H4.5a2.5 2.5 0 0 1 0-5H6'/><path d='M18 9h1.5a2.5 2.5 0 0 0 0-5H18'/><path d='M4 22h16'/><path d='M10 14.66V17c0 .55-.47.98-.97 1.21C7.85 18.75 7 20.24 7 22'/><path d='M14 14.66V17c0 .55.47.98.97 1.21C16.15 18.75 17 20.24 17 22'/><path d='M18 2H6v7a6 6 0 0 0 12 0V2z'/>",
        ["award"]     = "<circle cx='12' cy='8' r='6'/><path d='M15.47 12.89L17 22l-5-3-5 3 1.53-9.11'/>",
        ["calendar"]  = "<rect x='3' y='4' width='18' height='18' rx='2'/><path d='M16 2v4'/><path d='M8 2v4'/><path d='M3 10h18'/>",
        ["sliders"]   = "<path d='M4 21v-7'/><path d='M4 10V3'/><path d='M12 21v-9'/><path d='M12 8V3'/><path d='M20 21v-5'/><path d='M20 12V3'/><path d='M1 14h6'/><path d='M9 8h6'/><path d='M17 16h6'/>",
        ["landmark"]  = "<path d='M3 22h18'/><path d='M6 18v-7'/><path d='M10 18v-7'/><path d='M14 18v-7'/><path d='M18 18v-7'/><path d='M12 2L21 7H3z'/>",
        ["camera"]    = "<path d='M14.5 4h-5L7 7H4a2 2 0 0 0-2 2v9a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V9a2 2 0 0 0-2-2h-3l-2.5-3z'/><circle cx='12' cy='13' r='3'/>",
        ["info"]      = "<circle cx='12' cy='12' r='10'/><path d='M12 16v-4'/><path d='M12 8h.01'/>",
        ["check"]     = "<path d='M22 11.08V12a10 10 0 1 1-5.93-9.14'/><path d='M22 4L12 14.01l-3-3'/>",
        ["warning"]   = "<path d='M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z'/><path d='M12 9v4'/><path d='M12 17h.01'/>",
        ["mail"]      = "<rect x='2' y='4' width='20' height='16' rx='2'/><path d='M22 6l-10 7L2 6'/>",
        ["lock"]      = "<rect x='3' y='11' width='18' height='11' rx='2'/><path d='M7 11V7a5 5 0 0 1 10 0v4'/>",
    };

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!Paths.TryGetValue(Name, out var inner) || string.IsNullOrEmpty(inner))
        {
            output.SuppressOutput();
            return;
        }

        var extraClass = output.Attributes["class"]?.Value?.ToString();
        var cssClass = string.IsNullOrWhiteSpace(extraClass) ? "icon" : $"icon {extraClass}";

        output.TagName = "svg";
        // Force a start+end tag: <svg>…paths…</svg>. Without this the element
        // renders self-closing (<svg/>) and the inner paths are dropped.
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", cssClass);
        output.Attributes.SetAttribute("viewBox", "0 0 24 24");
        output.Attributes.SetAttribute("fill", "none");
        output.Attributes.SetAttribute("stroke", "currentColor");
        output.Attributes.SetAttribute("stroke-width", "2");
        output.Attributes.SetAttribute("stroke-linecap", "round");
        output.Attributes.SetAttribute("stroke-linejoin", "round");
        output.Attributes.SetAttribute("aria-hidden", "true");
        output.Content.SetHtmlContent(inner);
    }
}
