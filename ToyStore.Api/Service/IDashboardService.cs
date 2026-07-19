namespace ToyStore.Api.Service
{
    /// <summary>
    /// ANALYTICS & DASHBOARD METRICS INTERFACE
    /// MİMARİ AÇIKLAMA[cite: 11]: Rol tabanlı (RBAC) grafik ve metrik verilerini 
    /// controller katmanına aktaran, salt okunur (read-only) analitik veri sözleşmesidir.
    /// </summary>
    public interface IDashboardService
    {
        object GetAdminStats();
        object GetUserStats(string userId);
    }
}
