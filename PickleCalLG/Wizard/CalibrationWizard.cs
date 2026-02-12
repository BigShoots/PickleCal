using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PickleCalLG.ColorScience;
using PickleCalLG.Meters;

namespace PickleCalLG.Wizard
{
    /// <summary>
    /// Step-by-step calibration wizard that guides users through the entire
    /// TV calibration process — from connecting devices to running automated calibration.
    /// Uses FlowLayoutPanel for proper DPI-aware layout on 4K displays.
    /// </summary>
    public sealed class CalibrationWizard : Form
    {
        private enum WizardStep
        {
            Welcome,
            ConnectPickleGen,
            ConnectMeter,
            ConnectTv,
            PreCalibrationSettings,
            BaselineMeasurement,
            RunCalibration,
            VerifyResults,
            Complete
        }

        // Wizard state
        private WizardStep _currentStep = WizardStep.Welcome;
        private readonly MainForm _mainForm;
        private PickleCalRemoteClient? _pickleGen;
        private ITvController? _tvController;
        private CancellationTokenSource? _wizardCts;
        private TvBrand _selectedBrand = TvBrand.LG;

        // Calibration configuration
        private bool _isHdr;
        private string _pickleGenIp = "";
        private string _tvIp = "";
        private CalibrationProfile _profile = CalibrationProfile.QuickSdr;
        private float _dpi = 1f;

        // ---------- UI Controls ----------
        private Panel panelHeader = null!;
        private Label lblStepTitle = null!;
        private Label lblStepNumber = null!;
        private Panel panelContent = null!;
        private Panel panelFooter = null!;
        private Button btnBack = null!;
        private Button btnNext = null!;
        private Button btnCancel = null!;
        private ProgressBar prgWizard = null!;

        private Panel? _currentStepPanel;

        // Content width for flow panels
        private int ContentWidth => panelContent.ClientSize.Width - panelContent.Padding.Horizontal - 10;

        public CalibrationWizard(MainForm mainForm)
        {
            _mainForm = mainForm;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            InitializeWizardForm();
            ShowStep(WizardStep.Welcome);
        }

