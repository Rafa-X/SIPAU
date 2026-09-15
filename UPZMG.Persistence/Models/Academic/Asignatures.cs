using System.ComponentModel.DataAnnotations;

namespace UPZMG.Persistence.Models;

/// <summary>
/// Academic module.
/// Represents a course program in the academic system.
/// </summary>

public class Asignatures
{
    public Guid Id { get; set; }
    public Guid CareerId { get; set; } //fk
    public Guid AsignatureCode { get; set; }
    [MaxLength(120)]
    public string AsignatureName { get; set; } = null!;
    public int Credits { get; set; }
    public int Cuatrimestre { get; set; }
    public bool IsMandatory { get; set; } // Mandatory in the study plan or elective
}