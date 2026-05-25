
using Microsoft.EntityFrameworkCore;
using VehicleTracker.DataAccess.Context;
using VehicleTracker.Entities;

namespace VehicleTracker.DataAccess.Repositories;

public class VehicleRepository : GenericRepository<Vehicle>
{
	public VehicleRepository(AppDbContext context) : base(context) { }

	public override async Task<Vehicle?> GetByIdAsync(int id)
	{
		return await _dbSet
			.Include(v => v.Brand)
			.Include(v => v.Model)   			.FirstOrDefaultAsync(v => v.Id == id);
	}

	public override async Task<IList<Vehicle>> GetAllAsync()
	{
		return await _dbSet
			.AsNoTracking()
			.Include(v => v.Brand)
			.Include(v => v.Model)   			.Where(v => v.IsActive)
			.OrderBy(v => v.Plate)
			.ToListAsync();
	}

	public async Task<Vehicle?> GetByPlateAsync(string plate)
	{
		return await _dbSet
			.Include(v => v.Brand)
			.Include(v => v.Model)
			.FirstOrDefaultAsync(v => v.Plate == plate.ToUpperInvariant());
	}
}