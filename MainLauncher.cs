using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

partial class MainLauncher : Form
{
    public string? PlayerName { get; private set; }
    private int animationStep = 0;
    private Point lastMousePos = Point.Empty;
    private bool isDragging = false;
    private List<Snowflake> snowflakes;
    private System.Windows.Forms.Timer snowTimer;
    private Random snowRandom = new Random();
    private Panel pnlContent;
    private Button currentPlayButton;
    private bool isGameStarting = false;

    private class Snowflake
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float VelocityX { get; set; }
        public float VelocityY { get; set; }
        public float Size { get; set; }
        public float Opacity { get; set; }
    }

    public MainLauncher()
    {
        InitializeComponent();
        
        snowflakes = new List<Snowflake>();
        InitializeSnowflakes();
        
        snowTimer = new System.Windows.Forms.Timer();
        snowTimer.Interval = 30;
        snowTimer.Tick += SnowTimer_Tick;
        snowTimer.Start();
        
        DoubleBuffered = true;
        
        try
        {
            picLogo.Image = LoadUserLogo();
        }
        catch { }
        
        LoadSavedName();
        
        // Добавляем обработчик для изменения размера окна
        this.Resize += MainLauncher_Resize;
        
        // Создаем элементы интерфейса после инициализации
        CreateGameCards();
    }

    private void MainLauncher_Resize(object sender, EventArgs e)
    {
        InitializeSnowflakes();
    }

    private void InitializeSnowflakes()
    {
        snowflakes.Clear();
        int formWidth = ClientSize.Width > 0 ? ClientSize.Width : 1200;
        int formHeight = ClientSize.Height > 0 ? ClientSize.Height : 800;
        
        for (int i = 0; i < 50; i++)
        {
            snowflakes.Add(new Snowflake
            {
                X = snowRandom.Next(0, formWidth),
                Y = snowRandom.Next(-100, formHeight),
                VelocityX = (float)(snowRandom.NextDouble() - 0.5) * 1.5f,
                VelocityY = (float)snowRandom.NextDouble() * 1.5f + 0.5f,
                Size = (float)snowRandom.NextDouble() * 3 + 1,
                Opacity = (float)snowRandom.NextDouble() * 0.6f + 0.3f
            });
        }
    }

    private void SnowTimer_Tick(object? sender, EventArgs e)
    {
        int formWidth = ClientSize.Width > 0 ? ClientSize.Width : 1200;
        int formHeight = ClientSize.Height > 0 ? ClientSize.Height : 800;
        
        foreach (var snowflake in snowflakes)
        {
            snowflake.Y += snowflake.VelocityY;
            snowflake.X += snowflake.VelocityX;

            snowflake.VelocityX += (float)(snowRandom.NextDouble() - 0.5) * 0.1f;
            if (Math.Abs(snowflake.VelocityX) > 1.5f)
                snowflake.VelocityX = Math.Sign(snowflake.VelocityX) * 1.5f;

            if (snowflake.Y > formHeight + 50)
            {
                snowflake.Y = -10;
                snowflake.X = snowRandom.Next(0, formWidth);
            }

            if (snowflake.X < -10)
                snowflake.X = formWidth + 10;
            if (snowflake.X > formWidth + 10)
                snowflake.X = -10;
        }

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        
        if (snowflakes != null && snowflakes.Count > 0)
        {
            foreach (var snowflake in snowflakes)
            {
                try
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    
                    using (var brush = new SolidBrush(Color.FromArgb(
                        (int)(snowflake.Opacity * 255),
                        180, 180, 180)))
                    {
                        float x = snowflake.X - snowflake.Size / 2;
                        float y = snowflake.Y - snowflake.Size / 2;
                        e.Graphics.FillEllipse(brush, x, y, snowflake.Size, snowflake.Size);
                    }
                }
                catch { }
            }
        }
    }

    private Image LoadUserLogo()
    {
        try
        {
            string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                ".minecraft", "config");
            string logoPath = Path.Combine(configPath, "logo.png");

            if (File.Exists(logoPath))
            {
                using (var fs = File.OpenRead(logoPath))
                using (var ms = new MemoryStream())
                {
                    fs.CopyTo(ms);
                    ms.Position = 0;
                    using (var img = Image.FromStream(ms))
                    {
                        return new Bitmap(img);
                    }
                }
            }
        }
        catch { }

        return GenerateMinecraftLogo();
    }

    private void ChangeLogoFromDialog()
    {
        using (var dlg = new OpenFileDialog())
        {
            dlg.Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif";
            dlg.Title = "Выберите изображение для логотипа";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "config");
                    Directory.CreateDirectory(configPath);
                    string dest = Path.Combine(configPath, "logo.png");
                    File.Copy(dlg.FileName, dest, true);

                    picLogo.Image?.Dispose();
                    picLogo.Image = LoadUserLogo();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось установить логотип: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }

    private void CreateGameCards()
    {
        // Панель для контента
        pnlContent = new Panel
        {
            Left = 0,
            Top = 70,
            Width = 1200,
            Height = 680,
            BackColor = Color.FromArgb(20, 20, 30),
            AutoScroll = false
        };

        // Применяем фон к панели контента
        try
        {
            string bgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fon.png");
            if (File.Exists(bgPath))
            {
                using (var fs = File.OpenRead(bgPath))
                using (var img = Image.FromStream(fs))
                {
                    int w = pnlContent.Width;
                    int h = pnlContent.Height;
                    using (var scaled = new Bitmap(w, h))
                    using (var g = Graphics.FromImage(scaled))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(img, 0, 0, w, h);
                        using (var brush = new SolidBrush(Color.FromArgb(120, 8, 8, 12)))
                        {
                            g.FillRectangle(brush, 0, 0, w, h);
                        }
                        pnlContent.BackgroundImage = new Bitmap(scaled);
                        pnlContent.BackgroundImageLayout = ImageLayout.Stretch;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BG] Error applying background: {ex.Message}");
        }

        // Параметры карточки OneBlock
        int cardWidth = 720;
        int cardHeight = 260;

        int centerX = (pnlContent.Width - cardWidth) / 2;
        int centerY = 40;

        var cardColor = Color.FromArgb(120, 120, 120);
        var card = new Panel
        {
            Left = centerX,
            Top = centerY,
            Width = cardWidth,
            Height = cardHeight,
            BorderStyle = BorderStyle.None,
            Cursor = Cursors.Default
        };
        card.Region = CreateRoundedRegion(new Rectangle(0, 0, cardWidth, cardHeight), 18);

        var bg = GenerateCardBackground(cardWidth, cardHeight, cardColor);
        card.BackgroundImage = bg;
        card.BackgroundImageLayout = ImageLayout.Stretch;

        // Заголовок OneBlock
        var lblTitle = new Label
        {
            Text = "OneBlock",
            Font = new Font("Arial", 24, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Left = 28,
            Top = 22,
            AutoSize = false,
            Width = cardWidth - 200,
            Height = 40
        };
        card.Controls.Add(lblTitle);

        // Описание
        var lblDesc = new Label
        {
            Text = WrapText("ищи рыбу в банке", 60),
            Font = new Font("Arial", 12, FontStyle.Regular),
            ForeColor = Color.FromArgb(245, 245, 245),
            BackColor = Color.Transparent,
            Left = 28,
            Top = 70,
            Width = cardWidth - 260,
            Height = 90
        };
        card.Controls.Add(lblDesc);

        // Индикатор игроков
        var pnlPlayers = new Panel
        {
            Left = 28,
            Top = cardHeight - 64,
            Width = 160,
            Height = 28,
            BackColor = Color.Transparent
        };
        var dot = new Panel
        {
            Left = 0,
            Top = 8,
            Width = 12,
            Height = 12,
            BackColor = Color.FromArgb(0, 255, 0),
            Cursor = Cursors.Default
        };
        dot.Region = CreateRoundedRegion(new Rectangle(0, 0, 12, 12), 6);
        pnlPlayers.Controls.Add(dot);
        var lblPlayers = new Label
        {
            Text = "18 из 100",
            Left = 22,
            Top = 4,
            AutoSize = true,
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Font = new Font("Arial", 11, FontStyle.Regular)
        };
        pnlPlayers.Controls.Add(lblPlayers);
        card.Controls.Add(pnlPlayers);

        // Кнопка Играть справа
        var btnPlay = new Button
        {
            Name = "btnPlayOneBlock",
            Text = "Играть",
            Font = new Font("Arial", 14, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(120, 120, 120),
            FlatStyle = FlatStyle.Flat,
            Width = 160,
            Height = 52,
            Left = cardWidth - 196,
            Top = cardHeight - 70,
            Cursor = Cursors.Hand,
            Tag = "OneBlock"
        };
        btnPlay.FlatAppearance.BorderSize = 0;
        
        currentPlayButton = btnPlay;
        
        btnPlay.Click += async (s, e) => 
        {
            if (isGameStarting)
            {
                MessageBox.Show("Игра уже запускается. Пожалуйста, подождите.", "Запуск", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            isGameStarting = true;
            var button = s as Button;
            if (button != null)
            {
                button.Text = "Запуск...";
                button.Enabled = false;
            }
            
            try
            {
                // Проверяем имя игрока
                if (string.IsNullOrEmpty(PlayerName))
                {
                    MessageBox.Show("Имя игрока не установлено. Используйте кнопку профиля для настройки.", 
                        "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                Console.WriteLine($"\n=== Запуск игры с именем: {PlayerName} ===");
                
                // Запускаем игру
                await Program.LaunchMinecraftAsync(PlayerName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при запуске игры: {ex.Message}");
                MessageBox.Show($"Ошибка при запуске игры:\n\n{ex.Message}", 
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isGameStarting = false;
                if (button != null)
                {
                    button.Text = "Играть";
                    button.Enabled = true;
                }
            }
        };
        
        btnPlay.MouseEnter += (s, e) => {
            var btn = s as Button;
            if (btn != null) btn.BackColor = Color.FromArgb(140, 140, 140);
        };
        btnPlay.MouseLeave += (s, e) => {
            var btn = s as Button;
            if (btn != null) btn.BackColor = Color.FromArgb(120, 120, 120);
        };
        btnPlay.Region = CreateRoundedRegion(new Rectangle(0, 0, btnPlay.Width, btnPlay.Height), 26);
        card.Controls.Add(btnPlay);

        pnlContent.Controls.Add(card);
        Controls.Add(pnlContent);
    }

    private Image GenerateCardBackground(int width, int height, Color baseColor)
    {
        var bmp = new Bitmap(width, height);
        using (var g = Graphics.FromImage(bmp))
        {
            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Point(0, 0), new Point(0, height),
                ControlPaint.Light(baseColor), ControlPaint.Dark(baseColor)))
            {
                g.FillRectangle(brush, 0, 0, width, height);
            }

            using (var pen = new Pen(Color.FromArgb(24, Color.White), 40))
            {
                for (int x = -width; x < width * 2; x += 120)
                {
                    g.DrawLine(pen, x, 0, x + width, height);
                }
            }

            using (var overlay = new System.Drawing.SolidBrush(Color.FromArgb(30, 0, 0, 0)))
            {
                g.FillRectangle(overlay, 0, 0, width, 40);
            }
        }
        return bmp;
    }

    private string WrapText(string text, int charsPerLine)
    {
        var sb = new StringBuilder();
        int charCount = 0;
        foreach (var word in text.Split(' '))
        {
            if (charCount + word.Length > charsPerLine)
            {
                sb.AppendLine();
                charCount = 0;
            }
            sb.Append(word + " ");
            charCount += word.Length + 1;
        }
        return sb.ToString().Trim();
    }

    private Image GenerateMinecraftLogo()
    {
        var bitmap = new Bitmap(34, 34);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);
            g.FillRectangle(new SolidBrush(Color.FromArgb(255, 140, 0)), 4, 4, 26, 26);
            g.DrawRectangle(new Pen(Color.FromArgb(200, 100, 0), 1), 4, 4, 26, 26);
        }
        return bitmap;
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

    private void AnimationTimer_Tick(object sender, EventArgs e)
    {
        animationStep++;

        if (animationStep >= 20)
        {
            if (animationTimer != null)
            {
                animationTimer.Stop();
            }
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (animationTimer != null)
        {
            animationTimer.Start();
        }
        Invalidate();
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
                PlayerName = File.ReadAllText(saved).Trim();
                Console.WriteLine($"✓ Загружено имя игрока: {PlayerName}");
                btnProfile.Text = $"👤 {PlayerName}";
            }
            else
            {
                PlayerName = "Player";
                btnProfile.Text = "👤 Player";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Ошибка загрузки имени: {ex.Message}");
            PlayerName = "Player";
            btnProfile.Text = "👤 Player";
        }
    }

    // ============ Методы обработки событий интерфейса ============

    private void PnlTitleBar_MouseDown(object sender, MouseEventArgs e)
    {
        isDragging = true;
        lastMousePos = e.Location;
    }

    private void PnlTitleBar_MouseMove(object sender, MouseEventArgs e)
    {
        if (isDragging)
        {
            Location = new Point(Location.X + e.X - lastMousePos.X, Location.Y + e.Y - lastMousePos.Y);
        }
    }

    private void PnlTitleBar_MouseUp(object sender, MouseEventArgs e)
    {
        isDragging = false;
    }

    private void PicLogo_MouseDown(object sender, MouseEventArgs e)
    {
        isDragging = true;
        lastMousePos = e.Location;
    }

    private void PicLogo_MouseMove(object sender, MouseEventArgs e)
    {
        if (isDragging)
        {
            Location = new Point(Location.X + e.X - lastMousePos.X, Location.Y + e.Y - lastMousePos.Y);
        }
    }

    private void PicLogo_MouseUp(object sender, MouseEventArgs e)
    {
        isDragging = false;
    }

    private void LblTitle_MouseDown(object sender, MouseEventArgs e)
    {
        isDragging = true;
        lastMousePos = e.Location;
    }

    private void LblTitle_MouseMove(object sender, MouseEventArgs e)
    {
        if (isDragging)
        {
            Location = new Point(Location.X + e.X - lastMousePos.X, Location.Y + e.Y - lastMousePos.Y);
        }
    }

    private void LblTitle_MouseUp(object sender, MouseEventArgs e)
    {
        isDragging = false;
    }

    private void BtnProfile_MouseEnter(object sender, EventArgs e)
    {
        btnProfile.BackColor = Color.FromArgb(70, 70, 80);
    }

    private void BtnProfile_MouseLeave(object sender, EventArgs e)
    {
        btnProfile.BackColor = Color.FromArgb(50, 50, 60);
    }

    private void BtnSettings_MouseEnter(object sender, EventArgs e)
    {
        btnSettings.BackColor = Color.FromArgb(70, 70, 80);
    }

    private void BtnSettings_MouseLeave(object sender, EventArgs e)
    {
        btnSettings.BackColor = Color.FromArgb(50, 50, 60);
    }

    private void BtnSettings_Click(object sender, EventArgs e)
    {
        MessageBox.Show("Окно настроек будет добавлено позже", "Настройки", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void BtnExit_Click(object sender, EventArgs e)
    {
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            snowTimer?.Stop();
            snowTimer?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void BtnExit_MouseEnter(object sender, EventArgs e)
    {
        btnExit.BackColor = Color.FromArgb(70, 70, 80);
    }

    private void BtnExit_MouseLeave(object sender, EventArgs e)
    {
        btnExit.BackColor = Color.FromArgb(50, 50, 60);
    }

    private void BtnChangeLogo_Click(object sender, EventArgs e)
    {
        ChangeLogoFromDialog();
    }
}