using ToyStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ToyStore.Api.Data
{
    /// <summary>
    /// DATA ACCESS LAYER (ORM MERKEZİ)
    /// MİMARİ AÇIKLAMA: Entity Framework Core altyapısını kullanarak veritabanı ile uygulama katmanları 
    /// arasında köprü görevi gören, Unit of Work ve Repository desenlerinin tabanını oluşturan veri bağlamıdır.
    /// </summary>
    public class ToyStoreDbContext : DbContext
    {
        public ToyStoreDbContext(DbContextOptions<ToyStoreDbContext> options) : base(options)
        {
        }

        // --- ANA ENTİTE KÜMELERİ ---
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<User> Users { get; set; }

        // --- İLİŞKİSEL KÖPRÜ (MANY-TO-MANY) KÜMELERİ ---
        public DbSet<UserFavoriteProduct> UserFavoriteProducts { get; set; }
        public DbSet<UserFavoriteCategory> UserFavoriteCategories { get; set; }

        /// <summary>
        /// VERİTABANI MODEL ŞEKİLLENDİRME VE KISITLAMA ALANINA GİRİŞ (FLUENT API)
        /// MİMARİ STRATEJİ: Veri bütünlüğü kuralları ve indeks optimizasyonları Attribute'lar yerine 
        /// merkezi yönetim esnekliği sağlayan Fluent API aracılığıyla bu metotta yapılandırılır.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ==========================================
            // 1. İLİŞKİ KISITLAMALARI VE REFERANS BÜTÜNLÜĞÜ (REFERENTIAL INTEGRITY)
            // ==========================================

            // MİMARİ KALKAN: Kategori silindiğinde altındaki ürünlerin otomatik silinmesini (Cascade) engeller.
            // İş mantığı kuralları gereği ilişkili ürünü olan kategori silinmeye çalışırsa veritabanı düzeyinde operasyon bloklanır.
            modelBuilder.Entity<Category>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==========================================
            // 2. KATEGORİ TABLOSU DOĞRULAMALARI VE EŞ ZAMANLILIK KONTROLÜ
            // ==========================================
            modelBuilder.Entity<Category>()
                .Property(c => c.Name)
                .HasMaxLength(100)
                .IsRequired();

            // CONCURRENCY TOKEN (Eş Zamanlılık Yönetimi): 
            // Optimistic Concurrency mantığıyla çalışır. Aynı anda iki admin aynı kategoriyi güncellerken
            // veri üzerine yazma (Data Overwriting / Lost Update) çakışmasını engeller ve DbUpdateConcurrencyException fırlatır.
            modelBuilder.Entity<Category>()
                .Property(c => c.RowVersion)
                .IsRowVersion();

            modelBuilder.Entity<Product>()
                .Property(p => p.RowVersion)
                .IsRowVersion();

            // FİZİKSEL BENZERSİZLİK KİLİDİ (FILTERED UNIQUE INDEX):
            // Kategori isimlerinin benzersiz olmasını sağlar. Ancak Soft Delete (IsActive = 0) mantığıyla 
            // silinmiş eski kategorilerin isimleriyle çakışmaması için sadece aktif kayıtlarda benzersizlik arar.
            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique()
                .HasFilter("[IsActive] = 1");

            // ==========================================
            // 3. ÜRÜN (OYUNCAK) TABLOSU VE VERİTABANI KONTROL KISITLAMALARI
            // ==========================================
            modelBuilder.Entity<Product>()
                .Property(p => p.Name)
                .HasMaxLength(200)
                .IsRequired();

            // Hassas parasal veriler için kayan noktalı tipler (float/double) yerine kesinlik sağlayan decimal tipi seçilmiştir.
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            // SQL CHECK CONSTRAINT: Uygulama katmanındaki sızıntılara karşı son savunma hattı olarak
            // fiyatın 0'dan büyük, stoğun ise 0 veya pozitif olması veritabanı motoru düzeyinde de kilitlenmiştir.
            modelBuilder.Entity<Product>()
                .ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Product_Price", "[Price] > 0");
                    t.HasCheckConstraint("CK_Product_Stock", "[Stock] >= 0");
                });

            // ==========================================
            // 4. KULLANICI (USER) GÜVENLİK KISITLAMALARI
            // ==========================================

            // Aynı e-posta adresiyle mükerrer kayıt açılmasını mimari düzeyde engeller.
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(150);

            modelBuilder.Entity<User>()
                .Property(u => u.PasswordHash)
                .IsRequired();

            // ==========================================
            // 5. PERFORMANS OPTİMİZASYONU: FİLTRELENMİŞ İNDEKSLER (FILTERED INDEXES)
            // ==========================================
            // Arayüz listelemelerinde sürekli kullanılan 'IsActive = 1' (Aktif Kayıtlar) sorguları için 
            // SQL Server üzerinde önbelleğe alınmış kısmi indeks haritası oluşturur. Full Table Scan maliyetini önler.
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.IsActive)
                .HasFilter("[IsActive] = 1");

            modelBuilder.Entity<Category>()
                .HasIndex(c => c.IsActive)
                .HasFilter("[IsActive] = 1");

            base.OnModelCreating(modelBuilder);

            // ==========================================
            // 6. FAVORİ SİSTEMİ İLİŞKİLERİ VE CASCADE KURALLARI (MANY-TO-MANY COUPLING)
            // ==========================================

            // User - Favorite - Product Yapılandırması
            modelBuilder.Entity<UserFavoriteProduct>()
                .HasKey(ufp => new { ufp.UserId, ufp.ProductId }); // Bileşik Anahtar (Composite Key) tasarımı

            modelBuilder.Entity<UserFavoriteProduct>()
                .HasOne(ufp => ufp.User)
                .WithMany(u => u.FavoriteProducts)
                .HasForeignKey(ufp => ufp.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Bir kullanıcı silinirse onun ilişkili favori haritası da otomatik temizlenir.

            modelBuilder.Entity<UserFavoriteProduct>()
                .HasOne(ufp => ufp.Product)
                .WithMany(p => p.FavoritedByUsers)
                .HasForeignKey(ufp => ufp.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // Bir ürün sistemden tamamen silinirse kullanıcıların favorilerinden de düşer.

            // User - Favorite - Category Yapılandırması
            modelBuilder.Entity<UserFavoriteCategory>()
                .HasKey(ufc => new { ufc.UserId, ufc.CategoryId });

            modelBuilder.Entity<UserFavoriteCategory>()
                .HasOne(ufc => ufc.User)
                .WithMany(u => u.FavoriteCategories)
                .HasForeignKey(ufc => ufc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserFavoriteCategory>()
                .HasOne(ufc => ufc.Category)
                .WithMany(c => c.FavoritedByUsers)
                .HasForeignKey(ufc => ufc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}