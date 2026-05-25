
using VehicleTracker.DataAccess.Repositories;
using VehicleTracker.Entities;

namespace VehicleTracker.Business.Services;

public class VehicleService : IVehicleService
{
	private readonly VehicleRepository _vehicleRepository;

	public VehicleService(VehicleRepository vehicleRepository)
	{
		_vehicleRepository = vehicleRepository;
	}

	public async Task<IList<Vehicle>> GetAllVehiclesAsync()
		=> await _vehicleRepository.GetAllAsync();

	public async Task<Vehicle?> GetVehicleByIdAsync(int id)
		=> await _vehicleRepository.GetByIdAsync(id);

	public async Task<Vehicle?> GetVehicleByPlateAsync(string plate)
		=> await _vehicleRepository.GetByPlateAsync(plate);

	public async Task AddVehicleAsync(Vehicle vehicle)
	{
		ValidateVehicle(vehicle);

		var existing = await _vehicleRepository.GetByPlateAsync(vehicle.Plate);
		if (existing is not null)
			throw new InvalidOperationException(
				$"'{vehicle.Plate}' plakalı araç zaten kayıtlı.");

		vehicle.Plate = vehicle.Plate.ToUpperInvariant().Trim();
		vehicle.CreatedAt = DateTime.UtcNow;
		vehicle.UpdatedAt = DateTime.UtcNow;
		vehicle.IsActive = true;

		await _vehicleRepository.AddAsync(vehicle);
		await _vehicleRepository.SaveChangesAsync();
	}

	public async Task UpdateVehicleAsync(Vehicle vehicle)
	{
		ValidateVehicle(vehicle);
		vehicle.Plate = vehicle.Plate.ToUpperInvariant().Trim();
		vehicle.UpdatedAt = DateTime.UtcNow;
		await _vehicleRepository.UpdateAsync(vehicle);
		await _vehicleRepository.SaveChangesAsync();
	}

	public async Task DeleteVehicleAsync(int id)
	{
		var vehicle = await _vehicleRepository.GetByIdAsync(id)
			?? throw new KeyNotFoundException($"Araç bulunamadı. Id: {id}");

		vehicle.IsActive = false;
		vehicle.UpdatedAt = DateTime.UtcNow;
		await _vehicleRepository.UpdateAsync(vehicle);
		await _vehicleRepository.SaveChangesAsync();
	}

	public async Task UpdateKilometerAsync(int vehicleId, double newKm)
	{
		var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId)
			?? throw new KeyNotFoundException($"Araç bulunamadı. Id: {vehicleId}");

		if (newKm < vehicle.CurrentKm)
			throw new InvalidOperationException(
				$"Yeni kilometre ({newKm:N0} km), " +
				$"mevcut değerden ({vehicle.CurrentKm:N0} km) küçük olamaz.");

		vehicle.CurrentKm = newKm;
		vehicle.UpdatedAt = DateTime.UtcNow;
		await _vehicleRepository.UpdateAsync(vehicle);
		await _vehicleRepository.SaveChangesAsync();
	}

	private static void ValidateVehicle(Vehicle v)
	{
		if (string.IsNullOrWhiteSpace(v.Plate))
			throw new ArgumentException("Plaka boş olamaz.");

		if (v.ModelId <= 0)
			throw new ArgumentException("Geçerli bir model seçilmelidir.");

		if (v.Year < 1900 || v.Year > DateTime.Now.Year + 1)
			throw new ArgumentOutOfRangeException(nameof(v.Year), "Geçersiz üretim yılı.");

		if (v.CurrentKm < 0)
			throw new ArgumentOutOfRangeException(nameof(v.CurrentKm), "Kilometre negatif olamaz.");
	}
}