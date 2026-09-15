using UPZMG.Shared.Enums;

namespace UPZMG.Web.Models;

public sealed class ScheduleItemViewModel
{
    public Days? Day { get; set; }
    public ScheduleTime? StartTime { get; set; }
    public ScheduleTime? EndTime { get; set; }
}

public sealed class ClassGroupFormViewModel
{
    public Guid? PeriodId { get; set; }
    public Guid? CareerId { get; set; }
    public Guid? AsignatureId { get; set; }
    public Guid? TeacherId { get; set; }
    public Guid? FacilityId { get; set; }
    public bool IsTheory { get; set; } = true;
    public int? Capacity { get; set; }
    public int? Weekhours { get; set; }
    public int? Enrolled { get; set; }
    public List<ScheduleItemViewModel> Schedules { get; set; } = [];
}
