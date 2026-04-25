namespace VehicleTracker.Entities;

public class Vehicle
{
	public int Id { get; set; }
	public string Plate { get; set; } = string.Empty;
	public int BrandId { get; set; }
	public string ModelName { get; set; } = string.Empty;
	public int Year { get; set; }
	public double CurrentKm { get; set; }
	public bool IsActive { get; set; } = true;
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }

	// Navigation properties (EF Core için)
	public VehicleBrand? Brand { get; set; }
	public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}