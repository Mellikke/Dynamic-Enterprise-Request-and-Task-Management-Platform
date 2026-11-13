using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Platform.Web.Models.Enums;

namespace Platform.Web.Models;

/// <summary>
/// Bir gruptaki görev. Sınıfın adı .NET'in kendi Task sınıfıyla karışmasın diye
/// TaskItem; veritabanındaki tablo adı ise Tasks.
/// </summary>
[Table("Tasks")]
public class TaskItem
{
    public int Id { get; set; }

    public int GroupId { get; set; }

    [ForeignKey(nameof(GroupId))]
    public TaskGroup? Group { get; set; }

    [Required, MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public TaskItemStatus Status { get; set; } = TaskItemStatus.Pending;

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    // Görevi oluşturan çalışan.
    public int CreatedById { get; set; }

    [ForeignKey(nameof(CreatedById))]
    public Employee? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? StartDate { get; set; }

    public DateTime? DueDate { get; set; }

    // Görev "Completed" yapıldığında doldurulur.
    public DateTime? CompletedAt { get; set; }

    public ICollection<TaskAssignment> Assignments { get; set; } = new List<TaskAssignment>();
}
