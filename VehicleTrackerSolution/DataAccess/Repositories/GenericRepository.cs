
using Microsoft.EntityFrameworkCore;
using VehicleTracker.DataAccess.Context;

namespace VehicleTracker.DataAccess.Repositories;

public class GenericRepository<T> : IRepository<T> where T : class
{
	protected readonly AppDbContext _context;
	protected readonly DbSet<T> _dbSet;

	public GenericRepository(AppDbContext context)
	{
		_context = context;
		_dbSet = context.Set<T>();
	}

	
	public virtual async Task<T?> GetByIdAsync(int id)
	{
		return await _dbSet.FindAsync(id);
	}

	public virtual async Task<IList<T>> GetAllAsync()
	{
		return await _dbSet.AsNoTracking().ToListAsync();
	}

	
	public virtual async Task AddAsync(T entity)
	{
		await _dbSet.AddAsync(entity);
	}

	public virtual async Task UpdateAsync(T entity)
	{
						_dbSet.Update(entity);
		await Task.CompletedTask;
	}

	public virtual async Task DeleteAsync(int id)
	{
		var entity = await _dbSet.FindAsync(id);

		if (entity is null)
			throw new KeyNotFoundException($"{typeof(T).Name} bulunamadı. Id: {id}");

		_dbSet.Remove(entity);
	}

	
	public async Task SaveChangesAsync()
	{
		await _context.SaveChangesAsync();
	}
}