using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Web.Models;

/// <summary>Bir çalışanın bir gruba üyeliği. Aynı kişi aynı gruba iki kez eklenemez.</summary>
public class GroupMember
{
    public int Id { get; set; }

    public int GroupId { get; set; }

    [ForeignKey(nameof(GroupId))]
    public TaskGroup? Group { get; set; }

    public int EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    // Grup sahibi üye ekleyip çıkarabilir.
    public bool IsOwner { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
