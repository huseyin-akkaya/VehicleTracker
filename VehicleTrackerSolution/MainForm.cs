// --- UI/Forms/MainForm.cs ---

using Microsoft.Extensions.DependencyInjection;
using VehicleTracker.Business.Services;
using VehicleTracker.Business.Sync;
using VehicleTracker.Entities;
using VehicleTracker.UI.ViewModels;

namespace VehicleTracker.UI;

public class MainForm : Form
{
	// ── Bağımlılıklar ─────────────────────────────────────────
	private readonly IVehicleService _vehicleService;
	private readonly SyncManager _syncManager;
	private readonly IServiceProvider _serviceProvider;

	// ── UI Bileşenleri ────────────────────────────────────────
	private DataGridView _gridVehicles = null!;
	private StatusStrip _statusStrip = null!;
	private ToolStripStatusLabel _lblSyncStatus = null!;
	private ToolStripStatusLabel _lblRecordCount = null!;
	private Panel _topPanel = null!;

	// BUTONLAR (Sadece 1'er kere tanımlandı)
	private Button _btnRefresh = null!;
	private Button _btnAddVehicle = null!;
	private Button _btnAddExpense = null!;
	private Button _btnSimulator = null!;
	private Label _lblTitle = null!;

	public MainForm(
		IVehicleService vehicleService,
		SyncManager syncManager,
		IServiceProvider serviceProvider)
	{
		_vehicleService = vehicleService;
		_syncManager = syncManager;
		_serviceProvider = serviceProvider;

		_syncManager.SyncCompleted += OnSyncCompleted;
		InitializeComponents();
	}

	// ─────────────────────────────────────────────────────────
	//  Bileşen oluşturma — Designer yerine tam kod
	// ─────────────────────────────────────────────────────────
	private void InitializeComponents()
	{
		// ── Form ayarları ─────────────────────────────────────
		Text = "Araç Takip Sistemi";
		Size = new Size(1000, 640);
		MinimumSize = new Size(800, 500);
		StartPosition = FormStartPosition.CenterScreen;
		BackColor = Color.FromArgb(245, 245, 245);
		Font = new Font("Segoe UI", 9.5f);

		// ── Üst panel (başlık + butonlar) ─────────────────────
		_topPanel = new Panel
		{
			Dock = DockStyle.Top,
			Height = 64,
			BackColor = Color.FromArgb(30, 30, 60),
			Padding = new Padding(16, 0, 16, 0)
		};

		_lblTitle = new Label
		{
			Text = "🚗  Araç Takip Sistemi",
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 13f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(16, 18)
		};

		_btnAddVehicle = new Button
		{
			Text = "+ Araç Ekle",
			Size = new Size(110, 34),
			BackColor = Color.FromArgb(72, 199, 142),
			ForeColor = Color.White,
			FlatStyle = FlatStyle.Flat,
			Font = new Font("Segoe UI", 9f, FontStyle.Bold),
			Cursor = Cursors.Hand,
			Anchor = AnchorStyles.Right | AnchorStyles.Top
		};
		_btnAddVehicle.FlatAppearance.BorderSize = 0;

		_btnRefresh = new Button
		{
			Text = "↻  Yenile",
			Size = new Size(100, 34),
			BackColor = Color.FromArgb(52, 152, 219),
			ForeColor = Color.White,
			FlatStyle = FlatStyle.Flat,
			Font = new Font("Segoe UI", 9f, FontStyle.Bold),
			Cursor = Cursors.Hand,
			Anchor = AnchorStyles.Right | AnchorStyles.Top
		};
		_btnRefresh.FlatAppearance.BorderSize = 0;

		_btnAddExpense = new Button
		{
			Text = "+ Gider Ekle",
			Size = new Size(110, 34),
			BackColor = Color.FromArgb(231, 76, 60),
			ForeColor = Color.White,
			FlatStyle = FlatStyle.Flat,
			Font = new Font("Segoe UI", 9f, FontStyle.Bold),
			Cursor = Cursors.Hand,
			Anchor = AnchorStyles.Right | AnchorStyles.Top
		};
		_btnAddExpense.FlatAppearance.BorderSize = 0;

		_btnSimulator = new Button
		{
			Text = "🧮 Simülatör",
			Size = new Size(120, 34),
			BackColor = Color.FromArgb(142, 68, 173),
			ForeColor = Color.White,
			FlatStyle = FlatStyle.Flat,
			Font = new Font("Segoe UI", 9f, FontStyle.Bold),
			Cursor = Cursors.Hand,
			Anchor = AnchorStyles.Right | AnchorStyles.Top
		};
		_btnSimulator.FlatAppearance.BorderSize = 0;

		// TEK BİR Ekleme İşlemi (Çakışmalar silindi)
		_topPanel.Controls.AddRange(new Control[]
		{
			_lblTitle, _btnSimulator, _btnAddExpense, _btnAddVehicle, _btnRefresh
		});

		// TEK BİR Resize İşlemi (4 buton hizalaması)
		_topPanel.Resize += (_, _) =>
		{
			_btnRefresh.Left = _topPanel.Width - _btnRefresh.Width - 16;
			_btnAddVehicle.Left = _btnRefresh.Left - _btnAddVehicle.Width - 8;
			_btnAddExpense.Left = _btnAddVehicle.Left - _btnAddExpense.Width - 8;
			_btnSimulator.Left = _btnAddExpense.Left - _btnSimulator.Width - 8;

			foreach (Control btn in new[] { _btnRefresh, _btnAddVehicle, _btnAddExpense, _btnSimulator })
				btn.Top = (_topPanel.Height - btn.Height) / 2;
		};

		// ── DataGridView ──────────────────────────────────────
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
			GridColor = Color.FromArgb(230, 230, 230),
			Font = new Font("Segoe UI", 9.5f),
			CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
		};

