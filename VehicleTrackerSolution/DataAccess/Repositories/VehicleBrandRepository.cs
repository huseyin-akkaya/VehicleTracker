
using Microsoft.EntityFrameworkCore;
using VehicleTracker.DataAccess.Context;
using VehicleTracker.Entities;

namespace VehicleTracker.DataAccess.Repositories;

public class VehicleBrandRepository : GenericRepository<VehicleBrand>
{
	public VehicleBrandRepository(AppDbContext context) : base(context) { }

	public override async Task<IList<VehicleBrand>> GetAllAsync()
	{
		return await _dbSet
			.AsNoTracking()
			.OrderBy(b => b.Name)
			.ToListAsync();
	}

	public async Task<VehicleBrand?> GetByRemoteIdAsync(string remoteId)
	{
		return await _dbSet
			.FirstOrDefaultAsync(b => b.RemoteId == remoteId);
	}
}