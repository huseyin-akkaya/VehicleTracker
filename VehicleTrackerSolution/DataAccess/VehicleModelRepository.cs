using Microsoft.EntityFrameworkCore;
using VehicleTracker.DataAccess.Context;
using VehicleTracker.Entities;

namespace VehicleTracker.DataAccess.Repositories;

/// <summary>
/// Araç modelleri (VehicleModel) tablosu için özelleştirilmiş veritabanı işlemlerini yürüten repository sınıfı.
/// Temel CRUD işlemleri için GenericRepository'den miras alır.
/// </summary>
public class VehicleModelRepository : GenericRepository<VehicleModel>
{
	public VehicleModelRepository(AppDbContext context) : base(context) { }

	/// <summary>
	/// Belirtilen marka kimliğine (BrandId) ait tüm araç modellerini alfabetik sırayla listeler.
	/// Yalnızca okuma işlemi yapıldığı için Entity Framework'ün takip mekanizması devre dışı bırakılarak (AsNoTracking) performans optimizasyonu sağlanmıştır.
	/// </summary>
	/// <param name="brandId">Filtrelenecek markanın benzersiz veritabanı kimliği.</param>
	/// <returns>Markaya ait araç modellerini içeren asenkron liste.</returns>
	public async Task<IList<VehicleModel>> GetByBrandIdAsync(int brandId)
	{
		return await _dbSet
			.AsNoTracking()
			.Where(m => m.BrandId == brandId)
			.OrderBy(m => m.Name)
			.ToListAsync();
	}

	/// <summary>
	/// Dış kaynaktan (örneğin uzak JSON dosyasından) gelen model verilerindeki benzersiz kimlik (RemoteId) kullanılarak yerel veritabanında arama yapar.
	/// Çevrimiçi senkronizasyon sırasında mükerrer (duplicate) kayıt oluşumunu engellemek amacıyla eşleşme kontrolü için kullanılır.
	/// </summary>
	/// <param name="remoteId">Dış veri kaynağındaki benzersiz model kimliği.</param>
	/// <returns>Eşleşen araç modeli kaydı; eğer kayıt bulunamazsa null döner.</returns>
	public async Task<VehicleModel?> GetByRemoteIdAsync(string remoteId)
	{
		return await _dbSet
			.FirstOrDefaultAsync(m => m.RemoteId == remoteId);
	}
}