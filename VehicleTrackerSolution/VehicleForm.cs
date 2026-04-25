// --- UI/Forms/VehicleForm.cs ---

using VehicleTracker.Business.Services;
using VehicleTracker.DataAccess.Repositories;
using VehicleTracker.Entities;

namespace VehicleTracker.UI;

public class VehicleForm : Form
{
	// ── Bağımlılıklar ─────────────────────────────────────────
	private readonly IVehicleService _vehicleService;
	private readonly VehicleBrandRepository _brandRepository;

	// ── UI Bileşenleri ────────────────────────────────────────
	private TableLayoutPanel _layout = null!;
	private Label _lblPlate = null!;
	private Label _lblBrand = null!;
	private Label _lblModel = null!;
	private Label _lblYear = null!;
	private Label _lblKm = null!;
	private TextBox _txtPlate = null!;
	private ComboBox _cmbBrand = null!;
	private TextBox _txtModel = null!;
	private NumericUpDown _numYear = null!;
	private NumericUpDown _numKm = null!;
	private Button _btnSave = null!;
	private Button _btnCancel = null!;
	private Panel _headerPanel = null!;
	private Label _lblHeader = null!;
	private Panel _footerPanel = null!;
	private PictureBox _loadingIcon = null!;
	private Label _lblLoading = null!;

	/// <summary>
	/// Başarılı kayıt sonrası eklenen aracı dışarıya taşır.
	/// MainForm bu property'i okuyarak grid'i güncelleyebilir.
	/// </summary>
	public Vehicle? SavedVehicle { get; private set; }

	public VehicleForm(
		IVehicleService vehicleService,
		VehicleBrandRepository brandRepository)
	{
		_vehicleService = vehicleService;
		_brandRepository = brandRepository;

		InitializeComponents();
	}

	// ─────────────────────────────────────────────────────────
	//  Bileşen oluşturma
	// ─────────────────────────────────────────────────────────
	private void InitializeComponents()
	{
		// ── Form ayarları ─────────────────────────────────────
		Text = "Yeni Araç Ekle";
		Size = new Size(460, 440);
		MinimumSize = new Size(420, 420);
		MaximizeBox = false;
		StartPosition = FormStartPosition.CenterParent;
		BackColor = Color.FromArgb(245, 245, 245);
		Font = new Font("Segoe UI", 9.5f);
		FormBorderStyle = FormBorderStyle.FixedDialog;

		// ── Başlık paneli ─────────────────────────────────────
		_headerPanel = new Panel
		{
			Dock = DockStyle.Top,
			Height = 56,
			BackColor = Color.FromArgb(30, 30, 60)
		};

		_lblHeader = new Label
		{
			Text = "🚗  Yeni Araç Ekle",
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 12f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(20, 14)
		};
		_headerPanel.Controls.Add(_lblHeader);

		// ── Ana form alanları (TableLayoutPanel) ──────────────
		_layout = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 2,
			RowCount = 6,
			Padding = new Padding(24, 18, 24, 8),
			BackColor = Color.Transparent
		};

