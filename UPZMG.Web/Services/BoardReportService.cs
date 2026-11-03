using System.Net;
using System.Text.Json;
using UPZMG.Implementations.BoardReport;

namespace UPZMG.Web.Services;

/// <summary>
/// The board report is assembled in the API from the captures that were sent; the web only
/// asks for the finished document and sends back the wording the university writes.
/// </summary>
public sealed class BoardReportService(
    ISecurityService securityService,
    ILogger<BoardReportService> logger) : IBoardReportService
{
    private static readonly JsonSerializerOptions ResponseOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<BoardReportListItem>?> GetCyclesAsync() =>
        await GetOrDefaultAsync<List<BoardReportListItem>>("boardreport/cycles");

    public Task<BoardReportDocument?> GetDocumentAsync(string cycle) =>
        GetOrDefaultAsync<BoardReportDocument>($"boardreport/{Uri.EscapeDataString(cycle)}");

    public async Task<BoardReportSaveResult> SaveNarrativeAsync(string cycle, string blockKey, string? text, string? baseText)
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var request = new SaveBoardNarrative
        {
            RequesterKey = requesterKey,
            Cycle = cycle,
            BlockKey = blockKey,
            Text = text,
            BaseText = baseText,
        };

        return await PostAsync("boardreport/narrative", JsonSerializer.Serialize(request), requesterKey);
    }

    public async Task<BoardReportSaveResult> SubmitAsync(string cycle)
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var request = new SubmitBoardReport { RequesterKey = requesterKey, Cycle = cycle };

        return await PostAsync("boardreport/submit", JsonSerializer.Serialize(request), requesterKey);
    }

    private async Task<BoardReportSaveResult> PostAsync(string path, string json, Guid requesterKey)
    {
        var response = await securityService.SendPostRequestAsync<object, BoardReportSaveResult>(path, json, requesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<BoardReportSaveResult>(response.Result, ResponseOptions)
                ?? BoardReportSaveResult.Refused("La respuesta del sistema no se pudo leer. Vuelve a intentarlo; si sigue así, avisa a Tecnologías.");
        }

        return BoardReportSaveResult.Refused(response.StatusCode switch
        {
            HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized => "Sólo Planeación puede editar el informe.",
            _ => "No se pudo guardar: el sistema no respondió.",
        });
    }

    // SendGetRequestAsync throws on any error status or when the API is down.
    private async Task<T?> GetOrDefaultAsync<T>(string path) where T : class
    {
        try
        {
            var requesterKey = await securityService.GetRequesterKeyAsync();
            var response = await securityService.SendGetRequestAsync<T>(path, requesterKey);

            return response.StatusCode == HttpStatusCode.OK && response.Result is not null
                ? JsonSerializer.Deserialize<T>(response.Result, ResponseOptions)
                : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning("Board report request {Path} failed: {Message}", path, ex.Message);
            return null;
        }
    }
}
