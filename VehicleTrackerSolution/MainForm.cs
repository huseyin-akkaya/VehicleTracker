// --- UI/Forms/MainForm.cs ---

using Microsoft.Extensions.DependencyInjection;
using VehicleTracker.Business.Services;
using VehicleTracker.Business.Sync;
using VehicleTracker.DataAccess.Context;
using VehicleTracker.DataAccess.Repositories;
using VehicleTracker.UI.ViewModels;

namespace VehicleTracker.UI;

public class MainForm : Form
{
	private readonly IVehicleService _vehicleService;
	private readonly IExpenseService _expenseService;
	private readonly TaxCalculatorService _taxCalculator;
	private readonly IServiceProvider _serviceProvider;

	// ── UI ────────────────────────────────────────────────────
	private Panel _topPanel = null!;
	private Label _lblTitle = null!;
	private Button _btnAddVehicle = null!;
	private Button _btnAddExpense = null!;
	private Button _btnViewHistory = null!;
	private Button _btnRefresh = null!;
	private DataGridView _gridVehicles = null!;
	private StatusStrip _statusStrip = null!;
	private ToolStripStatusLabel _lblSyncStatus = null!;
	private ToolStripStatusLabel _lblRecordCount = null!;

	// ── Özet Paneli ───────────────────────────────────────────
	private Panel _summaryPanel = null!;
	private Label _lblRealExpense = null!;
	private Label _lblEstimatedExpense = null!;

	public MainForm(
		IVehicleService vehicleService,
		IExpenseService expenseService,
		TaxCalculatorService taxCalculator,
		IServiceProvider serviceProvider)
	{
		_vehicleService = vehicleService;
		_expenseService = expenseService;
		_taxCalculator = taxCalculator;
		_serviceProvider = serviceProvider;

		InitializeComponents();
	}

	private void InitializeComponents()
	{
		Text = "Araçlarım ve Giderler";
		Size = new Size(1020, 660);
		MinimumSize = new Size(800, 520);
		StartPosition = FormStartPosition.CenterScreen;
		BackColor = Color.FromArgb(245, 245, 245);
		Font = new Font("Segoe UI", 9.5f);

		// ── Top Panel ─────────────────────────────────────────
		_topPanel = new Panel
		{
			Dock = DockStyle.Top,
			Height = 64,
			BackColor = Color.FromArgb(30, 30, 60),
			Padding = new Padding(16, 0, 16, 0)
		};

		_lblTitle = new Label
		{
			Text = "🚗  Araçlarım",
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 13f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(16, 18)
		};

		_btnRefresh = MakeTopButton("↻  Yenile", Color.FromArgb(52, 152, 219));
		_btnAddVehicle = MakeTopButton("+ Araç Ekle", Color.FromArgb(72, 199, 142));
		_btnAddExpense = MakeTopButton("+ Gider Ekle", Color.FromArgb(231, 76, 60));
		_btnViewHistory = MakeTopButton("📋 Gider Geçmişi", Color.FromArgb(22, 160, 133));

		_topPanel.Controls.AddRange(new Control[]
			{ _lblTitle, _btnViewHistory, _btnAddExpense, _btnAddVehicle, _btnRefresh });

		_topPanel.Resize += (_, _) =>
		{
			_btnRefresh.Left = _topPanel.Width - _btnRefresh.Width - 16;
			_btnAddVehicle.Left = _btnRefresh.Left - _btnAddVehicle.Width - 8;
			_btnAddExpense.Left = _btnAddVehicle.Left - _btnAddExpense.Width - 8;
			_btnViewHistory.Left = _btnAddExpense.Left - _btnViewHistory.Width - 8;

			foreach (Control btn in new Control[]
				{ _btnRefresh, _btnAddVehicle, _btnAddExpense, _btnViewHistory })
				btn.Top = (_topPanel.Height - btn.Height) / 2;
		};

		// ── Özet Paneli ───────────────────────────────────────
		_summaryPanel = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = 56,
			BackColor = Color.FromArgb(28, 38, 68),
			Padding = new Padding(20, 0, 20, 0)
		};

