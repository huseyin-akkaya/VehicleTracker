// --- UI/Forms/SimulatorForm.cs ---

using VehicleTracker.Business.Services;
using VehicleTracker.DataAccess.Repositories;
using VehicleTracker.Entities;

namespace VehicleTracker.UI;

public class SimulatorForm : Form
{
	// ── Bağımlılıklar ─────────────────────────────────────────
	private readonly TaxCalculatorService _taxCalculator;
	private readonly VehicleBrandRepository _brandRepository;

	// ── Sabit hesap parametreleri ─────────────────────────────
	// Yakıt tüketimi: ortalama 7 lt/100km, güncel litre fiyatı
	private const double AvgFuelConsumptionPer100Km = 7.0;
	private const decimal FuelPricePerLitre = 42.0m;  // ₺ — kullanıcı değiştirebilir
	private const decimal AnnualMaintenanceBase = 8_000m; // ₺ temel bakım
	private const decimal InspectionFee = 1_200m; // ₺ iki yılda bir
	private const decimal OtvBase = 15_000m;// ₺ temsili ÖTV (yeni alımda)

	// ── Giriş bileşenleri ────────────────────────────────────
	private Panel _headerPanel = null!;
	private Label _lblHeader = null!;
	private GroupBox _grpInput = null!;
	private TableLayoutPanel _inputLayout = null!;
	private ComboBox _cmbBrand = null!;
	private TextBox _txtModel = null!;
	private NumericUpDown _numYear = null!;
	private NumericUpDown _numAnnualKm = null!;
	private NumericUpDown _numFuelPrice = null!;
	private Button _btnCalculate = null!;

	// ── Sonuç bileşenleri ────────────────────────────────────
	private GroupBox _grpResult = null!;
	private Panel _resultPanel = null!;
	private Label _lblNoResult = null!;

	public SimulatorForm(
		TaxCalculatorService taxCalculator,
		VehicleBrandRepository brandRepository)
	{
		_taxCalculator = taxCalculator;
		_brandRepository = brandRepository;

		InitializeComponents();
	}

