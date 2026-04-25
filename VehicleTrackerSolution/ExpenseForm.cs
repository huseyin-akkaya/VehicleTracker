// --- UI/Forms/ExpenseForm.cs ---

using VehicleTracker.Business.Services;
using VehicleTracker.DataAccess.Context;
using VehicleTracker.Entities;

namespace VehicleTracker.UI;

public class ExpenseForm : Form
{
	// ── Bağımlılıklar ─────────────────────────────────────────
	private readonly IExpenseService _expenseService;
	private readonly AppDbContext _context;
	private readonly Vehicle _vehicle;

	// ── UI Bileşenleri ────────────────────────────────────────
	private Panel _headerPanel = null!;
	private Label _lblHeader = null!;
	private TableLayoutPanel _layout = null!;
	private Panel _footerPanel = null!;

	private Label _lblCategory = null!;
	private Label _lblAmount = null!;
	private Label _lblDate = null!;
	private Label _lblKm = null!;
	private Label _lblDesc = null!;

	private ComboBox _cmbCategory = null!;
	private NumericUpDown _numAmount = null!;
	private DateTimePicker _dtpDate = null!;
	private NumericUpDown _numKm = null!;
	private TextBox _txtDesc = null!;

	private Button _btnSave = null!;
	private Button _btnCancel = null!;

	public ExpenseForm(
		IExpenseService expenseService,
		AppDbContext context,
		Vehicle vehicle)
	{
		_expenseService = expenseService;
		_context = context;
		_vehicle = vehicle;

		InitializeComponents();
	}

	private void InitializeComponents()
	{
		// ── Form ──────────────────────────────────────────────
		Text = "Gider Ekle";
		Size = new Size(480, 460);
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
			BackColor = Color.FromArgb(52, 73, 94)
		};

		_lblHeader = new Label
		{
			Text = $"💸  {_vehicle.Plate} İçin Gider Ekle",
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 11f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(18, 15)
		};
		_headerPanel.Controls.Add(_lblHeader);

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
			_layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));

		// Açıklama satırı biraz daha yüksek
		_layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));

		// ── Kategori ──────────────────────────────────────────
		_lblCategory = MakeLabel("Kategori *");
		_cmbCategory = new ComboBox
		{
			DropDownStyle = ComboBoxStyle.DropDownList,
			DisplayMember = "Name",
			ValueMember = "Id",
			Dock = DockStyle.Fill,
			BackColor = Color.White,
			FlatStyle = FlatStyle.Flat
		};

		// ── Tutar ─────────────────────────────────────────────
		_lblAmount = MakeLabel("Tutar (₺) *");
		_numAmount = new NumericUpDown
		{
			Dock = DockStyle.Fill,
			Minimum = 0,
			Maximum = 9_999_999,
			DecimalPlaces = 2,
			ThousandsSeparator = true,
			BackColor = Color.White
		};

		// ── Tarih ─────────────────────────────────────────────
		_lblDate = MakeLabel("İşlem Tarihi *");
		_dtpDate = new DateTimePicker
		{
			Dock = DockStyle.Fill,
			Format = DateTimePickerFormat.Short,
			Value = DateTime.Today,
			MaxDate = DateTime.Today,
			CalendarTitleBackColor = Color.FromArgb(30, 30, 60),
			CalendarTitleForeColor = Color.White
		};

		// ── Kilometre ─────────────────────────────────────────
		_lblKm = MakeLabel("O Anki Km");
		_numKm = new NumericUpDown
		{
			Dock = DockStyle.Fill,
			Minimum = 0,
			Maximum = 9_999_999,
			Value = (decimal)_vehicle.CurrentKm,
			ThousandsSeparator = true,
			BackColor = Color.White
		};

		// ── Açıklama ──────────────────────────────────────────
		_lblDesc = MakeLabel("Açıklama");
		_txtDesc = new TextBox
		{
			Dock = DockStyle.Fill,
			Multiline = true,
			MaxLength = 500,
			BackColor = Color.White,
			BorderStyle = BorderStyle.FixedSingle,
			ScrollBars = ScrollBars.Vertical
		};

		// Layout'a satır satır ekle
		AddRow(0, _lblCategory, _cmbCategory);
		AddRow(1, _lblAmount, _numAmount);
		AddRow(2, _lblDate, _dtpDate);
		AddRow(3, _lblKm, _numKm);
		AddRow(4, _lblDesc, _txtDesc);

		// ── Footer ────────────────────────────────────────────
		_footerPanel = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = 60,
			BackColor = Color.FromArgb(235, 235, 240)
		};

		_btnSave = new Button
		{
			Text = "💾  Kaydet",
			Size = new Size(120, 36),
			BackColor = Color.FromArgb(52, 152, 219),
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
			_btnSave.Top = _btnCancel.Top = (_footerPanel.ClientSize.Height - _btnSave.Height) / 2;
		};
		_footerPanel.Controls.AddRange(new Control[] { _btnSave, _btnCancel });

		// ── Yerleşim ──────────────────────────────────────────
		Controls.Add(_layout);
		Controls.Add(_footerPanel);
		Controls.Add(_headerPanel);

		// ── Events ────────────────────────────────────────────
		AcceptButton = _btnSave;
		CancelButton = _btnCancel;
		_btnSave.Click += async (_, _) => await OnSaveClickedAsync();
		_btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
		Load += async (_, _) => await LoadCategoriesAsync();
	}

	// ─────────────────────────────────────────────────────────
	//  Kategorileri yükle
	// ─────────────────────────────────────────────────────────
	private async Task LoadCategoriesAsync()
	{
		try
		{
			var categories = await Task.Run(() =>
				_context.Categories.OrderBy(c => c.Id).ToList());

			_cmbCategory.DataSource = categories;
			_cmbCategory.DisplayMember = "Name";
			_cmbCategory.ValueMember = "Id";
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Kategoriler yüklenemedi:\n{ex.Message}",
				"Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}
	}

	// ─────────────────────────────────────────────────────────
	//  Kaydet
	// ─────────────────────────────────────────────────────────
	private async Task OnSaveClickedAsync()
	{
		if (_cmbCategory.SelectedItem is null)
		{
			MessageBox.Show("Lütfen bir kategori seçin.", "Eksik Bilgi",
				MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}

		if (_numAmount.Value <= 0)
		{
			MessageBox.Show("Tutar sıfırdan büyük olmalıdır.", "Eksik Bilgi",
				MessageBoxButtons.OK, MessageBoxIcon.Warning);
			_numAmount.Focus();
			return;
		}

		_btnSave.Enabled = false;
		_btnSave.Text = "⏳  Kaydediliyor...";

		try
		{
			var selectedCat = (Category)_cmbCategory.SelectedItem!;

			var expense = new Expense
			{
				VehicleId = _vehicle.Id,
				CategoryId = selectedCat.Id,
				Amount = _numAmount.Value,
				ExpenseDate = _dtpDate.Value.Date,
				KmAtExpense = _numKm.Value > 0 ? (double?)_numKm.Value : null,
				Description = string.IsNullOrWhiteSpace(_txtDesc.Text)
							  ? null
							  : _txtDesc.Text.Trim(),
				CreatedAt = DateTime.UtcNow
			};

			await _expenseService.AddExpenseAsync(expense);

			DialogResult = DialogResult.OK;
			Close();
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Kayıt sırasında hata:\n{ex.Message}",
				"Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
		finally
		{
			_btnSave.Enabled = true;
			_btnSave.Text = "💾  Kaydet";
		}
	}

	// ─────────────────────────────────────────────────────────
	//  Yardımcılar
	// ─────────────────────────────────────────────────────────
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