        private void InitializeWizardForm()
        {
            Text = "PickleCal — Calibration Wizard";

            // DPI-scaled form size
            float dpi = DeviceDpi / 96f;
            if (dpi < 1f) dpi = 1f;
            _dpi = dpi;
            Size = new Size((int)(820 * dpi), (int)(660 * dpi));
            MinimumSize = new Size((int)(750 * dpi), (int)(600 * dpi));
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            BackColor = DarkTheme.Background;
            ForeColor = DarkTheme.TextPrimary;
            Font = DarkTheme.DefaultFont;
            DarkTheme.EnableDarkTitleBar(this);

            int headerH = (int)(68 * dpi);
            int btnH = (int)(36 * dpi);
            int footerH = btnH + (int)(28 * dpi);  // button + top/bottom padding

            // ── Footer (add FIRST so it docks bottom before Fill) ──
            panelFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = footerH,
                BackColor = DarkTheme.SurfaceDark,
                Padding = new Padding((int)(20 * dpi), (int)(10 * dpi), (int)(20 * dpi), (int)(10 * dpi))
            };
            panelFooter.Paint += (_, pe) =>
            {
                using var pen = new Pen(DarkTheme.Border, 1);
                pe.Graphics.DrawLine(pen, 0, 0, panelFooter.Width, 0);
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size((int)(90 * dpi), btnH),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                FlatStyle = FlatStyle.Flat,
                BackColor = DarkTheme.SurfaceLight,
                ForeColor = DarkTheme.TextSecondary,
                Font = DarkTheme.ButtonFont,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = DarkTheme.Border;
            btnCancel.FlatAppearance.BorderSize = 1;
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(62, 62, 62);
            btnCancel.Click += (_, _) =>
            {
                if (MessageBox.Show("Cancel the calibration wizard?", "PickleCal",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _wizardCts?.Cancel();
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            };

            btnNext = new Button
            {
                Text = "Next →",
                Size = new Size((int)(130 * dpi), btnH),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = DarkTheme.Accent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnNext.FlatAppearance.BorderSize = 0;
            btnNext.FlatAppearance.MouseOverBackColor = DarkTheme.AccentLight;
            btnNext.FlatAppearance.MouseDownBackColor = DarkTheme.AccentDark;
            btnNext.Click += async (_, _) => await GoNextAsync();

            btnBack = new Button
            {
                Text = "← Back",
                Size = new Size((int)(100 * dpi), btnH),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = DarkTheme.SurfaceLight,
                ForeColor = DarkTheme.TextPrimary,
                Font = DarkTheme.ButtonFont,
                Cursor = Cursors.Hand
            };
            btnBack.FlatAppearance.BorderColor = DarkTheme.Border;
            btnBack.FlatAppearance.BorderSize = 1;
            btnBack.FlatAppearance.MouseOverBackColor = Color.FromArgb(62, 62, 62);
            btnBack.Click += (_, _) => GoBack();

            prgWizard = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 4,
                Style = ProgressBarStyle.Continuous,
                Maximum = 8,
                BackColor = DarkTheme.SurfaceDark
            };

            // Use a TableLayoutPanel for footer to position buttons properly
            var footerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = Padding.Empty
            };
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));   // Cancel
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // spacer
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));   // Back
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));   // Next
            footerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            btnCancel.Anchor = AnchorStyles.Left;
            btnBack.Anchor = AnchorStyles.Right;
            btnNext.Anchor = AnchorStyles.Right;
            btnBack.Margin = new Padding(0, 0, 8, 0);

            footerLayout.Controls.Add(btnCancel, 0, 0);
            footerLayout.Controls.Add(new Panel { BackColor = Color.Transparent }, 1, 0);
            footerLayout.Controls.Add(btnBack, 2, 0);
            footerLayout.Controls.Add(btnNext, 3, 0);

            panelFooter.Controls.Add(footerLayout);
            panelFooter.Controls.Add(prgWizard);

            // ── Header (add SECOND so it docks top after footer) ──
            panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = headerH,
                BackColor = DarkTheme.SurfaceDark,
                Padding = new Padding(24, 10, 24, 10)
            };
            panelHeader.Paint += (_, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                // Gradient accent line at bottom
                using var brush = new LinearGradientBrush(
                    new Point(0, panelHeader.Height - 2),
                    new Point(panelHeader.Width, panelHeader.Height - 2),
                    DarkTheme.Accent, Color.FromArgb(0, DarkTheme.Accent));
                g.FillRectangle(brush, 0, panelHeader.Height - 2, panelHeader.Width, 2);
            };

            lblStepNumber = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = DarkTheme.Accent,
                Location = new Point(24, 8),
                BackColor = Color.Transparent
            };

            lblStepTitle = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(24, 30),
                BackColor = Color.Transparent
            };

            panelHeader.Controls.Add(lblStepNumber);
            panelHeader.Controls.Add(lblStepTitle);

            // ── Content (add LAST so it fills remaining space) ──
            panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 16, 28, 8),
                AutoScroll = true,
                BackColor = DarkTheme.Background
            };

            Controls.Add(panelContent);
            Controls.Add(panelHeader);
            Controls.Add(panelFooter);
        }

        // ---------- Navigation ----------

        private void ShowStep(WizardStep step)
        {
            _currentStep = step;

            _currentStepPanel?.Dispose();
            panelContent.Controls.Clear();

            int stepNum = (int)step + 1;
            int totalSteps = Enum.GetValues(typeof(WizardStep)).Length;
            lblStepNumber.Text = $"Step {stepNum} of {totalSteps}";
            prgWizard.Value = (int)step;

            btnBack.Enabled = step != WizardStep.Welcome;

            var panel = step switch
            {
                WizardStep.Welcome => BuildWelcomeStep(),
                WizardStep.ConnectPickleGen => BuildConnectPickleGenStep(),
                WizardStep.ConnectMeter => BuildConnectMeterStep(),
                WizardStep.ConnectTv => BuildConnectTvStep(),
                WizardStep.PreCalibrationSettings => BuildPreCalSettingsStep(),
                WizardStep.BaselineMeasurement => BuildBaselineMeasurementStep(),
                WizardStep.RunCalibration => BuildRunCalibrationStep(),
                WizardStep.VerifyResults => BuildVerifyResultsStep(),
                WizardStep.Complete => BuildCompleteStep(),
                _ => new Panel()
            };

            _currentStepPanel = panel;
            panel.Dock = DockStyle.Fill;
            panelContent.Controls.Add(panel);

            lblStepTitle.Text = step switch
            {
                WizardStep.Welcome => "Welcome to PickleCal",
                WizardStep.ConnectPickleGen => "Connect Pattern Generator",
                WizardStep.ConnectMeter => "Connect Color Meter",
                WizardStep.ConnectTv => "Connect to Your TV",
                WizardStep.PreCalibrationSettings => "Calibration Settings",
                WizardStep.BaselineMeasurement => "Baseline Measurement",
                WizardStep.RunCalibration => "Running Calibration",
                WizardStep.VerifyResults => "Verify Results",
                WizardStep.Complete => "Calibration Complete!",
                _ => ""
            };

            if (step == WizardStep.Complete)
            {
                btnNext.Text = "Finish ✓";
                btnBack.Enabled = false;
            }
            else if (step == WizardStep.RunCalibration)
            {
                btnNext.Text = "Start Calibration";
            }
            else
            {
                btnNext.Text = "Next →";
            }
        }

        private void GoBack()
        {
            if (_currentStep > WizardStep.Welcome)
                ShowStep(_currentStep - 1);
        }

        private async Task GoNextAsync()
        {
            bool canProceed = await ValidateCurrentStepAsync();
            if (!canProceed) return;

            if (_currentStep == WizardStep.Complete)
            {
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            if (_currentStep == WizardStep.RunCalibration)
            {
                await RunCalibrationAsync();
                ShowStep(WizardStep.VerifyResults);
                return;
            }

            if (_currentStep < WizardStep.Complete)
                ShowStep(_currentStep + 1);
        }

        private Task<bool> ValidateCurrentStepAsync()
        {
            switch (_currentStep)
            {
                case WizardStep.ConnectPickleGen:
                    if (_pickleGen == null || !_pickleGen.IsConnected)
                    {
                        MessageBox.Show(
                            "Connect to PickleGen before continuing.\n\n" +
                            "Make sure PickleGen is running in Easy Mode on your Android device.",
                            "PickleCal Wizard", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return Task.FromResult(false);
                    }
                    return Task.FromResult(true);

                case WizardStep.ConnectTv:
                    return Task.FromResult(true);

                default:
                    return Task.FromResult(true);
            }
        }

        // ---------- Step Builders (using FlowLayoutPanel) ----------

        private Panel BuildWelcomeStep()
        {
            var flow = CreateFlowPanel();

            AddFlowLabel(flow, "This wizard will guide you through calibrating your TV display.",
                fontSize: 12f);
            AddSpacer(flow, 12);
            AddFlowLabel(flow, "You'll need:", fontSize: 11f, bold: true);
            AddFlowLabel(flow, "  ✓  An Android device running PickleGen (phone, stick, or TV box)");
            AddFlowLabel(flow, "  ✓  A color meter (i1 Display Pro, ColorChecker, SpyderX, etc.)");
            AddFlowLabel(flow, "  ✓  Your TV (LG, Samsung, Sony, Panasonic, and TCL support automated calibration)");
            AddFlowLabel(flow, "  ✓  Both devices on the same Wi-Fi network");

            AddSpacer(flow, 16);
            AddFlowLabel(flow, "Select your TV brand:", fontSize: 11f, bold: true);

            var brandRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 4, 0, 4)
            };
            var cmbBrand = new ComboBox
            {
                Size = new Size((int)(200 * _dpi), (int)(28 * _dpi)),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = DarkTheme.InputBackground,
                ForeColor = DarkTheme.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = DarkTheme.DefaultFont
            };
            foreach (var brand in TvBrandCapabilities.AllBrands)
            {
                var caps = TvBrandCapabilities.For(brand);
                cmbBrand.Items.Add(caps.DisplayName);
                if (brand == _selectedBrand)
                    cmbBrand.SelectedIndex = cmbBrand.Items.Count - 1;
            }
            var lblBrandCaps = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9f),
                ForeColor = DarkTheme.Accent,
                Margin = new Padding(8, 6, 0, 0),
                BackColor = Color.Transparent
            };
            void UpdateBrandCaps()
            {
                int idx = cmbBrand.SelectedIndex;
                if (idx >= 0 && idx < TvBrandCapabilities.AllBrands.Length)
                {
                    _selectedBrand = TvBrandCapabilities.AllBrands[idx];
                    var caps = TvBrandCapabilities.For(_selectedBrand);
                    lblBrandCaps.Text = caps.Description;
                    lblBrandCaps.ForeColor = caps.SupportsAutoCalWhiteBalance
                        ? DarkTheme.Success : DarkTheme.TextSecondary;
                }
            }
            UpdateBrandCaps();
            cmbBrand.SelectedIndexChanged += (_, _) => UpdateBrandCaps();
            brandRow.Controls.AddRange(new Control[] { cmbBrand, lblBrandCaps });
            flow.Controls.Add(brandRow);

            AddSpacer(flow, 12);
            AddFlowLabel(flow, "Choose your calibration profile:", fontSize: 11f, bold: true);

            AddRadio(flow, "Quick SDR — Grayscale + basic colors (15 min)", true,
                () => _profile = CalibrationProfile.QuickSdr);
            AddRadio(flow, "Full SDR — Grayscale + saturation sweeps + verification (30 min)", false,
                () => _profile = CalibrationProfile.FullSdr);
            AddRadio(flow, "Quick HDR10 — HDR grayscale + basic colors (20 min)", false,
                () => { _profile = CalibrationProfile.QuickHdr; _isHdr = true; });
            AddRadio(flow, "Full HDR10 — Complete HDR calibration with metadata (45 min)", false,
                () => { _profile = CalibrationProfile.FullHdr; _isHdr = true; });

            AddSpacer(flow, 12);
            AddFlowLabel(flow, "No calibration experience needed — just follow each step!",
                color: DarkTheme.Success, fontSize: 10f);

            return WrapInScrollPanel(flow);
        }

        private Panel BuildConnectPickleGenStep()
        {
            var flow = CreateFlowPanel();

            AddFlowLabel(flow, "PickleGen generates test patterns on your TV via an Android device.", fontSize: 11f);
            AddSpacer(flow, 8);
            AddFlowLabel(flow, "1.  Open the PickleGen app on your Android device");
            AddFlowLabel(flow, "2.  Select 'Easy Mode'");
            AddFlowLabel(flow, "3.  Note the IP address shown on screen");
            AddFlowLabel(flow, "4.  Enter it below and click 'Connect'");

            AddSpacer(flow, 12);

            var connectRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 4, 0, 4)
            };
            var lblIp = new Label
            {
                Text = "PickleGen IP:",
                AutoSize = true,
                Font = DarkTheme.DefaultFont,
                ForeColor = DarkTheme.TextPrimary,
                Margin = new Padding(0, 7, 6, 0),
                BackColor = Color.Transparent
            };
            var txtIp = new TextBox
            {
                Size = new Size((int)(160 * _dpi), (int)(28 * _dpi)),
                Text = _pickleGenIp,
                BackColor = DarkTheme.InputBackground,
                ForeColor = DarkTheme.TextPrimary,
                Font = DarkTheme.DefaultFont,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 3, 8, 0)
            };
            var btnConn = new Button
            {
                Text = "Connect",
                Size = new Size((int)(110 * _dpi), (int)(32 * _dpi)),
                BackColor = DarkTheme.AccentDark,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = DarkTheme.ButtonFont,
                Cursor = Cursors.Hand
            };
            btnConn.FlatAppearance.BorderSize = 0;
            btnConn.FlatAppearance.MouseOverBackColor = DarkTheme.AccentLight;
            connectRow.Controls.AddRange(new Control[] { lblIp, txtIp, btnConn });
            flow.Controls.Add(connectRow);

            var lblStatus = new Label
            {
                Text = _pickleGen?.IsConnected == true ? "✓ Connected" : "Not connected",
                ForeColor = _pickleGen?.IsConnected == true ? DarkTheme.Success : DarkTheme.TextSecondary,
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                Margin = new Padding(0, 4, 0, 4),
                BackColor = Color.Transparent
            };
            flow.Controls.Add(lblStatus);

            btnConn.Click += async (_, _) =>
            {
                string ip = txtIp.Text.Trim();
                if (string.IsNullOrEmpty(ip))
                {
                    MessageBox.Show("Enter the IP address from the PickleGen screen.",
                        "PickleCal", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                _pickleGenIp = ip;
                btnConn.Enabled = false;
                lblStatus.Text = "Connecting...";
                lblStatus.ForeColor = DarkTheme.Warning;
                try
                {
                    if (_pickleGen != null) await _pickleGen.DisposeAsync();
                    _pickleGen = new PickleCalRemoteClient();
                    await _pickleGen.ConnectAsync(ip);
                    if (_isHdr) await _pickleGen.SetupForHdrCalibrationAsync();
                    else await _pickleGen.SetupForSdrCalibrationAsync();
                    lblStatus.Text = $"✓ Connected to {_pickleGen.DeviceName}";
                    lblStatus.ForeColor = DarkTheme.Success;
                }
                catch (Exception ex)
                {
                    lblStatus.Text = $"✗ Failed: {ex.Message}";
                    lblStatus.ForeColor = DarkTheme.Error;
                }
                btnConn.Enabled = true;
            };

            AddSpacer(flow, 8);
            AddFlowLabel(flow, "Tip: Make sure both devices are on the same Wi-Fi network.",
                color: DarkTheme.TextMuted, fontSize: 9f);

            return WrapInScrollPanel(flow);
        }

        private Panel BuildConnectMeterStep()
        {
            var flow = CreateFlowPanel();

            AddFlowLabel(flow, "Point your color meter at the TV screen, centered on the test pattern area.", fontSize: 11f);
            AddSpacer(flow, 8);
            AddFlowLabel(flow, "Supported meters:", bold: true);
            AddFlowLabel(flow, "  •  X-Rite i1 Display Pro / i1 Display Studio");
            AddFlowLabel(flow, "  •  Calibrite ColorChecker Display / Display Plus");
            AddFlowLabel(flow, "  •  Datacolor SpyderX / Spyder5");
            AddFlowLabel(flow, "  •  Any ArgyllCMS-compatible meter via spotread");
            AddSpacer(flow, 12);
            AddFlowLabel(flow, "The meter will be connected via the Meters tab in the main window.", fontSize: 10f);
            AddFlowLabel(flow, "If you haven't connected it yet, you can do so after this wizard.", fontSize: 10f);
            AddSpacer(flow, 12);
            AddFlowLabel(flow, "Make sure your meter is:", bold: true);
            AddFlowLabel(flow, "  ✓  Plugged into your PC via USB");
            AddFlowLabel(flow, "  ✓  Pointing at the TV screen center (use a suction cup if available)");
            AddFlowLabel(flow, "  ✓  The room is as dark as possible for accurate readings");

            return WrapInScrollPanel(flow);
        }

        private Panel BuildConnectTvStep()
        {
            var flow = CreateFlowPanel();
            var caps = TvBrandCapabilities.For(_selectedBrand);

            if (!caps.SupportsRemoteConnection)
            {
                AddFlowLabel(flow, $"{caps.DisplayName} TVs do not support remote connection.",
                    fontSize: 12f, bold: true);
                AddFlowLabel(flow, "You can still calibrate using measurements only (no automated adjustment).");
                AddFlowLabel(flow, "Adjust TV picture settings manually using your TV remote.");
                AddSpacer(flow, 12);
                AddFlowLabel(flow, "Click Next to continue.", color: DarkTheme.AccentLight);
                return WrapInScrollPanel(flow);
            }

            string brandName = caps.DisplayName;
            AddFlowLabel(flow, $"Connect to your {brandName} TV for automated picture settings and calibration.", fontSize: 11f);
            AddFlowLabel(flow, caps.SupportsAutoCalWhiteBalance
                ? "(Enables automated calibration — white balance & CMS adjusted automatically.)"
                : "(Enables remote picture settings, but automated calibration is not yet supported.)",
                color: DarkTheme.TextSecondary, fontSize: 9f);

            AddSpacer(flow, 10);
            AddFlowLabel(flow, "Find your TV's IP address:", bold: true);
            AddFlowLabel(flow, "  Settings → Network / About → IP Address");
            AddFlowLabel(flow, "  (or check your router's device list)");
            AddSpacer(flow, 10);

            var tvRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 4, 0, 4)
            };
            var lblIp = new Label { Text = "TV IP:", AutoSize = true, ForeColor = DarkTheme.TextPrimary, Margin = new Padding(0, 7, 6, 0), BackColor = Color.Transparent };
            var txtIp = new TextBox { Size = new Size((int)(150 * _dpi), (int)(28 * _dpi)), Text = _tvIp, BackColor = DarkTheme.InputBackground, ForeColor = DarkTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 3, 8, 0) };
            var chkSecure = new CheckBox { Text = "Secure (webOS 2023+)", AutoSize = true, ForeColor = DarkTheme.TextPrimary, Visible = _selectedBrand == TvBrand.LG, Margin = new Padding(8, 6, 0, 0), BackColor = Color.Transparent };
            tvRow.Controls.AddRange(new Control[] { lblIp, txtIp, chkSecure });
            flow.Controls.Add(tvRow);

            var btnConn = new Button
            {
                Text = $"Connect to {brandName} TV",
                Size = new Size((int)(240 * _dpi), (int)(34 * _dpi)),
                BackColor = DarkTheme.AccentDark,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = DarkTheme.ButtonFont,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 6, 0, 4)
            };
            btnConn.FlatAppearance.BorderSize = 0;
            btnConn.FlatAppearance.MouseOverBackColor = DarkTheme.AccentLight;
            flow.Controls.Add(btnConn);

            var lblStatus = new Label
            {
                Text = _tvController?.IsConnected == true ? "✓ Connected to TV" : "Not connected (optional)",
                ForeColor = _tvController?.IsConnected == true ? DarkTheme.Success : DarkTheme.TextSecondary,
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                Margin = new Padding(0, 4, 0, 4),
                BackColor = Color.Transparent
            };
            flow.Controls.Add(lblStatus);

            btnConn.Click += async (_, _) =>
            {
                _tvIp = txtIp.Text.Trim();
                if (string.IsNullOrEmpty(_tvIp))
                {
                    MessageBox.Show("Enter the TV IP address.", "PickleCal", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                btnConn.Enabled = false;
                lblStatus.Text = "Connecting...";
                lblStatus.ForeColor = DarkTheme.Warning;
                try
                {
                    if (_tvController != null) { await _tvController.DisconnectAsync(); _tvController = null; }
                    _tvController = TvControllerFactory.Create(_selectedBrand, _tvIp, chkSecure.Checked);
                    await _tvController.ConnectAsync();
                    if (_tvController.IsConnected && _tvController.IsPaired)
                    {
                        lblStatus.Text = "✓ Connected — TV ready for calibration";
                        lblStatus.ForeColor = DarkTheme.Success;
                    }
                    else
                    {
                        lblStatus.Text = "⚠ Check TV — accept the pairing prompt on screen";
                        lblStatus.ForeColor = DarkTheme.Warning;
                    }
                }
                catch (Exception ex)
                {
                    lblStatus.Text = $"✗ Failed: {ex.Message}";
                    lblStatus.ForeColor = DarkTheme.Error;
                    _tvController = null;
                }
                btnConn.Enabled = true;
            };

            AddSpacer(flow, 6);
            var btnSkip = new Button
            {
                Text = "Skip — manual calibration only",
                Size = new Size((int)(250 * _dpi), (int)(32 * _dpi)),
                FlatStyle = FlatStyle.Flat,
                BackColor = DarkTheme.SurfaceLight,
                ForeColor = DarkTheme.TextSecondary,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 0, 0)
            };
            btnSkip.FlatAppearance.BorderColor = DarkTheme.Border;
            btnSkip.FlatAppearance.BorderSize = 1;
            btnSkip.Click += (_, _) => ShowStep(WizardStep.PreCalibrationSettings);
            flow.Controls.Add(btnSkip);

            return WrapInScrollPanel(flow);
        }

        private Panel BuildPreCalSettingsStep()
        {
            var flow = CreateFlowPanel();

            string modeText = _isHdr ? "HDR10 (10-bit)" : "SDR (8-bit Rec.709)";
            AddFlowLabel(flow, $"Calibration Mode: {modeText}",
                fontSize: 12f, bold: true,
                color: _isHdr ? DarkTheme.Warning : DarkTheme.AccentLight);

            AddSpacer(flow, 6);
            AddFlowLabel(flow, "Prepare your TV with these settings:", fontSize: 11f);
            AddSpacer(flow, 4);

            if (_isHdr)
            {
                AddFlowLabel(flow, "  1.  Picture Mode → 'Cinema' or 'Filmmaker Mode'");
                AddFlowLabel(flow, "  2.  HDMI Ultra HD Deep Color → On (for HDR support)");
                AddFlowLabel(flow, "  3.  Color Gamut → 'Wide' or 'Auto'");
                AddFlowLabel(flow, "  4.  Dynamic Tone Mapping → Off");
                AddFlowLabel(flow, "  5.  Noise Reduction / MPEG NR → Off");
                AddFlowLabel(flow, "  6.  TruMotion / Motion Smoothing → Off");
                AddFlowLabel(flow, "  7.  OLED Light / Backlight → 100");
            }
            else
            {
                AddFlowLabel(flow, "  1.  Picture Mode → 'ISF Expert (Dark Room)' or 'Cinema'");
                AddFlowLabel(flow, "  2.  Color Space → 'Auto' (TV menu: Picture → Color Gamut)");
                AddFlowLabel(flow, "  3.  Gamma → BT.1886 or 2.2");
                AddFlowLabel(flow, "  4.  Color Temperature → 'Warm 2' (W50)");
                AddFlowLabel(flow, "  5.  Dynamic Contrast → Off");
                AddFlowLabel(flow, "  6.  Noise Reduction / MPEG NR → Off");
                AddFlowLabel(flow, "  7.  TruMotion / Motion Smoothing → Off");
                AddFlowLabel(flow, "  8.  Backlight → 50–80 for comfortable dark room viewing");
            }

            var brandCaps = TvBrandCapabilities.For(_selectedBrand);
            if (_tvController != null && _tvController.IsConnected && brandCaps.SupportsAutoApplySettings)
            {
                AddSpacer(flow, 10);
                var btnAutoApply = new Button
                {
                    Text = "Auto-Apply Recommended Settings to TV",
                    Size = new Size((int)(340 * _dpi), (int)(36 * _dpi)),
                    BackColor = DarkTheme.AccentDark,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0, 6, 0, 4)
                };
                btnAutoApply.FlatAppearance.BorderSize = 0;
                flow.Controls.Add(btnAutoApply);

                var lblAutoStatus = new Label
                {
                    Text = "",
                    AutoSize = true,
                    ForeColor = DarkTheme.TextSecondary,
                    BackColor = Color.Transparent,
                    Margin = new Padding(0, 4, 0, 0)
                };
                flow.Controls.Add(lblAutoStatus);

                btnAutoApply.Click += async (_, _) =>
                {
                    btnAutoApply.Enabled = false;
                    lblAutoStatus.Text = "Applying settings...";
                    lblAutoStatus.ForeColor = DarkTheme.Warning;
                    try
                    {
                        var tv = _tvController!;
                        if (_isHdr)
                        {
                            await tv.SetPictureModeAsync("hdr cinema");
                            await tv.SetColorGamutAsync("wide");
                        }
                        else
                        {
                            await tv.SetPictureModeAsync("expert1");
                            await tv.SetColorGamutAsync("auto");
                            await tv.SetGammaAsync("2.2");
                            await tv.SetColorTemperatureAsync("warm50");
                        }
                        lblAutoStatus.Text = "✓ Settings applied — verify on your TV and fine-tune if needed";
                        lblAutoStatus.ForeColor = DarkTheme.Success;
                    }
                    catch (Exception ex)
                    {
                        lblAutoStatus.Text = $"✗ Failed: {ex.Message} — apply settings manually";
                        lblAutoStatus.ForeColor = DarkTheme.Error;
                    }
                    btnAutoApply.Enabled = true;
                };
            }
            else
            {
                AddSpacer(flow, 8);
                AddFlowLabel(flow, "Tip: Connect your TV (Step 4) to auto-apply these settings.",
                    color: DarkTheme.TextMuted, fontSize: 9f);
            }

            return WrapInScrollPanel(flow);
        }

        private Panel BuildBaselineMeasurementStep()
        {
            var flow = CreateFlowPanel();

            AddFlowLabel(flow, "Taking a baseline measurement to check your current display state.", fontSize: 11f);
            AddSpacer(flow, 8);
            AddFlowLabel(flow, "What happens next:", bold: true);
            AddFlowLabel(flow, "  1.  A white test pattern will be displayed on your TV");
            AddFlowLabel(flow, "  2.  Your meter will measure the white point");
            AddFlowLabel(flow, "  3.  Results will show how far off the display currently is");
            AddSpacer(flow, 8);
            AddFlowLabel(flow, "This helps us understand what needs to be corrected.");
            AddSpacer(flow, 10);
            AddFlowLabel(flow, "Make sure:", bold: true);
            AddFlowLabel(flow, "  ✓  Meter is pointed at the TV screen center");
            AddFlowLabel(flow, "  ✓  Room is darkened");
            AddFlowLabel(flow, "  ✓  No reflections on the screen");
            AddFlowLabel(flow, "  ✓  TV has been on for at least 10 minutes (warm-up)");
            AddSpacer(flow, 12);
            AddFlowLabel(flow, "Click Next to proceed to calibration.", color: DarkTheme.Success);

            return WrapInScrollPanel(flow);
        }

        private Panel BuildRunCalibrationStep()
        {
            var flow = CreateFlowPanel();

            string profileName = _profile switch
            {
                CalibrationProfile.QuickSdr => "Quick SDR",
                CalibrationProfile.FullSdr => "Full SDR",
                CalibrationProfile.QuickHdr => "Quick HDR10",
                CalibrationProfile.FullHdr => "Full HDR10",
                _ => "Custom"
            };

            AddFlowLabel(flow, $"Ready to run: {profileName} Calibration", fontSize: 13f, bold: true);
            AddSpacer(flow, 8);
            AddFlowLabel(flow, "The calibration process will:", fontSize: 11f);
            AddFlowLabel(flow, "  1.  Measure grayscale response (dark → bright)");
            AddFlowLabel(flow, "  2.  Adjust white balance for accurate grays");
            AddFlowLabel(flow, "  3.  Measure and correct primary colors (if full profile)");
            AddFlowLabel(flow, "  4.  Verify final accuracy");
            AddSpacer(flow, 10);
            AddFlowLabel(flow, "During calibration:", bold: true);
            AddFlowLabel(flow, "  •  Test patterns will appear on your TV — this is normal");
            AddFlowLabel(flow, "  •  Don't touch the TV or move the meter");
            AddFlowLabel(flow, "  •  The process is fully automatic");

            AddSpacer(flow, 10);
            var brandCapabilities = TvBrandCapabilities.For(_selectedBrand);
            if (brandCapabilities.SupportsAutoCalWhiteBalance)
            {
                AddFlowLabel(flow, $"✓ Automated calibration available for {brandCapabilities.DisplayName} — white balance and CMS will be adjusted automatically.",
                    color: DarkTheme.Success);
            }
            else
            {
                AddFlowLabel(flow, $"Automated calibration is not available for {brandCapabilities.DisplayName}.", color: DarkTheme.Warning);
                AddFlowLabel(flow, "Measurements will be taken for analysis — adjust settings manually using your TV remote.",
                    color: DarkTheme.TextSecondary, fontSize: 9f);
            }

            string estimatedTime = _profile switch
            {
                CalibrationProfile.QuickSdr => "~15 minutes",
                CalibrationProfile.FullSdr => "~30 minutes",
                CalibrationProfile.QuickHdr => "~20 minutes",
                CalibrationProfile.FullHdr => "~45 minutes",
                _ => "varies"
            };
            AddSpacer(flow, 6);
            AddFlowLabel(flow, $"Estimated time: {estimatedTime}", color: DarkTheme.Warning, fontSize: 11f);
            AddSpacer(flow, 6);
            AddFlowLabel(flow, "Click 'Start Calibration' to begin.", color: DarkTheme.Success, bold: true);

            return WrapInScrollPanel(flow);
        }

        private Panel BuildVerifyResultsStep()
        {
            var flow = CreateFlowPanel();

            AddFlowLabel(flow, "Calibration measurements are complete!", fontSize: 13f, bold: true, color: DarkTheme.Success);
            AddSpacer(flow, 8);
            AddFlowLabel(flow, "Check the Analysis tab in the main window to review:", fontSize: 11f);
            AddFlowLabel(flow, "  •  CIE Diagram — color accuracy vs target gamut");
            AddFlowLabel(flow, "  •  Gamma Curve — tonal response (should track target)");
            AddFlowLabel(flow, "  •  RGB Balance — grayscale neutrality");
            AddFlowLabel(flow, "  •  Delta E — overall error (< 3 is good, < 1 is excellent)");
            AddFlowLabel(flow, "  •  CCT Tracking — color temperature consistency");
            AddSpacer(flow, 10);
            AddFlowLabel(flow, "What to look for:", bold: true);
            AddFlowLabel(flow, "  ✓  Delta E average below 3.0 — good calibration", color: DarkTheme.Success);
            AddFlowLabel(flow, "  ✓  Delta E average below 1.0 — excellent calibration", color: DarkTheme.Success);
            AddFlowLabel(flow, "  ✗  Delta E values above 5.0 may need manual adjustment", color: DarkTheme.Warning);
            AddSpacer(flow, 10);
            AddFlowLabel(flow, "You can export results via the Analysis tab (CSV or HTML report).");

            return WrapInScrollPanel(flow);
        }

        private Panel BuildCompleteStep()
        {
            var flow = CreateFlowPanel();

            AddFlowLabel(flow, "🎉 Calibration Wizard Complete!", fontSize: 14f, bold: true, color: DarkTheme.Success);
            AddSpacer(flow, 8);
            AddFlowLabel(flow, "Your TV has been calibrated. Here's what to do next:", fontSize: 11f);
            AddSpacer(flow, 4);
            AddFlowLabel(flow, "  1.  Enjoy your calibrated picture!", fontSize: 11f);
            AddFlowLabel(flow, "  2.  Check the Analysis tab for detailed results");
            AddFlowLabel(flow, "  3.  Export a report for your records (HTML or CSV)");
            AddFlowLabel(flow, "  4.  Re-run the wizard periodically (display drift over time)");
            AddSpacer(flow, 10);
            AddFlowLabel(flow, "Tips:", bold: true);
            AddFlowLabel(flow, "  •  If the image looks too warm/orange, your eyes will adjust in a few days");
            AddFlowLabel(flow, "  •  Use the calibrated picture mode for movies and TV shows");
            AddFlowLabel(flow, "  •  For gaming, you may want a separate uncalibrated mode");
            AddFlowLabel(flow, "  •  OLED screens should be re-calibrated every 6–12 months");
            AddSpacer(flow, 12);
            AddFlowLabel(flow, "Click 'Finish' to close the wizard and return to PickleCal.",
                color: DarkTheme.AccentLight);

            return WrapInScrollPanel(flow);
        }

        // ---------- Calibration Runner ----------

        private async Task RunCalibrationAsync()
        {
            _wizardCts = new CancellationTokenSource();
            btnBack.Enabled = false;
            btnNext.Enabled = false;
            btnCancel.Text = "Stop";

            panelContent.Controls.Clear();

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                BackColor = DarkTheme.Background,
                Padding = new Padding(8)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var lblProgress = new Label
            {
                Text = "Starting calibration...",
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                ForeColor = DarkTheme.AccentLight,
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 4, 4, 8),
                BackColor = Color.Transparent
            };

            var prgCal = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Height = 22,
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Margin = new Padding(4, 0, 4, 8)
            };

            var lstLog = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = DarkTheme.SurfaceDark,
                ForeColor = DarkTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = DarkTheme.MonoFont,
                Margin = new Padding(4)
            };

            layout.Controls.Add(lblProgress, 0, 0);
            layout.Controls.Add(prgCal, 0, 1);
            layout.Controls.Add(lstLog, 0, 2);
            panelContent.Controls.Add(layout);

            void Log(string msg)
            {
                if (InvokeRequired)
                    BeginInvoke(new Action(() => Log(msg)));
                else
                {
                    lstLog.Items.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
                    lstLog.TopIndex = lstLog.Items.Count - 1;
                }
            }

            try
            {
                Log("Calibration wizard started");
                if (_pickleGen != null && _pickleGen.IsConnected)
                {
                    Log("PickleGen connected — configuring display mode...");
                    if (_isHdr)
                    {
                        await _pickleGen.SetupForHdrCalibrationAsync();
                        Log("Display configured for HDR10 (10-bit)");
                    }
                    else
                    {
                        await _pickleGen.SetupForSdrCalibrationAsync();
                        Log("Display configured for SDR (8-bit)");
                    }
                    await _pickleGen.SendBlackAsync();
                    Log("Black pattern displayed — ready for measurements");
                    Log("Waiting 3 seconds for meter warm-up...");
                    await Task.Delay(3000, _wizardCts.Token);
                }
                Log("Starting measurement sequences via main window...");
                Log("Check the Meters tab for detailed progress.");
                Log("");
                Log("The wizard has configured PickleGen and your session.");
                Log("Use the main window's Meters tab to run measurement sequences,");
                Log("or use the White Balance tab for automated calibration.");
                Log("");
                Log("Calibration configuration complete.");
                lblProgress.Text = "Configuration complete — ready for measurements";
                prgCal.Style = ProgressBarStyle.Continuous;
                prgCal.Value = 100;
            }
            catch (OperationCanceledException)
            {
                Log("Calibration cancelled by user.");
                lblProgress.Text = "Cancelled";
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
                lblProgress.Text = $"Error: {ex.Message}";
            }

            btnBack.Enabled = false;
            btnNext.Enabled = true;
            btnNext.Text = "Next →";
            btnCancel.Text = "Cancel";
        }

        // ---------- Flow Layout Helpers ----------

        private FlowLayoutPanel CreateFlowPanel()
        {
            return new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = DarkTheme.Background,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
        }

        private Panel WrapInScrollPanel(FlowLayoutPanel flow)
        {
            var scroll = new Panel
            {
                AutoScroll = true,
                BackColor = DarkTheme.Background
            };
            flow.MaximumSize = new Size(0, 0); // unconstrain for measuring
            flow.Dock = DockStyle.Top;
            flow.AutoSize = true;
            scroll.Controls.Add(flow);

            // Update flow width when scroll panel resizes
            scroll.Resize += (_, _) =>
            {
                int w = scroll.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4;
                flow.MaximumSize = new Size(w, 0);
                flow.MinimumSize = new Size(w, 0);
                foreach (Control c in flow.Controls)
                {
                    if (c is Label lbl && lbl.AutoSize)
                        lbl.MaximumSize = new Size(w - 8, 0);
                }
            };

            return scroll;
        }

        private static Label AddFlowLabel(FlowLayoutPanel flow, string text,
            float fontSize = 10f, bool bold = false, Color? color = null)
        {
            var label = new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font("Segoe UI", fontSize, bold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = color ?? DarkTheme.TextPrimary,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 2, 0, 2),
                MaximumSize = new Size(700, 0)
            };
            flow.Controls.Add(label);
            return label;
        }

        private static void AddSpacer(FlowLayoutPanel flow, int height)
        {
            flow.Controls.Add(new Panel
            {
                Size = new Size(1, height),
                BackColor = Color.Transparent,
                Margin = Padding.Empty
            });
        }

        private static RadioButton AddRadio(FlowLayoutPanel flow, string text,
            bool isChecked, Action onChecked)
        {
            var rb = new RadioButton
            {
                Text = text,
                AutoSize = true,
                ForeColor = DarkTheme.TextPrimary,
                BackColor = Color.Transparent,
                Checked = isChecked,
                Margin = new Padding(0, 2, 0, 2),
                Font = DarkTheme.DefaultFont
            };
            rb.CheckedChanged += (_, _) => { if (rb.Checked) onChecked(); };
            flow.Controls.Add(rb);
            return rb;
        }

        // ---------- Public API ----------

        public PickleCalRemoteClient? GetPickleGenClient() => _pickleGen;
        public ITvController? GetTvController() => _tvController;
        public TvBrand SelectedBrand => _selectedBrand;
        public string GetTvIp() => _tvIp;
        public bool IsHdrMode => _isHdr;
        public CalibrationProfile SelectedProfile => _profile;

        protected override void Dispose(bool disposing)
        {
            if (disposing) _wizardCts?.Dispose();
            base.Dispose(disposing);
        }
    }

    public enum CalibrationProfile
    {
        QuickSdr,
        FullSdr,
        QuickHdr,
        FullHdr
    }
}
