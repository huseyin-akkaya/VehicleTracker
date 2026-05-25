
using VehicleTracker.Entities;

namespace VehicleTracker.Business.Services;

public interface IExpenseService
{
	Task<IList<Expense>> GetAllExpensesAsync();
	Task<IList<Expense>> GetExpensesByVehicleAsync(int vehicleId);
	Task<IList<Expense>> GetExpensesByDateRangeAsync(int vehicleId, DateTime start, DateTime end);
	Task<Expense?> GetExpenseByIdAsync(int id);
	Task AddExpenseAsync(Expense expense);
	Task UpdateExpenseAsync(Expense expense);
	Task DeleteExpenseAsync(int id);

				Task<ExpenseSummary> GetExpenseSummaryAsync(int vehicleId, DateTime start, DateTime end);
}

public record ExpenseSummary(
	decimal TotalAmount,
	decimal TotalTaxAmount,
	decimal GrandTotal,
	int ExpenseCount,
	Dictionary<string, decimal> ByCategory  );