using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Web.Models;

public class Employee
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(255), EmailAddress]
    public string Email { get; set; } = string.Empty;

    // Şifrenin kendisi değil, şifrelenmiş (hash) hali saklanır.
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }

    [ForeignKey(nameof(DepartmentId))]
    public Department? Department { get; set; }

    // Varsayılan rol: Çalışan.
    public int RoleId { get; set; } = Role.EmployeeId;

    public Role? Role { get; set; }

    // Pasif çalışan giriş yapamaz; kayıtları silinmez.
    public bool IsActive { get; set; } = true;

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
}
