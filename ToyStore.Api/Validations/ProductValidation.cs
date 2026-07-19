using FluentValidation;
using ToyStore.Api.DTOs;

namespace ToyStore.API.Validations
{
    /// <summary>
    /// PRODUCT CREATION REQUEST VALIDATOR
    /// </summary>
    public class CreateProductValidator : AbstractValidator<ProductCreateDTO>
    {
        public CreateProductValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Ürün adı boş bırakılamaz.")
                .MinimumLength(3).WithMessage("Ürün adı en az 3 karakter olmalıdır.")
                .MaximumLength(200).WithMessage("Ürün adı en fazla 200 karakter olabilir.")
                .Matches(@"^[a-zA-ZğüşıöçĞÜŞİÖÇ0-9 \-]+$").WithMessage("Ürün adı sadece harf, rakam, tekil boşluk ve tire (-) içerebilir.");

            // =========================================================================
            // APPSCEN KALKANI: DATA TYPE OVERFLOW PROTECTION (VERİ TAŞMASI ENGELLİ)
            // MİMARİ STRATEJİ: Saldırganın girdi alanlarına milyarlarca liralık veya adetlik 
            // astronomik sayılar yazarak veritabanı decimal/integer sınırlarını taşırmasını (Overflow Exception) 
            // ve sunucu boru hattını kilitlemesini mimari düzeyde kesin olarak engeller.
            // =========================================================================
            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Ürün fiyatı 0'dan büyük olmalıdır.")
                .LessThan(1000000).WithMessage("Ürün fiyatı 1 Milyon TL'den fazla olamaz.");

            RuleFor(x => x.Stock)
                .GreaterThanOrEqualTo(0).WithMessage("Stok adedi negatif olamaz.") // İş kuralı güvencesi
                .LessThan(100000).WithMessage("Stok adedi 100.000'den fazla olamaz.");

            // İlişkisel Bütünlük Ön Doğrulaması (Foreign Key Pre-Validation)
            RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("Geçerli bir Kategori ID'si girilmelidir.");
        }
    }

    /// <summary>
    /// PRODUCT UPDATE REQUEST VALIDATOR
    /// </summary>
    public class UpdateProductValidator : AbstractValidator<ProductUpdateDTO>
    {
        public UpdateProductValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Geçerli bir Ürün ID'si girilmelidir.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Ürün adı boş bırakılamaz.")
                .MinimumLength(3).WithMessage("Ürün adı en az 3 karakter olmalıdır.")
                .MaximumLength(200).WithMessage("Ürün adı en fazla 200 karakter olabilir.")
                .Matches(@"^[a-zA-ZğüşıöçĞÜŞİÖÇ0-9\s\-]+$").WithMessage("Ürün adı sadece harf, rakam, boşluk ve tire (-) içerebilir.");

            RuleFor(x => x.Price).GreaterThan(0).WithMessage("Ürün fiyatı 0'dan büyük olmalıdır.");
            RuleFor(x => x.Stock).GreaterThanOrEqualTo(0).WithMessage("Stok adedi negatif olamaz.");
            RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("Geçerli bir Kategori ID'si girilmelidir.");
        }
    }
}