using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace UPZMG.Web.TagHelpers;

/// <summary>
/// Renders the standard page header (title + optional subtitle + right-side
/// actions), replacing the repeated markup. Inner content becomes the actions.
///
/// Usage:
///   &lt;page-header title="Captura de Datos" subtitle="Registro por área"&gt;
///       &lt;a class="btn btn-accent"&gt;Nuevo&lt;/a&gt;
///   &lt;/page-header&gt;
/// </summary>
[HtmlTargetElement("page-header", TagStructure = TagStructure.NormalOrSelfClosing)]
public class PageHeaderTagHelper : TagHelper
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var actions = (await output.GetChildContentAsync()).GetContent();

        output.TagName = "div";
        output.Attributes.SetAttribute("class", "page-header animate-fade-in");

        var sb = new StringBuilder();
        sb.Append("<div class=\"page-header-left\">");
        sb.Append("<h1>").Append(HtmlEncoder.Default.Encode(Title)).Append("</h1>");
        if (!string.IsNullOrWhiteSpace(Subtitle))
            sb.Append("<p>").Append(HtmlEncoder.Default.Encode(Subtitle)).Append("</p>");
        sb.Append("</div>");

        if (!string.IsNullOrWhiteSpace(actions))
            sb.Append("<div class=\"page-header-right\">").Append(actions).Append("</div>");

        output.Content.SetHtmlContent(sb.ToString());
    }
}
