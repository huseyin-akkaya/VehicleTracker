using VehicleTracker.Business.Services;
using VehicleTracker.DataAccess.Repositories;
using VehicleTracker.Entities;

namespace VehicleTracker.UI;

/// <summary>
/// Kullanıcının seçtiği araç modeli, üretim yılı ve tahmini kullanım parametrelerine göre yıllık ortalama giderleri (yakıt, bakım, sigorta, vergi) simüle eden form.
/// Veritabanına herhangi bir kayıt yapmaz, sadece bilgilendirme amaçlı "What-If" (Ne Olurdu) analizi sunar.
/// </summary>
public class SimulatorForm : Form
{
	private readonly TaxCalculatorService _taxCalculator;
	private readonly VehicleBrandRepository _brandRepository;
	private readonly VehicleModelRepository _modelRepository;

	private const decimal InspectionFee = 1_200m;
	private const decimal AnnualMaintenanceBase = 8_000m;

	private Panel _headerPanel = null!;
	private GroupBox _grpInput = null!;
	private TableLayoutPanel _inputLayout = null!;
	private ComboBox _cmbBrand = null!;
	private bool _isBinding = false;
	private ComboBox _cmbModel = null!;
	private NumericUpDown _numYear = null!;
	private NumericUpDown _numAnnualKm = null!;
	private NumericUpDown _numFuelPrice = null!;
	private Button _btnCalculate = null!;

	private GroupBox _grpResult = null!;
	private Panel _resultPanel = null!;
	private Label _lblNoResult = null!;

	public SimulatorForm(
		TaxCalculatorService taxCalculator,
		VehicleBrandRepository brandRepository,
		VehicleModelRepository modelRepository)
	{
		_taxCalculator = taxCalculator;
		_brandRepository = brandRepository;
		_modelRepository = modelRepository;

		InitializeComponents();
	}

	/// <summary>
	/// Simülatör formunun giriş alanlarını (Input) ve sonuç panellerini (Result) programatik olarak oluşturur.
	/// </summary>
	private void InitializeComponents()
	{
		Text = "Tahmini Maliyet Simülatörü";
		Size = new Size(580, 680);
		MinimumSize = new Size(520, 620);
		MaximizeBox = false;
		StartPosition = FormStartPosition.CenterParent;
		BackColor = Color.FromArgb(245, 245, 245);
		Font = new Font("Segoe UI", 9.5f);

		_headerPanel = new Panel
		{ Dock = DockStyle.Top, Height = 56, BackColor = Color.FromArgb(44, 62, 80) };
		_headerPanel.Controls.Add(new Label
		{
			Text = "🧮  Tahmini Yıllık Maliyet Simülatörü",
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(18, 14)
		});

		_grpInput = new GroupBox
		{
			Text = "Araç Bilgileri",
			Dock = DockStyle.Fill,
			Padding = new Padding(14, 8, 14, 8),
			ForeColor = Color.FromArgb(44, 62, 80),
			Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
		};

		_inputLayout = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 2,
			RowCount = 6,
			BackColor = Color.Transparent,
			Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
		};
		_inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
		_inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
		for (int i = 0; i < 5; i++)
			_inputLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
		_inputLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

		_cmbBrand = new ComboBox
		{
			DropDownStyle = ComboBoxStyle.DropDownList,
			DisplayMember = "Name",
			ValueMember = "Id",
			Dock = DockStyle.Fill,
			BackColor = Color.White,
			FlatStyle = FlatStyle.Flat
		};

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

		_numYear = new NumericUpDown
		{
			Dock = DockStyle.Fill,
			Minimum = 1950,
			Maximum = DateTime.Now.Year,
			Value = DateTime.Now.Year - 3,
			BackColor = Color.White
		};

		_numAnnualKm = new NumericUpDown
		{
			Dock = DockStyle.Fill,
			Minimum = 1_000,
			Maximum = 500_000,
			Value = 15_000,
			ThousandsSeparator = true,
			BackColor = Color.White
		};

		_numFuelPrice = new NumericUpDown
		{
			Dock = DockStyle.Fill,
			Minimum = 1,
			Maximum = 999,
			Value = 42,
			DecimalPlaces = 2,
			BackColor = Color.White
		};