		// Sütun genişlikleri: etiket %35, giriş %65
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));

		// Satır yükseklikleri
		for (int i = 0; i < 5; i++)
			_layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // dolgu satırı

		// Plaka
		_lblPlate = CreateLabel("Plaka *");
		_txtPlate = new TextBox
		{
			CharacterCasing = CharacterCasing.Upper,
			MaxLength = 20,
			PlaceholderText = "06 ABC 123"
		};
		StyleTextBox(_txtPlate);

		// Marka
		_lblBrand = CreateLabel("Marka *");
		_cmbBrand = new ComboBox
		{
			DropDownStyle = ComboBoxStyle.DropDownList,
			DisplayMember = "Name",
			ValueMember = "Id",
			FlatStyle = FlatStyle.Flat,
			BackColor = Color.White
		};
		StyleComboBox(_cmbBrand);

		// Model
		_lblModel = CreateLabel("Model *");
		_txtModel = new TextBox { MaxLength = 100, PlaceholderText = "Corolla, Focus..." };
		StyleTextBox(_txtModel);

		// Yıl
		_lblYear = CreateLabel("Üretim Yılı *");
		_numYear = new NumericUpDown
		{
			Minimum = 1950,
			Maximum = DateTime.Now.Year + 1,
			Value = DateTime.Now.Year,
			ThousandsSeparator = false,
			BackColor = Color.White
		};
		StyleNumeric(_numYear);

		// Kilometre
		_lblKm = CreateLabel("Başlangıç Km");
		_numKm = new NumericUpDown
		{
			Minimum = 0,
			Maximum = 9_999_999,
			Value = 0,
			ThousandsSeparator = true,
			BackColor = Color.White
		};
		StyleNumeric(_numKm);

		// Layout'a ekle (satır satır)
		AddRow(0, _lblPlate, _txtPlate);
		AddRow(1, _lblBrand, _cmbBrand);
		AddRow(2, _lblModel, _txtModel);
		AddRow(3, _lblYear, _numYear);
		AddRow(4, _lblKm, _numKm);

		// ── Yükleniyor göstergesi (marka listesi çekilirken) ──
		_lblLoading = new Label
		{
			Text = "⏳  Markalar yükleniyor...",
			ForeColor = Color.FromArgb(120, 120, 140),
			Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
			AutoSize = true,
			Visible = false
		};
		_layout.Controls.Add(_lblLoading, 1, 1); // Marka ComboBox'ın üstüne binecek

		// ── Alt panel (butonlar) ──────────────────────────────
		_footerPanel = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = 60,
			BackColor = Color.FromArgb(235, 235, 240),
			Padding = new Padding(16, 12, 16, 12)
		};

		_btnCancel = new Button
		{
			Text = "İptal",
			Size = new Size(100, 36),
			BackColor = Color.FromArgb(200, 200, 210),
			ForeColor = Color.FromArgb(50, 50, 60),
			FlatStyle = FlatStyle.Flat,
			Cursor = Cursors.Hand,
			Anchor = AnchorStyles.Right | AnchorStyles.Top
		};
		_btnCancel.FlatAppearance.BorderSize = 0;

		_btnSave = new Button
		{
			Text = "💾  Kaydet",
			Size = new Size(120, 36),
			BackColor = Color.FromArgb(72, 199, 142),
			ForeColor = Color.White,
			FlatStyle = FlatStyle.Flat,
			Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
			Cursor = Cursors.Hand,
			Anchor = AnchorStyles.Right | AnchorStyles.Top
		};
		_btnSave.FlatAppearance.BorderSize = 0;

		// Butonları sağa yasla
		_footerPanel.Resize += (_, _) =>
		{
			_btnCancel.Left = _footerPanel.ClientSize.Width - _btnCancel.Width - 16;
			_btnSave.Left = _btnCancel.Left - _btnSave.Width - 8;
			_btnCancel.Top = (_footerPanel.ClientSize.Height - _btnCancel.Height) / 2;
			_btnSave.Top = _btnCancel.Top;
		};
		_footerPanel.Controls.AddRange(new Control[] { _btnSave, _btnCancel });

		// ── Controls'a ekleme ─────────────────────────────────
		Controls.Add(_layout);
		Controls.Add(_footerPanel);
		Controls.Add(_headerPanel);

		// ── Event bağlantıları ────────────────────────────────
		_btnSave.Click += async (_, _) => await OnSaveClickedAsync();
		_btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
		Load += async (_, _) => await LoadBrandsAsync();

		// Enter → kaydet, Escape → iptal
		AcceptButton = _btnSave;
		CancelButton = _btnCancel;
	}

	// ─────────────────────────────────────────────────────────
	//  Marka listesini veritabanından yükle
	// ─────────────────────────────────────────────────────────
	private async Task LoadBrandsAsync()
	{
		SetBrandLoadingState(true);

		try
		{
			var brands = await _brandRepository.GetAllAsync();

			_cmbBrand.DataSource = null;
			_cmbBrand.DataSource = brands.ToList();
			_cmbBrand.DisplayMember = "Name";
			_cmbBrand.ValueMember = "Id";

			if (_cmbBrand.Items.Count == 0)
			{
				// Hiç marka yoksa kullanıcıyı bilgilendir
				MessageBox.Show(
					"Henüz marka verisi bulunamadı.\n" +
					"Lütfen internet bağlantısını kontrol edin ve uygulamayı yeniden başlatın.",
					"Marka Listesi Boş",
					MessageBoxButtons.OK,
					MessageBoxIcon.Information
				);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(
				$"Marka listesi yüklenemedi:\n{ex.Message}",
				"Hata",
				MessageBoxButtons.OK,
				MessageBoxIcon.Warning
			);
		}
		finally
		{
			SetBrandLoadingState(false);
		}
	}

	private void SetBrandLoadingState(bool loading)
	{
		_cmbBrand.Visible = !loading;
		_lblLoading.Visible = loading;
		_btnSave.Enabled = !loading;
	}

	// ─────────────────────────────────────────────────────────
	//  Kaydetme işlemi
	// ─────────────────────────────────────────────────────────
	private async Task OnSaveClickedAsync()
	{
		if (!ValidateInputs(out string errorMessage))
		{
			MessageBox.Show(errorMessage, "Eksik Bilgi",
				MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}

		_btnSave.Enabled = false;
		_btnSave.Text = "⏳  Kaydediliyor...";

		try
		{
			var selectedBrand = (VehicleBrand)_cmbBrand.SelectedItem!;

			var vehicle = new Vehicle
			{
				Plate = _txtPlate.Text.Trim().ToUpperInvariant(),
				BrandId = selectedBrand.Id,
				ModelName = _txtModel.Text.Trim(),
				Year = (int)_numYear.Value,
				CurrentKm = (double)_numKm.Value,
			};

			await _vehicleService.AddVehicleAsync(vehicle);

			SavedVehicle = vehicle;
			DialogResult = DialogResult.OK;
			Close();
		}
		catch (InvalidOperationException ex)
		{
			// İş kuralı hatası (örn: aynı plaka)
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

	// ─────────────────────────────────────────────────────────
	//  Validasyon
	// ─────────────────────────────────────────────────────────
	private bool ValidateInputs(out string errorMessage)
	{
		if (string.IsNullOrWhiteSpace(_txtPlate.Text))
		{
			errorMessage = "Plaka alanı boş bırakılamaz.";
			_txtPlate.Focus();
			return false;
		}

		if (_cmbBrand.SelectedItem is null)
		{
			errorMessage = "Lütfen listeden bir marka seçin.";
			_cmbBrand.Focus();
			return false;
		}

		if (string.IsNullOrWhiteSpace(_txtModel.Text))
		{
			errorMessage = "Model adı boş bırakılamaz.";
			_txtModel.Focus();
			return false;
		}

		errorMessage = string.Empty;
		return true;
	}

	// ─────────────────────────────────────────────────────────
	//  Stil yardımcıları — tekrarlı kodu azaltır
	// ─────────────────────────────────────────────────────────
	private static Label CreateLabel(string text) => new()
	{
		Text = text,
		AutoSize = false,
		Dock = DockStyle.Fill,
		TextAlign = ContentAlignment.MiddleLeft,
		ForeColor = Color.FromArgb(60, 60, 80),
		Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
	};

	private static void StyleTextBox(TextBox tb)
	{
		tb.Dock = DockStyle.Fill;
		tb.BorderStyle = BorderStyle.FixedSingle;
		tb.BackColor = Color.White;
		tb.Height = 30;
	}

	private static void StyleComboBox(ComboBox cb)
	{
		cb.Dock = DockStyle.Fill;
		cb.BackColor = Color.White;
		cb.Height = 30;
	}

	private static void StyleNumeric(NumericUpDown nud)
	{
		nud.Dock = DockStyle.Fill;
		nud.Height = 30;
		nud.BackColor = Color.White;
	}

	private void AddRow(int row, Control label, Control input)
	{
		label.Margin = new Padding(0, 12, 8, 0);
		input.Margin = new Padding(0, 12, 0, 0);
		_layout.Controls.Add(label, 0, row);
		_layout.Controls.Add(input, 1, row);
	}
}