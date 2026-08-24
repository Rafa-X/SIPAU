using UPZMG.Web.Models;

namespace UPZMG.Web.Services;

/// <summary>Board minutes (Actas de Junta) — mock list + detail with signatories.</summary>
public interface IMinutesService
{
    IReadOnlyList<MinuteListItemModel> GetAll();
    MinuteDetailViewModel? GetDetail(int id);
}

public sealed class MinutesService : IMinutesService
{
    private static readonly List<MinuteListItemModel> Minutes = new()
    {
        new() { Id = 1, SessionName = "3ra Sesión Ordinaria 2026", Date = new DateTime(2026, 6, 12), Status = "Firmada" },
        new() { Id = 2, SessionName = "2da Sesión Ordinaria 2026", Date = new DateTime(2026, 3, 20), Status = "En revisión gobierno" },
        new() { Id = 3, SessionName = "1ra Sesión Extraordinaria 2026", Date = new DateTime(2026, 5, 5), Status = "Pendiente de firmas" },
        new() { Id = 4, SessionName = "1ra Sesión Ordinaria 2026", Date = new DateTime(2026, 1, 30), Status = "Firmada" },
    };

    public IReadOnlyList<MinuteListItemModel> GetAll() => Minutes;

    public MinuteDetailViewModel? GetDetail(int id)
    {
        var minute = Minutes.FirstOrDefault(m => m.Id == id);
        if (minute is null) return null;

        var isSigned = minute.Status == "Firmada";

        return new MinuteDetailViewModel
        {
            Id = minute.Id,
            SessionName = minute.SessionName,
            Date = minute.Date,
            Status = minute.Status,
            Signatories = new List<SignatoryModel>
            {
                Signatory("Dr. Fernando Ibarra Castillo", "Rector", "Firmado", minute.Date),
                Signatory("Lic. Patricia Núñez Solís", "Representante SEP", "Firmado", minute.Date.AddDays(1)),
                Signatory("Mtro. Jorge Aldana Ruiz", "Secretaría de Innovación, Ciencia y Tecnología", isSigned ? "Firmado" : "Pendiente", isSigned ? minute.Date.AddDays(2) : null),
                Signatory("C.P. Rosalba Márquez Toledo", "Contraloría del Estado", isSigned ? "Firmado" : "Pendiente", isSigned ? minute.Date.AddDays(2) : null),
                Signatory("Ing. Hugo Ramírez Ochoa", "Presidente Municipal", isSigned ? "Firmado" : "Pendiente", isSigned ? minute.Date.AddDays(3) : null),
                Signatory("Alma Delia Torres Vega", "Representante de Alumnas y Alumnos", "Pendiente", null),
                Signatory("Mtra. Claudia Reyes Ponce", "Secretaria Académica", "Firmado", minute.Date),
            },
        };
    }

    private static SignatoryModel Signatory(string name, string title, string status, DateTime? signedDate)
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
            Initials = initials.Length > 2 ? initials[..2] : initials,
        };
    }
}
