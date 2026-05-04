// --- UI/Forms/VehicleForm.cs ---

using VehicleTracker.Business.Services;
using VehicleTracker.DataAccess.Repositories;
using VehicleTracker.Entities;

namespace VehicleTracker.UI;

public class VehicleForm : Form
{
	private readonly IVehicleService _vehicleService;
	private readonly VehicleBrandRepository _brandRepository;
	private readonly VehicleModelRepository _modelRepository;

	private Panel _headerPanel = null!;
	private TableLayoutPanel _layout = null!;
	private Panel _footerPanel = null!;

	private ComboBox _cmbBrand = null!;
	private ComboBox _cmbModel = null!;   // ← TextBox yerine
	private TextBox _txtPlate = null!;
	private NumericUpDown _numYear = null!;
	private NumericUpDown _numKm = null!;
	private Label _lblModelHint = null!;

	private Button _btnSave = null!;
	private Button _btnCancel = null!;

	public Vehicle? SavedVehicle { get; private set; }

	public VehicleForm(
		IVehicleService vehicleService,
		VehicleBrandRepository brandRepository,
		VehicleModelRepository modelRepository)
	{
		_vehicleService = vehicleService;
		_brandRepository = brandRepository;
		_modelRepository = modelRepository;

		InitializeComponents();
	}