		_gridVehicles.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
		{
			BackColor = Color.FromArgb(30, 30, 60),
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
			Padding = new Padding(6, 0, 0, 0)
		};
		_gridVehicles.ColumnHeadersHeight = 38;
		_gridVehicles.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
		_gridVehicles.EnableHeadersVisualStyles = false;

		_gridVehicles.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
		{
			BackColor = Color.FromArgb(248, 249, 252)
		};
		_gridVehicles.DefaultCellStyle = new DataGridViewCellStyle
		{
			SelectionBackColor = Color.FromArgb(210, 228, 255),
			SelectionForeColor = Color.FromArgb(20, 20, 20),
			Padding = new Padding(4, 0, 0, 0)
		};
		_gridVehicles.RowTemplate.Height = 36;

		ConfigureGridColumns();

		// ── Status Strip ──────────────────────────────────────
		_statusStrip = new StatusStrip
		{
			BackColor = Color.FromArgb(235, 235, 240),
			SizingGrip = false
		};

		_lblSyncStatus = new ToolStripStatusLabel
		{
			Text = "⏳  Senkronizasyon bekleniyor...",
			ForeColor = Color.FromArgb(80, 80, 100),
			Font = new Font("Segoe UI", 8.5f)
		};

		var spacer = new ToolStripStatusLabel { Spring = true };

		_lblRecordCount = new ToolStripStatusLabel
		{
			Text = "Araç: —",
			ForeColor = Color.FromArgb(80, 80, 100),
			Font = new Font("Segoe UI", 8.5f)
		};

		_statusStrip.Items.AddRange(new ToolStripItem[]
		{
			_lblSyncStatus,
			spacer,
			_lblRecordCount
		});

		// ── Layout ────────────────────────────────────────────
		Controls.Add(_gridVehicles);
		Controls.Add(_statusStrip);
		Controls.Add(_topPanel);

