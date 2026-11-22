using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Platform.Web.Models.ViewModels;

/// <summary>
/// Departmana yönetici atama / yöneticiyi değiştirme formu.
/// Formdan SADECE YoneticiId alınır. "Yönetici yok" seçeneği bilerek yoktur:
/// mevcut yönetici yalnızca başka uygun bir personelle değiştirilebilir.
/// </summary>
public class DepartmanYoneticiViewModel
{
    // ---- Formdan alınan alan ----

    [Display(Name = "Yönetici")]
    [Required(ErrorMessage = "Yönetici seçimi zorunludur.")]
    public int? YoneticiId { get; set; }

    // ---- Sadece ekranda gösterilen alanlar (formdan değer alınmaz) ----

    [BindNever, ValidateNever]
    public int DepartmanId { get; set; }

    [BindNever, ValidateNever]
    public string DepartmanAdi { get; set; } = string.Empty;

    [BindNever, ValidateNever]
    public int? MevcutYoneticiId { get; set; }

    [BindNever, ValidateNever]
    public string? MevcutYoneticiAdi { get; set; }

    // Uygun adaylar: bu departmanda, aktif, Yönetici rolünde ve başka bir departmanı yönetmeyen personel.
    [BindNever, ValidateNever]
    public List<SelectListItem> Adaylar { get; set; } = new();
}
