using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;

partial class MainLauncher
{
    private Panel pnlTitleBar;
    private PictureBox picLogo;
    private Button btnChangeLogo;
    private Label lblTitle;
    private Button btnProfile;
    private Button btnSettings;
    private Button btnExit;
    private System.Windows.Forms.Timer animationTimer;

    private void InitializeComponent()
    {
        this.pnlTitleBar = new Panel();
        this.picLogo = new PictureBox();
        this.lblTitle = new Label();
        this.btnProfile = new Button();
        this.btnSettings = new Button();
        this.btnExit = new Button();
        this.animationTimer = new System.Windows.Forms.Timer();

        // pnlTitleBar
        this.pnlTitleBar.BackColor = Color.FromArgb(25, 25, 35);
        this.pnlTitleBar.Controls.Add(this.picLogo);
        this.pnlTitleBar.Controls.Add(this.lblTitle);
        this.pnlTitleBar.Controls.Add(this.btnProfile);
        this.pnlTitleBar.Controls.Add(this.btnSettings);
        this.pnlTitleBar.Controls.Add(this.btnExit);
        this.pnlTitleBar.Dock = DockStyle.Top;
        this.pnlTitleBar.Location = new Point(0, 0);
        this.pnlTitleBar.Size = new Size(1200, 70);
        this.pnlTitleBar.TabIndex = 0;
        this.pnlTitleBar.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PnlTitleBar_MouseDown);
        this.pnlTitleBar.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PnlTitleBar_MouseMove);
        this.pnlTitleBar.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PnlTitleBar_MouseUp);

        // picLogo
        ((System.ComponentModel.ISupportInitialize)(this.picLogo)).BeginInit();
        this.picLogo.Location = new Point(20, 15);
        this.picLogo.Size = new Size(40, 40);
        this.picLogo.SizeMode = PictureBoxSizeMode.Zoom;
        this.picLogo.Image = GenerateMinecraftLogo();
        this.picLogo.TabIndex = 0;
        this.picLogo.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PicLogo_MouseDown);
        this.picLogo.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PicLogo_MouseMove);
        this.picLogo.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PicLogo_MouseUp);
        ((System.ComponentModel.ISupportInitialize)(this.picLogo)).EndInit();

        // btnChangeLogo (overlay on picLogo)
        this.btnChangeLogo = new Button();
        this.btnChangeLogo.FlatAppearance.BorderSize = 0;
        this.btnChangeLogo.FlatStyle = FlatStyle.Flat;
        this.btnChangeLogo.BackColor = Color.Transparent;
        this.btnChangeLogo.Cursor = Cursors.Hand;
        this.btnChangeLogo.Location = new Point(20, 15);
        this.btnChangeLogo.Size = new Size(40, 40);
        this.btnChangeLogo.TabIndex = 4;
        this.btnChangeLogo.Text = "";
        this.btnChangeLogo.Click += new System.EventHandler(this.BtnChangeLogo_Click);
        this.btnChangeLogo.MouseEnter += new System.EventHandler(this.BtnProfile_MouseEnter);
        this.btnChangeLogo.MouseLeave += new System.EventHandler(this.BtnProfile_MouseLeave);
        this.btnChangeLogo.Region = CreateRoundedRegion(new Rectangle(0, 0, this.btnChangeLogo.Size.Width, this.btnChangeLogo.Size.Height), 8);

        // lblTitle
        this.lblTitle.AutoSize = false;
        this.lblTitle.Font = new Font("Arial", 18, FontStyle.Bold);
        this.lblTitle.ForeColor = Color.White;
        this.lblTitle.Location = new Point(70, 15);
        this.lblTitle.Size = new Size(400, 40);
        this.lblTitle.TabIndex = 1;
        this.lblTitle.Text = "SimpleMinecraft";
        this.lblTitle.TextAlign = ContentAlignment.MiddleLeft;
        this.lblTitle.MouseDown += new System.Windows.Forms.MouseEventHandler(this.LblTitle_MouseDown);
        this.lblTitle.MouseMove += new System.Windows.Forms.MouseEventHandler(this.LblTitle_MouseMove);
        this.lblTitle.MouseUp += new System.Windows.Forms.MouseEventHandler(this.LblTitle_MouseUp);

        // add change logo button to title bar (on top of picLogo)
        this.pnlTitleBar.Controls.Add(this.btnChangeLogo);

        // btnProfile
        this.btnProfile.BackColor = Color.FromArgb(50, 50, 60);
        this.btnProfile.Cursor = Cursors.Hand;
        this.btnProfile.FlatAppearance.BorderSize = 0;
        this.btnProfile.FlatStyle = FlatStyle.Flat;
        this.btnProfile.Font = new Font("Arial", 10);
        this.btnProfile.ForeColor = Color.White;
        this.btnProfile.Location = new Point(900, 18);
        this.btnProfile.Size = new Size(150, 34);
        this.btnProfile.TabIndex = 2;
        this.btnProfile.Text = "👤 iglobrix12";
        this.btnProfile.MouseEnter += new System.EventHandler(this.BtnProfile_MouseEnter);
        this.btnProfile.MouseLeave += new System.EventHandler(this.BtnProfile_MouseLeave);
        this.btnProfile.Region = CreateRoundedRegion(new Rectangle(0, 0, this.btnProfile.Size.Width, this.btnProfile.Size.Height), 12);

        // btnSettings
        this.btnSettings.BackColor = Color.FromArgb(50, 50, 60);
        this.btnSettings.Cursor = Cursors.Hand;
        this.btnSettings.FlatAppearance.BorderSize = 0;
        this.btnSettings.FlatStyle = FlatStyle.Flat;
        this.btnSettings.Font = new Font("Arial", 10);
        this.btnSettings.ForeColor = Color.White;
        this.btnSettings.Location = new Point(1065, 18);
        this.btnSettings.Size = new Size(40, 34);
        this.btnSettings.TabIndex = 5;
        this.btnSettings.Text = "⚙";
        this.btnSettings.MouseEnter += new System.EventHandler(this.BtnSettings_MouseEnter);
        this.btnSettings.MouseLeave += new System.EventHandler(this.BtnSettings_MouseLeave);
        this.btnSettings.Click += new System.EventHandler(this.BtnSettings_Click);
        this.btnSettings.Region = CreateRoundedRegion(new Rectangle(0, 0, this.btnSettings.Size.Width, this.btnSettings.Size.Height), 12);

        // btnExit
        this.btnExit.BackColor = Color.FromArgb(50, 50, 60);
        this.btnExit.Cursor = Cursors.Hand;
        this.btnExit.FlatAppearance.BorderSize = 0;
        this.btnExit.FlatStyle = FlatStyle.Flat;
        this.btnExit.Font = new Font("Arial", 10);
        this.btnExit.ForeColor = Color.White;
        this.btnExit.Location = new Point(1115, 18);
        this.btnExit.Size = new Size(75, 34);
        this.btnExit.TabIndex = 3;
        this.btnExit.Text = "Выйти";
        this.btnExit.Click += new System.EventHandler(this.BtnExit_Click);
        this.btnExit.MouseEnter += new System.EventHandler(this.BtnExit_MouseEnter);
        this.btnExit.MouseLeave += new System.EventHandler(this.BtnExit_MouseLeave);
        this.btnExit.Region = CreateRoundedRegion(new Rectangle(0, 0, this.btnExit.Size.Width, this.btnExit.Size.Height), 12);

        // animationTimer
        this.animationTimer.Interval = 15;
        this.animationTimer.Tick += new System.EventHandler(this.AnimationTimer_Tick);

        // MainLauncher
        this.BackColor = Color.FromArgb(20, 20, 30);
        this.ClientSize = new Size(1200, 800);
        this.Controls.Add(this.pnlTitleBar);
        this.FormBorderStyle = FormBorderStyle.None;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Opacity = 1.0;
        this.ShowInTaskbar = true;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Text = "SimpleMinecraft";
        this.Region = CreateRoundedRegion(new Rectangle(0, 0, this.ClientSize.Width, this.ClientSize.Height), 20);
    }
}
