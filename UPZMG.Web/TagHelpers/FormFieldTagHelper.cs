using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace UPZMG.Web.TagHelpers;

/// <summary>
/// Wraps a form control with its label, replacing the repeated
/// `&lt;div class="form-group"&gt;&lt;label&gt;…` boilerplate. The input goes
/// inside as child content so `asp-for` model binding keeps working.
///
/// Usage:
///   &lt;form-field label="Matrícula" for="EnrollmentNumber"&gt;
///       &lt;input asp-for="EnrollmentNumber" class="form-control" required /&gt;
///   &lt;/form-field&gt;
/// </summary>
[HtmlTargetElement("form-field")]
public class FormFieldTagHelper : TagHelper
{
    public string Label { get; set; } = string.Empty;
    public string For { get; set; } = string.Empty;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var input = (await output.GetChildContentAsync()).GetContent();

        output.TagName = "div";
        output.Attributes.SetAttribute("class", "form-group");

        var forAttr = string.IsNullOrWhiteSpace(For)
            ? string.Empty
            : $" for=\"{HtmlEncoder.Default.Encode(For)}\"";

        var sb = new StringBuilder();
        sb.Append("<label class=\"form-label\"").Append(forAttr).Append('>')
          .Append(HtmlEncoder.Default.Encode(Label)).Append("</label>");
        sb.Append(input);

        output.Content.SetHtmlContent(sb.ToString());
    }
}