	private void InitializeComponents()
	{
		// ── Form ──────────────────────────────────────────────
		Text = "Tahmini Maliyet Simülatörü";
		Size = new Size(580, 680);
		MinimumSize = new Size(520, 620);
		MaximizeBox = false;
		StartPosition = FormStartPosition.CenterParent;
		BackColor = Color.FromArgb(245, 245, 245);
		Font = new Font("Segoe UI", 9.5f);

		// ── Header ────────────────────────────────────────────
		_headerPanel = new Panel
		{
			Dock = DockStyle.Top,
			Height = 56,
			BackColor = Color.FromArgb(44, 62, 80)
		};
		_lblHeader = new Label
		{
			Text = "🧮  Tahmini Yıllık Maliyet Simülatörü",
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(18, 14)
		};
		_headerPanel.Controls.Add(_lblHeader);

		// ── Giriş GroupBox ────────────────────────────────────
		_grpInput = new GroupBox
		{
			Text = "Araç Bilgileri",
			Dock = DockStyle.Top,
			Height = 260,
			Padding = new Padding(14, 8, 14, 8),
			ForeColor = Color.FromArgb(44, 62, 80),
			Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
			Margin = new Padding(16, 12, 16, 0)
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

		// Model
		_txtModel = new TextBox
		{
			Dock = DockStyle.Fill,
			BackColor = Color.White,
			BorderStyle = BorderStyle.FixedSingle,
			PlaceholderText = "Corolla, Focus, Clio..."
		};

		// Üretim Yılı
		_numYear = new NumericUpDown
		{
			Dock = DockStyle.Fill,
			Minimum = 1950,
			Maximum = DateTime.Now.Year,
			Value = DateTime.Now.Year - 3,
			BackColor = Color.White
		};

		// Yıllık Tahmini Km
		_numAnnualKm = new NumericUpDown
		{
			Dock = DockStyle.Fill,
			Minimum = 1_000,
			Maximum = 500_000,
			Value = 15_000,
			ThousandsSeparator = true,
			BackColor = Color.White
		};

		// Yakıt Fiyatı (kullanıcı güncelleyebilir)
		_numFuelPrice = new NumericUpDown
		{
			Dock = DockStyle.Fill,
			Minimum = 1,
			Maximum = 999,
			Value = (decimal)FuelPricePerLitre,
			DecimalPlaces = 2,
			BackColor = Color.White
		};

		AddInputRow(0, "Marka", _cmbBrand);
		AddInputRow(1, "Model", _txtModel);
		AddInputRow(2, "Üretim Yılı", _numYear);
		AddInputRow(3, "Yıllık Tahmini Km", _numAnnualKm);
		AddInputRow(4, "Yakıt Fiyatı (₺/lt)", _numFuelPrice);

		_grpInput.Controls.Add(_inputLayout);

		// ── Hesapla Butonu ────────────────────────────────────
		_btnCalculate = new Button
		{
			Text = "🧮  Hesapla",
			Height = 42,
			Dock = DockStyle.Top,
			BackColor = Color.FromArgb(41, 128, 185),
			ForeColor = Color.White,
			FlatStyle = FlatStyle.Flat,
			Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
			Cursor = Cursors.Hand,
			Margin = new Padding(16, 10, 16, 0)
		};
		_btnCalculate.FlatAppearance.BorderSize = 0;

		// ── Sonuç GroupBox ────────────────────────────────────
		_grpResult = new GroupBox
		{
			Text = "Tahmini Yıllık Maliyet Raporu",
			Dock = DockStyle.Fill,
			Padding = new Padding(14, 10, 14, 10),
			ForeColor = Color.FromArgb(44, 62, 80),
			Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
		};

		_resultPanel = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = Color.White,
			AutoScroll = true
		};

		_lblNoResult = new Label
		{
			Text = "Araç bilgilerini girerek 'Hesapla' butonuna basın.",
			ForeColor = Color.FromArgb(150, 150, 170),
			Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
			AutoSize = false,
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleCenter
		};
		_resultPanel.Controls.Add(_lblNoResult);
		_grpResult.Controls.Add(_resultPanel);

		// ── Wrapper Panel (margin için) ───────────────────────
		var resultWrapper = new Panel
		{
			Dock = DockStyle.Fill,
			Padding = new Padding(16, 8, 16, 12)
		};
		resultWrapper.Controls.Add(_grpResult);

		var calcWrapper = new Panel
		{
			Dock = DockStyle.Top,
			Height = 56,
			Padding = new Padding(16, 7, 16, 7)
		};
		_btnCalculate.Dock = DockStyle.Fill;
		calcWrapper.Controls.Add(_btnCalculate);

		var inputWrapper = new Panel
		{
			Dock = DockStyle.Top,
			Height = 278,
			Padding = new Padding(16, 12, 16, 0)
		};
		_grpInput.Dock = DockStyle.Fill;
		inputWrapper.Controls.Add(_grpInput);

		// ── Yerleşim ──────────────────────────────────────────
		Controls.Add(resultWrapper);   // Fill
		Controls.Add(calcWrapper);     // Top (sondan önce)
		Controls.Add(inputWrapper);    // Top
		Controls.Add(_headerPanel);    // Top (en üstte)

		// ── Events ────────────────────────────────────────────
		_btnCalculate.Click += async (_, _) => await OnCalculateAsync();
		Load += async (_, _) => await LoadBrandsAsync();
	}

