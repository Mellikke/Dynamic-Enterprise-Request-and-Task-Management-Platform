using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Web.Models;

/// <summary>Kullanıcı rolü: Admin, Yönetici veya Çalışan.</summary>
public class Role
{
    // Roller sabit olduğu için Id'yi veritabanı üretmez, biz veririz.
    public const int AdminId = 1;
    public const int EmployeeId = 2;
    public const int ManagerId = 3;

    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
