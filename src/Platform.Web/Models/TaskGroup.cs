using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Web.Models;

/// <summary>Çalışanların görevlerini birlikte yönettiği çalışma grubu.</summary>
public class TaskGroup
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    // Grubu oluşturan çalışan.
    public int CreatedById { get; set; }

    [ForeignKey(nameof(CreatedById))]
    public Employee? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
