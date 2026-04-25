// --- Business/Services/ExpenseService.cs ---

using Microsoft.EntityFrameworkCore;
using VehicleTracker.DataAccess.Context;
using VehicleTracker.DataAccess.Repositories;
using VehicleTracker.Entities;

namespace VehicleTracker.Business.Services;

public class ExpenseService : IExpenseService
{
	private readonly ExpenseRepository _expenseRepository;
	private readonly VehicleRepository _vehicleRepository;
	private readonly TaxCalculatorService _taxCalculator;

	public ExpenseService(
		ExpenseRepository expenseRepository,
		VehicleRepository vehicleRepository,
		TaxCalculatorService taxCalculator)
	{
		_expenseRepository = expenseRepository;
		_vehicleRepository = vehicleRepository;
		_taxCalculator = taxCalculator;
	}

	// ── Okuma ──────────────────────────────────────────────────

	public async Task<IList<Expense>> GetAllExpensesAsync()
		=> await _expenseRepository.GetAllAsync();

	public async Task<IList<Expense>> GetExpensesByVehicleAsync(int vehicleId)
		=> await _expenseRepository.GetByVehicleIdAsync(vehicleId);

	public async Task<IList<Expense>> GetExpensesByDateRangeAsync(
		int vehicleId, DateTime start, DateTime end)
		=> await _expenseRepository.GetByDateRangeAsync(vehicleId, start, end);

	public async Task<Expense?> GetExpenseByIdAsync(int id)
		=> await _expenseRepository.GetByIdAsync(id);

	// ── Yazma ──────────────────────────────────────────────────

	public async Task AddExpenseAsync(Expense expense)
	{
		ValidateExpense(expense);

		// Araç var mı kontrol et
		var vehicle = await _vehicleRepository.GetByIdAsync(expense.VehicleId)
			?? throw new KeyNotFoundException($"Araç bulunamadı. VehicleId: {expense.VehicleId}");

		// Gider tarihinde km kaydedildiyse aracın sayacını ilerlet
		if (expense.KmAtExpense.HasValue && expense.KmAtExpense > vehicle.CurrentKm)
		{
			vehicle.CurrentKm = expense.KmAtExpense.Value;
			vehicle.UpdatedAt = DateTime.UtcNow;
			await _vehicleRepository.UpdateAsync(vehicle);
		}

		expense.CreatedAt = DateTime.UtcNow;

		await _expenseRepository.AddAsync(expense);
		await _expenseRepository.SaveChangesAsync(); // tek transaction'da commit
	}

	public async Task UpdateExpenseAsync(Expense expense)
	{
		ValidateExpense(expense);

		expense.CreatedAt = (await _expenseRepository.GetByIdAsync(expense.Id))?.CreatedAt
							 ?? DateTime.UtcNow; // orijinal oluşturma tarihini koru

		await _expenseRepository.UpdateAsync(expense);
		await _expenseRepository.SaveChangesAsync();
	}

	public async Task DeleteExpenseAsync(int id)
	{
		await _expenseRepository.DeleteAsync(id);
		await _expenseRepository.SaveChangesAsync();
	}

	// ── Özet / Raporlama ───────────────────────────────────────

	public async Task<ExpenseSummary> GetExpenseSummaryAsync(
		int vehicleId, DateTime start, DateTime end)
	{
		var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId)
			?? throw new KeyNotFoundException($"Araç bulunamadı. Id: {vehicleId}");

		var expenses = await _expenseRepository.GetByDateRangeAsync(vehicleId, start, end);

		decimal totalAmount = 0;
		decimal totalTaxAmount = 0;
		var byCategory = new Dictionary<string, decimal>();

		foreach (var expense in expenses)
		{
			// Her gider için vergi tutarını hesapla
			decimal taxAmount = await _taxCalculator.CalculateTaxAsync(
				expense.Amount,
				expense.Category?.Name ?? "Diğer",
				vehicle.Year
			);

			totalAmount += expense.Amount;
			totalTaxAmount += taxAmount;

			string catName = expense.Category?.Name ?? "Diğer";
			byCategory[catName] = byCategory.GetValueOrDefault(catName) + expense.Amount;
		}

		return new ExpenseSummary(
			TotalAmount: totalAmount,
			TotalTaxAmount: totalTaxAmount,
			GrandTotal: totalAmount + totalTaxAmount,
			ExpenseCount: expenses.Count,
			ByCategory: byCategory
		);
	}

	// ── Özel doğrulama ─────────────────────────────────────────

	private static void ValidateExpense(Expense e)
	{
		if (e.Amount <= 0)
			throw new ArgumentOutOfRangeException(nameof(e.Amount), "Gider tutarı sıfırdan büyük olmalı.");

		if (e.ExpenseDate > DateTime.UtcNow.AddDays(1))
			throw new ArgumentException("Gider tarihi gelecekte olamaz.");

		if (e.KmAtExpense.HasValue && e.KmAtExpense < 0)
			throw new ArgumentOutOfRangeException(nameof(e.KmAtExpense), "Kilometre negatif olamaz.");
	}
}