		AddInputRow(0, "Marka *", _cmbBrand);
		AddInputRow(1, "Model *", _cmbModel);
		AddInputRow(2, "Üretim Yılı", _numYear);
		AddInputRow(3, "Yıllık Tahmini Km", _numAnnualKm);
		AddInputRow(4, "Yakıt Fiyatı (₺/lt)", _numFuelPrice);

		_grpInput.Controls.Add(_inputLayout);

		_btnCalculate = new Button
		{
			Text = "🧮  Hesapla",
			Height = 42,
			Dock = DockStyle.Fill,
			BackColor = Color.FromArgb(41, 128, 185),
			ForeColor = Color.White,
			FlatStyle = FlatStyle.Flat,
			Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
			Cursor = Cursors.Hand
		};
		_btnCalculate.FlatAppearance.BorderSize = 0;

		_grpResult = new GroupBox
		{
			Text = "Tahmini Yıllık Maliyet Raporu",
			Dock = DockStyle.Fill,
			Padding = new Padding(14, 10, 14, 10),
			ForeColor = Color.FromArgb(44, 62, 80),
			Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
		};
		_resultPanel = new Panel
		{ Dock = DockStyle.Fill, BackColor = Color.White, AutoScroll = true };
		_lblNoResult = new Label
		{
			Text = "Marka ve model seçerek 'Hesapla' butonuna basın.",
			ForeColor = Color.FromArgb(150, 150, 170),
			Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
			AutoSize = false,
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleCenter
		};
		_resultPanel.Controls.Add(_lblNoResult);
		_grpResult.Controls.Add(_resultPanel);

