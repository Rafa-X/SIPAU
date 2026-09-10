namespace UPZMG.Web.Models;

/// <summary>
/// ViewModel for the data capture landing page (list of capture areas).
/// </summary>
public class DataCaptureIndexViewModel
{
    public string ActivePeriod { get; set; } = string.Empty;
    public List<DataCaptureAreaModel> Areas { get; set; } = new();
}
