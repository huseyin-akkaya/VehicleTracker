// --- UI/Forms/ExpenseHistoryForm.cs ---

using VehicleTracker.Business.Services;
using VehicleTracker.Entities;

namespace VehicleTracker.UI;

public class ExpenseHistoryForm : Form
{
	private readonly IExpenseService _expenseService;
	private readonly Vehicle _vehicle;

	private Panel _headerPanel = null!;
	private Label _lblHeader = null!;
	private DataGridView _grid = null!;
	private Panel _footerPanel = null!;
	private Label _lblTotal = null!;

	public ExpenseHistoryForm(IExpenseService expenseService, Vehicle vehicle)
	{
		_expenseService = expenseService;
		_vehicle = vehicle;
		InitializeComponents();
	}

	private void InitializeComponents()
	{
		Text = "Gider Geçmişi";
		Size = new Size(700, 500);
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
		_lblHeader = new Label
		{
			Text = $"📋  {_vehicle.Plate} - Gider Geçmişi",
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 12f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(20, 14)
		};
		_headerPanel.Controls.Add(_lblHeader);

		// ── Footer ────────────────────────────────────────────
		_footerPanel = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = 60,
			BackColor = Color.FromArgb(235, 235, 240),
			Padding = new Padding(20, 0, 20, 0)
		};
		_lblTotal = new Label
		{
			Text = "💰  Toplam Masraf: 0,00 ₺",
			ForeColor = Color.FromArgb(30, 130, 80),
			Font = new Font("Segoe UI", 12f, FontStyle.Bold),
			AutoSize = false,
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleRight
		};
		_footerPanel.Controls.Add(_lblTotal);

		// ── Grid ──────────────────────────────────────────────
		_grid = new DataGridView
		{
			Dock = DockStyle.Fill,
			BackgroundColor = Color.White,
			BorderStyle = BorderStyle.None,
			RowHeadersVisible = false,
			AllowUserToAddRows = false,
			ReadOnly = true,
			SelectionMode = DataGridViewSelectionMode.FullRowSelect,
			AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
			CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
			GridColor = Color.FromArgb(230, 230, 230)
		};

		_grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
		{
			BackColor = Color.FromArgb(44, 62, 80),
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
			Padding = new Padding(6, 0, 0, 0)
		};
		_grid.ColumnHeadersHeight = 38;
		_grid.EnableHeadersVisualStyles = false;
		_grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(248, 249, 252) };
		_grid.DefaultCellStyle = new DataGridViewCellStyle { SelectionBackColor = Color.FromArgb(210, 228, 255), SelectionForeColor = Color.FromArgb(20, 20, 20) };
		_grid.RowTemplate.Height = 36;

		_grid.AutoGenerateColumns = false;
		_grid.Columns.AddRange(
			new DataGridViewTextBoxColumn { DataPropertyName = "Date", HeaderText = "Tarih", FillWeight = 15 },
			new DataGridViewTextBoxColumn { DataPropertyName = "Category", HeaderText = "Kategori", FillWeight = 20 },
			new DataGridViewTextBoxColumn { DataPropertyName = "Amount", HeaderText = "Tutar", FillWeight = 15, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
			new DataGridViewTextBoxColumn { DataPropertyName = "Km", HeaderText = "Kilometre", FillWeight = 15, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
			new DataGridViewTextBoxColumn { DataPropertyName = "Description", HeaderText = "Açıklama", FillWeight = 35 }
		);

		Controls.Add(_grid);
		Controls.Add(_footerPanel);
		Controls.Add(_headerPanel);

		Load += async (_, _) => await LoadDataAsync();
	}

	private async Task LoadDataAsync()
	{
		try
		{
			var expenses = await _expenseService.GetExpensesByVehicleAsync(_vehicle.Id);

			// Verileri grid'e basmak için isimsiz bir listeye (Anonymous Type) çeviriyoruz
			var viewModels = expenses.OrderByDescending(e => e.ExpenseDate).Select(e => new
			{
				Date = e.ExpenseDate.ToString("dd.MM.yyyy"),
				Category = e.Category?.Name ?? "Diğer",
				Amount = $"{e.Amount:N2} ₺",
				Km = e.KmAtExpense.HasValue ? $"{e.KmAtExpense:N0} km" : "—",
				Description = e.Description ?? string.Empty
			}).ToList();

			_grid.DataSource = viewModels;

			// Alt kısımdaki genel toplamı hesapla
			decimal total = expenses.Sum(e => e.Amount);
			_lblTotal.Text = $"💰  Toplam Masraf: {total:N2} ₺";
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Giderler yüklenirken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}
	}
}