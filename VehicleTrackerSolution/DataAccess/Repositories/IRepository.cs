// --- DataAccess/Repositories/IRepository.cs ---

namespace VehicleTracker.DataAccess.Repositories;

/// <summary>
/// Tüm entity'ler için generic CRUD operasyonlarını tanımlar.
/// SOLID → Dependency Inversion: üst katmanlar bu interface'e bağlanır,
/// somut implementasyona değil.
/// </summary>
public interface IRepository<T> where T : class
{
	// ── Okuma ──────────────────────────────────────────────────
	Task<T?> GetByIdAsync(int id);
	Task<IList<T>> GetAllAsync();

	// ── Yazma ──────────────────────────────────────────────────
	Task AddAsync(T entity);
	Task UpdateAsync(T entity);
	Task DeleteAsync(int id);

	// ── Kaydetme — Unit of Work yaklaşımı ──────────────────────
	/// <summary>
	/// Birden fazla işlemi tek transaction'da commit etmek için
	/// servis katmanından çağrılır.
	/// </summary>
	Task SaveChangesAsync();
}