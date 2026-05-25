
using Microsoft.EntityFrameworkCore;
using VehicleTracker.DataAccess.Context;
using VehicleTracker.Entities;

namespace VehicleTracker.DataAccess.Repositories;

public class ExpenseRepository : GenericRepository<Expense>
{
	public ExpenseRepository(AppDbContext context) : base(context) { }

	public override async Task<Expense?> GetByIdAsync(int id)
	{
		return await _dbSet
			.Include(e => e.Vehicle)
			.Include(e => e.Category)
			.FirstOrDefaultAsync(e => e.Id == id);
	}

	public override async Task<IList<Expense>> GetAllAsync()
	{
		return await _dbSet
			.AsNoTracking()
			.Include(e => e.Vehicle)
			.Include(e => e.Category)
			.OrderByDescending(e => e.ExpenseDate)
			.ToListAsync();
	}

	public async Task<IList<Expense>> GetByVehicleIdAsync(int vehicleId)
	{
		return await _dbSet
			.AsNoTracking()
			.Include(e => e.Category)
			.Where(e => e.VehicleId == vehicleId)
			.OrderByDescending(e => e.ExpenseDate)
			.ToListAsync();
	}

	public async Task<IList<Expense>> GetByDateRangeAsync(
		int vehicleId, DateTime start, DateTime end)
	{
		return await _dbSet
			.AsNoTracking()
			.Include(e => e.Category)
			.Where(e => e.VehicleId == vehicleId
					 && e.ExpenseDate >= start
					 && e.ExpenseDate <= end)
			.OrderByDescending(e => e.ExpenseDate)
			.ToListAsync();
	}
}