using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PickleCalLG.ColorScience;
using PickleCalLG.Meters;

namespace PickleCalLG.Wizard
{
    /// <summary>
    /// Step-by-step calibration wizard that guides users through the entire
    /// TV calibration process — from connecting devices to running AutoCal.
    /// Designed for users of all experience levels.
    /// </summary>
    public sealed class CalibrationWizard : Form
    {
        // Steps
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

        // Step-specific controls
        private Panel? _currentStepPanel;

        public CalibrationWizard(MainForm mainForm)
        {
            _mainForm = mainForm;
            InitializeWizardForm();
            ShowStep(WizardStep.Welcome);
        }

        private void InitializeWizardForm()
        {
            Text = "PickleCal — Calibration Wizard";
            Size = new Size(700, 550);
            MinimumSize = new Size(650, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.FromArgb(224, 224, 224);

            // Header
            panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(20, 20, 20),
                Padding = new Padding(20, 10, 20, 10)
            };

            lblStepNumber = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 180, 255),
                Location = new Point(20, 12)
            };

            lblStepTitle = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 34)
            };

            panelHeader.Controls.Add(lblStepNumber);
            panelHeader.Controls.Add(lblStepTitle);

            // Content area
            panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(30, 20, 30, 10),
                AutoScroll = true
            };

            // Footer
            panelFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = Color.FromArgb(25, 25, 25),
                Padding = new Padding(20, 10, 20, 10)
            };

            prgWizard = new ProgressBar
            {
                Location = new Point(20, 5),
                Size = new Size(640, 8),
                Style = ProgressBarStyle.Continuous,
                Maximum = 8
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(90, 35),
                Location = new Point(20, 20),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.LightGray
            };
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

            btnBack = new Button
            {
                Text = "← Back",
                Size = new Size(90, 35),
                Location = new Point(460, 20),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.LightGray
            };
            btnBack.Click += (_, _) => GoBack();

            btnNext = new Button
            {
                Text = "Next →",
                Size = new Size(120, 35),
                Location = new Point(560, 20),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(26, 115, 232),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            btnNext.Click += async (_, _) => await GoNextAsync();

            panelFooter.Controls.AddRange(new Control[] { prgWizard, btnCancel, btnBack, btnNext });

            Controls.Add(panelContent);
            Controls.Add(panelHeader);
            Controls.Add(panelFooter);
        }

        // ---------- Navigation ----------

        private void ShowStep(WizardStep step)
        {
            _currentStep = step;

            // Clear current content
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

            // Update title
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

            // Change button text for last steps
            if (step == WizardStep.Complete)
            {
                btnNext.Text = "Finish";
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
            {
                ShowStep(_currentStep - 1);
            }
        }

        private async Task GoNextAsync()
        {
            // Validate current step before proceeding
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
                // Actually run the calibration, then auto-advance
                await RunCalibrationAsync();
                ShowStep(WizardStep.VerifyResults);
                return;
            }

            if (_currentStep < WizardStep.Complete)
            {
                ShowStep(_currentStep + 1);
            }
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
                    // TV connection is optional but recommended
                    return Task.FromResult(true);

                default:
                    return Task.FromResult(true);
            }
        }

        // ---------- Step Builders ----------

        private Panel BuildWelcomeStep()
        {
            var panel = CreateStepPanel();

            AddLabel(panel, "This wizard will guide you through calibrating your TV display.", 0, fontSize: 13f);
            AddLabel(panel, "You'll need:", 40, fontSize: 12f, bold: true);
            AddLabel(panel, "  ✓  An Android device running PickleGen (phone, stick, or TV box)", 65);
            AddLabel(panel, "  ✓  A color meter (i1 Display Pro, ColorChecker, SpyderX, etc.)", 88);
            AddLabel(panel, "  ✓  Your TV (LG TVs support full AutoCal)", 111);
            AddLabel(panel, "  ✓  Both devices on the same Wi-Fi network", 134);

            AddLabel(panel, "Select your TV brand:", 170, fontSize: 12f, bold: true);

            var cmbBrand = new ComboBox
            {
                Location = new Point(10, 195),
                Size = new Size(200, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.FromArgb(224, 224, 224),
                FlatStyle = FlatStyle.Flat
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
                Location = new Point(220, 195),
                AutoSize = true,
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(100, 180, 255)
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
                        ? Color.FromArgb(76, 175, 80)
                        : Color.FromArgb(158, 158, 158);
                }
            }
            UpdateBrandCaps();
            cmbBrand.SelectedIndexChanged += (_, _) => UpdateBrandCaps();

            panel.Controls.AddRange(new Control[] { cmbBrand, lblBrandCaps });

            AddLabel(panel, "Choose your calibration profile:", 230, fontSize: 12f, bold: true);

            var rdoQuickSdr = new RadioButton
            {
                Text = "Quick SDR — Grayscale + basic colors (15 min)",
                Location = new Point(10, 255),
                AutoSize = true,
                ForeColor = Color.FromArgb(224, 224, 224),
                Checked = true
            };
            rdoQuickSdr.CheckedChanged += (_, _) => { if (rdoQuickSdr.Checked) _profile = CalibrationProfile.QuickSdr; };

            var rdoFullSdr = new RadioButton
            {
                Text = "Full SDR — Grayscale + saturation sweeps + verification (30 min)",
                Location = new Point(10, 280),
                AutoSize = true,
                ForeColor = Color.FromArgb(224, 224, 224)
            };
            rdoFullSdr.CheckedChanged += (_, _) => { if (rdoFullSdr.Checked) _profile = CalibrationProfile.FullSdr; };

            var rdoQuickHdr = new RadioButton
            {
                Text = "Quick HDR10 — HDR grayscale + basic colors (20 min)",
                Location = new Point(10, 305),
                AutoSize = true,
                ForeColor = Color.FromArgb(224, 224, 224)
            };
            rdoQuickHdr.CheckedChanged += (_, _) => { if (rdoQuickHdr.Checked) { _profile = CalibrationProfile.QuickHdr; _isHdr = true; } };

            var rdoFullHdr = new RadioButton
            {
                Text = "Full HDR10 — Complete HDR calibration with metadata (45 min)",
                Location = new Point(10, 330),
                AutoSize = true,
                ForeColor = Color.FromArgb(224, 224, 224)
            };
            rdoFullHdr.CheckedChanged += (_, _) => { if (rdoFullHdr.Checked) { _profile = CalibrationProfile.FullHdr; _isHdr = true; } };

            panel.Controls.AddRange(new Control[] { rdoQuickSdr, rdoFullSdr, rdoQuickHdr, rdoFullHdr });

            AddLabel(panel, "No calibration experience needed — just follow each step!", 370,
                color: Color.FromArgb(76, 175, 80), fontSize: 11f);

            return panel;
        }

        private Panel BuildConnectPickleGenStep()
        {
            var panel = CreateStepPanel();

            AddLabel(panel, "PickleGen generates test patterns on your TV via an Android device.", 0, fontSize: 12f);
            AddLabel(panel, "1. Open the PickleGen app on your Android device", 30);
            AddLabel(panel, "2. Select 'Easy Mode'", 53);
            AddLabel(panel, "3. Note the IP address shown on screen", 76);
            AddLabel(panel, "4. Enter it below and click 'Connect'", 99);

            var lblIp = new Label { Text = "PickleGen IP:", AutoSize = true, Location = new Point(10, 140), ForeColor = Color.FromArgb(224, 224, 224) };
            var txtIp = new TextBox { Location = new Point(110, 137), Size = new Size(150, 23), Text = _pickleGenIp };

            var btnConnect = new Button
            {
                Text = "Connect",
                Location = new Point(270, 135),
                Size = new Size(100, 28),
                BackColor = Color.FromArgb(26, 115, 232),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            var lblStatus = new Label
            {
                Text = _pickleGen?.IsConnected == true ? "✓ Connected" : "Not connected",
                ForeColor = _pickleGen?.IsConnected == true ? Color.FromArgb(76, 175, 80) : Color.FromArgb(158, 158, 158),
                AutoSize = true,
                Location = new Point(10, 180),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold)
            };

            btnConnect.Click += async (_, _) =>
            {
                string ip = txtIp.Text.Trim();
                if (string.IsNullOrEmpty(ip))
                {
                    MessageBox.Show("Enter the IP address from the PickleGen screen.", "PickleCal", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                _pickleGenIp = ip;
                btnConnect.Enabled = false;
                lblStatus.Text = "Connecting...";
                lblStatus.ForeColor = Color.FromArgb(255, 171, 0);

                try
                {
                    if (_pickleGen != null) await _pickleGen.DisposeAsync();
                    _pickleGen = new PickleCalRemoteClient();
                    await _pickleGen.ConnectAsync(ip);

                    // Configure for selected mode
                    if (_isHdr)
                        await _pickleGen.SetupForHdrCalibrationAsync();
                    else
                        await _pickleGen.SetupForSdrCalibrationAsync();

                    lblStatus.Text = $"✓ Connected to {_pickleGen.DeviceName}";
                    lblStatus.ForeColor = Color.FromArgb(76, 175, 80);
                }
                catch (Exception ex)
                {
                    lblStatus.Text = $"✗ Failed: {ex.Message}";
                    lblStatus.ForeColor = Color.FromArgb(244, 67, 54);
                }

                btnConnect.Enabled = true;
            };

            panel.Controls.AddRange(new Control[] { lblIp, txtIp, btnConnect, lblStatus });

            AddLabel(panel, "Tip: Make sure both devices are on the same Wi-Fi network.", 220,
                color: Color.FromArgb(158, 158, 158), fontSize: 10f);

            return panel;
        }

        private Panel BuildConnectMeterStep()
        {
            var panel = CreateStepPanel();

            AddLabel(panel, "Point your color meter at the TV screen, centered on the test pattern area.", 0, fontSize: 12f);
            AddLabel(panel, "Supported meters:", 30, bold: true);
            AddLabel(panel, "  •  X-Rite i1 Display Pro / i1 Display Studio", 55);
            AddLabel(panel, "  •  Calibrite ColorChecker Display / Display Plus", 78);
            AddLabel(panel, "  •  Datacolor SpyderX / Spyder5", 101);
            AddLabel(panel, "  •  Any ArgyllCMS-compatible meter via spotread", 124);

            AddLabel(panel, "The meter will be connected via the Meters tab in the main window.", 165, fontSize: 11f);
            AddLabel(panel, "If you haven't connected it yet, you can do so after this wizard.", 188, fontSize: 11f);

            AddLabel(panel, "Make sure your meter is:", 230, bold: true);
            AddLabel(panel, "  ✓  Plugged into your PC via USB", 255);
            AddLabel(panel, "  ✓  Pointing at the TV screen center (use a suction cup if available)", 278);
            AddLabel(panel, "  ✓  The room is as dark as possible for accurate readings", 301);

            return panel;
        }

        private Panel BuildConnectTvStep()
        {
            var panel = CreateStepPanel();
            var caps = TvBrandCapabilities.For(_selectedBrand);

            if (!caps.SupportsRemoteConnection)
            {
                // Brand doesn't support remote connection — skip TV connection
                AddLabel(panel, $"{caps.DisplayName} TVs do not support remote connection.", 0, fontSize: 13f, bold: true);
                AddLabel(panel, "You can still calibrate using measurements only (no AutoCal).", 30);
                AddLabel(panel, "Adjust TV picture settings manually using your TV remote.", 55);
                AddLabel(panel, "Click Next to continue.", 95,
                    color: Color.FromArgb(100, 180, 255), fontSize: 12f);
                return panel;
            }

            string brandName = caps.DisplayName;
            AddLabel(panel, $"Connect to your {brandName} TV for automated picture settings and calibration.", 0, fontSize: 12f);

            string autoCalNote = caps.SupportsAutoCalWhiteBalance
                ? "(Enables AutoCal — automatic white balance & CMS adjustment.)"
                : "(Enables remote picture settings, but AutoCal is not yet supported.)";
            AddLabel(panel, autoCalNote, 25,
                color: Color.FromArgb(158, 158, 158), fontSize: 10f);

            AddLabel(panel, "Find your TV's IP address:", 60, bold: true);
            AddLabel(panel, "  Settings → Network / About → IP Address", 85);
            AddLabel(panel, "  (or check your router's device list)", 108);

            var lblIp = new Label { Text = "TV IP:", AutoSize = true, Location = new Point(10, 150), ForeColor = Color.FromArgb(224, 224, 224) };
            var txtIp = new TextBox { Location = new Point(60, 147), Size = new Size(150, 23), Text = _tvIp };
            var chkSecure = new CheckBox
            {
                Text = "Secure (webOS 2023+)",
                AutoSize = true,
                Location = new Point(220, 150),
                ForeColor = Color.FromArgb(200, 200, 200),
                Visible = _selectedBrand == TvBrand.LG // Only LG uses secure WebSocket
            };

            var btnConnect = new Button
            {
                Text = $"Connect to {brandName} TV",
                Location = new Point(10, 185),
                Size = new Size(180, 32),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnConnect.FlatAppearance.BorderSize = 0;

            var lblStatus = new Label
            {
                Text = _tvController?.IsConnected == true ? "✓ Connected to TV" : "Not connected (optional)",
                ForeColor = _tvController?.IsConnected == true ? Color.FromArgb(76, 175, 80) : Color.FromArgb(158, 158, 158),
                AutoSize = true,
                Location = new Point(10, 228),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold)
            };

            btnConnect.Click += async (_, _) =>
            {
                _tvIp = txtIp.Text.Trim();
                if (string.IsNullOrEmpty(_tvIp))
                {
                    MessageBox.Show("Enter the TV IP address.", "PickleCal", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                btnConnect.Enabled = false;
                lblStatus.Text = "Connecting...";
                lblStatus.ForeColor = Color.FromArgb(255, 171, 0);

                try
                {
                    if (_tvController != null)
                    {
                        await _tvController.DisconnectAsync();
                        _tvController = null;
                    }

                    // Create appropriate controller based on brand
                    _tvController = _selectedBrand == TvBrand.LG
                        ? new LgTvController(_tvIp, chkSecure.Checked)
                        : new GenericTvController(_selectedBrand, _tvIp);

                    await _tvController.ConnectAsync();

                    if (_tvController.IsConnected && _tvController.IsPaired)
                    {
                        lblStatus.Text = "✓ Connected — TV ready for calibration";
                        lblStatus.ForeColor = Color.FromArgb(76, 175, 80);
                    }
                    else
                    {
                        lblStatus.Text = "⚠ Check TV — accept the pairing prompt on screen";
                        lblStatus.ForeColor = Color.FromArgb(255, 171, 0);
                    }
                }
                catch (Exception ex)
                {
                    lblStatus.Text = $"✗ Failed: {ex.Message}";
                    lblStatus.ForeColor = Color.FromArgb(244, 67, 54);
                    _tvController = null;
                }

                btnConnect.Enabled = true;
            };

            var btnSkip = new Button
            {
                Text = "Skip — manual calibration only",
                Location = new Point(10, 270),
                Size = new Size(230, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.LightGray
            };
            btnSkip.Click += (_, _) => ShowStep(WizardStep.PreCalibrationSettings);

            panel.Controls.AddRange(new Control[] { lblIp, txtIp, chkSecure, btnConnect, lblStatus, btnSkip });

            return panel;
        }

        private Panel BuildPreCalSettingsStep()
        {
            var panel = CreateStepPanel();

            string modeText = _isHdr ? "HDR10 (10-bit)" : "SDR (8-bit Rec.709)";
            AddLabel(panel, $"Calibration Mode: {modeText}", 0, fontSize: 13f, bold: true,
                color: _isHdr ? Color.FromArgb(255, 171, 0) : Color.FromArgb(100, 180, 255));

            AddLabel(panel, "Prepare your TV with these settings:", 35, fontSize: 12f);

            if (_isHdr)
            {
                AddLabel(panel, "  1. Picture Mode → 'Cinema' or 'Filmmaker Mode'", 60);
                AddLabel(panel, "  2. HDMI Ultra HD Deep Color → On (for HDR support)", 83);
                AddLabel(panel, "  3. Color Gamut → 'Wide' or 'Auto'", 106);
                AddLabel(panel, "  4. Dynamic Tone Mapping → Off", 129);
                AddLabel(panel, "  5. Noise Reduction / MPEG NR → Off", 152);
                AddLabel(panel, "  6. TruMotion / Motion Smoothing → Off", 175);
                AddLabel(panel, "  7. OLED Light / Backlight → 100", 198);
            }
            else
            {
                AddLabel(panel, "  1. Picture Mode → 'ISF Expert (Dark Room)' or 'Cinema'", 60);
                AddLabel(panel, "  2. Color Space → 'Auto' (TV menu: Picture → Color Gamut)", 83);
                AddLabel(panel, "  3. Gamma → BT.1886 or 2.2", 106);
                AddLabel(panel, "  4. Color Temperature → 'Warm 2' (W50)", 129);
                AddLabel(panel, "  5. Dynamic Contrast → Off", 152);
                AddLabel(panel, "  6. Noise Reduction / MPEG NR → Off", 175);
                AddLabel(panel, "  7. TruMotion / Motion Smoothing → Off", 198);
                AddLabel(panel, "  8. Backlight → 50–80 for comfortable dark room viewing", 221);
            }

            // Auto-apply button when TV is connected and brand supports it
            var brandCaps = TvBrandCapabilities.For(_selectedBrand);
            if (_tvController != null && _tvController.IsConnected && brandCaps.SupportsAutoApplySettings)
            {
                var btnAutoApply = new Button
                {
                    Text = "Auto-Apply Recommended Settings to TV",
                    Location = new Point(10, _isHdr ? 235 : 258),
                    Size = new Size(300, 35),
                    BackColor = Color.FromArgb(0, 122, 204),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold)
                };
                btnAutoApply.FlatAppearance.BorderSize = 0;

                var lblAutoStatus = new Label
                {
                    Text = "",
                    AutoSize = true,
                    Location = new Point(10, _isHdr ? 278 : 300),
                    ForeColor = Color.FromArgb(158, 158, 158)
                };

                btnAutoApply.Click += async (_, _) =>
                {
                    btnAutoApply.Enabled = false;
                    lblAutoStatus.Text = "Applying settings...";
                    lblAutoStatus.ForeColor = Color.FromArgb(255, 171, 0);

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
                        lblAutoStatus.ForeColor = Color.FromArgb(76, 175, 80);
                    }
                    catch (Exception ex)
                    {
                        lblAutoStatus.Text = $"✗ Failed: {ex.Message} — apply settings manually";
                        lblAutoStatus.ForeColor = Color.FromArgb(244, 67, 54);
                    }

                    btnAutoApply.Enabled = true;
                };

                panel.Controls.AddRange(new Control[] { btnAutoApply, lblAutoStatus });
            }
            else
            {
                int yOffset = _isHdr ? 235 : 258;
                AddLabel(panel, "Tip: Connect your TV (Step 4) to auto-apply these settings.", yOffset,
                    color: Color.FromArgb(158, 158, 158), fontSize: 10f);
            }

            return panel;
        }

        private Panel BuildBaselineMeasurementStep()
        {
            var panel = CreateStepPanel();

            AddLabel(panel, "Taking a baseline measurement to check your current display state.", 0, fontSize: 12f);

            AddLabel(panel, "What happens next:", 35, bold: true);
            AddLabel(panel, "  1. A white test pattern will be displayed on your TV", 60);
            AddLabel(panel, "  2. Your meter will measure the white point", 83);
            AddLabel(panel, "  3. Results will show how far off the display currently is", 106);

            AddLabel(panel, "This helps us understand what needs to be corrected.", 145);

            AddLabel(panel, "Make sure:", 180, bold: true);
            AddLabel(panel, "  ✓  Meter is pointed at the TV screen center", 205);
            AddLabel(panel, "  ✓  Room is darkened", 228);
            AddLabel(panel, "  ✓  No reflections on the screen", 251);
            AddLabel(panel, "  ✓  TV has been on for at least 10 minutes (warm-up)", 274);

            AddLabel(panel, "Click Next to proceed to calibration.", 315,
                color: Color.FromArgb(76, 175, 80), fontSize: 11f);

            return panel;
        }

        private Panel BuildRunCalibrationStep()
        {
            var panel = CreateStepPanel();

            string profileName = _profile switch
            {
                CalibrationProfile.QuickSdr => "Quick SDR",
                CalibrationProfile.FullSdr => "Full SDR",
                CalibrationProfile.QuickHdr => "Quick HDR10",
                CalibrationProfile.FullHdr => "Full HDR10",
                _ => "Custom"
            };

            AddLabel(panel, $"Ready to run: {profileName} Calibration", 0, fontSize: 14f, bold: true);

            AddLabel(panel, "The calibration process will:", 40, fontSize: 12f);
            AddLabel(panel, "  1. Measure grayscale response (dark → bright)", 65);
            AddLabel(panel, "  2. Adjust white balance for accurate grays", 88);
            AddLabel(panel, "  3. Measure and correct primary colors (if full profile)", 111);
            AddLabel(panel, "  4. Verify final accuracy", 134);

            AddLabel(panel, "During calibration:", 175, bold: true);
            AddLabel(panel, "  •  Test patterns will appear on your TV — this is normal", 200);
            AddLabel(panel, "  •  Don't touch the TV or move the meter", 223);
            AddLabel(panel, "  •  The process is fully automatic", 246);

            var brandCapabilities = TvBrandCapabilities.For(_selectedBrand);
            if (brandCapabilities.SupportsAutoCalWhiteBalance)
            {
                AddLabel(panel, $"✓ AutoCal is available for {brandCapabilities.DisplayName} — white balance and CMS will be adjusted automatically.", 270,
                    color: Color.FromArgb(76, 175, 80), fontSize: 11f);
            }
            else
            {
                AddLabel(panel, $"AutoCal is not available for {brandCapabilities.DisplayName}.", 270,
                    color: Color.FromArgb(255, 171, 0), fontSize: 11f);
                AddLabel(panel, "Measurements will be taken for analysis — adjust settings manually using your TV remote.", 293,
                    color: Color.FromArgb(158, 158, 158), fontSize: 10f);
            }

            string estimatedTime = _profile switch
            {
                CalibrationProfile.QuickSdr => "~15 minutes",
                CalibrationProfile.FullSdr => "~30 minutes",
                CalibrationProfile.QuickHdr => "~20 minutes",
                CalibrationProfile.FullHdr => "~45 minutes",
                _ => "varies"
            };
            AddLabel(panel, $"Estimated time: {estimatedTime}", 320, fontSize: 12f,
                color: Color.FromArgb(255, 171, 0));

            AddLabel(panel, "Click 'Start Calibration' to begin.", 350,
                color: Color.FromArgb(76, 175, 80), fontSize: 12f, bold: true);

            return panel;
        }

        private Panel BuildVerifyResultsStep()
        {
            var panel = CreateStepPanel();

            AddLabel(panel, "Calibration measurements are complete!", 0, fontSize: 14f, bold: true,
                color: Color.FromArgb(76, 175, 80));

            AddLabel(panel, "Check the Analysis tab in the main window to review:", 40, fontSize: 12f);
            AddLabel(panel, "  •  CIE Diagram — color accuracy vs target gamut", 65);
            AddLabel(panel, "  •  Gamma Curve — tonal response (should track target)", 88);
            AddLabel(panel, "  •  RGB Balance — grayscale neutrality", 111);
            AddLabel(panel, "  •  Delta E — overall error (< 3 is good, < 1 is excellent)", 134);
            AddLabel(panel, "  •  CCT Tracking — color temperature consistency", 157);

            AddLabel(panel, "What to look for:", 200, bold: true);
            AddLabel(panel, "  ✓  Delta E average below 3.0 — good calibration", 225,
                color: Color.FromArgb(76, 175, 80));
            AddLabel(panel, "  ✓  Delta E average below 1.0 — excellent calibration", 248,
                color: Color.FromArgb(76, 175, 80));
            AddLabel(panel, "  ✗  Delta E values above 5.0 may need manual adjustment", 271,
                color: Color.FromArgb(255, 171, 0));

            AddLabel(panel, "You can export results via the Analysis tab (CSV or HTML report).", 310);

            return panel;
        }

        private Panel BuildCompleteStep()
        {
            var panel = CreateStepPanel();

            AddLabel(panel, "🎉 Calibration Wizard Complete!", 0, fontSize: 16f, bold: true,
                color: Color.FromArgb(76, 175, 80));

            AddLabel(panel, "Your TV has been calibrated. Here's what to do next:", 45, fontSize: 12f);

            AddLabel(panel, "  1. Enjoy your calibrated picture!", 75, fontSize: 12f);
            AddLabel(panel, "  2. Check the Analysis tab for detailed results", 100);
            AddLabel(panel, "  3. Export a report for your records (HTML or CSV)", 123);
            AddLabel(panel, "  4. Re-run the wizard periodically (display drift over time)", 146);

            AddLabel(panel, "Tips:", 190, bold: true);
            AddLabel(panel, "  •  If the image looks too warm/orange, your eyes will adjust in a few days", 215);
            AddLabel(panel, "  •  Use the calibrated picture mode for movies and TV shows", 238);
            AddLabel(panel, "  •  For gaming, you may want a separate uncalibrated mode", 261);
            AddLabel(panel, "  •  OLED screens should be re-calibrated every 6-12 months", 284);

            AddLabel(panel, "Click 'Finish' to close the wizard and return to PickleCal.", 325,
                color: Color.FromArgb(100, 180, 255), fontSize: 11f);

            return panel;
        }

        // ---------- Calibration Runner ----------

        private async Task RunCalibrationAsync()
        {
            _wizardCts = new CancellationTokenSource();
            btnBack.Enabled = false;
            btnNext.Enabled = false;
            btnCancel.Text = "Stop";

            // Show progress UI
            panelContent.Controls.Clear();
            var progressPanel = CreateStepPanel();

            var lblProgress = new Label
            {
                Text = "Starting calibration...",
                Location = new Point(10, 0),
                AutoSize = true,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 180, 255)
            };

            var prgCal = new ProgressBar
            {
                Location = new Point(10, 35),
                Size = new Size(600, 25),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30
            };

            var lstLog = new ListBox
            {
                Location = new Point(10, 70),
                Size = new Size(600, 250),
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.FromArgb(200, 200, 200),
                BorderStyle = BorderStyle.FixedSingle
            };

            progressPanel.Controls.AddRange(new Control[] { lblProgress, prgCal, lstLog });
            progressPanel.Dock = DockStyle.Fill;
            panelContent.Controls.Add(progressPanel);

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

                    // Send initial black pattern
                    await _pickleGen.SendBlackAsync();
                    Log("Black pattern displayed — ready for measurements");

                    // Wait for meter warm-up
                    Log("Waiting 3 seconds for meter warm-up...");
                    await Task.Delay(3000, _wizardCts.Token);
                }

                // Dispatch to the main form's measurement system
                Log("Starting measurement sequences via main window...");
                Log("Check the Meters tab for detailed progress.");
                Log("");
                Log("The wizard has configured PickleGen and your session.");
                Log("Use the main window's Meters tab to run measurement sequences,");
                Log("or use AutoCal on the White Balance tab for automatic calibration.");
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

        // ---------- Helpers ----------

        private static Panel CreateStepPanel()
        {
            return new Panel
            {
                AutoScroll = true,
                BackColor = Color.FromArgb(30, 30, 30)
            };
        }

        private static Label AddLabel(Panel panel, string text, int top,
            float fontSize = 11f, bool bold = false, Color? color = null)
        {
            var label = new Label
            {
                Text = text,
                AutoSize = true,
                Location = new Point(10, top),
                Font = new Font("Segoe UI", fontSize, bold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = color ?? Color.FromArgb(224, 224, 224),
                MaximumSize = new Size(600, 0)
            };
            panel.Controls.Add(label);
            return label;
        }

        /// <summary>Access the configured PickleGen client for the main form to use.</summary>
        public PickleCalRemoteClient? GetPickleGenClient() => _pickleGen;

        /// <summary>Access the connected TV controller for the main form to use.</summary>
        public ITvController? GetTvController() => _tvController;

        /// <summary>Access the selected TV brand.</summary>
        public TvBrand SelectedBrand => _selectedBrand;

        /// <summary>Access the TV IP entered in the wizard.</summary>
        public string GetTvIp() => _tvIp;

        /// <summary>Whether HDR mode was selected.</summary>
        public bool IsHdrMode => _isHdr;

        /// <summary>The selected calibration profile.</summary>
        public CalibrationProfile SelectedProfile => _profile;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _wizardCts?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>Calibration profile presets for the wizard.</summary>
    public enum CalibrationProfile
    {
        QuickSdr,
        FullSdr,
        QuickHdr,
        FullHdr
    }
}
