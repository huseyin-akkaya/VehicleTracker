using Microsoft.Extensions.DependencyInjection;
using VehicleTracker.Business.Services;
using VehicleTracker.Business.Sync;
using VehicleTracker.DataAccess.Repositories;

namespace VehicleTracker.UI;

/// <summary>
/// Uygulamanın ana giriş ekranıdır (Dashboard). 
/// Kullanıcıyı karşılar, arka planda senkronizasyon sürecini başlatır ve alt modüllere (Araç Yönetimi, Simülatör) yönlendirme sağlar.
/// Arayüz bileşenleri sürükle-bırak yerine performans ve esneklik amacıyla programatik olarak oluşturulmuştur.
/// </summary>
public class DashboardForm : Form
{
	private readonly IServiceProvider _serviceProvider;

	private Panel _bgPanel = null!;
	private Label _lblTitle = null!;
	private Label _lblSubtitle = null!;
	private Button _btnVehicles = null!;
	private Button _btnSimulator = null!;
	private Label _lblFooter = null!;
	private Panel _syncBand = null!;
	private Label _lblSync = null!;

	public DashboardForm(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
		InitializeComponents();
	}

	/// <summary>
	/// Form üzerindeki tüm görsel bileşenleri, boyutları, renkleri ve olay dinleyicilerini (Event Listeners) inşa eder.
	/// </summary>
	private void InitializeComponents()
	{
		Text = "Araç Takip Sistemi";
		Size = new Size(700, 520);
		MinimumSize = new Size(600, 460);
		StartPosition = FormStartPosition.CenterScreen;
		FormBorderStyle = FormBorderStyle.FixedSingle;
		MaximizeBox = false;
		BackColor = Color.FromArgb(15, 20, 40);

		_bgPanel = new GradientPanel
		{
			Dock = DockStyle.Fill,
			TopColor = Color.FromArgb(15, 20, 40),
			BottomColor = Color.FromArgb(28, 38, 68)
		};

		_syncBand = new Panel
		{
			Dock = DockStyle.Top,
			Height = 30,
			BackColor = Color.FromArgb(10, 15, 30)
		};

		_lblSync = new Label
		{
			Text = "⏳  Senkronizasyon başlatılıyor...",
			ForeColor = Color.FromArgb(120, 140, 180),
			Font = new Font("Segoe UI", 8.5f),
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleCenter
		};
		_syncBand.Controls.Add(_lblSync);

		_lblTitle = new Label
		{
			Text = "🚗  Araç Takip Sistemi",
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 26f, FontStyle.Bold),
			AutoSize = false,
			TextAlign = ContentAlignment.MiddleCenter,
			Dock = DockStyle.Top,
			Height = 80,
			Padding = new Padding(0, 20, 0, 0)
		};

		_lblSubtitle = new Label
		{
			Text = "Araçlarınızı yönetin · Giderlerinizi takip edin · Maliyetlerinizi simüle edin",
			ForeColor = Color.FromArgb(140, 165, 210),
			Font = new Font("Segoe UI", 10f, FontStyle.Italic),
			AutoSize = false,
			TextAlign = ContentAlignment.MiddleCenter,
			Dock = DockStyle.Top,
			Height = 34
		};

		var divider = new Panel
		{
			Dock = DockStyle.Top,
			Height = 1,
			BackColor = Color.FromArgb(50, 70, 120),
			Margin = new Padding(60, 0, 60, 0)
		};

