using System.ComponentModel.DataAnnotations;

namespace Platform.Web.Models.ViewModels;

/// <summary>Yeni departman ekleme formu. Sadece burada tanımlı alanlar formdan alınır.</summary>
public class DepartmanEkleViewModel
{
    // Üst sınır, Department modelindeki MaxLength(150) ile aynı.
    [Display(Name = "Departman adı")]
    [Required(ErrorMessage = "Departman adı zorunludur.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Departman adı 2 ile 150 karakter arasında olmalıdır.")]
    public string Ad { get; set; } = string.Empty;
}
