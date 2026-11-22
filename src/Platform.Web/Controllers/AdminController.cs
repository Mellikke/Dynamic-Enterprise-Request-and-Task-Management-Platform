using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Platform.Web.Data;
using Platform.Web.Models;
using Platform.Web.Models.ViewModels;

namespace Platform.Web.Controllers;

// NOT: Cookie girişi tamamlanınca sınıfın başına [Authorize(Roles = "Admin")] eklenecek.
public class AdminController : Controller
{
    private static readonly CultureInfo Turkce = CultureInfo.GetCultureInfo("tr-TR");

    // Personel ekleme ekranından atanabilecek roller. Admin (1) bilerek yok.
    private static readonly int[] AtanabilirRoller = { Role.EmployeeId, Role.ManagerId };

    private readonly AppDbContext _db;
    private readonly IPasswordHasher<Employee> _hasher;

    public AdminController(AppDbContext db, IPasswordHasher<Employee> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    // Dashboard: sadece okur (AsNoTracking), hiçbir kaydı değiştirmez. Toplam 2 sorgu.
    // Admin hesapları personel sayılarına dahil edilmez.
    [HttpGet]
    public async Task<IActionResult> adminDashboard()
    {
        // 1) Personel özeti — tek sorgu (filtreli COUNT'lar). Tablo boşsa satır dönmez, değerler 0 kalır.
        var personel = await _db.Employees
            .AsNoTracking()
            .Where(e => e.RoleId != Role.AdminId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Toplam = g.Count(),
                Aktif = g.Count(e => e.IsActive),
                AktifCalisan = g.Count(e => e.IsActive && e.RoleId == Role.EmployeeId),
                AktifYonetici = g.Count(e => e.IsActive && e.RoleId == Role.ManagerId),
                DepartmansizAktif = g.Count(e => e.IsActive && e.DepartmentId == null)
            })
            .FirstOrDefaultAsync();

        // 2) Departman özeti — tek sorgu. Departman sayısı ve yöneticisi atanmış departman sayısı bu listeden hesaplanır.
        var departmanlar = await _db.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DepartmanOzetSatiri
            {
                Id = d.Id,
                Ad = d.Name,
                AktifPersonel = d.Employees.Count(e => e.IsActive && e.RoleId != Role.AdminId),
                YoneticiAdi = d.Manager == null ? null : d.Manager.FirstName + " " + d.Manager.LastName,
                YoneticiAktif = d.Manager != null && d.Manager.IsActive
            })
            .ToListAsync();

        var model = new AdminDashboardViewModel
        {
            ToplamPersonel = personel?.Toplam ?? 0,
            AktifPersonel = personel?.Aktif ?? 0,
            AktifCalisanRolu = personel?.AktifCalisan ?? 0,
            AktifYoneticiRolu = personel?.AktifYonetici ?? 0,
            DepartmansizAktifPersonel = personel?.DepartmansizAktif ?? 0,
            Departmanlar = departmanlar,

            // Görev yönetimi modülü tamamlanınca burada doldurulacak (TaskGroups, Tasks).
            Gorevler = null
        };

