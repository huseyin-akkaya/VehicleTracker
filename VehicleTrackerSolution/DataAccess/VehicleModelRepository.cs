// --- DataAccess/Repositories/VehicleModelRepository.cs ---

using Microsoft.EntityFrameworkCore;
using VehicleTracker.DataAccess.Context;
using VehicleTracker.Entities;

namespace VehicleTracker.DataAccess.Repositories;

public class VehicleModelRepository : GenericRepository<VehicleModel>
{
	public VehicleModelRepository(AppDbContext context) : base(context) { }

	/// <summary>Belirli bir markaya ait tüm modelleri döner.</summary>
	public async Task<IList<VehicleModel>> GetByBrandIdAsync(int brandId)
	{
		return await _dbSet
			.AsNoTracking()
			.Where(m => m.BrandId == brandId)
			.OrderBy(m => m.Name)
			.ToListAsync();
	}

	public async Task<VehicleModel?> GetByRemoteIdAsync(string remoteId)
	{
		return await _dbSet
			.FirstOrDefaultAsync(m => m.RemoteId == remoteId);
	}
}