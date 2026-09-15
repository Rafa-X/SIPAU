using System.ComponentModel.DataAnnotations;

namespace UPZMG.Persistence.Models;

/// <summary>
/// Academic module.
/// Represents a class group in the academic system.
/// </summary>

public class ClassGroups
{
    public Guid Id { get; set; }
    public Guid FacilityId { get; set; } //fk to SchoolFacility - Classroom details
    public List<Schedule> Schedule { get; set; } = [];
    public int Capacity { get; set; }
    public int Enrolled { get; set; }
    public double Weekhours { get; set; }
    public Guid PeriodId { get; set; } //fk
    public Guid CareerId { get; set; } //fk
    public Guid TeacherId { get; set; } //fk
    public Guid AsignatureId { get; set; } //fk
    public bool IsTheory { get; set; } // theoretical (true) or laboratory (false)
}

public class Schedule
{
    public string Day { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
}