        return View(model);
    }

    // ---------------- Departman Yönetimi ----------------

    [HttpGet]
    public async Task<IActionResult> Departmanlar()
    {
        var departmanlar = await _db.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DepartmanListeViewModel
            {
                Id = d.Id,
                Ad = d.Name,
                // Aktif personel, Admin hariç (dashboard ile aynı kural).
                PersonelSayisi = d.Employees.Count(e => e.IsActive && e.RoleId != Role.AdminId),
                YoneticiAdi = d.Manager == null ? null : d.Manager.FirstName + " " + d.Manager.LastName
            })
            .ToListAsync();

        return View(departmanlar);
    }

    [HttpGet]
    public IActionResult DepartmanEkle()
    {
        return View(new DepartmanEkleViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DepartmanEkle(DepartmanEkleViewModel model)
    {
        // Yetki kontrolü — anti-forgery doğrulamasından (attribute) hemen sonra, her şeyden ÖNCE.
        // Yetkisiz istekte veritabanına gidilmez ve form yeniden gösterilmez; sadece 403 döner.
        if (!AdminGirisiYapilmis())
        {
            return YetkisizIstek();
        }

        // Baştaki/sondaki boşlukları sil, aradaki birden fazla boşluğu teke indir; sonra yeniden doğrula.
        model.Ad = AdiDuzenle(model.Ad);
        ModelState.Clear();
        TryValidateModel(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Aynı isim kontrolü: büyük/küçük harf farkı gözetmeden, Türkçe kurallarıyla (İ/i, I/ı).
        var yeniAd = model.Ad.ToUpper(Turkce);
        var mevcutAdlar = await _db.Departments.AsNoTracking().Select(d => d.Name).ToListAsync();

        if (mevcutAdlar.Any(ad => AdiDuzenle(ad).ToUpper(Turkce) == yeniAd))
        {
            ModelState.AddModelError(nameof(model.Ad), "Bu isimde bir departman zaten var.");
            return View(model);
        }

        _db.Departments.Add(new Department { Name = model.Ad });
        await _db.SaveChangesAsync();

        TempData["Basari"] = $"\"{model.Ad}\" departmanı eklendi.";
        return RedirectToAction(nameof(Departmanlar));
    }

    // ---------------- Departmana Yönetici Atama ----------------
    // Atanabilecek kişi: bu departmanda çalışan, aktif, Yönetici rolünde ve başka bir departmanı yönetmeyen personel.
    // "Yönetici yok" seçeneği yoktur; mevcut yönetici yalnızca başka biriyle değiştirilebilir.
    // Eski yöneticinin rolü otomatik değiştirilmez.

    [HttpGet]
    public async Task<IActionResult> DepartmanYoneticisi([FromRoute] int id)
    {
        return await YoneticiFormunuGosterAsync(id, new DepartmanYoneticiViewModel(), mevcutDegeriSec: true);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // id sadece adresten alınır ([FromRoute]); formdaki bir alan adresteki id'yi ezemez.
    public async Task<IActionResult> DepartmanYoneticisi([FromRoute] int id, DepartmanYoneticiViewModel model)
    {
        // 1) Yetki kontrolü — anti-forgery doğrulamasından (attribute) hemen sonra, her şeyden ÖNCE.
        if (!AdminGirisiYapilmis())
        {
            return YetkisizIstek();
        }

        // 2) Veritabanına gitmeyen kontrol. (Boş seçim [Required] ile yakalanır.)
        if (model.YoneticiId is <= 0)
        {
            ModelState.AddModelError(nameof(model.YoneticiId), "Geçersiz personel seçimi.");
        }

        // 3) Departman.
        var departman = await _db.Departments.FirstOrDefaultAsync(d => d.Id == id);
        if (departman is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return await YoneticiFormunuGosterAsync(id, model);
        }

        var yoneticiId = model.YoneticiId!.Value;

        // 4) Değişiklik yoksa veritabanına yazma.
        if (departman.ManagerId == yoneticiId)
        {
            TempData["Bilgi"] = $"{departman.Name} departmanının yöneticisi zaten seçilen kişi. Değişiklik yapılmadı.";
            return RedirectToAction(nameof(Departmanlar));
        }

        // 5) Aday kontrolleri — açılır listeye güvenilmez, hepsi sunucuda yeniden kontrol edilir.
        var aday = await _db.Employees
            .AsNoTracking()
            .Where(e => e.Id == yoneticiId)
            .Select(e => new { e.FirstName, e.LastName, e.IsActive, e.RoleId, e.DepartmentId })
            .FirstOrDefaultAsync();

        if (aday is null)
        {
            ModelState.AddModelError(nameof(model.YoneticiId), "Seçilen personel bulunamadı.");
        }
        else
        {
            if (aday.RoleId == Role.AdminId)
            {
                ModelState.AddModelError(nameof(model.YoneticiId),
                    "Admin hesapları departman yöneticisi olarak atanamaz.");
            }
            else if (aday.RoleId != Role.ManagerId)
            {
                ModelState.AddModelError(nameof(model.YoneticiId),
                    "Sadece Yönetici rolündeki personel atanabilir. Bu kişiyi atamak için önce Personel Düzenleme ekranından rolünü Yönetici yapın.");
            }

            if (!aday.IsActive)
            {
                ModelState.AddModelError(nameof(model.YoneticiId), "Pasif personel yönetici olarak atanamaz.");
            }

            if (aday.DepartmentId != departman.Id)
            {
                ModelState.AddModelError(nameof(model.YoneticiId),
                    $"Seçilen personel {departman.Name} departmanında çalışmıyor. Sadece bu departmanın personeli yönetici olabilir.");
            }

            var yonettigiDiger = await _db.Departments
                .AsNoTracking()
                .Where(d => d.ManagerId == yoneticiId && d.Id != departman.Id)
                .OrderBy(d => d.Name)
                .Select(d => d.Name)
                .ToListAsync();

            if (yonettigiDiger.Count > 0)
            {
                ModelState.AddModelError(nameof(model.YoneticiId),
                    $"Seçilen personel zaten {YonetilenDepartmanMetni(yonettigiDiger)} yöneticisi. " +
                    "Bir personel aynı anda yalnızca bir departmanı yönetebilir.");
            }
        }

        if (!ModelState.IsValid)
        {
            return await YoneticiFormunuGosterAsync(id, model);
        }

        // 6) Kayıt: sadece ManagerId değişir. Eski yöneticinin rolüne dokunulmaz.
        var ilkAtama = departman.ManagerId is null;
        departman.ManagerId = yoneticiId;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Hata"] = $"{departman.Name} departmanı güncellenemedi: kayıt bu sırada değişmiş veya silinmiş olabilir.";
            return RedirectToAction(nameof(Departmanlar));
        }

        var adSoyad = $"{aday!.FirstName} {aday.LastName}";
        TempData["Basari"] = ilkAtama
            ? $"{adSoyad}, {departman.Name} departmanının yöneticisi olarak atandı."
            : $"{departman.Name} departmanının yöneticisi {adSoyad} olarak değiştirildi.";
        return RedirectToAction(nameof(Departmanlar));
    }

    // Yönetici atama formunu gösterir. Ekrandaki bilgiler her zaman veritabanından okunur;
    // formdan gelen seçim korunur. POST'ta sadece yetki kontrolü geçildikten sonra çağrılır.
    private async Task<IActionResult> YoneticiFormunuGosterAsync(int id, DepartmanYoneticiViewModel model,
        bool mevcutDegeriSec = false)
    {
        var departman = await _db.Departments
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new
            {
                d.Id,
                d.Name,
                d.ManagerId,
                YoneticiAdi = d.Manager == null ? null : d.Manager.FirstName + " " + d.Manager.LastName
            })
            .FirstOrDefaultAsync();

        if (departman is null)
        {
            return NotFound();
        }

        model.DepartmanId = departman.Id;
        model.DepartmanAdi = departman.Name;
        model.MevcutYoneticiId = departman.ManagerId;
        model.MevcutYoneticiAdi = departman.YoneticiAdi;

        if (mevcutDegeriSec)
        {
            model.YoneticiId = departman.ManagerId;
        }

        model.Adaylar = await _db.Employees
            .AsNoTracking()
            .Where(e => e.DepartmentId == id
                        && e.IsActive
                        && e.RoleId == Role.ManagerId
                        && !_db.Departments.Any(d => d.ManagerId == e.Id && d.Id != id))
            .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
            .Select(e => new SelectListItem { Value = e.Id.ToString(), Text = e.FirstName + " " + e.LastName })
            .ToListAsync();

        return View(nameof(DepartmanYoneticisi), model);
    }

    // ---------------- Personel Yönetimi ----------------

    [HttpGet]
    public async Task<IActionResult> Personeller()
    {
        var personeller = await _db.Employees
            .AsNoTracking()
            .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
            .Select(e => new PersonelListeViewModel
            {
                Id = e.Id,
                Ad = e.FirstName,
                Soyad = e.LastName,
                Eposta = e.Email,
                DepartmanAdi = e.Department == null ? null : e.Department.Name,
                RolId = e.RoleId,
                RolAdi = e.Role == null ? string.Empty : e.Role.Name,
                Aktif = e.IsActive
            })
            .ToListAsync();

        return View(personeller);
    }

    [HttpGet]
    public async Task<IActionResult> PersonelEkle()
    {
        var model = new PersonelEkleViewModel
        {
            Departmanlar = await DepartmanSecenekleriAsync(),
            Roller = await RolSecenekleriAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PersonelEkle(PersonelEkleViewModel model)
    {
        // 0) Yetki kontrolü — anti-forgery doğrulamasından (attribute) hemen sonra, her şeyden ÖNCE.
        // Yetkisiz istekte veritabanına gidilmez, form yeniden gösterilmez ve şifre hash'lenmez; sadece 403 döner.
        if (!AdminGirisiYapilmis())
        {
            return YetkisizIstek();
        }

        // 1) Girdileri temizle ve yeniden doğrula (veritabanı sorgusu yok).
        model.Ad = AdiDuzenle(model.Ad);
        model.Soyad = AdiDuzenle(model.Soyad);
        model.Eposta = (model.Eposta ?? string.Empty).Trim().ToLowerInvariant();
        ModelState.Clear();
        TryValidateModel(model);

        // Rol sadece Çalışan veya Yönetici olabilir; formdan Admin ya da başka bir değer gelirse reddedilir.
        if (model.RolId is int rolId && !AtanabilirRoller.Contains(rolId))
        {
            ModelState.AddModelError(nameof(model.RolId), "Geçersiz rol seçimi.");
        }

        if (model.DepartmanId is <= 0)
        {
            ModelState.AddModelError(nameof(model.DepartmanId), "Geçersiz departman seçimi.");
        }

        if (!ModelState.IsValid)
        {
            return await PersonelFormunuGeriDonAsync(model);
        }

        // 2) Veritabanına bağlı kontroller (yetki 0. adımda kontrol edildi;
        // böylece yetkisiz biri e-posta çakışma mesajı üzerinden kayıtlı adresleri öğrenemez).
        if (model.DepartmanId is int departmanId &&
            !await _db.Departments.AnyAsync(d => d.Id == departmanId))
        {
            ModelState.AddModelError(nameof(model.DepartmanId), "Seçilen departman bulunamadı.");
        }

        if (await _db.Employees.AnyAsync(e => e.Email == model.Eposta))
        {
            ModelState.AddModelError(nameof(model.Eposta), EpostaCakismaMesaji);
        }

        if (!ModelState.IsValid)
        {
            return await PersonelFormunuGeriDonAsync(model);
        }

        // 3) Kayıt. Sadece formdaki izin verilen alanlar aktarılır.
        var personel = new Employee
        {
            FirstName = model.Ad,
            LastName = model.Soyad,
            Email = model.Eposta,
            DepartmentId = model.DepartmanId,
            RoleId = model.RolId!.Value,
            IsActive = true
        };

        // ENTEGRASYON NOTU: Şifre IPasswordHasher<Employee> (ASP.NET Core Identity PBKDF2) ile hash'lenir.
        // Giriş sistemi de şifreyi aynı servisle, VerifyHashedPassword ile doğrulamalıdır;
        // aksi halde buradan eklenen personel giriş yapamaz.
        personel.PasswordHash = _hasher.HashPassword(personel, model.Sifre);

        _db.Employees.Add(personel);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EpostaBenzersizlikIhlaliMi(ex))
        {
            // Kontrolden sonra, aynı anda başka bir istek aynı e-postayı kaydettiyse.
            // Diğer veritabanı hataları burada yakalanmaz, olduğu gibi yukarı iletilir.
            _db.Entry(personel).State = EntityState.Detached;
            ModelState.AddModelError(nameof(model.Eposta), EpostaCakismaMesaji);
            return await PersonelFormunuGeriDonAsync(model);
        }

        TempData["Basari"] = $"{personel.FirstName} {personel.LastName} personeli eklendi.";
        return RedirectToAction(nameof(Personeller));
    }

    // ---------------- Personel Düzenleme ----------------
    // Sadece departman, rol (Çalışan/Yönetici) ve aktiflik değiştirilebilir.

    [HttpGet]
    public async Task<IActionResult> PersonelDuzenle([FromRoute] int id)
    {
        var model = new PersonelDuzenleViewModel();
        return await DuzenlemeFormunuGosterAsync(id, model, mevcutDegerleriYukle: true);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // id sadece adresten alınır ([FromRoute]); formdaki "Id" alanı adresteki id'yi ezemez.
    public async Task<IActionResult> PersonelDuzenle([FromRoute] int id, PersonelDuzenleViewModel model)
    {
        // 1) Yetki kontrolü — anti-forgery doğrulamasından (attribute) hemen sonra, her şeyden ÖNCE.
        // Yetkisiz istekte veritabanına hiç gidilmez ve form yeniden gösterilmez; sadece 403 döner.
        if (!AdminGirisiYapilmis())
        {
            return YetkisizIstek();
        }

        // 2) Veritabanına gitmeyen kontroller.
        if (model.RolId is int rolId && !AtanabilirRoller.Contains(rolId))
        {
            ModelState.AddModelError(nameof(model.RolId), "Geçersiz rol seçimi.");
        }

        if (model.DepartmanId is <= 0)
        {
            ModelState.AddModelError(nameof(model.DepartmanId), "Geçersiz departman seçimi.");
        }

        if (!ModelState.IsValid)
        {
            return await DuzenlemeFormunuGosterAsync(id, model);
        }

        // 3) Veritabanına bağlı kontroller.
        var personel = await _db.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (personel is null)
        {
            return NotFound();
        }

        // Admin hesapları bu ekrandan düzenlenemez (form elle değiştirilip gönderilse bile).
        if (personel.RoleId == Role.AdminId)
        {
            TempData["Hata"] = AdminDuzenlenemezMesaji;
            return RedirectToAction(nameof(Personeller));
        }

        if (model.DepartmanId is int departmanId &&
            !await _db.Departments.AnyAsync(d => d.Id == departmanId))
        {
            ModelState.AddModelError(nameof(model.DepartmanId), "Seçilen departman bulunamadı.");
        }

        // Departman yöneticisi kuralları: yerine başka yönetici atanmadan rolü, aktifliği ve departmanı değişemez.
        var yonettigi = await YonettigiDepartmanlarAsync(id);
        if (yonettigi.Count > 0)
        {
            var departmanlar = YonetilenDepartmanMetni(yonettigi);

            if (model.RolId != Role.ManagerId)
            {
                ModelState.AddModelError(nameof(model.RolId),
                    $"Bu personel {departmanlar} yöneticisi olduğu için rolü Çalışan yapılamaz. " +
                    "Önce departmana başka bir yönetici atanmalıdır.");
            }

            if (!model.Aktif)
            {
                ModelState.AddModelError(nameof(model.Aktif),
                    $"Bu personel {departmanlar} yöneticisi olduğu için pasif yapılamaz. " +
                    "Önce departmana başka bir yönetici atanmalıdır.");
            }

            if (model.DepartmanId != personel.DepartmentId)
            {
                ModelState.AddModelError(nameof(model.DepartmanId),
                    $"Bu personel {departmanlar} yöneticisi olduğu için departmanı değiştirilemez. " +
                    "Önce departmana başka bir yönetici atanmalıdır.");
            }
        }

        if (!ModelState.IsValid)
        {
            return await DuzenlemeFormunuGosterAsync(id, model);
        }

        var adSoyad = $"{personel.FirstName} {personel.LastName}";

        // 4) Değişiklik yoksa veritabanına yazma.
        if (personel.DepartmentId == model.DepartmanId &&
            personel.RoleId == model.RolId &&
            personel.IsActive == model.Aktif)
        {
            TempData["Bilgi"] = $"{adSoyad} için değişiklik yapılmadı.";
            return RedirectToAction(nameof(Personeller));
        }

        // 5) Sadece izin verilen üç alan güncellenir; ad, soyad, e-posta ve şifreye dokunulmaz.
        personel.DepartmentId = model.DepartmanId;
        personel.RoleId = model.RolId!.Value;

        // ENTEGRASYON NOTU: IsActive = false olan personel giriş yapamamalıdır. Giriş sistemi hem girişte
        // hem de mevcut oturumlarda (cookie doğrulanırken) IsActive alanını kontrol etmelidir.
        personel.IsActive = model.Aktif;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Kayıt, okunduğu ile kaydedildiği an arasında silindiyse.
            TempData["Hata"] = $"{adSoyad} güncellenemedi: kayıt bu sırada değişmiş veya silinmiş olabilir.";
            return RedirectToAction(nameof(Personeller));
        }

        TempData["Basari"] = $"{adSoyad} güncellendi.";
        return RedirectToAction(nameof(Personeller));
    }

    // Düzenleme formunu gösterir. Ekrandaki bilgiler (ad, e-posta, yönettiği departmanlar) her zaman
    // veritabanından okunur; formdan gelen değerler (departman, rol, aktiflik) korunur.
    // POST'ta sadece yetki kontrolü geçildikten sonra çağrılır.
    private async Task<IActionResult> DuzenlemeFormunuGosterAsync(int id, PersonelDuzenleViewModel model,
        bool mevcutDegerleriYukle = false)
    {
        var personel = await _db.Employees
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new { e.Id, e.FirstName, e.LastName, e.Email, e.DepartmentId, e.RoleId, e.IsActive })
            .FirstOrDefaultAsync();

        if (personel is null)
        {
            return NotFound();
        }

        if (personel.RoleId == Role.AdminId)
        {
            TempData["Hata"] = AdminDuzenlenemezMesaji;
            return RedirectToAction(nameof(Personeller));
        }

        if (mevcutDegerleriYukle)
        {
            model.DepartmanId = personel.DepartmentId;
            model.RolId = personel.RoleId;
            model.Aktif = personel.IsActive;
        }

        model.Id = personel.Id;
        model.Ad = personel.FirstName;
        model.Soyad = personel.LastName;
        model.Eposta = personel.Email;
        model.YonettigiDepartmanlar = await YonettigiDepartmanlarAsync(id);
        model.Departmanlar = await DepartmanSecenekleriAsync();
        model.Roller = await RolSecenekleriAsync();

        return View(nameof(PersonelDuzenle), model);
    }

    private Task<List<string>> YonettigiDepartmanlarAsync(int personelId)
    {
        return _db.Departments
            .AsNoTracking()
            .Where(d => d.ManagerId == personelId)
            .OrderBy(d => d.Name)
            .Select(d => d.Name)
            .ToListAsync();
    }

    // "Muhasebe departmanının" / "Muhasebe, Satış departmanlarının"
    private static string YonetilenDepartmanMetni(List<string> departmanlar)
    {
        return departmanlar.Count == 1
            ? $"{departmanlar[0]} departmanının"
            : $"{string.Join(", ", departmanlar)} departmanlarının";
    }

    // ---------------- Yardımcılar ----------------

    private const string EpostaCakismaMesaji = "Bu e-posta adresiyle kayıtlı bir personel zaten var.";
    private const string AdminDuzenlenemezMesaji = "Admin hesapları bu ekrandan düzenlenemez.";

    // Sadece oturum açmış ve rolü Admin olan kullanıcı için true döner.
    private bool AdminGirisiYapilmis()
    {
        return User.Identity?.IsAuthenticated == true && User.IsInRole("Admin");
    }

    // Yetkisiz POST için 403. Veritabanına gitmez, form göstermez.
    // Not: Forbid() kullanılmadı; cookie şemasında AccessDeniedPath ayarlı olmadığı için 404 sayfasına yönlendirirdi.
    private ContentResult YetkisizIstek()
    {
        return new ContentResult
        {
            StatusCode = StatusCodes.Status403Forbidden,
            ContentType = "text/plain; charset=utf-8",
            Content = "Bu işlem için Admin yetkisi gerekiyor. Değişiklik yapılmadı."
        };
    }

    // Boşlukları düzenler ve Unicode NFC'ye çevirir (macOS'ten "ş" gibi harfler iki ayrı karakter olarak gelebilir).
    private static string AdiDuzenle(string? ad)
    {
        return Regex.Replace(ad ?? string.Empty, @"\s+", " ").Trim().Normalize(NormalizationForm.FormC);
    }

    // Sadece Employees.Email benzersiz index'inin ihlalini tanır (SQL Server hata 2601/2627).
    private static bool EpostaBenzersizlikIhlaliMi(DbUpdateException ex)
    {
        return ex.InnerException is SqlException { Number: 2601 or 2627 } sql
            && sql.Message.Contains("IX_Employees_Email", StringComparison.OrdinalIgnoreCase);
    }

    // Hata durumunda formu geri gösterir: şifre alanları boşaltılır, açılır listeler yeniden doldurulur.
    private async Task<IActionResult> PersonelFormunuGeriDonAsync(PersonelEkleViewModel model)
    {
        model.Sifre = string.Empty;
        model.SifreTekrar = string.Empty;
        model.Departmanlar = await DepartmanSecenekleriAsync();
        model.Roller = await RolSecenekleriAsync();
        return View(nameof(PersonelEkle), model);
    }

    // Açılır listeler (personel ekleme ve düzenleme formlarında ortak).
    private Task<List<SelectListItem>> DepartmanSecenekleriAsync()
    {
        return _db.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
            .ToListAsync();
    }

    private Task<List<SelectListItem>> RolSecenekleriAsync()
    {
        return _db.Roles
            .AsNoTracking()
            .Where(r => AtanabilirRoller.Contains(r.Id))
            .OrderBy(r => r.Id)
            .Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Name })
            .ToListAsync();
    }
}
