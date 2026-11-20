using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Platform.Web.Models;

public class AdminController : Controller
{
   private readonly UserManager<Employee> _employee;


   public AdminController(UserManager<Employee> employee)
   {
       _employee = employee;
   }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> adminDashboard(string? Email, string? sifre)
    {
        if (!string.IsNullOrWhiteSpace(Email) && !string.IsNullOrEmpty(sifre))
        {
            var kullanici = await _employee.FindByEmailAsync(Email.Trim());

            if (kullanici is not null && await _employee.IsInRoleAsync(kullanici, "Admin"))
            {
                var sifreKontrol = await _employee.CheckPasswordAsync(kullanici, sifre);

                if (sifreKontrol)
                {
                    // Giriş başarılı, yönlendirme yapabilirsiniz.
                    return RedirectToAction("Index", "AdminDashboard");
                }
            }
        }

        ModelState.AddModelError(string.Empty, "Giriş yapılamadı. Bilgilerinizi kontrol edin.");
        return View("adminGiris");
    }
}