using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Web.Models;

/// <summary>Bir görevin bir çalışana atanması. Bir görev birden fazla kişiye atanabilir.</summary>
public class TaskAssignment
{
    public int Id { get; set; }

    public int TaskId { get; set; }

    [ForeignKey(nameof(TaskId))]
    public TaskItem? TaskItem { get; set; }

    public int EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
