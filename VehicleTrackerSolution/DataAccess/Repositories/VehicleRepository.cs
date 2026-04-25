// --- DataAccess/Repositories/VehicleRepository.cs ---

using Microsoft.EntityFrameworkCore;
using VehicleTracker.DataAccess.Context;
using VehicleTracker.Entities;

namespace VehicleTracker.DataAccess.Repositories;

public class VehicleRepository : GenericRepository<Vehicle>
{
	public VehicleRepository(AppDbContext context) : base(context) { }

	// Brand navigation property'sini de yükle
	public override async Task<Vehicle?> GetByIdAsync(int id)
	{
		return await _dbSet
			.Include(v => v.Brand)
			.FirstOrDefaultAsync(v => v.Id == id);
	}

	// Tüm aktif araçları markasıyla birlikte getir
	public override async Task<IList<Vehicle>> GetAllAsync()
	{
		return await _dbSet
			.AsNoTracking()
			.Include(v => v.Brand)
			.Where(v => v.IsActive)
			.OrderBy(v => v.Plate)
			.ToListAsync();
	}

	// Plakaya göre arama
	public async Task<Vehicle?> GetByPlateAsync(string plate)
	{
		return await _dbSet
			.Include(v => v.Brand)
			.FirstOrDefaultAsync(v => v.Plate == plate.ToUpperInvariant());
	}
}