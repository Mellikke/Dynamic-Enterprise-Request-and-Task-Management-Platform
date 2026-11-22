namespace Platform.Web.Models.ViewModels;

/// <summary>Personel listesindeki bir satır.</summary>
public class PersonelListeViewModel
{
    public int Id { get; set; }

    public string Ad { get; set; } = string.Empty;

    public string Soyad { get; set; } = string.Empty;

    public string Eposta { get; set; } = string.Empty;

    // Departman atanmadıysa boş.
    public string? DepartmanAdi { get; set; }

    public int RolId { get; set; }

    public string RolAdi { get; set; } = string.Empty;

    public bool Aktif { get; set; }
}
