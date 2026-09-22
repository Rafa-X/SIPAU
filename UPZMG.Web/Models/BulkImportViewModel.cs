using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace UPZMG.Web.Models;

/// <summary>
/// ViewModel for the bulk import form — lets an area upload a delimited text file
/// instead of typing each record. Mock only: the file is not actually parsed yet.
/// </summary>
public class BulkImportViewModel
{
    public string AreaKey { get; set; } = string.Empty;
    public bool HasHeaderRow { get; set; } = true;
    public IFormFile? File { get; set; }

    // Dropdown options (mock data, populated by the controller)
    public List<SelectListItem> Areas { get; set; } = new();
}
