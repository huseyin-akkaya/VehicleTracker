namespace VehicleTracker.Entities;

using VehicleTracker.Entities;

public class Expense
{
	public int Id { get; set; }
	public int VehicleId { get; set; }
	public int CategoryId { get; set; }
	public decimal Amount { get; set; }
	public string CurrencyCode { get; set; } = "TRY";
	public double? KmAtExpense { get; set; }
	public DateTime ExpenseDate { get; set; }
	public string? Description { get; set; }
	public string? ReceiptPath { get; set; }
	public DateTime CreatedAt { get; set; }

	public Vehicle? Vehicle { get; set; }
	public Category? Category { get; set; }
}