using System.ComponentModel.DataAnnotations;
using UPZMG.Persistence.Models;

namespace UPZMG.Implementations.DataCapture;

public sealed class CreateClassGroup
{
    public required Guid RequesterKey { get; set; }
    public required List<Schedule> Schedule { get; set; } = [];
    public required Guid AsignatureId { get; set; }
    public required Guid PeriodId { get; set; }
    public required Guid CareerId { get; set; }
    public required Guid TeacherId { get; set; }
    public required Guid FacilityId { get; set; }
    public bool IsTheory { get; set; }
    [Range(1, 500)]
    public int Capacity { get; set; }
    public double Weekhours { get; set; }
}