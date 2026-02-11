using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PickleCalLG
{
    public partial class MainForm : Form
    {
        private LgTvController? _tvController;
        private PGenServer? _pgenServer;
        private string _tvIp = "";

        private TabControl tabControl1;
        private TabPage tabPageConnection;
        private TabPage tabPageTvControl;
        private TabPage tabPagePictureSettings;
        private TabPage tabPageWhiteBalance;
        private TabPage tabPagePatternGen;
        private GroupBox groupBox1;
        private CheckBox chkSecureConnection;
        private TextBox txtTvIp;
        private Label label1;
        private Button btnDisconnect;
        private Button btnConnect;
        private Label lblStatus;
        private GroupBox groupBox2;
        private Button btnApplyPictureMode;
        private ComboBox cmbPictureMode;
        private Label label8;
        private GroupBox groupBox3;
        private Button btnApplyColorSettings;
        private ComboBox cmbColorTemp;
        private ComboBox cmbGamma;
        private ComboBox cmbColorGamut;
        private Label label11;
        private Label label10;
        private Label label9;
        private GroupBox groupBox4;
        private Button btnDisableProcessing;
        private Button btnReadSettings;

        public MainForm()
        {
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeComponent()
        {
            tabControl1 = new TabControl();
            tabPageConnection = new TabPage();
            tabPageTvControl = new TabPage();
            tabPagePictureSettings = new TabPage();
            tabPageWhiteBalance = new TabPage();
            tabPagePatternGen = new TabPage();
            groupBox1 = new GroupBox();
            chkSecureConnection = new CheckBox();
            txtTvIp = new TextBox();
            label1 = new Label();
            btnDisconnect = new Button();
            btnConnect = new Button();
            lblStatus = new Label();
            groupBox2 = new GroupBox();
            btnApplyPictureMode = new Button();
            cmbPictureMode = new ComboBox();
            label8 = new Label();
            groupBox3 = new GroupBox();
            btnApplyColorSettings = new Button();
            cmbColorTemp = new ComboBox();
            cmbGamma = new ComboBox();
            cmbColorGamut = new ComboBox();
            label11 = new Label();
            label10 = new Label();
            label9 = new Label();
            groupBox4 = new GroupBox();
            btnDisableProcessing = new Button();
            btnReadSettings = new Button();

            tabControl1.SuspendLayout();
            tabPageConnection.SuspendLayout();
            groupBox1.SuspendLayout();
            tabPageTvControl.SuspendLayout();
            groupBox2.SuspendLayout();
            tabPagePictureSettings.SuspendLayout();
            groupBox3.SuspendLayout();
            tabPageWhiteBalance.SuspendLayout();
            SuspendLayout();

            // tabControl1
            tabControl1.Controls.Add(tabPageConnection);
            tabControl1.Controls.Add(tabPageTvControl);
            tabControl1.Controls.Add(tabPagePictureSettings);
            tabControl1.Controls.Add(tabPageWhiteBalance);
            tabControl1.Controls.Add(tabPagePatternGen);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(800, 600);
            tabControl1.TabIndex = 0;

            // tabPageConnection
            tabPageConnection.Controls.Add(groupBox1);
            tabPageConnection.Controls.Add(lblStatus);
            tabPageConnection.Location = new Point(4, 24);
            tabPageConnection.Name = "tabPageConnection";
            tabPageConnection.Padding = new Padding(3);
            tabPageConnection.Size = new Size(792, 572);
            tabPageConnection.TabIndex = 0;
            tabPageConnection.Text = "Connection";

            // groupBox1
            groupBox1.Controls.Add(chkSecureConnection);
            groupBox1.Controls.Add(txtTvIp);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(btnDisconnect);
            groupBox1.Controls.Add(btnConnect);
            groupBox1.Location = new Point(20, 20);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(350, 150);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "TV Connection";

            // chkSecureConnection
            chkSecureConnection.AutoSize = true;
            chkSecureConnection.Location = new Point(10, 70);
            chkSecureConnection.Name = "chkSecureConnection";
            chkSecureConnection.Size = new Size(100, 17);
            chkSecureConnection.TabIndex = 4;
            chkSecureConnection.Text = "Secure Connection";
            chkSecureConnection.UseVisualStyleBackColor = true;

            // txtTvIp
            txtTvIp.Location = new Point(100, 35);
            txtTvIp.Name = "txtTvIp";
            txtTvIp.Size = new Size(150, 20);
            txtTvIp.TabIndex = 3;
            txtTvIp.Text = "192.168.1.";

            // label1
            label1.AutoSize = true;
            label1.Location = new Point(10, 38);
            label1.Name = "label1";
            label1.Size = new Size(85, 13);
            label1.TabIndex = 2;
            label1.Text = "TV IP Address:";

            // btnDisconnect
            btnDisconnect.Enabled = false;
            btnDisconnect.Location = new Point(260, 100);
            btnDisconnect.Name = "btnDisconnect";
            btnDisconnect.Size = new Size(75, 25);
            btnDisconnect.TabIndex = 1;
            btnDisconnect.Text = "Disconnect";
            btnDisconnect.UseVisualStyleBackColor = true;
            btnDisconnect.Click += btnDisconnect_Click;

            // btnConnect
            btnConnect.Location = new Point(175, 100);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(75, 25);
            btnConnect.TabIndex = 0;
            btnConnect.Text = "Connect";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;

            // lblStatus
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(20, 180);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(35, 13);
            lblStatus.TabIndex = 1;
            lblStatus.Text = "Ready";

            // tabPageTvControl
            tabPageTvControl.Controls.Add(groupBox2);
            tabPageTvControl.Location = new Point(4, 24);
            tabPageTvControl.Name = "tabPageTvControl";
            tabPageTvControl.Padding = new Padding(3);
            tabPageTvControl.Size = new Size(792, 572);
            tabPageTvControl.TabIndex = 1;
            tabPageTvControl.Text = "TV Control";

            // groupBox2
            groupBox2.Controls.Add(btnApplyPictureMode);
            groupBox2.Controls.Add(cmbPictureMode);
            groupBox2.Controls.Add(label8);
            groupBox2.Location = new Point(20, 20);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(300, 100);
            groupBox2.TabIndex = 0;
            groupBox2.TabStop = false;
            groupBox2.Text = "Picture Mode";

            // btnApplyPictureMode
            btnApplyPictureMode.Location = new Point(215, 40);
            btnApplyPictureMode.Name = "btnApplyPictureMode";
            btnApplyPictureMode.Size = new Size(75, 25);
            btnApplyPictureMode.TabIndex = 2;
            btnApplyPictureMode.Text = "Apply";
            btnApplyPictureMode.UseVisualStyleBackColor = true;
            btnApplyPictureMode.Click += btnApplyPictureMode_Click;

            // cmbPictureMode
            cmbPictureMode.FormattingEnabled = true;
            cmbPictureMode.Location = new Point(10, 42);
            cmbPictureMode.Name = "cmbPictureMode";
            cmbPictureMode.Size = new Size(150, 21);
            cmbPictureMode.TabIndex = 1;

            // label8
            label8.AutoSize = true;
            label8.Location = new Point(10, 20);
            label8.Name = "label8";
            label8.Size = new Size(69, 13);
            label8.TabIndex = 0;
            label8.Text = "Picture Mode:";

            // tabPagePictureSettings
            tabPagePictureSettings.Controls.Add(groupBox3);
            tabPagePictureSettings.Location = new Point(4, 24);
            tabPagePictureSettings.Name = "tabPagePictureSettings";
            tabPagePictureSettings.Padding = new Padding(3);
            tabPagePictureSettings.Size = new Size(792, 572);
            tabPagePictureSettings.TabIndex = 2;
            tabPagePictureSettings.Text = "Picture Settings";

            // groupBox3
            groupBox3.Controls.Add(btnApplyColorSettings);
            groupBox3.Controls.Add(cmbColorTemp);
            groupBox3.Controls.Add(cmbGamma);
            groupBox3.Controls.Add(cmbColorGamut);
            groupBox3.Controls.Add(label11);
            groupBox3.Controls.Add(label10);
            groupBox3.Controls.Add(label9);
            groupBox3.Location = new Point(20, 20);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(350, 150);
            groupBox3.TabIndex = 0;
            groupBox3.TabStop = false;
            groupBox3.Text = "Color Settings";

            // btnApplyColorSettings
            btnApplyColorSettings.Location = new Point(260, 100);
            btnApplyColorSettings.Name = "btnApplyColorSettings";
            btnApplyColorSettings.Size = new Size(75, 25);
            btnApplyColorSettings.TabIndex = 6;
            btnApplyColorSettings.Text = "Apply";
            btnApplyColorSettings.UseVisualStyleBackColor = true;
            btnApplyColorSettings.Click += btnApplyColorSettings_Click;

            // cmbColorTemp
            cmbColorTemp.FormattingEnabled = true;
            cmbColorTemp.Location = new Point(100, 70);
            cmbColorTemp.Name = "cmbColorTemp";
            cmbColorTemp.Size = new Size(150, 21);
            cmbColorTemp.TabIndex = 5;

            // cmbGamma
            cmbGamma.FormattingEnabled = true;
            cmbGamma.Location = new Point(100, 45);
            cmbGamma.Name = "cmbGamma";
            cmbGamma.Size = new Size(150, 21);
            cmbGamma.TabIndex = 4;

            // cmbColorGamut
            cmbColorGamut.FormattingEnabled = true;
            cmbColorGamut.Location = new Point(100, 20);
            cmbColorGamut.Name = "cmbColorGamut";
            cmbColorGamut.Size = new Size(150, 21);
            cmbColorGamut.TabIndex = 3;

            // label11
            label11.AutoSize = true;
            label11.Location = new Point(10, 73);
            label11.Name = "label11";
            label11.Size = new Size(83, 13);
            label11.TabIndex = 2;
            label11.Text = "Color Temperature";

            // label10
            label10.AutoSize = true;
            label10.Location = new Point(10, 48);
            label10.Name = "label10";
            label10.Size = new Size(42, 13);
            label10.TabIndex = 1;
            label10.Text = "Gamma";

            // label9
            label9.AutoSize = true;
            label9.Location = new Point(10, 23);
            label9.Name = "label9";
            label9.Size = new Size(70, 13);
            label9.TabIndex = 0;
            label9.Text = "Color Gamut";

            // tabPageWhiteBalance
            tabPageWhiteBalance.Location = new Point(4, 24);
            tabPageWhiteBalance.Name = "tabPageWhiteBalance";
            tabPageWhiteBalance.Padding = new Padding(3);
            tabPageWhiteBalance.Size = new Size(792, 572);
            tabPageWhiteBalance.TabIndex = 3;
            tabPageWhiteBalance.Text = "White Balance";

            // tabPagePatternGen
            tabPagePatternGen.Location = new Point(4, 24);
            tabPagePatternGen.Name = "tabPagePatternGen";
            tabPagePatternGen.Padding = new Padding(3);
            tabPagePatternGen.Size = new Size(792, 572);
            tabPagePatternGen.TabIndex = 4;
            tabPagePatternGen.Text = "Pattern Gen";

            // MainForm
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 600);
            Controls.Add(tabControl1);
            Name = "MainForm";
            Text = "PickleCal - LG TV Calibration";
            tabControl1.ResumeLayout(false);
            tabPageConnection.ResumeLayout(false);
            tabPageConnection.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            tabPageTvControl.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            tabPagePictureSettings.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            tabPageWhiteBalance.ResumeLayout(false);
            ResumeLayout(false);
        }

        private void InitializeUI()
        {
            txtTvIp.Text = "192.168.1.";
            cmbPictureMode.Items.AddRange(new[] {
                "cinema", "expert1", "game", "sports", "vivid", "standard",
                "eco", "hdr cinema", "hdr game", "hdr vivid"
            });
            cmbPictureMode.SelectedIndex = 1;
            cmbColorGamut.Items.AddRange(new[] { "auto", "extended", "wide", "srgb", "native" });
            cmbColorGamut.SelectedIndex = 0;
            cmbGamma.Items.AddRange(new[] { "low", "medium", "high1", "high2", "2.2", "2.4" });
            cmbGamma.SelectedIndex = 4;
            cmbColorTemp.Items.AddRange(new[] { "warm50", "warm40", "medium", "cool10", "cool20" });
            cmbColorTemp.SelectedIndex = 2;
            lblStatus.Text = "Ready";
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            _tvIp = txtTvIp.Text.Trim();
            if (string.IsNullOrEmpty(_tvIp))
            {
                MessageBox.Show("Please enter the LG TV IP address", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnConnect.Enabled = false;
            lblStatus.Text = $"Connecting to {_tvIp}...";

            try
            {
                _tvController = new LgTvController(_tvIp, chkSecureConnection.Checked);
                _tvController.OnStatusChange += TvController_OnStatusChange;
                _tvController.OnDisconnect += TvController_OnDisconnect;

                await _tvController.ConnectAsync();

                if (_tvController.IsConnected && _tvController.IsPaired)
                {
                    lblStatus.Text = $"Connected to {_tvIp}";
                    lblStatus.ForeColor = Color.Green;
                    btnDisconnect.Enabled = true;
                    btnConnect.Enabled = false;
                }
                else
                {
                    lblStatus.Text = "Connection failed - check pairing";
                    lblStatus.ForeColor = Color.Red;
                    btnConnect.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error: {ex.Message}";
                lblStatus.ForeColor = Color.Red;
                btnConnect.Enabled = true;
            }
        }

        private async void btnDisconnect_Click(object sender, EventArgs e)
        {
            if (_tvController != null)
            {
                await _tvController.DisconnectAsync();
                lblStatus.Text = "Disconnected";
                lblStatus.ForeColor = Color.Black;
                btnConnect.Enabled = true;
                btnDisconnect.Enabled = false;
            }
        }

        private void TvController_OnStatusChange(string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(TvController_OnStatusChange), status);
                return;
            }
            lblStatus.Text = status;
        }

        private void TvController_OnDisconnect()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(TvController_OnDisconnect));
                return;
            }
            lblStatus.Text = "Disconnected";
            lblStatus.ForeColor = Color.Black;
            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
        }

        private async void btnApplyPictureMode_Click(object sender, EventArgs e)
        {
            if (_tvController == null)
            {
                MessageBox.Show("Not connected to TV", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string mode = cmbPictureMode.SelectedItem?.ToString() ?? "cinema";
            await _tvController.SetPictureModeAsync(mode);
            lblStatus.Text = $"Picture mode set to {mode}";
        }

        private async void btnApplyColorSettings_Click(object sender, EventArgs e)
        {
            if (_tvController == null)
            {
                MessageBox.Show("Not connected to TV", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string gamut = cmbColorGamut.SelectedItem?.ToString() ?? "auto";
            string gamma = cmbGamma.SelectedItem?.ToString() ?? "2.2";
            string temp = cmbColorTemp.SelectedItem?.ToString() ?? "medium";

            await _tvController.SetColorGamutAsync(gamut);
            await _tvController.SetGammaAsync(gamma);
            await _tvController.SetColorTemperatureAsync(temp);

            lblStatus.Text = $"Color settings applied: {gamut}/{gamma}/{temp}";
        }
    }
}