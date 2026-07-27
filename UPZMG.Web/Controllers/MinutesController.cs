using Microsoft.AspNetCore.Mvc;
using UPZMG.Web.Models;

namespace UPZMG.Web.Controllers;

/// <summary>
/// Manages board minutes (Actas de Junta Directiva) — list and detail views.
/// </summary>
public class MinutesController : SigieBaseController
{
    // Mock in-memory catalog (to be replaced with real DB/API queries later)
    private static readonly List<MinuteListItemModel> MockMinutes = new()
    {
        new() { Id = 1, SessionName = "3ra Sesión Ordinaria 2026", Date = new DateTime(2026, 6, 12), Status = "Firmada" },
        new() { Id = 2, SessionName = "2da Sesión Ordinaria 2026", Date = new DateTime(2026, 3, 20), Status = "En revisión gobierno" },
        new() { Id = 3, SessionName = "1ra Sesión Extraordinaria 2026", Date = new DateTime(2026, 5, 5), Status = "Pendiente de firmas" },
        new() { Id = 4, SessionName = "1ra Sesión Ordinaria 2026", Date = new DateTime(2026, 1, 30), Status = "Firmada" },
    };

    public IActionResult Index()
    {
        var (displayName, initials) = SetUserViewData();

        var model = new MinutesIndexViewModel
        {
            Minutes = MockMinutes,
            UserDisplayName = displayName,
            UserInitials = initials
        };

        return View(model);
    }

    public IActionResult Detail(int id)
    {
        var (displayName, initials) = SetUserViewData();

        var minute = MockMinutes.FirstOrDefault(a => a.Id == id) ?? MockMinutes[0];
        var isSigned = minute.Status == "Firmada";

        var model = new MinuteDetailViewModel
        {
            Id = minute.Id,
            SessionName = minute.SessionName,
            Date = minute.Date,
            Status = minute.Status,
            UserDisplayName = displayName,
            UserInitials = initials,
            Signatories = new List<SignatoryModel>
            {
                CreateSignatory("Dr. Fernando Ibarra Castillo", "Rector", "Firmado", minute.Date),
                CreateSignatory("Lic. Patricia Núñez Solís", "Representante SEP", "Firmado", minute.Date.AddDays(1)),
                CreateSignatory("Mtro. Jorge Aldana Ruiz", "Secretaría de Innovación, Ciencia y Tecnología", isSigned ? "Firmado" : "Pendiente", isSigned ? minute.Date.AddDays(2) : null),
                CreateSignatory("C.P. Rosalba Márquez Toledo", "Contraloría del Estado", isSigned ? "Firmado" : "Pendiente", isSigned ? minute.Date.AddDays(2) : null),
                CreateSignatory("Ing. Hugo Ramírez Ochoa", "Presidente Municipal", isSigned ? "Firmado" : "Pendiente", isSigned ? minute.Date.AddDays(3) : null),
                CreateSignatory("Alma Delia Torres Vega", "Representante de Alumnas y Alumnos", "Pendiente", null),
                CreateSignatory("Mtra. Claudia Reyes Ponce", "Secretaria Académica", "Firmado", minute.Date),
            }
        };

        return View(model);
    }

    /// <summary>
    /// Creates a <see cref="SignatoryModel"/> with auto-generated initials from the full name.
    /// </summary>
    private static SignatoryModel CreateSignatory(string name, string title, string status, DateTime? signedDate)
    {
        var initials = string.Concat(
            name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(p => char.IsLetter(p[0]))
                .Select(p => char.ToUpperInvariant(p[0])));

        return new SignatoryModel
        {
            Name = name,
            Title = title,
            Status = status,
            SignedDate = signedDate,
            Initials = initials.Length > 2 ? initials[..2] : initials
        };
    }
}