	private void InitializeComponents()
	{
		Text = "Yeni Araç Ekle";
		Size = new Size(460, 420);
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MaximizeBox = false;
		StartPosition = FormStartPosition.CenterParent;
		BackColor = Color.FromArgb(245, 245, 245);
		Font = new Font("Segoe UI", 9.5f);

		// ── Header ────────────────────────────────────────────
		_headerPanel = new Panel
		{
			Dock = DockStyle.Top,
			Height = 56,
			BackColor = Color.FromArgb(30, 30, 60)
		};
		_headerPanel.Controls.Add(new Label
		{
			Text = "🚗  Yeni Araç Ekle",
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 12f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(20, 14)
		});

		// ── Layout ────────────────────────────────────────────
		_layout = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 2,
			RowCount = 6,
			Padding = new Padding(24, 16, 24, 8),
			BackColor = Color.Transparent
		};
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64f));
		for (int i = 0; i < 5; i++)
			_layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

		// Plaka
		_txtPlate = new TextBox
		{
			CharacterCasing = CharacterCasing.Upper,
			MaxLength = 20,
			PlaceholderText = "06 ABC 123",
			Dock = DockStyle.Fill,
			BackColor = Color.White
		};

		// Marka
		_cmbBrand = new ComboBox
		{
			DropDownStyle = ComboBoxStyle.DropDownList,
			DisplayMember = "Name",
			ValueMember = "Id",
			Dock = DockStyle.Fill,
			BackColor = Color.White,
			FlatStyle = FlatStyle.Flat
		};

		// Model — başta pasif, marka seçilince dolacak
		_cmbModel = new ComboBox
		{
			DropDownStyle = ComboBoxStyle.DropDownList,
			DisplayMember = "Name",
			ValueMember = "Id",
			Dock = DockStyle.Fill,
			BackColor = Color.White,
			FlatStyle = FlatStyle.Flat,
			Enabled = false
		};

		// Model ComboBox'ın altında küçük ipucu metni
		_lblModelHint = new Label
		{
			Text = "Önce marka seçin",
			ForeColor = Color.FromArgb(160, 160, 180),
			Font = new Font("Segoe UI", 8f, FontStyle.Italic),
			AutoSize = true,
			Visible = true
		};

		// Üretim Yılı
		_numYear = new NumericUpDown
		{
			Minimum = 1950,
			Maximum = DateTime.Now.Year + 1,
			Value = DateTime.Now.Year,
			Dock = DockStyle.Fill,
			BackColor = Color.White
		};

		// Kilometre
		_numKm = new NumericUpDown
		{
			Minimum = 0,
			Maximum = 9_999_999,
			Value = 0,
			ThousandsSeparator = true,
			Dock = DockStyle.Fill,
			BackColor = Color.White
		};

		AddRow(0, MakeLabel("Plaka *"), _txtPlate);
		AddRow(1, MakeLabel("Marka *"), _cmbBrand);
		AddRow(2, MakeLabel("Model *"), _cmbModel);
		AddRow(3, MakeLabel("Üretim Yılı *"), _numYear);
		AddRow(4, MakeLabel("Başlangıç Km"), _numKm);

		// Model ipucu — 3. satırın giriş hücresine bindiriyoruz
		_layout.Controls.Add(_lblModelHint, 1, 2);

		// ── Footer ────────────────────────────────────────────
		_footerPanel = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = 58,
			BackColor = Color.FromArgb(235, 235, 240)
		};

		_btnSave = new Button
		{
			Text = "💾  Kaydet",
			Size = new Size(120, 36),
			BackColor = Color.FromArgb(72, 199, 142),
			ForeColor = Color.White,
			FlatStyle = FlatStyle.Flat,
			Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
			Cursor = Cursors.Hand
		};
		_btnSave.FlatAppearance.BorderSize = 0;

		_btnCancel = new Button
		{
			Text = "İptal",
			Size = new Size(90, 36),
			BackColor = Color.FromArgb(200, 200, 210),
			ForeColor = Color.FromArgb(50, 50, 60),
			FlatStyle = FlatStyle.Flat,
			Cursor = Cursors.Hand
		};
		_btnCancel.FlatAppearance.BorderSize = 0;

		_footerPanel.Resize += (_, _) =>
		{
			_btnCancel.Left = _footerPanel.ClientSize.Width - _btnCancel.Width - 16;
			_btnSave.Left = _btnCancel.Left - _btnSave.Width - 8;
			_btnSave.Top = _btnCancel.Top =
				(_footerPanel.ClientSize.Height - _btnSave.Height) / 2;
		};
		_footerPanel.Controls.AddRange(new Control[] { _btnSave, _btnCancel });

		Controls.Add(_layout);
		Controls.Add(_footerPanel);
		Controls.Add(_headerPanel);

		AcceptButton = _btnSave;
		CancelButton = _btnCancel;

		Load += async (_, _) => await LoadBrandsAsync();
		_cmbBrand.SelectedIndexChanged += async (_, _) => await OnBrandChangedAsync();
		_btnSave.Click += async (_, _) => await OnSaveClickedAsync();
		_btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
	}

	// ── Marka yükle ───────────────────────────────────────────
	private async Task LoadBrandsAsync()
	{
		try
		{
			var brands = await _brandRepository.GetAllAsync();
			_cmbBrand.DataSource = brands.ToList();
			_cmbBrand.DisplayMember = "Name";
			_cmbBrand.ValueMember = "Id";
			_cmbBrand.SelectedIndex = -1;
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Markalar yüklenemedi:\n{ex.Message}",
				"Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}
	}

	// ── Marka seçilince modelleri doldur ──────────────────────
	private async Task OnBrandChangedAsync()
	{
		_cmbModel.DataSource = null;
		_cmbModel.Enabled = false;
		_lblModelHint.Text = "Yükleniyor...";
		_lblModelHint.Visible = true;

		if (_cmbBrand.SelectedItem is not VehicleBrand brand)
		{
			_lblModelHint.Text = "Önce marka seçin";
			return;
		}

		try
		{
			var models = await _modelRepository.GetByBrandIdAsync(brand.Id);

			if (models.Count == 0)
			{
				_lblModelHint.Text = "Bu marka için model verisi yok";
				return;
			}

			_cmbModel.DataSource = models.ToList();
			_cmbModel.DisplayMember = "Name";
			_cmbModel.ValueMember = "Id";
			_cmbModel.SelectedIndex = 0;
			_cmbModel.Enabled = true;
			_lblModelHint.Visible = false;
		}
		catch (Exception ex)
		{
			_lblModelHint.Text = "Modeller yüklenemedi";
			MessageBox.Show($"Model listesi yüklenemedi:\n{ex.Message}",
				"Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}
	}

	// ── Kaydet ────────────────────────────────────────────────
	private async Task OnSaveClickedAsync()
	{
		if (string.IsNullOrWhiteSpace(_txtPlate.Text))
		{
			MessageBox.Show("Plaka boş bırakılamaz.", "Eksik Bilgi",
				MessageBoxButtons.OK, MessageBoxIcon.Warning);
			_txtPlate.Focus(); return;
		}
		if (_cmbBrand.SelectedItem is not VehicleBrand brand)
		{
			MessageBox.Show("Lütfen bir marka seçin.", "Eksik Bilgi",
				MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}
		if (_cmbModel.SelectedItem is not VehicleModel model)
		{
			MessageBox.Show("Lütfen bir model seçin.", "Eksik Bilgi",
				MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}

		_btnSave.Enabled = false;
		_btnSave.Text = "⏳  Kaydediliyor...";

		try
		{
			var vehicle = new Vehicle
			{
				Plate = _txtPlate.Text.Trim(),
				BrandId = brand.Id,
				ModelId = model.Id,
				Year = (int)_numYear.Value,
				CurrentKm = (double)_numKm.Value
			};

			await _vehicleService.AddVehicleAsync(vehicle);
			SavedVehicle = vehicle;
			DialogResult = DialogResult.OK;
			Close();
		}
		catch (InvalidOperationException ex)
		{
			MessageBox.Show(ex.Message, "Kayıt Hatası",
				MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Beklenmedik hata:\n{ex.Message}", "Hata",
				MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
		finally
		{
			_btnSave.Enabled = true;
			_btnSave.Text = "💾  Kaydet";
		}
	}

	private static Label MakeLabel(string text) => new()
	{
		Text = text,
		AutoSize = false,
		Dock = DockStyle.Fill,
		TextAlign = ContentAlignment.MiddleLeft,
		ForeColor = Color.FromArgb(60, 60, 80)
	};

	private void AddRow(int row, Control lbl, Control input)
	{
		lbl.Margin = new Padding(0, 10, 8, 0);
		input.Margin = new Padding(0, 10, 0, 0);
		_layout.Controls.Add(lbl, 0, row);
		_layout.Controls.Add(input, 1, row);
	}
}