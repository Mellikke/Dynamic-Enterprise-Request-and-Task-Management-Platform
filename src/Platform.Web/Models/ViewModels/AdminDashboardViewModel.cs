using Platform.Web.Models.Enums;

namespace Platform.Web.Models.ViewModels;

/// <summary>
/// Admin paneli (dashboard) verileri. Sadece okunur.
///
/// Sayım kuralları:
///  - "Personel" = Çalışan + Yönetici rolündeki hesaplar. Admin hesapları hiçbir personel sayısına dahil değildir.
///  - "Yönetici rolündeki personel" (bir kişinin rolü) ile "departman yöneticisi" (Department.ManagerId)
///    farklı kavramlardır ve ayrı gösterilir.
/// </summary>
public class AdminDashboardViewModel
{
    // ---- Personel (Admin hariç) ----

    // Aktif + pasif.
    public int ToplamPersonel { get; set; }

    public int AktifPersonel { get; set; }

    public int PasifPersonel => ToplamPersonel - AktifPersonel;

    // Rol dağılımı — sadece aktif personel.
    public int AktifCalisanRolu { get; set; }

    public int AktifYoneticiRolu { get; set; }

    // Departmanı atanmamış aktif personel.
    public int DepartmansizAktifPersonel { get; set; }

    // ---- Departmanlar ----

    public List<DepartmanOzetSatiri> Departmanlar { get; set; } = new();

    public int DepartmanSayisi => Departmanlar.Count;

    // Departman yöneticisi atanmış departman sayısı (Department.ManagerId dolu).
    public int YoneticisiAtanmisDepartmanSayisi => Departmanlar.Count(d => d.YoneticiAdi is not null);

    // ---- Görevler ----
    // Görev yönetimi modülü tamamlanana kadar null kalır; ekranda "—" ve "Görev verileri henüz mevcut değil." gösterilir.
    // Modül tamamlanınca AdminController.adminDashboard içinde doldurulması yeterlidir; view hazırdır.
    public GorevOzeti? Gorevler { get; set; }

    // Payda 0 ise 0 döner (boş veritabanında sıfıra bölme olmaz).
    public static int Yuzde(int pay, int payda)
    {
        return payda <= 0 ? 0 : (int)Math.Round(pay * 100.0 / payda);
    }
}

/// <summary>Dashboard'daki departman özeti tablosunun bir satırı.</summary>
public class DepartmanOzetSatiri
{
    public int Id { get; set; }

    public string Ad { get; set; } = string.Empty;

    // Departmandaki aktif personel (Admin hariç).
    public int AktifPersonel { get; set; }

    // Departman yöneticisi (Department.ManagerId). Atanmadıysa null.
    public string? YoneticiAdi { get; set; }

    public bool YoneticiAktif { get; set; }
}

/// <summary>Görev istatistikleri. Görev modülü tamamlanınca doldurulacak.</summary>
public class GorevOzeti
{
    public int CalismaGrubuSayisi { get; set; }

    public int ToplamGorev { get; set; }

    public int Bekliyor { get; set; }

    public int DevamEdiyor { get; set; }

    public int Tamamlandi { get; set; }

    public List<SonGorevSatiri> SonGorevler { get; set; } = new();
}

/// <summary>"Son eklenen görevler" tablosunun bir satırı.</summary>
public class SonGorevSatiri
{
    public string Baslik { get; set; } = string.Empty;

    public string GrupAdi { get; set; } = string.Empty;

    public TaskItemStatus Durum { get; set; }

    public DateTime? BitisTarihi { get; set; }
}
