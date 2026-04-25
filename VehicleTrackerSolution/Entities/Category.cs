namespace VehicleTracker.Entities;

public class Category
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string? Icon { get; set; }
	public bool IsSystem { get; set; }
	public string? ColorHex { get; set; }

	public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}