		var resultWrapper = new Panel
		{ Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 12) };
		_grpResult.Dock = DockStyle.Fill;
		resultWrapper.Controls.Add(_grpResult);

		var calcWrapper = new Panel
		{ Dock = DockStyle.Top, Height = 56, Padding = new Padding(16, 7, 16, 7) };
		calcWrapper.Controls.Add(_btnCalculate);

		var inputWrapper = new Panel
		{ Dock = DockStyle.Top, Height = 258, Padding = new Padding(16, 12, 16, 0) };
		_grpInput.Dock = DockStyle.Fill;
		inputWrapper.Controls.Add(_grpInput);

		Controls.Add(resultWrapper);
		Controls.Add(calcWrapper);
		Controls.Add(inputWrapper);
		Controls.Add(_headerPanel);

		Load += async (_, _) => await LoadBrandsAsync();
		_cmbBrand.SelectedIndexChanged += async (_, _) => await OnBrandChangedAsync();
		_btnCalculate.Click += async (_, _) => await OnCalculateAsync();
	}

	/// <summary>
	/// Form yüklendiğinde veritabanındaki marka listesini çekerek ilk açılır listeyi (ComboBox) doldurur.
	/// Event tetiklenmelerini engellemek için _isBinding bayrağı (flag) kullanılır.
	/// </summary>
	private async Task LoadBrandsAsync()
	{
		try
		{
			_isBinding = true;
			var brands = await _brandRepository.GetAllAsync();
			_cmbBrand.DataSource = brands.ToList();
			_cmbBrand.DisplayMember = "Name";
			_cmbBrand.ValueMember = "Id";
			_cmbBrand.SelectedIndex = -1;
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Markalar yüklenemedi:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}
		finally
		{
			_isBinding = false;
		}
	}

	/// <summary>
	/// Kullanıcı bir marka seçtiğinde (Kaskad Dropdown mantığı) tetiklenir ve seçilen markaya ait modelleri getirerek ikinci açılır listeyi doldurur.
	/// Çevrimdışı durumlarda model bulunamazsa kullanıcıyı uyarır.
	/// </summary>
	private async Task OnBrandChangedAsync()
	{
		_cmbModel.DataSource = null;
		_cmbModel.Enabled = false;
		_resultPanel.Controls.Clear();
		_resultPanel.Controls.Add(_lblNoResult);

		if (_isBinding) return;

		if (_cmbBrand.SelectedItem is not VehicleBrand selectedBrand)
			return;

		try
		{
			var models = await _modelRepository.GetByBrandIdAsync(selectedBrand.Id);

			if (models.Count == 0)
			{
				MessageBox.Show(
					$"'{selectedBrand.Name}' için henüz model verisi yok.\n" +
					"Senkronizasyon tamamlandıktan sonra tekrar deneyin.",
					"Model Bulunamadı",
					MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			_cmbModel.DataSource = models.ToList();
			_cmbModel.DisplayMember = "Name";
			_cmbModel.ValueMember = "Id";
			_cmbModel.SelectedIndex = 0;
			_cmbModel.Enabled = true;
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Modeller yüklenemedi:\n{ex.Message}",
				"Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}
	}

	/// <summary>
	/// Girilen parametrelere göre yakıt, bakım, Motorlu Taşıtlar Vergisi (MTV) ve kasko/sigorta maliyetlerini hesaplayarak sonucu ekrana çizer.
	/// Elektrikli araçlar için özel mantık içerir (yakıt maliyeti hesaplanmaz).
	/// </summary>
	private async Task OnCalculateAsync()
	{
		if (_cmbBrand.SelectedItem is not VehicleBrand selectedBrand)
		{
			MessageBox.Show("Lütfen bir marka seçin.", "Eksik Bilgi",
				MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}

		if (_cmbModel.SelectedItem is not VehicleModel selectedModel)
		{
			MessageBox.Show("Lütfen bir model seçin.", "Eksik Bilgi",
				MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}

		_btnCalculate.Enabled = false;
		_btnCalculate.Text = "⏳  Hesaplanıyor...";

		try
		{
			int year = (int)_numYear.Value;
			double annualKm = (double)_numAnnualKm.Value;
			decimal fuelPrice = _numFuelPrice.Value;
			bool isElectric = selectedModel.AvgFuelConsumption <= 0;

			decimal fuelBase = 0m;
			decimal fuelTax = 0m;
			decimal fuelTotal = 0m;

			// Elektrikli değilse yakıt maliyeti hesapla
			if (!isElectric)
			{
				fuelBase = (decimal)(annualKm / 100.0 * selectedModel.AvgFuelConsumption) * fuelPrice;
				fuelTax = await _taxCalculator.CalculateTaxAsync(fuelBase, "Yakıt", year);
				fuelTotal = fuelBase + fuelTax;
			}

			// Bakım maliyeti: Ağırlık 1200kg üzerindeyse ağırlığa göre çarpan ekle
			decimal weightFactor = 1m + (selectedModel.WeightKg > 1200
				? (decimal)(selectedModel.WeightKg - 1200) / 10000m
				: 0m);

			decimal maintBase = Math.Round(AnnualMaintenanceBase * weightFactor, 0);
			decimal maintTax = await _taxCalculator.CalculateTaxAsync(maintBase, "Bakım", year);
			decimal maintTotal = maintBase + maintTax;

			// Vergi ve Sigorta Hesaplamaları
			decimal mtvBase = CalculateMtvByEngine(selectedModel.EngineVolume, year);
			decimal mtvTax = await _taxCalculator.CalculateTaxAsync(mtvBase, "Vergi", year);
			decimal mtvTotal = mtvBase + mtvTax;

			decimal insuranceBase = CalculateInsuranceBase(year);
			decimal insuranceTax = await _taxCalculator.CalculateTaxAsync(insuranceBase, "Sigorta", year);
			decimal insuranceTotal = insuranceBase + insuranceTax;

			// Yıllıklandırılmış muayene ücreti (2 yılda 1 olduğu için /2)
			decimal annualInspection = InspectionFee / 2m;

			decimal grandTotal = fuelTotal + maintTotal + mtvTotal + insuranceTotal + annualInspection;

			// Sonuç nesnesini oluştur ve UI tetikleyicisine gönder
			RenderResult(new SimulationResult(
				ModelLabel: $"{selectedBrand.Name} {selectedModel.Name}",
				Year: year,
				AnnualKm: annualKm,
				EngineVolume: selectedModel.EngineVolume,
				WeightKg: selectedModel.WeightKg,
				AvgConsumption: selectedModel.AvgFuelConsumption,
				IsElectric: isElectric,
				FuelBase: fuelBase,
				FuelTax: fuelTax,
				FuelTotal: fuelTotal,
				MaintBase: maintBase,
				MaintTax: maintTax,
				MaintTotal: maintTotal,
				MtvBase: mtvBase,
				MtvTax: mtvTax,
				MtvTotal: mtvTotal,
				InsuranceBase: insuranceBase,
				InsuranceTax: insuranceTax,
				InsuranceTotal: insuranceTotal,
				AnnualInspection: annualInspection,
				GrandTotal: grandTotal
			));
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Hesaplama hatası:\n{ex.Message}",
				"Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
		finally
		{
			_btnCalculate.Enabled = true;
			_btnCalculate.Text = "🧮  Hesapla";
		}
	}

	/// <summary>
	/// Hesaplanan SimulationResult verisini kullanarak alt panele (ResultPanel) dinamik bir rapor arayüzü çizer.
	/// Her bir gider kalemi için renkli bilgi satırları (Paneller) oluşturur.
	/// </summary>
	private void RenderResult(SimulationResult r)
	{
		_resultPanel.Controls.Clear();
		_resultPanel.AutoScrollPosition = Point.Empty;

		int age = DateTime.Now.Year - r.Year;

		var flow = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false,
			AutoScroll = true,
			Padding = new Padding(8),
			BackColor = Color.White
		};

		flow.Controls.Add(MakeHeader(
			$"{r.ModelLabel}  ·  {r.Year} model  ·  " +
			$"{r.AnnualKm:N0} km/yıl  ·  " +
			(r.IsElectric ? "⚡ Elektrikli" : $"{r.EngineVolume:F1}L / {r.AvgConsumption:F1}lt/100km")));

		flow.Controls.Add(MakeInfoBand(
			$"Ağırlık: {r.WeightKg:N0} kg   |   " +
			(r.IsElectric
				? "Elektrikli araç — yakıt gideri hesaplanmadı"
				: $"Yakıt tüketimi: {r.AvgConsumption:F1} lt/100km")));

		flow.Controls.Add(MakeDivider());

		if (!r.IsElectric)
			flow.Controls.Add(MakeResultRow("⛽ Yakıt",
				r.FuelBase, r.FuelTax, r.FuelTotal, Color.FromArgb(255, 243, 220)));
		else
			flow.Controls.Add(MakeInfoBand("⚡ Elektrikli araç — yakıt kalemi yok (şarj maliyeti hesaba katılmamıştır)"));

		flow.Controls.Add(MakeResultRow("🔧 Bakım",
			r.MaintBase, r.MaintTax, r.MaintTotal, Color.FromArgb(220, 237, 255)));
		flow.Controls.Add(MakeResultRow("📋 MTV / Vergi",
			r.MtvBase, r.MtvTax, r.MtvTotal, Color.FromArgb(255, 220, 220)));
		flow.Controls.Add(MakeResultRow("🛡️ Sigorta",
			r.InsuranceBase, r.InsuranceTax, r.InsuranceTotal, Color.FromArgb(220, 255, 235)));

		flow.Controls.Add(MakeDivider());

		flow.Controls.Add(MakeNote(
			$"🔍  Muayene: 2 yılda bir ~{InspectionFee:N0} ₺  →  yıllık karşılık: {r.AnnualInspection:N0} ₺"));

		if (age >= 16)
			flow.Controls.Add(MakeNote(
				$"⚠️  {age} yaşında araç — MTV ve bakım çarpanları yüksek uygulandı."));

		flow.Controls.Add(MakeDivider());
		flow.Controls.Add(MakeGrandTotal(r.GrandTotal));
		flow.Controls.Add(MakeNote(
			"* Bu hesaplama tahminidir, veritabanına kaydedilmemektedir."));

		_resultPanel.Controls.Add(flow);
	}

	private static Label MakeHeader(string text) => new()
	{
		Text = text,
		Width = 500,
		Height = 28,
		AutoSize = false,
		Font = new Font("Segoe UI", 9f, FontStyle.Bold),
		ForeColor = Color.FromArgb(44, 62, 80),
		TextAlign = ContentAlignment.MiddleLeft
	};

	private static Label MakeInfoBand(string text) => new()
	{
		Text = text,
		Width = 510,
		Height = 26,
		AutoSize = false,
		Font = new Font("Segoe UI", 8.5f),
		ForeColor = Color.FromArgb(80, 80, 110),
		BackColor = Color.FromArgb(240, 242, 250),
		TextAlign = ContentAlignment.MiddleLeft,
		Padding = new Padding(6, 0, 0, 0),
		Margin = new Padding(0, 2, 0, 2)
	};

	private static Panel MakeResultRow(
		string label, decimal baseAmt, decimal tax, decimal total, Color bg)
	{
		var row = new Panel
		{
			Width = 510,
			Height = 64,
			BackColor = bg,
			Margin = new Padding(0, 3, 0, 0),
			Padding = new Padding(10, 6, 10, 6)
		};
		row.Controls.Add(new Label
		{
			Text = $"{label}\nGider: {baseAmt:N2} ₺   |   Vergi payı: {tax:N2} ₺",
			Font = new Font("Segoe UI", 8.5f),
			ForeColor = Color.FromArgb(50, 50, 70),
			AutoSize = false,
			Size = new Size(320, 52),
			Location = new Point(10, 6)
		});
		row.Controls.Add(new Label
		{
			Text = $"{total:N2} ₺",
			Font = new Font("Segoe UI", 12f, FontStyle.Bold),
			ForeColor = Color.FromArgb(30, 30, 60),
			AutoSize = false,
			Size = new Size(150, 52),
			Location = new Point(340, 6),
			TextAlign = ContentAlignment.MiddleRight
		});
		return row;
	}

	private static Label MakeGrandTotal(decimal total) => new()
	{
		Text = $"💰  Tahmini Toplam Yıllık Maliyet:  {total:N2} ₺",
		Font = new Font("Segoe UI", 11f, FontStyle.Bold),
		ForeColor = Color.White,
		BackColor = Color.FromArgb(44, 62, 80),
		AutoSize = false,
		Width = 510,
		Height = 40,
		TextAlign = ContentAlignment.MiddleCenter,
		Margin = new Padding(0, 6, 0, 6)
	};

	private static Label MakeNote(string text) => new()
	{
		Text = text,
		Width = 510,
		Height = 30,
		AutoSize = false,
		Font = new Font("Segoe UI", 8f, FontStyle.Italic),
		ForeColor = Color.FromArgb(120, 120, 140),
		TextAlign = ContentAlignment.MiddleLeft
	};

	private static Panel MakeDivider() => new()
	{
		Width = 510,
		Height = 1,
		BackColor = Color.FromArgb(210, 210, 220),
		Margin = new Padding(0, 5, 0, 5)
	};

	/// <summary>
	/// Motor hacmine ve araç yaşına göre Türkiye standartlarına (MTV) benzer tahmini bir vergi katsayısı hesaplar.
	/// </summary>
	private static decimal CalculateMtvByEngine(double engineVolume, int vehicleYear)
	{
		int age = DateTime.Now.Year - vehicleYear;

		decimal baseByEngine = engineVolume switch
		{
			<= 0 => 3_500m,
			<= 0.25 => 1_200m,
			<= 0.65 => 2_500m,
			<= 1.3 => 4_800m,
			<= 1.6 => 8_200m,
			<= 2.0 => 14_500m,
			<= 2.5 => 22_000m,
			_ => 35_000m
		};

		decimal ageFactor = age switch
		{
			<= 3 => 1.00m,
			<= 7 => 0.80m,
			<= 11 => 0.65m,
			<= 15 => 0.50m,
			_ => 0.35m
		};

		return Math.Round(baseByEngine * ageFactor, 0);
	}

	private static decimal CalculateInsuranceBase(int vehicleYear)
	{
		int age = DateTime.Now.Year - vehicleYear;
		return age switch
		{
			<= 3 => 20_000m,
			<= 7 => 15_000m,
			<= 15 => 10_000m,
			_ => 7_000m
		};
	}

	private void AddInputRow(int row, string labelText, Control input)
	{
		var lbl = new Label
		{
			Text = labelText,
			AutoSize = false,
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleLeft,
			ForeColor = Color.FromArgb(60, 60, 80),
			Font = new Font("Segoe UI", 9.5f),
			Margin = new Padding(0, 6, 8, 0)
		};
		input.Margin = new Padding(0, 6, 0, 0);
		_inputLayout.Controls.Add(lbl, 0, row);
		_inputLayout.Controls.Add(input, 1, row);
	}
}

/// <summary>
/// Hesaplama sonuçlarını UI katmanına (RenderResult) taşımak için kullanılan hafif (lightweight) veri nesnesi (DTO).
/// </summary>
internal record SimulationResult(
	string ModelLabel,
	int Year,
	double AnnualKm,
	double EngineVolume,
	int WeightKg,
	double AvgConsumption,
	bool IsElectric,
	decimal FuelBase,
	decimal FuelTax,
	decimal FuelTotal,
	decimal MaintBase,
	decimal MaintTax,
	decimal MaintTotal,
	decimal MtvBase,
	decimal MtvTax,
	decimal MtvTotal,
	decimal InsuranceBase,
	decimal InsuranceTax,
	decimal InsuranceTotal,
	decimal AnnualInspection,
	decimal GrandTotal
);