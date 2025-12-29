using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;

class LauncherForm : Form
{
    public string? PlayerName { get; private set; }
    private TextBox txtName;
    private CheckBox chkRemember;
    private Button btnPlay;
    private Button btnOpen;
    private Label lbl;
    private System.Windows.Forms.Timer animationTimer;
    private int animationStep = 0;
    private Point lastMousePos = Point.Empty;
    private bool isDragging = false;
    private bool isLoading = false;  // Флаг для отслеживания загрузки

    // Импорт для изменения границ окна
    [DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HT_CAPTION = 0x2;

    public LauncherForm()
    {
        Text = "Minecraft Forge Launcher";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        ClientSize = new Size(400, 250);
        BackColor = Color.FromArgb(30, 30, 40);
        Opacity = 0;

        // Добавляем округленные углы
        Region = CreateRoundedRegion(new Rectangle(0, 0, ClientSize.Width, ClientSize.Height), 20);

        // Панель заголовка с кнопками управления
        var pnlTitleBar = new Panel
        {
            Left = 0,
            Top = 0,
            Width = ClientSize.Width,
            Height = 45,
            BackColor = Color.FromArgb(25, 25, 35)
        };
        pnlTitleBar.MouseDown += (s, e) =>
        {
            isDragging = true;
            lastMousePos = e.Location;
        };
        pnlTitleBar.MouseMove += (s, e) =>
        {
            if (isDragging)
            {
                Location = new Point(Location.X + e.X - lastMousePos.X, Location.Y + e.Y - lastMousePos.Y);
            }
        };
        pnlTitleBar.MouseUp += (s, e) => isDragging = false;

        // Заголовок
        var lblTitle = new Label
        {
            Text = "🎮 Minecraft Launcher",
            Left = 15,
            Top = 7,
            Width = 280,
            Height = 30,
            ForeColor = Color.FromArgb(100, 200, 255),
            Font = new Font("Arial", 14, FontStyle.Bold),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft
        };
        lblTitle.MouseDown += (s, e) =>
        {
            isDragging = true;
            lastMousePos = e.Location;
        };
        lblTitle.MouseMove += (s, e) =>
        {
            if (isDragging)
            {
                Location = new Point(Location.X + e.X - lastMousePos.X, Location.Y + e.Y - lastMousePos.Y);
            }
        };
        lblTitle.MouseUp += (s, e) => isDragging = false;

        // Кнопка свертывания
        var btnMinimize = new Button
        {
            Text = "−",
            Left = 310,
            Top = 7,
            Width = 35,
            Height = 30,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(50, 50, 60),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Arial", 14, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnMinimize.FlatAppearance.BorderSize = 0;
        btnMinimize.MouseEnter += (s, e) => btnMinimize.BackColor = Color.FromArgb(70, 70, 80);
        btnMinimize.MouseLeave += (s, e) => btnMinimize.BackColor = Color.FromArgb(50, 50, 60);
        btnMinimize.Click += (s, e) => WindowState = FormWindowState.Minimized;

        // Кнопка закрытия
        var btnClose = new Button
        {
            Text = "✕",
            Left = 350,
            Top = 7,
            Width = 35,
            Height = 30,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(200, 50, 50),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Arial", 12, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.MouseEnter += (s, e) => btnClose.BackColor = Color.FromArgb(220, 70, 70);
        btnClose.MouseLeave += (s, e) => btnClose.BackColor = Color.FromArgb(200, 50, 50);
        btnClose.Click += (s, e) => Close();

        pnlTitleBar.Controls.Add(lblTitle);
        pnlTitleBar.Controls.Add(btnMinimize);
        pnlTitleBar.Controls.Add(btnClose);
        Controls.Add(pnlTitleBar);

        // Имя игрока
        lbl = new Label
        {
            Text = "Имя игрока:",
            Left = 20,
            Top = 60,
            AutoSize = true,
            ForeColor = Color.FromArgb(200, 200, 200),
            Font = new Font("Arial", 10, FontStyle.Regular)
        };

        txtName = new TextBox
        {
            Left = 20,
            Top = 85,
            Width = 360,
            Height = 35,
            Font = new Font("Arial", 11),
            BackColor = Color.FromArgb(45, 45, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.None
        };

        txtName.Enter += (s, e) => txtName.BackColor = Color.FromArgb(50, 60, 80);
        txtName.Leave += (s, e) => txtName.BackColor = Color.FromArgb(45, 45, 55);

        chkRemember = new CheckBox
        {
            Left = 20,
            Top = 130,
            Text = "  Запомнить имя",
            ForeColor = Color.FromArgb(150, 150, 150),
            Font = new Font("Arial", 9),
            AutoSize = true
        };

        btnPlay = new Button
        {
            Text = "▶ Играть",
            Left = 20,
            Top = 160,
            Width = 170,
            Height = 45,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(0, 120, 215),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Arial", 11, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnPlay.FlatAppearance.BorderSize = 0;

        btnOpen = new Button
        {
            Text = "📁 Папка игры",
            Left = 210,
            Top = 160,
            Width = 170,
            Height = 45,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(0, 150, 100),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Arial", 11, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnOpen.FlatAppearance.BorderSize = 0;

        // Эффекты наведения для кнопок
        btnPlay.MouseEnter += (s, e) => btnPlay.BackColor = Color.FromArgb(0, 140, 235);
        btnPlay.MouseLeave += (s, e) => btnPlay.BackColor = Color.FromArgb(0, 120, 215);

        btnOpen.MouseEnter += (s, e) => btnOpen.BackColor = Color.FromArgb(0, 170, 120);
        btnOpen.MouseLeave += (s, e) => btnOpen.BackColor = Color.FromArgb(0, 150, 100);

        btnPlay.Click += BtnPlay_Click;
        btnOpen.Click += BtnOpen_Click;

        // Загрузка сохраненного имени
        LoadSavedName();

        Controls.Add(lbl);
        Controls.Add(txtName);
        Controls.Add(chkRemember);
        Controls.Add(btnPlay);
        Controls.Add(btnOpen);

        AcceptButton = btnPlay;

        // Настройка анимации появления
        animationTimer = new System.Windows.Forms.Timer();
        animationTimer.Interval = 15;
        animationTimer.Tick += AnimationTimer_Tick;
    }

    private Region CreateRoundedRegion(Rectangle rect, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
        path.AddArc(rect.X + rect.Width - radius, rect.Y, radius, radius, 270, 90);
        path.AddArc(rect.X + rect.Width - radius, rect.Y + rect.Height - radius, radius, radius, 0, 90);
        path.AddArc(rect.X, rect.Y + rect.Height - radius, radius, radius, 90, 90);
        path.CloseFigure();
        return new Region(path);
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        animationStep++;
        Opacity = Math.Min(animationStep * 0.05, 1.0);

        if (Opacity >= 1.0)
        {
            animationTimer.Stop();
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        animationTimer.Start();
    }

    private void LoadSavedName()
    {
        try
        {
            string saved = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ".minecraft", "config", "player_name.txt");

            if (File.Exists(saved))
            {
                txtName.Text = File.ReadAllText(saved).Trim();
                chkRemember.Checked = true;
            }
        }
        catch { }
    }

    private void BtnPlay_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            MessageBox.Show("Введите имя игрока.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Проверка на двойной клик
        if (isLoading)
        {
            MessageBox.Show("Уже выполняется загрузка. Пожалуйста, подождите...", "Загрузка", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        PlayerName = txtName.Text.Trim();
        isLoading = true;
        btnPlay.Enabled = false;
        btnPlay.Text = "⏳ Загрузка...";

        if (chkRemember.Checked)
        {
            try
            {
                string configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    ".minecraft", "config");
                Directory.CreateDirectory(configPath);
                File.WriteAllText(Path.Combine(configPath, "player_name.txt"), PlayerName);
                Console.WriteLine($"✓ Имя игрока сохранено: {PlayerName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ошибка при сохранении имени: {ex.Message}");
            }
        }

        // Запускаем Minecraft Forge 1.7.10 асинхронно
        Task.Run(async () => 
        {
            try
            {
                Console.WriteLine($"\n>>> Нажата кнопка 'Играть' с именем: {PlayerName}");
                await Program.LaunchMinecraftAsync(PlayerName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n!!! КРИТИЧЕСКАЯ ОШИБКА В LAUNCHER !!!");
                Console.WriteLine($"Тип исключения: {ex.GetType().Name}");
                Console.WriteLine($"Сообщение: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"InnerException тип: {ex.InnerException.GetType().Name}");
                    Console.WriteLine($"InnerException сообщение: {ex.InnerException.Message}");
                }
                Console.WriteLine($"!!! КОНЕЦ ОТЧЁТА ОБ ОШИБКЕ !!!\n");
                
                Invoke(new Action(() =>
                {
                    isLoading = false;
                    btnPlay.Enabled = true;
                    btnPlay.Text = "▶ Играть";
                    
                    MessageBox.Show(
                        $"Ошибка при запуске:\n\n{ex.Message}\n\nПроверьте консоль для подробностей",
                        "Ошибка запуска",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }));
            }
            finally
            {
                isLoading = false;
                btnPlay.Enabled = true;
                btnPlay.Text = "▶ Играть";
            }
        });
        
        DialogResult = DialogResult.OK;
        Close();
    }

    private void BtnOpen_Click(object? sender, EventArgs e)
    {
        try
        {
            string minecraftFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ".minecraft");
            Process.Start(new ProcessStartInfo { FileName = minecraftFolder, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось открыть папку: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