	// ─────────────────────────────────────────────────────────
	//  Marka yükle
	// ─────────────────────────────────────────────────────────
	private async Task LoadBrandsAsync()
	{
		try
		{
			var brands = await _brandRepository.GetAllAsync();
			_cmbBrand.DataSource = brands.ToList();
			_cmbBrand.DisplayMember = "Name";
			_cmbBrand.ValueMember = "Id";
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Markalar yüklenemedi:\n{ex.Message}",
				"Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}
	}

	// ─────────────────────────────────────────────────────────
	//  Hesaplama
	// ─────────────────────────────────────────────────────────
	private async Task OnCalculateAsync()
	{
		if (_cmbBrand.SelectedItem is null)
		{
			MessageBox.Show("Lütfen bir marka seçin.", "Eksik Bilgi",
				MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}

		_btnCalculate.Enabled = false;
		_btnCalculate.Text = "⏳  Hesaplanıyor...";

		try
		{
			var brand = (VehicleBrand)_cmbBrand.SelectedItem!;
			int year = (int)_numYear.Value;
			double annualKm = (double)_numAnnualKm.Value;
			decimal fuelPrice = _numFuelPrice.Value;

			// ── Yakıt hesabı ──────────────────────────────────
			// (yıllık km / 100) × tüketim × litre fiyatı
			decimal fuelBase = (decimal)(annualKm / 100.0 * AvgFuelConsumptionPer100Km) * fuelPrice;

			decimal fuelTax = await _taxCalculator.CalculateTaxAsync(fuelBase, "Yakıt", year);
			decimal fuelTotal = fuelBase + fuelTax;

			// ── Bakım hesabı ──────────────────────────────────
			// Araç yaşı arttıkça bakım maliyeti yükselir (AgeMultiplier TaxCalculator içinde)
			decimal maintTax = await _taxCalculator.CalculateTaxAsync(AnnualMaintenanceBase, "Bakım", year);
			decimal maintTotal = AnnualMaintenanceBase + maintTax;

			// ── Vergi / MTV (temsili) ─────────────────────────
			// Motor hacmi bilinmediği için yaşa dayalı sabit katsayı uygulanır
			decimal mtvBase = CalculateMtvBase(year);
			decimal mtvTax = await _taxCalculator.CalculateTaxAsync(mtvBase, "Vergi", year);
			decimal mtvTotal = mtvBase + mtvTax;

			// ── Sigorta tahmini ───────────────────────────────
			decimal insuranceBase = CalculateInsuranceBase(year);
			decimal insuranceTax = await _taxCalculator.CalculateTaxAsync(insuranceBase, "Sigorta", year);
			decimal insuranceTotal = insuranceBase + insuranceTax;

			// ── Muayene (iki yılda bir, yıllık karşılığı) ────
			decimal annualInspection = InspectionFee / 2m;

			// ── Toplam ────────────────────────────────────────
			decimal grandTotal = fuelTotal + maintTotal + mtvTotal + insuranceTotal + annualInspection;

			// ── Raporu göster ─────────────────────────────────
			string modelLabel = string.IsNullOrWhiteSpace(_txtModel.Text)
				? brand.Name
				: $"{brand.Name} {_txtModel.Text.Trim()}";

			RenderResult(new SimulationResult(
				ModelLabel: modelLabel,
				Year: year,
				AnnualKm: annualKm,
				FuelBase: fuelBase,
				FuelTax: fuelTax,
				FuelTotal: fuelTotal,
				MaintBase: AnnualMaintenanceBase,
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

	// ─────────────────────────────────────────────────────────
	//  Sonuç render
	// ─────────────────────────────────────────────────────────
	private void RenderResult(SimulationResult r)
	{
		_resultPanel.Controls.Clear();
		_resultPanel.AutoScrollPosition = Point.Empty;

		int age = DateTime.Now.Year - r.Year;

		// Tüm satırları bir FlowLayoutPanel'de üret
		var flow = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false,
			AutoScroll = true,
			Padding = new Padding(8, 8, 8, 8),
			BackColor = Color.White
		};

		// Başlık
		flow.Controls.Add(MakeResultHeader(
			$"{r.ModelLabel}  |  {r.Year} model  |  {r.AnnualKm:N0} km/yıl"));

		flow.Controls.Add(MakeDivider());

		// Satırlar
		flow.Controls.Add(MakeResultRow("⛽ Yakıt",
			r.FuelBase, r.FuelTax, r.FuelTotal, Color.FromArgb(255, 243, 220)));

		flow.Controls.Add(MakeResultRow("🔧 Bakım",
			r.MaintBase, r.MaintTax, r.MaintTotal, Color.FromArgb(220, 237, 255)));

		flow.Controls.Add(MakeResultRow("📋 MTV / Vergi",
			r.MtvBase, r.MtvTax, r.MtvTotal, Color.FromArgb(255, 220, 220)));

		flow.Controls.Add(MakeResultRow("🛡️ Sigorta",
			r.InsuranceBase, r.InsuranceTax, r.InsuranceTotal, Color.FromArgb(220, 255, 235)));

		flow.Controls.Add(MakeDivider());

		// Muayene notu
		flow.Controls.Add(MakeNoteLabel(
			$"🔍  Not: Muayene bedeli iki yılda bir yaklaşık {InspectionFee:N0} ₺'dir." +
			$" Yıllık karşılığı: {r.AnnualInspection:N0} ₺"));

		if (age >= 16)
			flow.Controls.Add(MakeNoteLabel(
				$"⚠️  {age} yaşında araç — MTV ve bakım çarpanları yüksek uygulanmıştır."));

		flow.Controls.Add(MakeDivider());

		// Genel Toplam
		flow.Controls.Add(MakeGrandTotalLabel(r.GrandTotal));

		flow.Controls.Add(MakeNoteLabel(
			"* Bu hesaplama tahminidir, veritabanına kaydedilmemektedir." +
			" Gerçek maliyetler yakıt fiyatı, sigorta poliçesi ve kullanıma göre değişir."));

		_resultPanel.Controls.Add(flow);
	}

	// ── Render yardımcıları ───────────────────────────────────

	private static Label MakeResultHeader(string text)
	{
		return new Label
		{
			Text = text,
			Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
			ForeColor = Color.FromArgb(44, 62, 80),
			AutoSize = false,
			Width = 500,
			Height = 28,
			TextAlign = ContentAlignment.MiddleLeft
		};
	}

	private static Panel MakeResultRow(
		string label,
		decimal baseAmt, decimal tax, decimal total,
		Color bgColor)
	{
		var row = new Panel
		{
			Width = 510,
			Height = 68,
			BackColor = bgColor,
			Margin = new Padding(0, 4, 0, 0),
			Padding = new Padding(10, 6, 10, 6)
		};

		// Sol: kategori adı + base + vergi
		var lblLeft = new Label
		{
			Text = $"{label}\nGider: {baseAmt:N2} ₺   |   Vergi Payı: {tax:N2} ₺",
			Font = new Font("Segoe UI", 8.5f),
			ForeColor = Color.FromArgb(50, 50, 70),
			AutoSize = false,
			Size = new Size(320, 56),
			Location = new Point(10, 6)
		};

		// Sağ: toplam
		var lblRight = new Label
		{
			Text = $"{total:N2} ₺",
			Font = new Font("Segoe UI", 12f, FontStyle.Bold),
			ForeColor = Color.FromArgb(30, 30, 60),
			AutoSize = false,
			Size = new Size(150, 56),
			Location = new Point(340, 6),
			TextAlign = ContentAlignment.MiddleRight
		};

		row.Controls.Add(lblLeft);
		row.Controls.Add(lblRight);

		return row;
	}

	private static Label MakeGrandTotalLabel(decimal total)
	{
		return new Label
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
	}

	private static Label MakeNoteLabel(string text)
	{
		return new Label
		{
			Text = text,
			Font = new Font("Segoe UI", 8f, FontStyle.Italic),
			ForeColor = Color.FromArgb(120, 120, 140),
			AutoSize = false,
			Width = 510,
			Height = 32,
			TextAlign = ContentAlignment.MiddleLeft
		};
	}

	private static Panel MakeDivider()
	{
		return new Panel
		{
			Width = 510,
			Height = 1,
			BackColor = Color.FromArgb(210, 210, 220),
			Margin = new Padding(0, 6, 0, 6)
		};
	}

	// ── İş hesap yardımcıları ────────────────────────────────

	private static decimal CalculateMtvBase(int vehicleYear)
	{
		// MTV araç yaşına ve (temsili) motor hacmine göre değişir
		// Motor hacmi bilinmediği için segment tahmini yapılır
		int age = DateTime.Now.Year - vehicleYear;
		return age switch
		{
			<= 3 => 18_000m,
			<= 7 => 12_000m,
			<= 15 => 6_000m,
			_ => 3_500m
		};
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

	// ── Input row yardımcısı ─────────────────────────────────

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

// ── Sonuç veri modeli (record) ────────────────────────────────
// Bu model sadece UI içinde kullanılır, veritabanına yazılmaz.
internal record SimulationResult(
	string ModelLabel,
	int Year,
	double AnnualKm,
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