using System;
using System.Windows.Forms;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;

class SplashForm : Form
{
    private Label lblTitle;
    private Label lblSubtitle;
    private ProgressBar prgProgress;
    private List<Snowflake> snowflakes;
    private System.Windows.Forms.Timer snowTimer;
    private Random snowRandom = new Random();

    private class Snowflake
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float VelocityX { get; set; }
        public float VelocityY { get; set; }
        public float Size { get; set; }
        public float Opacity { get; set; }
    }

    public SplashForm()
    {
        Text = "Minecraft Launcher";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(500, 300);
        BackColor = Color.FromArgb(20, 20, 30);
        TopMost = true;

        // Инициализируем снег
        snowflakes = new List<Snowflake>();
        InitializeSnowflakes();
        
        // Таймер для анимации снега
        snowTimer = new System.Windows.Forms.Timer();
        snowTimer.Interval = 30;
        snowTimer.Tick += SnowTimer_Tick;
        snowTimer.Start();

        // Двойная буферизация для гладкой анимации
        DoubleBuffered = true;


        // Заголовок
        lblTitle = new Label
        {
            Text = "Поиск обновлений",
            Left = 70,
            Top = 40,
            Width = 400,
            Height = 40,
            ForeColor = Color.White,
            Font = new Font("Arial", 24, FontStyle.Bold),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Подзаголовок
        lblSubtitle = new Label
        {
            Text = "это займет пару мгновений...",
            Left = 70,
            Top = 80,
            Width = 400,
            Height = 30,
            ForeColor = Color.FromArgb(150, 150, 150),
            Font = new Font("Arial", 11),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Progress bar
        prgProgress = new ProgressBar
        {
            Left = 40,
            Top = 220,
            Width = 420,
            Height = 20,
            BackColor = Color.FromArgb(40, 40, 50),
            ForeColor = Color.FromArgb(200, 200, 0),
            Style = ProgressBarStyle.Continuous
        };

        Controls.Add(lblTitle);
        Controls.Add(lblSubtitle);
        Controls.Add(prgProgress);

        // Подписываемся на событие рисования
        Paint += SplashForm_Paint;
    }

    private void InitializeSnowflakes()
    {
        snowflakes.Clear();
        for (int i = 0; i < 40; i++)
        {
            snowflakes.Add(new Snowflake
            {
                X = snowRandom.Next(0, ClientSize.Width),
                Y = snowRandom.Next(-100, 0),
                VelocityX = (float)(snowRandom.NextDouble() - 0.5) * 2,
                VelocityY = (float)snowRandom.NextDouble() * 2 + 1,
                Size = (float)snowRandom.NextDouble() * 3 + 1,
                Opacity = (float)snowRandom.NextDouble() * 0.7f + 0.3f
            });
        }
    }

    private void SnowTimer_Tick(object? sender, EventArgs e)
    {
        foreach (var snowflake in snowflakes)
        {
            // Движение вниз с небольшим ветром
            snowflake.Y += snowflake.VelocityY;
            snowflake.X += snowflake.VelocityX;

            // Колебание из стороны в сторону (волнистое движение)
            snowflake.VelocityX += (float)(snowRandom.NextDouble() - 0.5) * 0.2f;
            if (Math.Abs(snowflake.VelocityX) > 2)
                snowflake.VelocityX = Math.Sign(snowflake.VelocityX) * 2;

            // Если снежинка ушла за границы, переместить её вверх
            if (snowflake.Y > ClientSize.Height + 50)
            {
                snowflake.Y = -10;
                snowflake.X = snowRandom.Next(0, ClientSize.Width);
            }

            // Если ушла за левую/правую границу, вернуть в поле
            if (snowflake.X < -10)
                snowflake.X = ClientSize.Width + 10;
            if (snowflake.X > ClientSize.Width + 10)
                snowflake.X = -10;
        }

        Invalidate();
    }

    private void SplashForm_Paint(object? sender, PaintEventArgs e)
    {
        // Рисуем снег
        foreach (var snowflake in snowflakes)
        {
            using (var brush = new SolidBrush(Color.FromArgb(
                (int)(snowflake.Opacity * 255),
                180, 180, 180)))
            {
                e.Graphics.FillEllipse(brush, 
                    snowflake.X - snowflake.Size / 2,
                    snowflake.Y - snowflake.Size / 2,
                    snowflake.Size,
                    snowflake.Size);
            }
        }
    }

    private Image GenerateMinecraftLogo()
    {
        var bitmap = new Bitmap(64, 64);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);
            // Простой оранжевый квадрат как логотип
            g.FillRectangle(new SolidBrush(Color.FromArgb(255, 140, 0)), 8, 8, 48, 48);
            g.DrawRectangle(new Pen(Color.FromArgb(200, 100, 0), 2), 8, 8, 48, 48);
        }
        return bitmap;
    }

    public void UpdateProgress(int value, string message = "")
    {
        try
        {
            if (prgProgress.InvokeRequired)
            {
                prgProgress.BeginInvoke(new Action(() =>
                {
                    prgProgress.Value = Math.Min(value, 100);
                    if (!string.IsNullOrEmpty(message))
                        lblSubtitle.Text = message;
                    Application.DoEvents();
                }));
            }
            else
            {
                prgProgress.Value = Math.Min(value, 100);
                if (!string.IsNullOrEmpty(message))
                    lblSubtitle.Text = message;
                Application.DoEvents();
            }
        }
        catch { }
    }

    public void Complete()
    {
        try
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                {
                    prgProgress.Value = 100;
                    lblTitle.Text = "Готово!";
                    Application.DoEvents();
                }));
            }
            else
            {
                prgProgress.Value = 100;
                lblTitle.Text = "Готово!";
                Application.DoEvents();
            }
        }
        catch { }
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
}
