namespace UPZMG.Web.Models;

/// <summary>
/// ViewModel for the board report landing page (list of generated reports).
/// </summary>
public class BoardReportIndexViewModel
{
    public List<BoardReportListItemModel> Reports { get; set; } = new();
}
