using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace Platform.Web.Models.Validation;

/// <summary>
/// [EmailAddress]'e ek olarak e-postanın yapısını kontrol eder:
/// isim içeren biçimleri ("Ali &lt;a@b.com&gt;"), boşlukları, hatalı noktaları ve hatalı alan adı parçalarını reddeder.
/// Alan adında nokta ZORUNLU DEĞİLDİR (örn. "ali@localhost" geçerlidir).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class GecerliEpostaAttribute : ValidationAttribute
{
    private static readonly EmailAddressAttribute TemelKontrol = new();

    public GecerliEpostaAttribute() : base("Geçerli bir e-posta adresi girin.")
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // Boş değer [Required]'ın, temel biçim hatası [EmailAddress]'in işi; aynı hata iki kez gösterilmesin.
        if (value is not string eposta || string.IsNullOrWhiteSpace(eposta) || !TemelKontrol.IsValid(eposta))
        {
            return ValidationResult.Success;
        }

        return YapiGecerliMi(eposta)
            ? ValidationResult.Success
            : new ValidationResult(FormatErrorMessage(validationContext.DisplayName),
                validationContext.MemberName is null ? null : new[] { validationContext.MemberName });
    }

    private static bool YapiGecerliMi(string eposta)
    {
        if (eposta.Any(char.IsWhiteSpace) || eposta.Any(char.IsControl))
        {
            return false;
        }

        // Adres, girilen metnin kendisi olmalı ("Ali <a@b.com>" gibi isimli biçimler reddedilir).
        if (!MailAddress.TryCreate(eposta, out var adres) ||
            !string.IsNullOrEmpty(adres.DisplayName) ||
            !string.Equals(adres.Address, eposta, StringComparison.Ordinal))
        {
            return false;
        }

        var at = eposta.LastIndexOf('@');
        var yerel = eposta[..at];
        var alanAdi = eposta[(at + 1)..];

        // Yerel kısım: en fazla 64 karakter, noktayla başlayıp bitemez, art arda nokta içeremez.
        if (yerel.Length is 0 or > 64 || yerel.StartsWith('.') || yerel.EndsWith('.') || yerel.Contains(".."))
        {
            return false;
        }

        // Alan adı parçaları: boş olamaz, en fazla 63 karakter, harf/rakam/tire; tireyle başlayıp bitemez.
        foreach (var parca in alanAdi.Split('.'))
        {
            if (parca.Length is 0 or > 63 ||
                parca.StartsWith('-') || parca.EndsWith('-') ||
                !parca.All(c => char.IsLetterOrDigit(c) || c == '-'))
            {
                return false;
            }
        }

        return true;
    }
}
