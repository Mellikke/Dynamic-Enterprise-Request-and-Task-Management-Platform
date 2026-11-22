namespace Platform.Web.Models.ViewModels;

/// <summary>Departman listesindeki bir satır.</summary>
public class DepartmanListeViewModel
{
    public int Id { get; set; }

    public string Ad { get; set; } = string.Empty;

    // Departmandaki aktif personel sayısı.
    public int PersonelSayisi { get; set; }

    // Yönetici atanmadıysa boş.
    public string? YoneticiAdi { get; set; }
}
