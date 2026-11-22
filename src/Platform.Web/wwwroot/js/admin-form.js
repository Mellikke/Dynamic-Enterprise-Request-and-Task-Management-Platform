// Admin formları için anlık doğrulama ve çift gönderim koruması.
// Sadece kullanıcıya hızlı geri bildirim içindir; asıl doğrulama her zaman sunucuda yapılır.
//
// Kullanım: <form data-admin-form> ve doğrulanacak alanlarda data-dogrula.
// Mesajlar alanın data-msg-* özelliklerinden okunur ve data-valmsg-for="<alan adı>" olan <span>'a yazılır.
//   data-msg-zorunlu, data-msg-uzunluk, data-msg-desen, data-msg-eposta, data-msg-eslesme
//   data-duzenle="bosluk"  -> alandan çıkınca baştaki/sondaki boşluklar silinir, aradakiler teke iner
//   data-duzenle="kirp"    -> alandan çıkınca sadece baştaki/sondaki boşluklar silinir
//   data-eslesme="<id>"    -> değer, bu id'deki alanla aynı olmalı (şifre tekrarı)
(function () {
  "use strict";

  document.querySelectorAll("form[data-admin-form]").forEach(formuKur);

  function formuKur(form) {
    // JavaScript çalışıyorsa tarayıcının kendi baloncukları yerine bizim mesajlarımız gösterilir.
    // JavaScript kapalıysa tarayıcının kendi doğrulaması ve sunucu doğrulaması çalışmaya devam eder.
    form.noValidate = true;

    const alanlar = Array.from(form.querySelectorAll("[data-dogrula]"));
    const buton = form.querySelector("button[type=submit]");
    const butonIcerigi = buton ? buton.innerHTML : "";

    alanlar.forEach(function (alan) {
      alan.addEventListener("blur", function () {
        duzenle(alan);
        alan.dataset.dokunuldu = "1";
        kontrolEt(form, alan);
      });

      alan.addEventListener("input", function () {
        if (alan.dataset.dokunuldu) kontrolEt(form, alan);
        // Şifre değişince, ona bağlı "şifre tekrarı" alanı da yeniden kontrol edilir.
        alanlar
          .filter(function (a) { return a.dataset.eslesme === alan.id && a.dataset.dokunuldu; })
          .forEach(function (a) { kontrolEt(form, a); });
      });

      alan.addEventListener("change", function () {
        alan.dataset.dokunuldu = "1";
        kontrolEt(form, alan);
      });
    });

    form.addEventListener("submit", function (e) {
      // Çift tıklama: form zaten gönderiliyorsa ikinci gönderimi durdur.
      if (form.dataset.gonderiliyor) {
        e.preventDefault();
        return;
      }

      let ilkHatali = null;
      alanlar.forEach(function (alan) {
        duzenle(alan);
        alan.dataset.dokunuldu = "1";
        if (!kontrolEt(form, alan) && !ilkHatali) ilkHatali = alan;
      });

      if (ilkHatali) {
        // Hata varsa form gönderilmez, buton açık kalır; kullanıcı düzeltip tekrar gönderebilir.
        e.preventDefault();
        ilkHatali.focus();
        return;
      }

      form.dataset.gonderiliyor = "1";
      if (buton) {
        buton.disabled = true;
        buton.textContent = "Kaydediliyor...";
      }
    });

    // Tarayıcının "geri" tuşuyla sayfaya dönülürse buton tekrar kullanılabilir olsun.
    window.addEventListener("pageshow", function () {
      delete form.dataset.gonderiliyor;
      if (buton) {
        buton.disabled = false;
        buton.innerHTML = butonIcerigi;
      }
    });
  }

  function duzenle(alan) {
    if (alan.dataset.duzenle === "bosluk") {
      alan.value = alan.value.replace(/\s+/g, " ").trim();
    } else if (alan.dataset.duzenle === "kirp") {
      alan.value = alan.value.trim();
    }
  }

  // Tarayıcının type="email" kontrolü @ öncesindeki hatalı noktaları kabul eder; sunucu (GecerliEpostaAttribute)
  // reddeder. Aynı kural burada da uygulanır: en fazla 64 karakter, noktayla başlayıp bitemez, ".." olamaz.
  function yerelKisimGecerli(eposta) {
    const at = eposta.lastIndexOf("@");
    if (at < 0) return true; // Biçim hatası typeMismatch ile zaten yakalanır.
    const yerel = eposta.slice(0, at);
    return yerel.length > 0 && yerel.length <= 64 &&
      !yerel.startsWith(".") && !yerel.endsWith(".") && !yerel.includes("..");
  }

  // Alanı kontrol eder, mesajı yazar; geçerliyse true döner.
  function kontrolEt(form, alan) {
    const deger = alan.value;
    let mesaj = "";

    if (alan.required && deger.trim() === "") {
      mesaj = alan.dataset.msgZorunlu || "";
    } else if (deger !== "" && alan.minLength > 0 && deger.length < alan.minLength) {
      mesaj = alan.dataset.msgUzunluk || "";
    } else if (deger !== "" && alan.maxLength > 0 && deger.length > alan.maxLength) {
      mesaj = alan.dataset.msgUzunluk || "";
    } else if (deger !== "" && alan.validity.patternMismatch) {
      mesaj = alan.dataset.msgDesen || "";
    } else if (deger !== "" && (alan.validity.typeMismatch || (alan.type === "email" && !yerelKisimGecerli(deger)))) {
      mesaj = alan.dataset.msgEposta || "";
    } else if (deger !== "" && alan.dataset.eslesme) {
      const diger = document.getElementById(alan.dataset.eslesme);
      if (diger && diger.value !== deger) mesaj = alan.dataset.msgEslesme || "";
    }

    const yer = form.querySelector('[data-valmsg-for="' + alan.name + '"]');
    if (yer) yer.textContent = mesaj;
    alan.classList.toggle("input-validation-error", mesaj !== "");
    alan.setAttribute("aria-invalid", mesaj !== "" ? "true" : "false");

    return mesaj === "";
  }
})();
