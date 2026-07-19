using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ToyStore.UI.Pages // Kendi namespace yapına göre kalabilir
{
    public class LoginModel : PageModel
    {
        // EĞER BURADA "public void OnGet()" VARSA ONU TAMAMEN SİLİN!

        // Sadece bu tek metot kalmalı:
        public IActionResult OnGet()
        {
            // Sunucu bazlı cookie/kimlik doğrulaması varsa kontrol et ve Dashboard'a salla
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Dashboard");
            }

            return Page();
        }

        // Mevcut OnPost metotlarınız aşağıda aynen kalabilir...
    }
}