		// ── Event bağlantıları ────────────────────────────────
		_btnRefresh.Click += async (_, _) => await LoadVehiclesAsync();
		_btnAddVehicle.Click += OnAddVehicleClicked;
		_btnAddExpense.Click += OnAddExpenseClicked;
		_btnSimulator.Click += OnSimulatorClicked;
		Load += OnFormLoad;
	}

	private void ConfigureGridColumns()
	{
		_gridVehicles.AutoGenerateColumns = false;
		_gridVehicles.Columns.Clear();

		_gridVehicles.Columns.AddRange(
			new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "ID", Width = 50, FillWeight = 5 },
			new DataGridViewTextBoxColumn { DataPropertyName = "Plate", HeaderText = "Plaka", FillWeight = 15 },
			new DataGridViewTextBoxColumn { DataPropertyName = "BrandName", HeaderText = "Marka", FillWeight = 18 },
			new DataGridViewTextBoxColumn { DataPropertyName = "ModelName", HeaderText = "Model", FillWeight = 20 },
			new DataGridViewTextBoxColumn { DataPropertyName = "Year", HeaderText = "Yıl", FillWeight = 10 },
			new DataGridViewTextBoxColumn { DataPropertyName = "CurrentKmFormatted", HeaderText = "Kilometre", FillWeight = 15, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
			new DataGridViewTextBoxColumn { DataPropertyName = "IsActiveText", HeaderText = "Durum", FillWeight = 12 }
		);
	}

	private async void OnFormLoad(object? sender, EventArgs e)
	{
		SetStatus("⏳  Senkronizasyon başlatılıyor...", Color.FromArgb(180, 140, 0));
		SetUiEnabled(false);

		try
		{
			await _syncManager.SynchronizeAsync();
			await LoadVehiclesAsync();
		}
		finally
		{
			SetUiEnabled(true);
		}
	}

	private async Task LoadVehiclesAsync()
	{
		try
		{
			_btnRefresh.Enabled = false;
			_btnRefresh.Text = "⏳  Yükleniyor...";

			IList<Vehicle> vehicles = await _vehicleService.GetAllVehiclesAsync();

			var viewModels = vehicles.Select(v => new VehicleViewModel(v)).ToList();

			if (InvokeRequired)
			{
				Invoke(() => BindGrid(viewModels));
			}
			else
			{
				BindGrid(viewModels);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Araçlar yüklenirken hata oluştu:\n{ex.Message}", "Yükleme Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}
		finally
		{
			_btnRefresh.Enabled = true;
			_btnRefresh.Text = "↻  Yenile";
		}
	}

	private void BindGrid(IList<VehicleViewModel> viewModels)
	{
		_gridVehicles.DataSource = null;
		_gridVehicles.DataSource = new BindingSource { DataSource = viewModels };
		_lblRecordCount.Text = $"Araç: {viewModels.Count}";
	}

	private void OnSyncCompleted(object? sender, SyncResultEventArgs e)
	{
		if (InvokeRequired)
		{
			Invoke(() => ApplySyncResult(e));
			return;
		}
		ApplySyncResult(e);
	}

	private void ApplySyncResult(SyncResultEventArgs e)
	{
		var (icon, color) = e.Status switch
		{
			SyncStatus.Success => ("✅", Color.FromArgb(30, 130, 80)),
			SyncStatus.UpToDate => ("✅", Color.FromArgb(30, 130, 80)),
			SyncStatus.Offline => ("📶", Color.FromArgb(150, 100, 0)),
			SyncStatus.Skipped => ("⏭️", Color.FromArgb(80, 80, 80)),
			SyncStatus.Failed => ("⚠️", Color.FromArgb(180, 50, 50)),
			_ => ("ℹ️", Color.FromArgb(50, 100, 180))
		};

		SetStatus($"{icon}  {e.Message}", color);
	}

	private async void OnAddVehicleClicked(object? sender, EventArgs e)
	{
		using var form = _serviceProvider.GetRequiredService<VehicleForm>();
		DialogResult result = form.ShowDialog(this);

		if (result == DialogResult.OK && form.SavedVehicle is not null)
		{
			await LoadVehiclesAsync();
			SetStatus($"✅  '{form.SavedVehicle.Plate}' plakalı araç başarıyla eklendi.", Color.FromArgb(30, 130, 80));
		}
	}

	private void OnAddExpenseClicked(object? sender, EventArgs e)
	{
		if (_gridVehicles.SelectedRows.Count == 0)
		{
			MessageBox.Show("Lütfen önce listeden bir araç seçin.", "Araç Seçilmedi", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}

		if (_gridVehicles.SelectedRows[0].DataBoundItem is not VehicleTracker.UI.ViewModels.VehicleViewModel vm)
			return;

		Vehicle? vehicle = Task.Run(() => _vehicleService.GetVehicleByIdAsync(vm.Id)).GetAwaiter().GetResult();

		if (vehicle is null)
		{
			MessageBox.Show("Araç bulunamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}

		// KIRIK PARANTEZLER BURADA DÜZELTİLDİ
		var expenseService = _serviceProvider.GetRequiredService<IExpenseService>();
		var dbContext = _serviceProvider.GetRequiredService<VehicleTracker.DataAccess.Context.AppDbContext>();

		using var form = new ExpenseForm(expenseService, dbContext, vehicle);
		DialogResult result = form.ShowDialog(this);

		if (result == DialogResult.OK)
		{
			SetStatus($"✅  '{vehicle.Plate}' için gider başarıyla kaydedildi.", Color.FromArgb(30, 130, 80));
		}
	}

	private void OnSimulatorClicked(object? sender, EventArgs e)
	{
		var taxCalc = _serviceProvider.GetRequiredService<TaxCalculatorService>();
		var brandRepo = _serviceProvider.GetRequiredService<VehicleTracker.DataAccess.Repositories.VehicleBrandRepository>();

		// YENİ EKLENEN SATIR: Model Repository'i de DI Container'dan çekiyoruz
		var modelRepo = _serviceProvider.GetRequiredService<VehicleTracker.DataAccess.Repositories.VehicleModelRepository>();

		// 3. PARAMETREYİ EKLİYORUZ: modelRepo
		using var form = new SimulatorForm(taxCalc, brandRepo, modelRepo);
		form.ShowDialog(this);
	}

	private void SetStatus(string message, Color color)
	{
		_lblSyncStatus.Text = message;
		_lblSyncStatus.ForeColor = color;
	}

	private void SetUiEnabled(bool enabled)
	{
		_btnAddVehicle.Enabled = enabled;
		_btnRefresh.Enabled = enabled;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
			_syncManager.SyncCompleted -= OnSyncCompleted;

		base.Dispose(disposing);
	}
}