namespace UPZMG.Web.Models;

/// <summary>
/// ViewModel for a single board report's detail page
/// (table of contents + atypical events photo gallery).
/// </summary>
public class BoardReportDetailViewModel
{
    public int Id { get; set; }
    public string Period { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }
    public string Status { get; set; } = "Borrador";
    public List<BoardReportSectionModel> Sections { get; set; } = new();

    /// <summary> Mock photo count for the atypical events gallery (max 20 per client spec). </summary>
    public int TotalAtypicalEventPhotos { get; set; }
}