		_lblRealExpense = new Label
		{
			Text = "💰  Toplam Gerçekleşen Masraf:  —",
			ForeColor = Color.FromArgb(130, 200, 255),
			Font = new Font("Segoe UI", 10f, FontStyle.Bold),
			AutoSize = false,
			Width = 400,
			Dock = DockStyle.Left,
			TextAlign = ContentAlignment.MiddleLeft
		};

		_lblEstimatedExpense = new Label
		{
			Text = "🧮  Tahmini Yıllık Toplam:  —",
			ForeColor = Color.FromArgb(160, 230, 180),
			Font = new Font("Segoe UI", 10f, FontStyle.Bold),
			AutoSize = false,
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleRight
		};

		_summaryPanel.Controls.Add(_lblRealExpense);
		_summaryPanel.Controls.Add(_lblEstimatedExpense);

		// ── Grid ──────────────────────────────────────────────
		_gridVehicles = new DataGridView
		{
			Dock = DockStyle.Fill,
			BackgroundColor = Color.White,
			BorderStyle = BorderStyle.None,
			RowHeadersVisible = false,
			AllowUserToAddRows = false,
			AllowUserToDeleteRows = false,
			ReadOnly = true,
			SelectionMode = DataGridViewSelectionMode.FullRowSelect,
			AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
			GridColor = Color.FromArgb(230, 230, 235),
			CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
			Font = new Font("Segoe UI", 9.5f)
		};

		_gridVehicles.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
		{
			BackColor = Color.FromArgb(30, 30, 60),
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
			Padding = new Padding(6, 0, 0, 0)
		};
		_gridVehicles.ColumnHeadersHeight = 38;
		_gridVehicles.ColumnHeadersHeightSizeMode =
			DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
		_gridVehicles.EnableHeadersVisualStyles = false;

		_gridVehicles.DefaultCellStyle = new DataGridViewCellStyle
		{
			SelectionBackColor = Color.FromArgb(210, 228, 255),
			SelectionForeColor = Color.FromArgb(20, 20, 20),
			Padding = new Padding(4, 0, 0, 0)
		};
		_gridVehicles.AlternatingRowsDefaultCellStyle =
			new DataGridViewCellStyle { BackColor = Color.FromArgb(248, 249, 252) };
		_gridVehicles.RowTemplate.Height = 36;

		ConfigureGridColumns();

		// ── StatusStrip ───────────────────────────────────────
		_statusStrip = new StatusStrip
		{
			BackColor = Color.FromArgb(235, 235, 240),
			SizingGrip = false
		};
		_lblSyncStatus = new ToolStripStatusLabel
		{
			Text = "Hazır",
			ForeColor = Color.FromArgb(80, 80, 100),
			Font = new Font("Segoe UI", 8.5f)
		};
		_lblRecordCount = new ToolStripStatusLabel
		{
			Text = "Araç: —",
			ForeColor = Color.FromArgb(80, 80, 100),
			Font = new Font("Segoe UI", 8.5f)
		};
		_statusStrip.Items.AddRange(new ToolStripItem[]
		{
			_lblSyncStatus, new ToolStripStatusLabel { Spring = true }, _lblRecordCount
		});

		// ── Yerleşim ──────────────────────────────────────────
		Controls.Add(_gridVehicles);
		Controls.Add(_summaryPanel);
		Controls.Add(_statusStrip);
		Controls.Add(_topPanel);

