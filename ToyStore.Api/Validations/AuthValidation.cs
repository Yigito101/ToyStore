using FluentValidation;
using ToyStore.Api.DTOs.Auth;

namespace ToyStore.API.Validations
{
    /// <summary>
    /// USER REGISTER REQUEST VALIDATOR
    /// MİMARİ AÇIKLAMA: Yeni kullanıcı kayıt isteklerini sisteme kabul etmeden önce 
    /// filtreleyen, veri tutarlılığı ve siber güvenlik odaklı ilk savunma kalkanıdır.
    /// </summary>
    public class UserRegisterValidator : AbstractValidator<UserRegisterDTO>
    {
        public UserRegisterValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-posta adresi boş bırakılamaz.")
                .MaximumLength(100).WithMessage("E-posta adresi çok uzun.") // VERİTABANI KORUMASI: Sütun taşmasını önler.
                .EmailAddress().WithMessage("Lütfen geçerli bir e-posta formatı giriniz.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Şifre boş bırakılamaz.")
                .MinimumLength(6).WithMessage("Şifre en az 6 karakter uzunluğunda olmalıdır.")

                // =========================================================================
                // APPSCEN KALKANI: BCRYPT HASHING DoS ATTACK PREVENTION
                // STRATEJİK DETAY: Kötü niyetli bir saldırgan şifre alanına 50.000 karakterlik astronomik 
                // bir metin gönderirse, sunucu BCrypt CPU döngülerinde boğulur ve kilitlenirdi (Denial of Service). 
                // 50 karakter üst sınırı ile bu siber güvenlik açığı API sınırında kapatılmıştır.
                // =========================================================================
                .MaximumLength(50).WithMessage("Şifre en fazla 50 karakter olabilir (Güvenlik sınırı).")

                .Matches("[A-Z]").WithMessage("Şifre en az bir büyük harf içermelidir.")
                .Matches("[a-z]").WithMessage("Şifre en az bir küçük harf içermelidir.") // Karmaşıklık denetimi
                .Matches("[0-9]").WithMessage("Şifre en az bir rakam içermelidir.")

                // EMOJI & SIZMA KALKANI: UTF-8 manipülasyonlarını engellemek adına şifreyi standart Latin ASCII aralığına kilitler.
                .Matches(@"^[\x20-\x7E]+$").WithMessage("Şifre sadece standart Latin karakterleri, rakam ve semboller içerebilir (Emoji kullanılamaz).");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Şifre onayı boş bırakılamaz.")
                .Equal(x => x.Password).WithMessage("Şifreler birbiriyle uyuşmuyor."); // İstemci-Sunucu senkronizasyonu
        }
    }

    /// <summary>
    /// USER LOGIN REQUEST VALIDATOR
    /// </summary>
    public class UserLoginValidator : AbstractValidator<UserLoginDTO>
    {
        public UserLoginValidator()
        {
            // Giriş denemelerinde sunucu işlem yükünü (DB Query) azaltmak için eksik verileri anında eler.
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-posta alanı zorunludur.")
                .EmailAddress().WithMessage("Geçerli bir e-posta formatı giriniz.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Şifre alanı zorunludur.");
        }
    }
}