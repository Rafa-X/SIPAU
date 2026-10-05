using System.ComponentModel.DataAnnotations;
using UPZMG.Persistence.Models;

namespace UPZMG.Implementations.DataCapture;

public class CreateClassGroup
{
    public required Guid RequesterKey { get; set; }
    public required List<Schedule> Schedule { get; set; } = [];
    public required Guid CourseId { get; set; }
    public required Guid PeriodId { get; set; }
    public required Guid CareerId { get; set; }
    public required Guid TeacherId { get; set; }
    public required Guid FacilityId { get; set; }
    public bool IsTheory { get; set; }
    [Range(1, 500)]
    public int Capacity { get; set; }
    public double Weekhours { get; set; }
}

/// <summary>A correction to a class group; the schedule is re-checked against the other groups.</summary>
public sealed class UpdateClassGroup : CreateClassGroup
{
    public required Guid Id { get; set; }
}

public sealed class ClassGroupListItem
{
    public Guid Id { get; set; }
    public Guid PeriodId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid CareerId { get; set; }
    public string Career { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public string Teacher { get; set; } = string.Empty;
    public string Facility { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public List<Schedule> Schedule { get; set; } = [];
    public bool IsTheory { get; set; }
    public int Capacity { get; set; }
    public int Enrolled { get; set; }
}