		// ── Events ────────────────────────────────────────────
		Load += async (_, _) => await LoadVehiclesAsync();
		_btnRefresh.Click += async (_, _) => await LoadVehiclesAsync();
		_btnAddVehicle.Click += OnAddVehicleClicked;
		_btnAddExpense.Click += OnAddExpenseClicked;
		_btnViewHistory.Click += OnViewHistoryClicked;
	}

	private void ConfigureGridColumns()
	{
		_gridVehicles.AutoGenerateColumns = false;
		_gridVehicles.Columns.Clear();
		_gridVehicles.Columns.AddRange(
			new DataGridViewTextBoxColumn
			{ DataPropertyName = "Id", HeaderText = "ID", FillWeight = 5 },
			new DataGridViewTextBoxColumn
			{ DataPropertyName = "Plate", HeaderText = "Plaka", FillWeight = 15 },
			new DataGridViewTextBoxColumn
			{ DataPropertyName = "BrandName", HeaderText = "Marka", FillWeight = 18 },
			new DataGridViewTextBoxColumn
			{ DataPropertyName = "ModelName", HeaderText = "Model", FillWeight = 20 },
			new DataGridViewTextBoxColumn
			{ DataPropertyName = "Year", HeaderText = "Yıl", FillWeight = 10 },
			new DataGridViewTextBoxColumn
			{
				DataPropertyName = "CurrentKmFormatted",
				HeaderText = "Kilometre",
				FillWeight = 15,
				DefaultCellStyle = new DataGridViewCellStyle
				{ Alignment = DataGridViewContentAlignment.MiddleRight }
			},
			new DataGridViewTextBoxColumn
			{ DataPropertyName = "IsActiveText", HeaderText = "Durum", FillWeight = 12 }
		);
	}

	// ─────────────────────────────────────────────────────────
	//  Araçları yükle + özet hesapla
	// ─────────────────────────────────────────────────────────
	private async Task LoadVehiclesAsync()
	{
		_btnRefresh.Enabled = false;
		_btnRefresh.Text = "⏳";

		try
		{
			var vehicles = await _vehicleService.GetAllVehiclesAsync();

			var viewModels = vehicles
				.Select(v => new VehicleViewModel(v))
				.ToList();

			_gridVehicles.DataSource =
				new System.Windows.Forms.BindingSource { DataSource = viewModels };

			_lblRecordCount.Text = $"Araç: {viewModels.Count}";

			// Özet panelini güncelle
			await UpdateSummaryAsync(vehicles);
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Araçlar yüklenirken hata:\n{ex.Message}",
				"Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}
		finally
		{
			_btnRefresh.Enabled = true;
			_btnRefresh.Text = "↻  Yenile";
		}
	}

	// ─────────────────────────────────────────────────────────
	//  Özet paneli hesaplama
	// ─────────────────────────────────────────────────────────
	private async Task UpdateSummaryAsync(IList<VehicleTracker.Entities.Vehicle> vehicles)
	{
		try
		{
			// 1 — Tüm araçların gerçek gider toplamı
			decimal realTotal = 0m;
			foreach (var v in vehicles)
			{
				var expenses = await _expenseService.GetExpensesByVehicleAsync(v.Id);
				realTotal += expenses.Sum(e => e.Amount);
			}

			// 2 — Her araç için TaxCalculator ile tahmini yıllık maliyet
			decimal estimatedTotal = 0m;
			const double annualKm = 15_000;
			const decimal fuelPrice = 42m;

			foreach (var v in vehicles)
			{
				if (v.Model is null) continue;

				bool isElectric = v.Model.AvgFuelConsumption <= 0;

				// Yakıt
				if (!isElectric)
				{
					decimal fuelBase = (decimal)(annualKm / 100.0
						* v.Model.AvgFuelConsumption) * fuelPrice;
					decimal fuelTax = await _taxCalculator
						.CalculateTaxAsync(fuelBase, "Yakıt", v.Year);
					estimatedTotal += fuelBase + fuelTax;
				}

				// Bakım
				decimal maintBase = 8_000m;
				decimal maintTax = await _taxCalculator
					.CalculateTaxAsync(maintBase, "Bakım", v.Year);
				estimatedTotal += maintBase + maintTax;

				// MTV (motor hacmine göre)
				decimal mtvBase = v.Model.EngineVolume switch
				{
					<= 0 => 3_500m,
					<= 1.3 => 4_800m,
					<= 1.6 => 8_200m,
					<= 2.0 => 14_500m,
					_ => 22_000m
				};
				decimal mtvTax = await _taxCalculator
					.CalculateTaxAsync(mtvBase, "Vergi", v.Year);
				estimatedTotal += mtvBase + mtvTax;

				// Sigorta (yaşa göre)
				int age = DateTime.Now.Year - v.Year;
				decimal insBase = age switch
				{
					<= 3 => 20_000m,
					<= 7 => 15_000m,
					<= 15 => 10_000m,
					_ => 7_000m
				};
				decimal insTax = await _taxCalculator
					.CalculateTaxAsync(insBase, "Sigorta", v.Year);
				estimatedTotal += insBase + insTax;
			}

			_lblRealExpense.Text =
				$"💰  Toplam Gerçekleşen Masraf:  {realTotal:N2} ₺";
			_lblEstimatedExpense.Text =
				$"🧮  Tahmini Yıllık Toplam (tüm araçlar):  {estimatedTotal:N2} ₺";
		}
		catch
		{
			// Özet hesaplanamasa bile araç listesi bozulmasın
			_lblRealExpense.Text = "💰  Toplam Masraf: hesaplanamadı";
			_lblEstimatedExpense.Text = "🧮  Tahmini Yıllık: hesaplanamadı";
		}
	}

	// ─────────────────────────────────────────────────────────
	//  Event handler'lar
	// ─────────────────────────────────────────────────────────
	private async void OnAddVehicleClicked(object? sender, EventArgs e)
	{
		using var form = _serviceProvider.GetRequiredService<VehicleForm>();
		if (form.ShowDialog(this) == DialogResult.OK && form.SavedVehicle is not null)
		{
			await LoadVehiclesAsync();
			SetStatus($"✅  '{form.SavedVehicle.Plate}' eklendi.",
				Color.FromArgb(30, 130, 80));
		}
	}

	private void OnAddExpenseClicked(object? sender, EventArgs e)
	{
		var vehicle = GetSelectedVehicle();
		if (vehicle is null) return;

		var expenseService = _serviceProvider.GetRequiredService<IExpenseService>();
		var dbContext = _serviceProvider.GetRequiredService<AppDbContext>();

		using var form = new ExpenseForm(expenseService, dbContext, vehicle);
		if (form.ShowDialog(this) == DialogResult.OK)
			SetStatus($"✅  '{vehicle.Plate}' için gider kaydedildi.",
				Color.FromArgb(30, 130, 80));
	}

	private void OnViewHistoryClicked(object? sender, EventArgs e)
	{
		var vehicle = GetSelectedVehicle();
		if (vehicle is null) return;

		var expenseService = _serviceProvider.GetRequiredService<IExpenseService>();
		using var form = new ExpenseHistoryForm(expenseService, vehicle);
		form.ShowDialog(this);
	}

	// ─────────────────────────────────────────────────────────
	//  Yardımcılar
	// ─────────────────────────────────────────────────────────
	private VehicleTracker.Entities.Vehicle? GetSelectedVehicle()
	{
		if (_gridVehicles.SelectedRows.Count == 0)
		{
			MessageBox.Show("Lütfen önce listeden bir araç seçin.",
				"Araç Seçilmedi", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return null;
		}

		if (_gridVehicles.SelectedRows[0].DataBoundItem is not VehicleViewModel vm)
			return null;

		return Task.Run(() =>
			_vehicleService.GetVehicleByIdAsync(vm.Id)).GetAwaiter().GetResult();
	}

	private void SetStatus(string message, Color color)
	{
		_lblSyncStatus.Text = message;
		_lblSyncStatus.ForeColor = color;
	}

	private static Button MakeTopButton(string text, Color color)
	{
		var btn = new Button
		{
			Text = text,
			Size = new Size(130, 34),
			BackColor = color,
			ForeColor = Color.White,
			FlatStyle = FlatStyle.Flat,
			Font = new Font("Segoe UI", 9f, FontStyle.Bold),
			Cursor = Cursors.Hand,
			Anchor = AnchorStyles.Right | AnchorStyles.Top
		};
		btn.FlatAppearance.BorderSize = 0;
		return btn;
	}
}