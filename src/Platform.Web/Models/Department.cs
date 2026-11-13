using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Web.Models;

public class Department
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    // Departmanın yöneticisi (henüz atanmadıysa boş).
    public int? ManagerId { get; set; }

    [ForeignKey(nameof(ManagerId))]
    public Employee? Manager { get; set; }

    // Bu departmanda çalışanlar.
    [InverseProperty(nameof(Employee.Department))]
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
