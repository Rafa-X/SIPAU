using System.Net;
using System.Text.Json;
using UPZMG.Implementations.Reports;

namespace UPZMG.Web.Services;

public sealed class ReportsService(ISecurityService securityService) : IReportsService
{
    private static readonly JsonSerializerOptions ResponseOptions = new(JsonSerializerDefaults.Web);

    public Task<IReadOnlyList<ReportTemplateItem>> GetTemplatesAsync() =>
        GetListAsync<ReportTemplateItem>("reports/templates");

    public Task<ReportTemplateDefinition?> GetTemplateAsync(string reportType, string version) =>
        GetOrNullAsync<ReportTemplateDefinition>(
            $"reports/templates/{Uri.EscapeDataString(reportType)}/{Uri.EscapeDataString(version)}");

    public Task<IReadOnlyList<ReportListItem>> GetMyReportsAsync() =>
        GetListAsync<ReportListItem>("reports/mine");

    public Task<ReportDetail?> GetReportAsync(Guid reportId) =>
        GetOrNullAsync<ReportDetail>($"reports/{reportId}");

    public async Task<ReportSaveResponse> SaveAsync(SaveReport request)
    {
        var response = await securityService.SendPostRequestAsync<SaveReport, ReportSaved>(
            "reports/save", JsonSerializer.Serialize(request), request.RequesterKey);

        var saved = response.Sucess && response.Result is not null
            ? JsonSerializer.Deserialize<ReportSaved>(response.Result, ResponseOptions)
            : null;

        return new ReportSaveResponse(response.StatusCode, saved, response.ErrorMessage);
    }

    public async Task<string?> GetTemplateSourceAsync(string reportType, string version)
    {
        var source = await GetOrNullAsync<ReportTemplateSource>(
            $"reports/templates/{Uri.EscapeDataString(reportType)}/{Uri.EscapeDataString(version)}/source");

        return source?.Json;
    }

    public Task<TemplateSaveResult> SaveTemplateAsync(SaveReportTemplate request) =>
        WriteTemplateAsync("reports/templates/save", JsonSerializer.Serialize(request), request.RequesterKey);

    public Task<TemplateSaveResult> DeleteTemplateAsync(DeleteReportTemplate request) =>
        WriteTemplateAsync("reports/templates/delete", JsonSerializer.Serialize(request), request.RequesterKey);

    private async Task<TemplateSaveResult> WriteTemplateAsync(string path, string json, Guid requesterKey)
    {
        var response = await securityService.SendPostRequestAsync<object, TemplateSaveResult>(path, json, requesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<TemplateSaveResult>(response.Result, ResponseOptions)
                ?? TemplateSaveResult.Refused("La respuesta del sistema no se pudo leer. Vuelve a intentarlo; si sigue así, avisa a Tecnologías.");
        }

        return TemplateSaveResult.Refused(response.StatusCode switch
        {
            HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized => "Sólo un administrador puede editar plantillas.",
            _ => "No se pudo guardar: el sistema no respondió.",
        });
    }

    private async Task<IReadOnlyList<T>> GetListAsync<T>(string path)
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var response = await securityService.SendGetRequestAsync<IReadOnlyList<T>>(path, requesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<IReadOnlyList<T>>(response.Result, ResponseOptions) ?? [];
        }
        return [];
    }

    // SendGetRequestAsync throws on any error status; here a 404 only means "not there".
    private async Task<T?> GetOrNullAsync<T>(string path) where T : class
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        try
        {
            var response = await securityService.SendGetRequestAsync<T>(path, requesterKey);
            return response.StatusCode == HttpStatusCode.OK && response.Result is not null
                ? JsonSerializer.Deserialize<T>(response.Result, ResponseOptions)
                : null;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
