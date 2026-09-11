using System.ComponentModel.DataAnnotations;

namespace UPZMG.Persistence.Models;

public class MaxDegrees
{
    public Guid Id { get; set; }

    [MaxLength(150)]
    public required string Name { get; set; }
}
