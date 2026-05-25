
namespace VehicleTracker.DataAccess.Repositories;

public interface IRepository<T> where T : class
{
		Task<T?> GetByIdAsync(int id);
	Task<IList<T>> GetAllAsync();

		Task AddAsync(T entity);
	Task UpdateAsync(T entity);
	Task DeleteAsync(int id);

						Task SaveChangesAsync();
}