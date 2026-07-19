using System;

namespace ToyStore.Api.Models
{
    /// <summary>
    /// SOYUT TEMEL ENTİTE (BASE ENTITY PATTERN)
    /// MİMARİ STRATEJİ: Tüm veritabanı tablolarında ortak olan audit (denetim) alanlarını 
    /// tek merkezde toplar (DRY Prensibi). Kalıtım (Inheritance) yoluyla kod tekrarını sıfırlar.
    /// </summary>
    public class BaseEntity
    {
        // Birincil anahtar (Primary Key) standardı.
        public int Id { get; set; }

        // Verinin ilk oluşturulma zaman damgası.
        public DateTime CreatedAt { get; set; }

        // PITFALL ENGELEYİCİ: '?' (Nullable) ifadesiyle ilk kayıt anında güncelleme 
        // zamanının boş kalabilmesi sağlanmış ve mantıksal çakışmalar engellenmiştir.
        public DateTime? UpdatedAt { get; set; }

        // SOFT DELETE FIZIKSEL ALT YAPISI[cite: 7]: Verilerin diskten kalıcı silinmesini engeller.
        // 1 (True) = Aktif/Canlı Kayıt , 0 (False) = Silinmiş/Arşivlenmiş Kayıt.
        public bool IsActive { get; set; }

        // Verinin soft-delete operasyonuna uğradığı anlık zaman damgası.
        public DateTime? DeletedAt { get; set; }
    }
}