		var btnWrapper = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 2,
			RowCount = 1,
			BackColor = Color.Transparent,
			Padding = new Padding(50, 40, 50, 40)
		};
		btnWrapper.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
		btnWrapper.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
		btnWrapper.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

		_btnVehicles = MakeDashboardButton(
			"🚗",
			"Araçlarım ve Giderler",
			"Araç ekle, gider kaydet\nve geçmişi görüntüle",
			Color.FromArgb(41, 98, 180),
			Color.FromArgb(55, 120, 210)
		);

		_btnSimulator = MakeDashboardButton(
			"🧮",
			"Maliyet Simülatörü",
			"Tahmini yıllık giderleri\nhesapla ve karşılaştır",
			Color.FromArgb(30, 120, 100),
			Color.FromArgb(40, 150, 125)
		);

		btnWrapper.Controls.Add(_btnVehicles, 0, 0);
		btnWrapper.Controls.Add(_btnSimulator, 1, 0);

		_lblFooter = new Label
		{
			Text = $"v1.0  ·  {DateTime.Now.Year}  ·  Araç Takip Sistemi",
			ForeColor = Color.FromArgb(60, 80, 120),
			Font = new Font("Segoe UI", 8f),
			AutoSize = false,
			TextAlign = ContentAlignment.MiddleCenter,
			Dock = DockStyle.Bottom,
			Height = 28
		};

		_bgPanel.Controls.Add(btnWrapper);
		_bgPanel.Controls.Add(divider);
		_bgPanel.Controls.Add(_lblSubtitle);
		_bgPanel.Controls.Add(_lblTitle);
		_bgPanel.Controls.Add(_syncBand);
		_bgPanel.Controls.Add(_lblFooter);

		Controls.Add(_bgPanel);

		Load += async (_, _) => await OnFormLoadAsync();
		_btnVehicles.Click += OnVehiclesClicked;
		_btnSimulator.Click += OnSimulatorClicked;
	}

	/// <summary>
	/// Form yüklendiğinde, dış veri kaynağı (GitHub Gist) ile yerel veritabanı (SQLite) arasındaki asenkron senkronizasyon sürecini tetikler.
	/// </summary>
	private async Task OnFormLoadAsync()
	{
		var syncManager = _serviceProvider.GetRequiredService<SyncManager>();
		syncManager.SyncCompleted += OnSyncCompleted;

		try
		{
			await syncManager.SynchronizeAsync();
		}
		finally
		{
			syncManager.SyncCompleted -= OnSyncCompleted;
		}
	}

	/// <summary>
	/// Senkronizasyon tamamlandığında tetiklenen olay dinleyicisidir. 
	/// Arka plan iş parçacığından (Thread) gelen güncellemelerin ana UI iş parçacığında güvenle işlenmesi için Invoke kullanılmıştır.
	/// </summary>
	private void OnSyncCompleted(object? sender, SyncResultEventArgs e)
	{
		if (InvokeRequired) { Invoke(() => ApplySyncResult(e)); return; }
		ApplySyncResult(e);
	}

	/// <summary>
	/// Senkronizasyon sonucuna göre (Başarılı, Çevrimdışı, Hata vb.) kullanıcıyı bilgilendiren üst bilgi çubuğunun renk ve metnini günceller.
	/// </summary>
	private void ApplySyncResult(SyncResultEventArgs e)
	{
		var (icon, color) = e.Status switch
		{
			SyncStatus.Success => ("✅", Color.FromArgb(50, 160, 100)),
			SyncStatus.UpToDate => ("✅", Color.FromArgb(50, 130, 80)),
			SyncStatus.Offline => ("📶", Color.FromArgb(180, 130, 30)),
			SyncStatus.Skipped => ("⏭️", Color.FromArgb(100, 100, 130)),
			SyncStatus.Failed => ("⚠️", Color.FromArgb(180, 60, 60)),
			_ => ("ℹ️", Color.FromArgb(80, 100, 160))
		};

		_lblSync.Text = $"{icon}  {e.Message}";
		_lblSync.ForeColor = color;
	}

	/// <summary>
	/// Araç Yönetimi butonuna tıklandığında MainForm'u DI Container üzerinden çağırır ve gösterir. 
	/// Kullanıcı ana forma geçtiğinde Dashboard gizlenir, ana form kapatıldığında tekrar görünür hale gelir.
	/// </summary>
	private void OnVehiclesClicked(object? sender, EventArgs e)
	{
		var mainForm = _serviceProvider.GetRequiredService<MainForm>();
		mainForm.Show();
		mainForm.FormClosed += (_, _) => { Show(); };
		Hide();
	}

	/// <summary>
	/// Maliyet Simülatörü butonuna tıklandığında gerekli repository ve servisleri DI üzerinden çözümleyerek simülatör formunu açar.
	/// </summary>
	private void OnSimulatorClicked(object? sender, EventArgs e)
	{
		var taxCalc = _serviceProvider.GetRequiredService<TaxCalculatorService>();
		var brandRepo = _serviceProvider.GetRequiredService<VehicleBrandRepository>();
		var modelRepo = _serviceProvider.GetRequiredService<VehicleModelRepository>();

		using var form = new SimulatorForm(taxCalc, brandRepo, modelRepo);
		form.ShowDialog(this);
	}

	/// <summary>
	/// Dashboard üzerindeki büyük navigasyon butonlarını standart bir tasarımla dinamik olarak üretmek için kullanılan yardımcı metot.
	/// </summary>
	private static Button MakeDashboardButton(
		string emoji, string title, string subtitle,
		Color baseColor, Color hoverColor)
	{
		var btn = new Button
		{
			Text = $"{emoji}\n{title}\n\n{subtitle}",
			Dock = DockStyle.Fill,
			BackColor = baseColor,
			ForeColor = Color.White,
			FlatStyle = FlatStyle.Flat,
			Font = new Font("Segoe UI", 11f, FontStyle.Bold),
			Cursor = Cursors.Hand,
			Margin = new Padding(12),
			TextAlign = ContentAlignment.MiddleCenter,
			UseCompatibleTextRendering = false
		};
		btn.FlatAppearance.BorderSize = 0;
		btn.FlatAppearance.BorderColor = baseColor;

		btn.MouseEnter += (_, _) =>
		{
			btn.BackColor = hoverColor;
			btn.Font = new Font("Segoe UI", 11.5f, FontStyle.Bold);
		};
		btn.MouseLeave += (_, _) =>
		{
			btn.BackColor = baseColor;
			btn.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
		};

		return btn;
	}
}

/// <summary>
/// Arayüzün modern görünmesi için üstten alta doğru renk geçişi (Linear Gradient) sağlayan özelleştirilmiş arka plan paneli.
/// </summary>
internal class GradientPanel : Panel
{
	[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
	public Color TopColor { get; set; } = Color.FromArgb(15, 20, 40);

	[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
	public Color BottomColor { get; set; } = Color.FromArgb(28, 38, 68);

	protected override void OnPaint(PaintEventArgs e)
	{
		using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
			ClientRectangle, TopColor, BottomColor,
			System.Drawing.Drawing2D.LinearGradientMode.Vertical);
		e.Graphics.FillRectangle(brush, ClientRectangle);
	}
}