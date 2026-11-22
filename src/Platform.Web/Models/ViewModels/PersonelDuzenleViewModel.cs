using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Platform.Web.Models.ViewModels;

/// <summary>
/// Personel düzenleme formu. Formdan SADECE departman, rol ve aktiflik alınır;
/// ad, soyad, e-posta ve şifre bu ekrandan değiştirilemez (sadece gösterilir).
/// </summary>
public class PersonelDuzenleViewModel
{
    // ---- Formdan alınan alanlar ----

    [Display(Name = "Departman")]
    public int? DepartmanId { get; set; }

    // Sadece Çalışan (2) veya Yönetici (3) kabul edilir; kontrol sunucuda yapılır.
    [Display(Name = "Rol")]
    [Required(ErrorMessage = "Rol seçimi zorunludur.")]
    public int? RolId { get; set; }

    [Display(Name = "Aktif")]
    public bool Aktif { get; set; }

    // ---- Sadece ekranda gösterilen alanlar (formdan değer alınmaz) ----

    [BindNever, ValidateNever]
    public int Id { get; set; }

    [BindNever, ValidateNever]
    public string Ad { get; set; } = string.Empty;

    [BindNever, ValidateNever]
    public string Soyad { get; set; } = string.Empty;

    [BindNever, ValidateNever]
    public string Eposta { get; set; } = string.Empty;

    // Bu personelin yöneticisi olduğu departmanlar (Department.ManagerId == Id).
    [BindNever, ValidateNever]
    public List<string> YonettigiDepartmanlar { get; set; } = new();

    [BindNever, ValidateNever]
    public List<SelectListItem> Departmanlar { get; set; } = new();

    [BindNever, ValidateNever]
    public List<SelectListItem> Roller { get; set; } = new();
}
