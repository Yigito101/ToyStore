using FluentValidation;
using ToyStore.Api.DTOs;

namespace ToyStore.API.Validations
{
    /// <summary>
    /// CATEGORY CREATION REQUEST VALIDATOR
    /// </summary>
    public class CreateCategoryValidator : AbstractValidator<CategoryCreateDTO>
    {
        public CreateCategoryValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Kategori adı boş bırakılamaz.")
                .MinimumLength(3).WithMessage("Kategori adı en az 3 karakter olmalıdır.")
                .MaximumLength(100).WithMessage("Kategori adı en fazla 100 karakter olabilir.") // DbContext şeması ile tam uyum

                // MİMARİ ENJEKSİYON KALKANI: '\s' yerine ' ' (gerçek boşluk) kullanılarak 
                // HTTP Header veya SQL loglarına sızabilecek satır atlama (Line-break/\n) ve Tab (\t) manipülasyonları engellenmiştir.
                .Matches(@"^[a-zA-ZğüşıöçĞÜŞİÖÇ0-9 \-]+$").WithMessage("Kategori adı sadece harf, rakam, tekil boşluk ve tire (-) içerebilir.");
        }
    }

    /// <summary>
    /// CATEGORY UPDATE REQUEST VALIDATOR
    /// </summary>
    public class UpdateCategoryValidator : AbstractValidator<CategoryUpdateDTO>
    {
        public UpdateCategoryValidator()
        {
            // URL-Body entegrasyon kontrolü öncesi ID formatının pozitif tam sayı olduğunu doğrular.
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Geçerli bir Kategori ID'si girilmelidir.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Kategori adı boş bırakılamaz.")
                .MinimumLength(3).WithMessage("Kategori adı en az 3 karakter olmalıdır.")
                .MaximumLength(100).WithMessage("Kategori adı en fazla 100 karakter olabilir.")
                .Matches(@"^[a-zA-ZğüşıöçĞÜŞİÖÇ0-9\s\-]+$").WithMessage("Kategori adı sadece harf, rakam, boşluk ve tire (-) içerebilir.");
        }
    }
}