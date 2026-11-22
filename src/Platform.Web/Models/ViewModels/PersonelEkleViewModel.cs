using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Platform.Web.Models.Validation;

namespace Platform.Web.Models.ViewModels;

/// <summary>
/// Yeni personel ekleme formu. Sadece burada tanımlı alanlar formdan alınır;
/// PasswordHash, IsActive gibi alanlar formdan gönderilse bile dikkate alınmaz.
/// </summary>
public class PersonelEkleViewModel
{
    // Ad/soyad: harfler (Türkçe dahil tüm diller), tek boşluk, kesme işareti (' veya ’), tire ve
    // kısaltma noktası ("M. Ali", "M.Ali"). Rakam, sembol, ayraçla başlama veya art arda ayraç kabul edilmez.
    // Aynı kalıp formda (pattern özelliği) da kullanılır: Views/Admin/PersonelEkle.cshtml
    public const string IsimDeseni = @"^[\p{L}\p{M}]+(?:(?:\. ?|[ '’\-])[\p{L}\p{M}]+)*\.?$";
    public const string IsimDeseniMesaji = "sadece harf, boşluk, kesme işareti, tire ve nokta içerebilir.";

    // Şifre: en az bir büyük harf, bir küçük harf ve bir rakam. Formda da aynı kalıp kullanılır.
    public const string SifreDeseni = @"^(?=.*\p{Lu})(?=.*\p{Ll})(?=.*\d).+$";

    // Üst sınırlar Employee modelindeki MaxLength değerleriyle aynı.
    [Display(Name = "Ad")]
    [Required(ErrorMessage = "Ad zorunludur.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Ad 2 ile 100 karakter arasında olmalıdır.")]
    [RegularExpression(IsimDeseni, ErrorMessage = "Ad " + IsimDeseniMesaji)]
    public string Ad { get; set; } = string.Empty;

    [Display(Name = "Soyad")]
    [Required(ErrorMessage = "Soyad zorunludur.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Soyad 2 ile 100 karakter arasında olmalıdır.")]
    [RegularExpression(IsimDeseni, ErrorMessage = "Soyad " + IsimDeseniMesaji)]
    public string Soyad { get; set; } = string.Empty;

    [Display(Name = "E-posta")]
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [StringLength(255, ErrorMessage = "E-posta en fazla 255 karakter olabilir.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [GecerliEposta]
    public string Eposta { get; set; } = string.Empty;

    // İsteğe bağlı. Seçildiyse sunucuda veritabanında var olup olmadığı kontrol edilir.
    [Display(Name = "Departman")]
    public int? DepartmanId { get; set; }

    // Sadece Çalışan (2) veya Yönetici (3) kabul edilir; kontrol sunucuda yapılır.
    [Display(Name = "Rol")]
    [Required(ErrorMessage = "Rol seçimi zorunludur.")]
    public int? RolId { get; set; } = Role.EmployeeId;

    [Display(Name = "Başlangıç şifresi")]
    [Required(ErrorMessage = "Şifre zorunludur.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
    [RegularExpression(SifreDeseni,
        ErrorMessage = "Şifre en az bir büyük harf, bir küçük harf ve bir rakam içermelidir.")]
    [DataType(DataType.Password)]
    public string Sifre { get; set; } = string.Empty;

    [Display(Name = "Şifre tekrarı")]
    [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
    [Compare(nameof(Sifre), ErrorMessage = "Şifreler eşleşmiyor.")]
    [DataType(DataType.Password)]
    public string SifreTekrar { get; set; } = string.Empty;

    // Açılır liste seçenekleri: sadece ekranda gösterilir, formdan değer alınmaz.
    [BindNever, ValidateNever]
    public List<SelectListItem> Departmanlar { get; set; } = new();

    [BindNever, ValidateNever]
    public List<SelectListItem> Roller { get; set; } = new();
}
