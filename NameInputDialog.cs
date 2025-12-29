using System;
using System.Windows.Forms;
using System.Drawing;

class NameInputDialog : Form
{
    public string PlayerName { get; private set; }
    private TextBox txtName;
    private CheckBox chkRemember;
    
    public NameInputDialog()
    {
        InitializeComponents();
    }
    
    private void InitializeComponents()
    {
        this.Text = "Введите имя игрока";
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.ClientSize = new Size(350, 180);
        this.BackColor = Color.FromArgb(30, 30, 40);
        
        // Заголовок
        var lblTitle = new Label
        {
            Text = "Введите имя игрока",
            Font = new Font("Arial", 14, FontStyle.Bold),
            ForeColor = Color.White,
            Left = 20,
            Top = 20,
            AutoSize = true
        };
        
        // Поле ввода
        txtName = new TextBox
        {
            Left = 20,
            Top = 60,
            Width = 310,
            Height = 35,
            Font = new Font("Arial", 12),
            BackColor = Color.FromArgb(45, 45, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        
        // Чекбокс "Запомнить"
        chkRemember = new CheckBox
        {
            Text = "Запомнить имя",
            ForeColor = Color.FromArgb(200, 200, 200),
            Font = new Font("Arial", 10),
            Left = 20,
            Top = 105,
            AutoSize = true,
            Checked = true
        };
        
        // Кнопка OK
        var btnOk = new Button
        {
            Text = "OK",
            Left = 170,
            Top = 105,
            Width = 80,
            Height = 35,
            DialogResult = DialogResult.OK,
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += (s, e) => 
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Введите имя игрока", "Ошибка", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            PlayerName = txtName.Text.Trim();
            this.DialogResult = DialogResult.OK;
            this.Close();
        };
        
        // Кнопка Cancel
        var btnCancel = new Button
        {
            Text = "Отмена",
            Left = 260,
            Top = 105,
            Width = 80,
            Height = 35,
            DialogResult = DialogResult.Cancel,
            BackColor = Color.FromArgb(70, 70, 80),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (s, e) => this.Close();
        
        this.Controls.Add(lblTitle);
        this.Controls.Add(txtName);
        this.Controls.Add(chkRemember);
        this.Controls.Add(btnOk);
        this.Controls.Add(btnCancel);
        
        this.AcceptButton = btnOk;
        this.CancelButton = btnCancel;
        
        // Загружаем сохраненное имя если есть
        LoadSavedName();
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
}