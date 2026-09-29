using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UPZMG.Implementations.BoardReport;
using UPZMG.Persistence.Repository;

using UPZMG.API.Security;
using UPZMG.Implementations.Security;

namespace UPZMG.Api.Controllers;

[ApiController]
[Route("boardreport")]
[RequirePermission(AppPermissions.ReadBoardReport)]
public class BoardReportController(
    BoardReportRepository boardReportRepository) : ControllerBase
{
    // Every area can read the report it contributed to.

    // Planeación writes and submits it.

    private const int MaxNarrative = 8_000;

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
            ? id
            : Guid.Empty;

    [HttpGet("cycles")]
    // One entry per cycle: how many formats arrived and whether the report was sent
    public async Task<ActionResult<List<BoardReportListItem>>> GetCycles() =>
        Ok(await boardReportRepository.GetCyclesAsync());

    [HttpGet("{cycle}")]
    // The whole document, built from the captures that were sent and the operational data
    public async Task<ActionResult<BoardReportDocument>> GetDocument(string cycle)
    {
        var document = await boardReportRepository.GetDocumentAsync(cycle);
        return document is null ? NotFound() : Ok(document);
    }

    [HttpPost("narrative")]
    [RequirePermission(AppPermissions.WriteBoardReport)]
    // Replaces the wording of one block; empty text restores the suggested one
    public async Task<ActionResult<BoardReportSaveResult>> SaveNarrative([FromBody] SaveBoardNarrative request)
    {
        if (CurrentUserId == Guid.Empty) return Unauthorized();
        if (request.Text is { Length: > MaxNarrative })
            return Ok(BoardReportSaveResult.Refused("El texto es demasiado largo."));

        return Ok(await boardReportRepository.SaveNarrativeAsync(request, CurrentUserId));
    }

    [HttpPost("submit")]
    [RequirePermission(AppPermissions.WriteBoardReport)]
    // Freezes the document once every format of the cycle has been sent
    public async Task<ActionResult<BoardReportSaveResult>> Submit([FromBody] SubmitBoardReport request)
    {
        if (CurrentUserId == Guid.Empty) return Unauthorized();
        return Ok(await boardReportRepository.SubmitAsync(request, CurrentUserId));
    }
}
