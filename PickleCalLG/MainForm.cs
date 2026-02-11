using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PickleCalLG.AutoCal;
using PickleCalLG.ColorScience;
using PickleCalLG.Export;
using PickleCalLG.Meters;
using PickleCalLG.Meters.Argyll;
using PickleCalLG.Meters.Simulation;
using PickleCalLG.Meters.Sequences;
using PickleCalLG.Views;

namespace PickleCalLG
{
    public partial class MainForm : Form
    {
        private ITvController? _tvController;
        private TvBrand _selectedBrand = TvBrand.LG;
        private PGenServer? _pgenServer;
        private PGenClient? _pgenClient;
        private PickleCalRemoteClient? _pickleGenRemote;
        private string _tvIp = "";
        private MeterManager _meterManager;
        private readonly CancellationTokenSource _meterCancellation = new();
        private MeterMeasurementState _lastMeterState = MeterMeasurementState.Disconnected;
        private MeasurementQueueRunner? _sequenceRunner;
        private CancellationTokenSource? _sequenceCancellation;
        private bool _sequenceRunning;

        private TabControl tabControl1 = null!;
        private TabPage tabPageConnection = null!;
        private TabPage tabPageTvControl = null!;
        private TabPage tabPagePictureSettings = null!;
        private TabPage tabPageWhiteBalance = null!;
        private TabPage tabPagePatternGen = null!;
        private TabPage tabPageMeter = null!;
        private GroupBox groupBox1 = null!;
        private ComboBox cmbTvBrand = null!;
        private Label lblTvBrand = null!;
        private CheckBox chkSecureConnection = null!;
        private TextBox txtTvIp = null!;
        private Label label1 = null!;
        private Button btnDisconnect = null!;
        private Button btnConnect = null!;
        private Label lblStatus = null!;
        private GroupBox groupBox2 = null!;
        private Button btnApplyPictureMode = null!;
        private ComboBox cmbPictureMode = null!;
        private Label label8 = null!;
        private GroupBox groupBox3 = null!;
        private Button btnApplyColorSettings = null!;
        private ComboBox cmbColorTemp = null!;
        private ComboBox cmbGamma = null!;
        private ComboBox cmbColorGamut = null!;
        private Label label11 = null!;
        private Label label10 = null!;
        private Label label9 = null!;
        private GroupBox groupBox4 = null!;
        private Button btnDisableProcessing = null!;
        private Button btnReadSettings = null!;
        private GroupBox groupBoxMeterSelect = null!;
        private GroupBox groupBoxMeterControl = null!;
        private ComboBox cmbMeters = null!;
        private Button btnMeterRefresh = null!;
        private Button btnMeterConnect = null!;
        private Button btnMeterDisconnect = null!;
        private Button btnMeterCalibrate = null!;
        private Button btnMeterMeasure = null!;
        private CheckBox chkMeterAveraging = null!;
        private CheckBox chkMeterHighRes = null!;
        private TextBox txtMeterDisplayType = null!;
        private Label lblMeterStatus = null!;
        private Label lblMeterMeasurement = null!;
        private Label lblMeterDisplayType = null!;
        private GroupBox groupBoxMeasurementSequence = null!;
        private Button btnRunGrayscale = null!;
        private Button btnRunColorSweep = null!;
        private Button btnStopSequence = null!;
        private ListBox lstMeasurementLog = null!;
        private Label lblSequenceStatus = null!;
        private ComboBox cmbPatternSource = null!;
        private Label lblPatternSource = null!;
        private PatternPlaybackMode _patternMode = PatternPlaybackMode.Manual;

        // Calibration session & analysis
        private CalibrationSession? _session;

        // Analysis tab pages
        private TabPage tabPageAnalysis = null!;
        private TabControl tabAnalysisViews = null!;
        private TabPage tabAnalysisCie = null!;
        private TabPage tabAnalysisGamma = null!;
        private TabPage tabAnalysisRgb = null!;
        private TabPage tabAnalysisLuminance = null!;
        private TabPage tabAnalysisDeltaE = null!;
        private TabPage tabAnalysisCct = null!;
        private TabPage tabAnalysisGrid = null!;

        // Analysis chart controls
        private CieDiagramControl _cieDiagram = null!;
        private GammaCurveControl _gammaChart = null!;
        private RgbBalanceControl _rgbBalance = null!;
        private LuminanceCurveControl _luminanceChart = null!;
        private DeltaEChartControl _deltaEChart = null!;
        private CctTrackingControl _cctChart = null!;
        private MeasurementDataGrid _dataGrid = null!;

        // Session settings controls
        private GroupBox groupBoxSession = null!;
        private ComboBox cmbTargetColorSpace = null!;
        private ComboBox cmbTargetEotf = null!;
        private TextBox txtTargetGamma = null!;
        private TextBox txtDisplayName = null!;
        private Button btnNewSession = null!;
        private Label lblSessionInfo = null!;
        private Label lblTargetColorSpace = null!;
        private Label lblTargetEotf = null!;
        private Label lblTargetGamma = null!;
        private Label lblDisplayName = null!;

        // Export buttons
        private Button btnExportCsv = null!;
        private Button btnExportHtml = null!;

        // Additional sequence buttons
        private Button btnRunNearBlack = null!;
        private Button btnRunNearWhite = null!;
        private Button btnRunSaturation = null!;
        private Button btnRunMeasureAll = null!;

        // AutoCal controls
        private GroupBox groupBoxAutoCal = null!;
        private Button btnAutoCal2ptWb = null!;
        private Button btnAutoCal20ptWb = null!;
        private Button btnAutoCalCms = null!;
        private Button btnAutoCalFull = null!;
        private Button btnAutoCalStop = null!;
        private Button btnResetWb = null!;
        private Button btnResetCms = null!;
        private Label lblAutoCalStatus = null!;
        private ProgressBar prgAutoCal = null!;
        private CancellationTokenSource? _autoCalCancellation;

        // PickleGen Easy mode controls (Pattern Gen tab)
        private GroupBox groupBoxPickleGen = null!;
        private TextBox txtPickleGenIp = null!;
        private Label lblPickleGenIp = null!;
        private Button btnPickleGenConnect = null!;
        private Button btnPickleGenDisconnect = null!;
        private Button btnPickleGenDiscover = null!;
        private Label lblPickleGenStatus = null!;
        private GroupBox groupBoxPickleGenMode = null!;
        private RadioButton rdoPickleGenSdr = null!;
        private RadioButton rdoPickleGenHdr = null!;
        private Button btnPickleGenApplyMode = null!;
        private Button btnPickleGenTestWhite = null!;
        private Button btnPickleGenTestBlack = null!;
        private Label lblPickleGenDevice = null!;
        private Button btnLaunchWizard = null!;

        public MainForm()
        {
            InitializeComponent();
            InitializeUI();

            // Apply modern dark theme
            DarkTheme.Apply(this);
            StyleAccentControls();

            _meterManager = new MeterManager(
                new CompositeMeterDiscoveryService(
                    new ArgyllSpotreadDiscoveryService(),
                    new SimulatedMeterDiscoveryService()));
            _meterManager.MeterStateChanged += MeterManagerOnStateChanged;
            _meterManager.MeasurementAvailable += MeterManagerOnMeasurement;
            Load += MainForm_LoadAsync;
            FormClosing += MainForm_FormClosing;
        }

        private void StyleAccentControls()
        {
            // Primary action buttons (accent blue)
            DarkTheme.MakeAccentButton(btnConnect);
            DarkTheme.MakeAccentButton(btnMeterConnect);
            DarkTheme.MakeAccentButton(btnPickleGenConnect);
            DarkTheme.MakeAccentButton(btnApplyPictureMode);
            DarkTheme.MakeAccentButton(btnApplyColorSettings);
            DarkTheme.MakeAccentButton(btnPickleGenApplyMode);
            DarkTheme.MakeAccentButton(btnNewSession);
            DarkTheme.MakeAccentButton(btnMeterMeasure);
            DarkTheme.MakeAccentButton(btnMeterCalibrate);

            // Success buttons (green) - full calibration
            DarkTheme.MakeSuccessButton(btnAutoCalFull);
            DarkTheme.MakeSuccessButton(btnRunMeasureAll);

            // Danger buttons (red) - stop/disconnect
            DarkTheme.MakeDangerButton(btnStopSequence);
            DarkTheme.MakeDangerButton(btnAutoCalStop);
            DarkTheme.MakeDangerButton(btnDisconnect);
            DarkTheme.MakeDangerButton(btnMeterDisconnect);
            DarkTheme.MakeDangerButton(btnPickleGenDisconnect);

            // Wizard hero button
            DarkTheme.MakeHeroButton(btnLaunchWizard,
                "Launch Calibration Wizard\r\nStep-by-step guided calibration");

            // Form styling
            Text = "PickleCal";
            StartPosition = FormStartPosition.CenterScreen;
        }

        private void InitializeComponent()
        {
            tabControl1 = new TabControl();
            tabPageConnection = new TabPage();
            tabPageTvControl = new TabPage();
            tabPagePictureSettings = new TabPage();
            tabPageWhiteBalance = new TabPage();
            tabPagePatternGen = new TabPage();
            tabPageMeter = new TabPage();
            groupBox1 = new GroupBox();
            cmbTvBrand = new ComboBox();
            lblTvBrand = new Label();
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
            groupBoxMeterSelect = new GroupBox();
            groupBoxMeterControl = new GroupBox();
            groupBoxMeasurementSequence = new GroupBox();
            cmbMeters = new ComboBox();
            btnMeterRefresh = new Button();
            btnMeterConnect = new Button();
            btnMeterDisconnect = new Button();
            btnMeterCalibrate = new Button();
            btnMeterMeasure = new Button();
            chkMeterAveraging = new CheckBox();
            chkMeterHighRes = new CheckBox();
            txtMeterDisplayType = new TextBox();
            lblMeterStatus = new Label();
            lblMeterMeasurement = new Label();
            lblMeterDisplayType = new Label();
            btnRunGrayscale = new Button();
            btnRunColorSweep = new Button();
            btnStopSequence = new Button();
            lstMeasurementLog = new ListBox();
            lblSequenceStatus = new Label();
            cmbPatternSource = new ComboBox();
            lblPatternSource = new Label();

            // Analysis tab + sub-tabs + chart controls
            tabPageAnalysis = new TabPage();
            tabAnalysisViews = new TabControl();
            tabAnalysisCie = new TabPage();
            tabAnalysisGamma = new TabPage();
            tabAnalysisRgb = new TabPage();
            tabAnalysisLuminance = new TabPage();
            tabAnalysisDeltaE = new TabPage();
            tabAnalysisCct = new TabPage();
            tabAnalysisGrid = new TabPage();

            _cieDiagram = new CieDiagramControl();
            _gammaChart = new GammaCurveControl();
            _rgbBalance = new RgbBalanceControl();
            _luminanceChart = new LuminanceCurveControl();
            _deltaEChart = new DeltaEChartControl();
            _cctChart = new CctTrackingControl();
            _dataGrid = new MeasurementDataGrid();

            // Session settings
            groupBoxSession = new GroupBox();
            cmbTargetColorSpace = new ComboBox();
            cmbTargetEotf = new ComboBox();
            txtTargetGamma = new TextBox();
            txtDisplayName = new TextBox();
            btnNewSession = new Button();
            lblSessionInfo = new Label();
            lblTargetColorSpace = new Label();
            lblTargetEotf = new Label();
            lblTargetGamma = new Label();
            lblDisplayName = new Label();

            // Export buttons
            btnExportCsv = new Button();
            btnExportHtml = new Button();

            // Additional sequence buttons
            btnRunNearBlack = new Button();
            btnRunNearWhite = new Button();
            btnRunSaturation = new Button();
            btnRunMeasureAll = new Button();

            // AutoCal controls
            groupBoxAutoCal = new GroupBox();
            btnAutoCal2ptWb = new Button();
            btnAutoCal20ptWb = new Button();
            btnAutoCalCms = new Button();
            btnAutoCalFull = new Button();
            btnAutoCalStop = new Button();
            btnResetWb = new Button();
            btnResetCms = new Button();
            lblAutoCalStatus = new Label();
            prgAutoCal = new ProgressBar();

            // PickleGen Easy mode controls
            groupBoxPickleGen = new GroupBox();
            txtPickleGenIp = new TextBox();
            lblPickleGenIp = new Label();
            btnPickleGenConnect = new Button();
            btnPickleGenDisconnect = new Button();
            btnPickleGenDiscover = new Button();
            lblPickleGenStatus = new Label();
            groupBoxPickleGenMode = new GroupBox();
            rdoPickleGenSdr = new RadioButton();
            rdoPickleGenHdr = new RadioButton();
            btnPickleGenApplyMode = new Button();
            btnPickleGenTestWhite = new Button();
            btnPickleGenTestBlack = new Button();
            lblPickleGenDevice = new Label();
            btnLaunchWizard = new Button();

            tabControl1.SuspendLayout();
            tabPageConnection.SuspendLayout();
            groupBox1.SuspendLayout();
            tabPageTvControl.SuspendLayout();
            groupBox2.SuspendLayout();
            tabPagePictureSettings.SuspendLayout();
            groupBox3.SuspendLayout();
            tabPageWhiteBalance.SuspendLayout();
            tabPageMeter.SuspendLayout();
            groupBoxMeterSelect.SuspendLayout();
            groupBoxMeterControl.SuspendLayout();
            groupBoxMeasurementSequence.SuspendLayout();
            SuspendLayout();

            // tabControl1
            tabControl1.Controls.Add(tabPageConnection);
            tabControl1.Controls.Add(tabPageTvControl);
            tabControl1.Controls.Add(tabPagePictureSettings);
            tabControl1.Controls.Add(tabPageWhiteBalance);
            tabControl1.Controls.Add(tabPagePatternGen);
            tabControl1.Controls.Add(tabPageMeter);
            tabControl1.Controls.Add(tabPageAnalysis);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1100, 700);
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
            groupBox1.Controls.Add(cmbTvBrand);
            groupBox1.Controls.Add(lblTvBrand);
            groupBox1.Controls.Add(chkSecureConnection);
            groupBox1.Controls.Add(txtTvIp);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(btnDisconnect);
            groupBox1.Controls.Add(btnConnect);
            groupBox1.Location = new Point(20, 20);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(450, 185);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "TV Connection";

            // lblTvBrand
            lblTvBrand.AutoSize = true;
            lblTvBrand.Location = new Point(10, 25);
            lblTvBrand.Name = "lblTvBrand";
            lblTvBrand.Text = "TV Brand:";

            // cmbTvBrand
            cmbTvBrand.Location = new Point(100, 22);
            cmbTvBrand.Name = "cmbTvBrand";
            cmbTvBrand.Size = new Size(170, 23);
            cmbTvBrand.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (var brand in TvBrandCapabilities.AllBrands)
            {
                var caps = TvBrandCapabilities.For(brand);
                cmbTvBrand.Items.Add(caps.DisplayName);
            }
            cmbTvBrand.SelectedIndex = 1; // LG default
            cmbTvBrand.SelectedIndexChanged += cmbTvBrand_SelectedIndexChanged;

            // chkSecureConnection
            chkSecureConnection.AutoSize = true;
            chkSecureConnection.Location = new Point(10, 100);
            chkSecureConnection.Name = "chkSecureConnection";
            chkSecureConnection.Size = new Size(100, 17);
            chkSecureConnection.TabIndex = 4;
            chkSecureConnection.Text = "Secure Connection (LG webOS 2023+)";
            chkSecureConnection.UseVisualStyleBackColor = true;

            // txtTvIp
            txtTvIp.Location = new Point(100, 60);
            txtTvIp.Name = "txtTvIp";
            txtTvIp.Size = new Size(170, 20);
            txtTvIp.TabIndex = 3;
            txtTvIp.Text = "192.168.1.";

            // label1
            label1.AutoSize = true;
            label1.Location = new Point(10, 63);
            label1.Name = "label1";
            label1.Size = new Size(85, 13);
            label1.TabIndex = 2;
            label1.Text = "TV IP Address:";

            // btnDisconnect
            btnDisconnect.Enabled = false;
            btnDisconnect.Location = new Point(260, 140);
            btnDisconnect.Name = "btnDisconnect";
            btnDisconnect.Size = new Size(100, 28);
            btnDisconnect.TabIndex = 1;
            btnDisconnect.Text = "Disconnect";
            btnDisconnect.UseVisualStyleBackColor = true;
            btnDisconnect.Click += btnDisconnect_Click;

            // btnConnect
            btnConnect.Location = new Point(100, 140);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(150, 28);
            btnConnect.TabIndex = 0;
            btnConnect.Text = "Connect";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;

            // lblStatus
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(20, 215);
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
            tabPageWhiteBalance.Controls.Add(groupBoxAutoCal);
            tabPageWhiteBalance.Location = new Point(4, 24);
            tabPageWhiteBalance.Name = "tabPageWhiteBalance";
            tabPageWhiteBalance.Padding = new Padding(3);
            tabPageWhiteBalance.Size = new Size(792, 572);
            tabPageWhiteBalance.TabIndex = 3;
            tabPageWhiteBalance.Text = "White Balance";

            // groupBoxAutoCal
            groupBoxAutoCal.Controls.Add(btnAutoCal2ptWb);
            groupBoxAutoCal.Controls.Add(btnAutoCal20ptWb);
            groupBoxAutoCal.Controls.Add(btnAutoCalCms);
            groupBoxAutoCal.Controls.Add(btnAutoCalFull);
            groupBoxAutoCal.Controls.Add(btnAutoCalStop);
            groupBoxAutoCal.Controls.Add(btnResetWb);
            groupBoxAutoCal.Controls.Add(btnResetCms);
            groupBoxAutoCal.Controls.Add(lblAutoCalStatus);
            groupBoxAutoCal.Controls.Add(prgAutoCal);
            groupBoxAutoCal.Location = new Point(20, 20);
            groupBoxAutoCal.Name = "groupBoxAutoCal";
            groupBoxAutoCal.Size = new Size(740, 200);
            groupBoxAutoCal.TabIndex = 0;
            groupBoxAutoCal.TabStop = false;
            groupBoxAutoCal.Text = "Automated Calibration";

            // btnAutoCal2ptWb
            btnAutoCal2ptWb.Location = new Point(15, 30);
            btnAutoCal2ptWb.Size = new Size(130, 30);
            btnAutoCal2ptWb.Text = "2pt White Balance";
            btnAutoCal2ptWb.UseVisualStyleBackColor = true;
            btnAutoCal2ptWb.Click += btnAutoCal2ptWb_Click;

            // btnAutoCal20ptWb
            btnAutoCal20ptWb.Location = new Point(155, 30);
            btnAutoCal20ptWb.Size = new Size(130, 30);
            btnAutoCal20ptWb.Text = "20pt White Balance";
            btnAutoCal20ptWb.UseVisualStyleBackColor = true;
            btnAutoCal20ptWb.Click += btnAutoCal20ptWb_Click;

            // btnAutoCalCms
            btnAutoCalCms.Location = new Point(295, 30);
            btnAutoCalCms.Size = new Size(130, 30);
            btnAutoCalCms.Text = "CMS Calibration";
            btnAutoCalCms.UseVisualStyleBackColor = true;
            btnAutoCalCms.Click += btnAutoCalCms_Click;

            // btnAutoCalFull
            btnAutoCalFull.Location = new Point(435, 30);
            btnAutoCalFull.Size = new Size(130, 30);
            btnAutoCalFull.Text = "Full AutoCal";
            btnAutoCalFull.UseVisualStyleBackColor = true;
            btnAutoCalFull.Click += btnAutoCalFull_Click;

            // btnAutoCalStop
            btnAutoCalStop.Location = new Point(575, 30);
            btnAutoCalStop.Size = new Size(75, 30);
            btnAutoCalStop.Text = "Stop";
            btnAutoCalStop.UseVisualStyleBackColor = true;
            btnAutoCalStop.Enabled = false;
            btnAutoCalStop.Click += btnAutoCalStop_Click;

            // btnResetWb
            btnResetWb.Location = new Point(15, 70);
            btnResetWb.Size = new Size(130, 25);
            btnResetWb.Text = "Reset White Balance";
            btnResetWb.UseVisualStyleBackColor = true;
            btnResetWb.Click += btnResetWb_Click;

            // btnResetCms
            btnResetCms.Location = new Point(155, 70);
            btnResetCms.Size = new Size(130, 25);
            btnResetCms.Text = "Reset CMS";
            btnResetCms.UseVisualStyleBackColor = true;
            btnResetCms.Click += btnResetCms_Click;

            // prgAutoCal
            prgAutoCal.Location = new Point(15, 110);
            prgAutoCal.Size = new Size(700, 20);
            prgAutoCal.Style = ProgressBarStyle.Continuous;

            // lblAutoCalStatus
            lblAutoCalStatus.AutoSize = true;
            lblAutoCalStatus.Location = new Point(15, 140);
            lblAutoCalStatus.Text = "AutoCal: Ready";

            // ---------- PickleGen Easy Mode controls ----------

            // groupBoxPickleGen
            groupBoxPickleGen.Controls.Add(lblPickleGenIp);
            groupBoxPickleGen.Controls.Add(txtPickleGenIp);
            groupBoxPickleGen.Controls.Add(btnPickleGenDiscover);
            groupBoxPickleGen.Controls.Add(btnPickleGenConnect);
            groupBoxPickleGen.Controls.Add(btnPickleGenDisconnect);
            groupBoxPickleGen.Controls.Add(lblPickleGenStatus);
            groupBoxPickleGen.Controls.Add(lblPickleGenDevice);
            groupBoxPickleGen.Location = new Point(10, 10);
            groupBoxPickleGen.Size = new Size(370, 210);
            groupBoxPickleGen.Text = "PickleGen Connection (Easy Mode)";

            // lblPickleGenIp
            lblPickleGenIp.AutoSize = true;
            lblPickleGenIp.Location = new Point(15, 25);
            lblPickleGenIp.Text = "PickleGen IP:";

            // txtPickleGenIp
            txtPickleGenIp.Location = new Point(110, 22);
            txtPickleGenIp.Size = new Size(140, 23);

            // btnPickleGenDiscover
            btnPickleGenDiscover.Location = new Point(15, 55);
            btnPickleGenDiscover.Size = new Size(110, 30);
            btnPickleGenDiscover.Text = "Auto-Discover";
            btnPickleGenDiscover.UseVisualStyleBackColor = true;
            btnPickleGenDiscover.Click += btnPickleGenDiscover_Click;

            // btnPickleGenConnect
            btnPickleGenConnect.Location = new Point(135, 55);
            btnPickleGenConnect.Size = new Size(110, 30);
            btnPickleGenConnect.Text = "Connect";
            btnPickleGenConnect.UseVisualStyleBackColor = true;
            btnPickleGenConnect.Click += btnPickleGenConnect_Click;

            // btnPickleGenDisconnect
            btnPickleGenDisconnect.Location = new Point(255, 55);
            btnPickleGenDisconnect.Size = new Size(100, 30);
            btnPickleGenDisconnect.Text = "Disconnect";
            btnPickleGenDisconnect.UseVisualStyleBackColor = true;
            btnPickleGenDisconnect.Enabled = false;
            btnPickleGenDisconnect.Click += btnPickleGenDisconnect_Click;

            // lblPickleGenStatus
            lblPickleGenStatus.AutoSize = true;
            lblPickleGenStatus.Location = new Point(15, 100);
            lblPickleGenStatus.Text = "Status: Not connected";

            // lblPickleGenDevice
            lblPickleGenDevice.AutoSize = true;
            lblPickleGenDevice.Location = new Point(15, 125);
            lblPickleGenDevice.Text = "";

            // groupBoxPickleGenMode
            groupBoxPickleGenMode.Controls.Add(rdoPickleGenSdr);
            groupBoxPickleGenMode.Controls.Add(rdoPickleGenHdr);
            groupBoxPickleGenMode.Controls.Add(btnPickleGenApplyMode);
            groupBoxPickleGenMode.Controls.Add(btnPickleGenTestWhite);
            groupBoxPickleGenMode.Controls.Add(btnPickleGenTestBlack);
            groupBoxPickleGenMode.Location = new Point(10, 230);
            groupBoxPickleGenMode.Size = new Size(370, 130);
            groupBoxPickleGenMode.Text = "PickleGen Display Mode";

            // rdoPickleGenSdr
            rdoPickleGenSdr.AutoSize = true;
            rdoPickleGenSdr.Checked = true;
            rdoPickleGenSdr.Location = new Point(15, 25);
            rdoPickleGenSdr.Text = "SDR (8-bit)";

            // rdoPickleGenHdr
            rdoPickleGenHdr.AutoSize = true;
            rdoPickleGenHdr.Location = new Point(130, 25);
            rdoPickleGenHdr.Text = "HDR10 (10-bit)";

            // btnPickleGenApplyMode
            btnPickleGenApplyMode.Location = new Point(270, 20);
            btnPickleGenApplyMode.Size = new Size(85, 30);
            btnPickleGenApplyMode.Text = "Apply";
            btnPickleGenApplyMode.UseVisualStyleBackColor = true;
            btnPickleGenApplyMode.Click += btnPickleGenApplyMode_Click;

            // btnPickleGenTestWhite
            btnPickleGenTestWhite.Location = new Point(15, 60);
            btnPickleGenTestWhite.Size = new Size(110, 30);
            btnPickleGenTestWhite.Text = "Test White";
            btnPickleGenTestWhite.UseVisualStyleBackColor = true;
            btnPickleGenTestWhite.Click += btnPickleGenTestWhite_Click;

            // btnPickleGenTestBlack
            btnPickleGenTestBlack.Location = new Point(135, 60);
            btnPickleGenTestBlack.Size = new Size(110, 30);
            btnPickleGenTestBlack.Text = "Test Black";
            btnPickleGenTestBlack.UseVisualStyleBackColor = true;
            btnPickleGenTestBlack.Click += btnPickleGenTestBlack_Click;

            // btnLaunchWizard
            btnLaunchWizard.Location = new Point(400, 10);
            btnLaunchWizard.Size = new Size(370, 70);
            btnLaunchWizard.Text = "Launch Calibration Wizard\r\nStep-by-step guided calibration";
            btnLaunchWizard.Click += btnLaunchWizard_Click;

            // tabPagePatternGen
            tabPagePatternGen.Controls.Add(groupBoxPickleGen);
            tabPagePatternGen.Controls.Add(groupBoxPickleGenMode);
            tabPagePatternGen.Controls.Add(btnLaunchWizard);
            tabPagePatternGen.Location = new Point(4, 24);
            tabPagePatternGen.Name = "tabPagePatternGen";
            tabPagePatternGen.Padding = new Padding(3);
            tabPagePatternGen.Size = new Size(792, 572);
            tabPagePatternGen.TabIndex = 4;
            tabPagePatternGen.Text = "PickleGen";

            // tabPageMeter
            tabPageMeter.Controls.Add(groupBoxMeasurementSequence);
            tabPageMeter.Controls.Add(groupBoxMeterControl);
            tabPageMeter.Controls.Add(groupBoxMeterSelect);
            tabPageMeter.Controls.Add(lblMeterMeasurement);
            tabPageMeter.Controls.Add(lblMeterStatus);
            tabPageMeter.Location = new Point(4, 24);
            tabPageMeter.Name = "tabPageMeter";
            tabPageMeter.Padding = new Padding(3);
            tabPageMeter.Size = new Size(792, 572);
            tabPageMeter.TabIndex = 5;
            tabPageMeter.Text = "Meters";

            // groupBoxMeterSelect
            groupBoxMeterSelect.Controls.Add(btnMeterDisconnect);
            groupBoxMeterSelect.Controls.Add(btnMeterConnect);
            groupBoxMeterSelect.Controls.Add(btnMeterRefresh);
            groupBoxMeterSelect.Controls.Add(cmbMeters);
            groupBoxMeterSelect.Location = new Point(20, 20);
            groupBoxMeterSelect.Name = "groupBoxMeterSelect";
            groupBoxMeterSelect.Size = new Size(360, 130);
            groupBoxMeterSelect.TabIndex = 0;
            groupBoxMeterSelect.TabStop = false;
            groupBoxMeterSelect.Text = "Meter";

            // cmbMeters
            cmbMeters.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMeters.FormattingEnabled = true;
            cmbMeters.Location = new Point(15, 35);
            cmbMeters.Name = "cmbMeters";
            cmbMeters.Size = new Size(200, 23);
            cmbMeters.TabIndex = 0;

            // btnMeterRefresh
            btnMeterRefresh.Location = new Point(230, 34);
            btnMeterRefresh.Name = "btnMeterRefresh";
            btnMeterRefresh.Size = new Size(110, 25);
            btnMeterRefresh.TabIndex = 1;
            btnMeterRefresh.Text = "Refresh";
            btnMeterRefresh.UseVisualStyleBackColor = true;
            btnMeterRefresh.Click += btnMeterRefresh_Click;

            // btnMeterConnect
            btnMeterConnect.Location = new Point(15, 75);
            btnMeterConnect.Name = "btnMeterConnect";
            btnMeterConnect.Size = new Size(110, 25);
            btnMeterConnect.TabIndex = 2;
            btnMeterConnect.Text = "Connect";
            btnMeterConnect.UseVisualStyleBackColor = true;
            btnMeterConnect.Click += btnMeterConnect_Click;

            // btnMeterDisconnect
            btnMeterDisconnect.Location = new Point(230, 75);
            btnMeterDisconnect.Name = "btnMeterDisconnect";
            btnMeterDisconnect.Size = new Size(110, 25);
            btnMeterDisconnect.TabIndex = 3;
            btnMeterDisconnect.Text = "Disconnect";
            btnMeterDisconnect.UseVisualStyleBackColor = true;
            btnMeterDisconnect.Click += btnMeterDisconnect_Click;

            // groupBoxMeterControl
            groupBoxMeterControl.Controls.Add(btnMeterMeasure);
            groupBoxMeterControl.Controls.Add(btnMeterCalibrate);
            groupBoxMeterControl.Controls.Add(txtMeterDisplayType);
            groupBoxMeterControl.Controls.Add(lblMeterDisplayType);
            groupBoxMeterControl.Controls.Add(chkMeterHighRes);
            groupBoxMeterControl.Controls.Add(chkMeterAveraging);
            groupBoxMeterControl.Location = new Point(20, 170);
            groupBoxMeterControl.Name = "groupBoxMeterControl";
            groupBoxMeterControl.Size = new Size(360, 180);
            groupBoxMeterControl.TabIndex = 1;
            groupBoxMeterControl.TabStop = false;
            groupBoxMeterControl.Text = "Measurement";

            // groupBoxMeasurementSequence
            groupBoxMeasurementSequence.Controls.Add(lstMeasurementLog);
            groupBoxMeasurementSequence.Controls.Add(lblSequenceStatus);
            groupBoxMeasurementSequence.Controls.Add(btnStopSequence);
            groupBoxMeasurementSequence.Controls.Add(btnRunColorSweep);
            groupBoxMeasurementSequence.Controls.Add(btnRunGrayscale);
            groupBoxMeasurementSequence.Controls.Add(btnRunNearBlack);
            groupBoxMeasurementSequence.Controls.Add(btnRunNearWhite);
            groupBoxMeasurementSequence.Controls.Add(btnRunSaturation);
            groupBoxMeasurementSequence.Controls.Add(btnRunMeasureAll);
            groupBoxMeasurementSequence.Controls.Add(cmbPatternSource);
            groupBoxMeasurementSequence.Controls.Add(lblPatternSource);
            groupBoxMeasurementSequence.Location = new Point(400, 20);
            groupBoxMeasurementSequence.Name = "groupBoxMeasurementSequence";
            groupBoxMeasurementSequence.Size = new Size(360, 390);
            groupBoxMeasurementSequence.TabIndex = 4;
            groupBoxMeasurementSequence.TabStop = false;
            groupBoxMeasurementSequence.Text = "Measurement Sequences";

            // lblPatternSource
            lblPatternSource.AutoSize = true;
            lblPatternSource.Location = new Point(15, 30);
            lblPatternSource.Name = "lblPatternSource";
            lblPatternSource.Size = new Size(123, 15);
            lblPatternSource.TabIndex = 5;
            lblPatternSource.Text = "Pattern playback via:";

            // cmbPatternSource
            cmbPatternSource.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPatternSource.FormattingEnabled = true;
            cmbPatternSource.Location = new Point(150, 26);
            cmbPatternSource.Name = "cmbPatternSource";
            cmbPatternSource.Size = new Size(195, 23);
            cmbPatternSource.TabIndex = 6;
            cmbPatternSource.SelectedIndexChanged += cmbPatternSource_SelectedIndexChanged;

            // btnRunGrayscale
            btnRunGrayscale.Location = new Point(15, 60);
            btnRunGrayscale.Name = "btnRunGrayscale";
            btnRunGrayscale.Size = new Size(100, 25);
            btnRunGrayscale.TabIndex = 1;
            btnRunGrayscale.Text = "10pt Grayscale";
            btnRunGrayscale.UseVisualStyleBackColor = true;
            btnRunGrayscale.Click += btnRunGrayscale_Click;

            // btnRunColorSweep
            btnRunColorSweep.Location = new Point(130, 60);
            btnRunColorSweep.Name = "btnRunColorSweep";
            btnRunColorSweep.Size = new Size(120, 25);
            btnRunColorSweep.TabIndex = 2;
            btnRunColorSweep.Text = "Primaries/Secondaries";
            btnRunColorSweep.UseVisualStyleBackColor = true;
            btnRunColorSweep.Click += btnRunColorSweep_Click;

            // btnRunNearBlack
            btnRunNearBlack.Location = new Point(15, 90);
            btnRunNearBlack.Name = "btnRunNearBlack";
            btnRunNearBlack.Size = new Size(100, 25);
            btnRunNearBlack.TabIndex = 8;
            btnRunNearBlack.Text = "Near Black";
            btnRunNearBlack.UseVisualStyleBackColor = true;
            btnRunNearBlack.Click += btnRunNearBlack_Click;

            // btnRunNearWhite
            btnRunNearWhite.Location = new Point(130, 90);
            btnRunNearWhite.Name = "btnRunNearWhite";
            btnRunNearWhite.Size = new Size(100, 25);
            btnRunNearWhite.TabIndex = 9;
            btnRunNearWhite.Text = "Near White";
            btnRunNearWhite.UseVisualStyleBackColor = true;
            btnRunNearWhite.Click += btnRunNearWhite_Click;

            // btnRunSaturation
            btnRunSaturation.Location = new Point(15, 120);
            btnRunSaturation.Name = "btnRunSaturation";
            btnRunSaturation.Size = new Size(100, 25);
            btnRunSaturation.TabIndex = 10;
            btnRunSaturation.Text = "Saturation";
            btnRunSaturation.UseVisualStyleBackColor = true;
            btnRunSaturation.Click += btnRunSaturation_Click;

            // btnRunMeasureAll
            btnRunMeasureAll.Location = new Point(130, 120);
            btnRunMeasureAll.Name = "btnRunMeasureAll";
            btnRunMeasureAll.Size = new Size(120, 25);
            btnRunMeasureAll.TabIndex = 11;
            btnRunMeasureAll.Text = "Measure Everything";
            btnRunMeasureAll.UseVisualStyleBackColor = true;
            btnRunMeasureAll.Click += btnRunMeasureAll_Click;

            // btnStopSequence
            btnStopSequence.Location = new Point(270, 90);
            btnStopSequence.Name = "btnStopSequence";
            btnStopSequence.Size = new Size(75, 25);
            btnStopSequence.TabIndex = 3;
            btnStopSequence.Text = "Stop";
            btnStopSequence.UseVisualStyleBackColor = true;
            btnStopSequence.Click += btnStopSequence_Click;

            // lblSequenceStatus
            lblSequenceStatus.AutoSize = true;
            lblSequenceStatus.Location = new Point(15, 155);
            lblSequenceStatus.Name = "lblSequenceStatus";
            lblSequenceStatus.Size = new Size(110, 15);
            lblSequenceStatus.TabIndex = 4;
            lblSequenceStatus.Text = "Sequence: not running";

            // lstMeasurementLog
            lstMeasurementLog.FormattingEnabled = true;
            lstMeasurementLog.ItemHeight = 15;
            lstMeasurementLog.Location = new Point(15, 175);
            lstMeasurementLog.Name = "lstMeasurementLog";
            lstMeasurementLog.Size = new Size(330, 199);
            lstMeasurementLog.TabIndex = 7;

            // chkMeterAveraging
            chkMeterAveraging.AutoSize = true;
            chkMeterAveraging.Location = new Point(15, 60);
            chkMeterAveraging.Name = "chkMeterAveraging";
            chkMeterAveraging.Size = new Size(98, 19);
            chkMeterAveraging.TabIndex = 1;
            chkMeterAveraging.Text = "Use averaging";
            chkMeterAveraging.UseVisualStyleBackColor = true;

            // chkMeterHighRes
            chkMeterHighRes.AutoSize = true;
            chkMeterHighRes.Location = new Point(15, 30);
            chkMeterHighRes.Name = "chkMeterHighRes";
            chkMeterHighRes.Size = new Size(115, 19);
            chkMeterHighRes.TabIndex = 0;
            chkMeterHighRes.Text = "High resolution";
            chkMeterHighRes.UseVisualStyleBackColor = true;

            // lblMeterDisplayType
            lblMeterDisplayType.AutoSize = true;
            lblMeterDisplayType.Location = new Point(15, 95);
            lblMeterDisplayType.Name = "lblMeterDisplayType";
            lblMeterDisplayType.Size = new Size(136, 15);
            lblMeterDisplayType.TabIndex = 2;
            lblMeterDisplayType.Text = "Display type (optional):";

            // txtMeterDisplayType
            txtMeterDisplayType.Location = new Point(170, 92);
            txtMeterDisplayType.Name = "txtMeterDisplayType";
            txtMeterDisplayType.Size = new Size(150, 23);
            txtMeterDisplayType.TabIndex = 3;

            // btnMeterCalibrate
            btnMeterCalibrate.Location = new Point(15, 135);
            btnMeterCalibrate.Name = "btnMeterCalibrate";
            btnMeterCalibrate.Size = new Size(110, 25);
            btnMeterCalibrate.TabIndex = 4;
            btnMeterCalibrate.Text = "Calibrate";
            btnMeterCalibrate.UseVisualStyleBackColor = true;
            btnMeterCalibrate.Click += btnMeterCalibrate_Click;

            // btnMeterMeasure
            btnMeterMeasure.Location = new Point(170, 135);
            btnMeterMeasure.Name = "btnMeterMeasure";
            btnMeterMeasure.Size = new Size(110, 25);
            btnMeterMeasure.TabIndex = 5;
            btnMeterMeasure.Text = "Measure";
            btnMeterMeasure.UseVisualStyleBackColor = true;
            btnMeterMeasure.Click += btnMeterMeasure_Click;

            // lblMeterStatus
            lblMeterStatus.AutoSize = true;
            lblMeterStatus.Location = new Point(20, 370);
            lblMeterStatus.Name = "lblMeterStatus";
            lblMeterStatus.Size = new Size(107, 15);
            lblMeterStatus.TabIndex = 2;
            lblMeterStatus.Text = "Meter status: Idle";

            // lblMeterMeasurement
            lblMeterMeasurement.AutoSize = true;
            lblMeterMeasurement.Location = new Point(20, 400);
            lblMeterMeasurement.Name = "lblMeterMeasurement";
            lblMeterMeasurement.Size = new Size(191, 15);
            lblMeterMeasurement.TabIndex = 3;
            lblMeterMeasurement.Text = "Last reading: waiting for measure";

            // Session settings group (on Meter tab, below meter controls)
            groupBoxSession.Controls.Add(lblDisplayName);
            groupBoxSession.Controls.Add(txtDisplayName);
            groupBoxSession.Controls.Add(lblTargetColorSpace);
            groupBoxSession.Controls.Add(cmbTargetColorSpace);
            groupBoxSession.Controls.Add(lblTargetEotf);
            groupBoxSession.Controls.Add(cmbTargetEotf);
            groupBoxSession.Controls.Add(lblTargetGamma);
            groupBoxSession.Controls.Add(txtTargetGamma);
            groupBoxSession.Controls.Add(btnNewSession);
            groupBoxSession.Controls.Add(lblSessionInfo);
            groupBoxSession.Controls.Add(btnExportCsv);
            groupBoxSession.Controls.Add(btnExportHtml);
            groupBoxSession.Location = new Point(20, 420);
            groupBoxSession.Name = "groupBoxSession";
            groupBoxSession.Size = new Size(740, 100);
            groupBoxSession.TabIndex = 5;
            groupBoxSession.TabStop = false;
            groupBoxSession.Text = "Calibration Session";
            tabPageMeter.Controls.Add(groupBoxSession);

            // lblDisplayName
            lblDisplayName.AutoSize = true;
            lblDisplayName.Location = new Point(15, 25);
            lblDisplayName.Name = "lblDisplayName";
            lblDisplayName.Size = new Size(55, 15);
            lblDisplayName.Text = "Display:";

            // txtDisplayName
            txtDisplayName.Location = new Point(75, 22);
            txtDisplayName.Name = "txtDisplayName";
            txtDisplayName.Size = new Size(120, 23);

            // lblTargetColorSpace
            lblTargetColorSpace.AutoSize = true;
            lblTargetColorSpace.Location = new Point(210, 25);
            lblTargetColorSpace.Name = "lblTargetColorSpace";
            lblTargetColorSpace.Size = new Size(50, 15);
            lblTargetColorSpace.Text = "Gamut:";

            // cmbTargetColorSpace
            cmbTargetColorSpace.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTargetColorSpace.Location = new Point(265, 22);
            cmbTargetColorSpace.Name = "cmbTargetColorSpace";
            cmbTargetColorSpace.Size = new Size(100, 23);

            // lblTargetEotf
            lblTargetEotf.AutoSize = true;
            lblTargetEotf.Location = new Point(380, 25);
            lblTargetEotf.Name = "lblTargetEotf";
            lblTargetEotf.Size = new Size(40, 15);
            lblTargetEotf.Text = "EOTF:";

            // cmbTargetEotf
            cmbTargetEotf.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTargetEotf.Location = new Point(425, 22);
            cmbTargetEotf.Name = "cmbTargetEotf";
            cmbTargetEotf.Size = new Size(80, 23);

            // lblTargetGamma
            lblTargetGamma.AutoSize = true;
            lblTargetGamma.Location = new Point(520, 25);
            lblTargetGamma.Name = "lblTargetGamma";
            lblTargetGamma.Size = new Size(50, 15);
            lblTargetGamma.Text = "Gamma:";

            // txtTargetGamma
            txtTargetGamma.Location = new Point(575, 22);
            txtTargetGamma.Name = "txtTargetGamma";
            txtTargetGamma.Size = new Size(50, 23);
            txtTargetGamma.Text = "2.2";

            // btnNewSession
            btnNewSession.Location = new Point(15, 55);
            btnNewSession.Name = "btnNewSession";
            btnNewSession.Size = new Size(120, 25);
            btnNewSession.Text = "New Session";
            btnNewSession.UseVisualStyleBackColor = true;
            btnNewSession.Click += btnNewSession_Click;

            // lblSessionInfo
            lblSessionInfo.AutoSize = true;
            lblSessionInfo.Location = new Point(150, 60);
            lblSessionInfo.Name = "lblSessionInfo";
            lblSessionInfo.Text = "No active session";

            // btnExportCsv
            btnExportCsv.Location = new Point(520, 55);
            btnExportCsv.Size = new Size(100, 25);
            btnExportCsv.Text = "Export CSV";
            btnExportCsv.UseVisualStyleBackColor = true;
            btnExportCsv.Click += btnExportCsv_Click;

            // btnExportHtml
            btnExportHtml.Location = new Point(630, 55);
            btnExportHtml.Size = new Size(100, 25);
            btnExportHtml.Text = "Export Report";
            btnExportHtml.UseVisualStyleBackColor = true;
            btnExportHtml.Click += btnExportHtml_Click;

            // Analysis tab page - contains sub-tab control with all chart views
            tabPageAnalysis.Controls.Add(tabAnalysisViews);
            tabPageAnalysis.Location = new Point(4, 24);
            tabPageAnalysis.Name = "tabPageAnalysis";
            tabPageAnalysis.Padding = new Padding(3);
            tabPageAnalysis.Size = new Size(1092, 672);
            tabPageAnalysis.TabIndex = 6;
            tabPageAnalysis.Text = "Analysis";

            // tabAnalysisViews - sub-tab control inside the Analysis tab
            tabAnalysisViews.Controls.Add(tabAnalysisCie);
            tabAnalysisViews.Controls.Add(tabAnalysisGamma);
            tabAnalysisViews.Controls.Add(tabAnalysisRgb);
            tabAnalysisViews.Controls.Add(tabAnalysisLuminance);
            tabAnalysisViews.Controls.Add(tabAnalysisDeltaE);
            tabAnalysisViews.Controls.Add(tabAnalysisCct);
            tabAnalysisViews.Controls.Add(tabAnalysisGrid);
            tabAnalysisViews.Dock = DockStyle.Fill;
            tabAnalysisViews.Name = "tabAnalysisViews";
            tabAnalysisViews.SelectedIndex = 0;

            // CIE Diagram tab
            tabAnalysisCie.Controls.Add(_cieDiagram);
            tabAnalysisCie.Text = "CIE 1931";
            tabAnalysisCie.Padding = new Padding(3);
            _cieDiagram.Dock = DockStyle.Fill;

            // Gamma Curve tab
            tabAnalysisGamma.Controls.Add(_gammaChart);
            tabAnalysisGamma.Text = "Gamma";
            tabAnalysisGamma.Padding = new Padding(3);
            _gammaChart.Dock = DockStyle.Fill;

            // RGB Balance tab
            tabAnalysisRgb.Controls.Add(_rgbBalance);
            tabAnalysisRgb.Text = "RGB Balance";
            tabAnalysisRgb.Padding = new Padding(3);
            _rgbBalance.Dock = DockStyle.Fill;

            // Luminance Curve tab
            tabAnalysisLuminance.Controls.Add(_luminanceChart);
            tabAnalysisLuminance.Text = "Luminance";
            tabAnalysisLuminance.Padding = new Padding(3);
            _luminanceChart.Dock = DockStyle.Fill;

            // Delta E tab
            tabAnalysisDeltaE.Controls.Add(_deltaEChart);
            tabAnalysisDeltaE.Text = "Delta E";
            tabAnalysisDeltaE.Padding = new Padding(3);
            _deltaEChart.Dock = DockStyle.Fill;

            // CCT Tracking tab
            tabAnalysisCct.Controls.Add(_cctChart);
            tabAnalysisCct.Text = "CCT";
            tabAnalysisCct.Padding = new Padding(3);
            _cctChart.Dock = DockStyle.Fill;

            // Data Grid tab
            tabAnalysisGrid.Controls.Add(_dataGrid);
            tabAnalysisGrid.Text = "Data Grid";
            tabAnalysisGrid.Padding = new Padding(3);
            _dataGrid.Dock = DockStyle.Fill;

            // MainForm
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1200, 780);
            Controls.Add(tabControl1);
            Name = "MainForm";
            Text = "PickleCal";
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
            tabPageMeter.ResumeLayout(false);
            tabPageMeter.PerformLayout();
            groupBoxMeterSelect.ResumeLayout(false);
            groupBoxMeterControl.ResumeLayout(false);
            groupBoxMeterControl.PerformLayout();
            groupBoxMeasurementSequence.ResumeLayout(false);
            groupBoxMeasurementSequence.PerformLayout();
            groupBoxSession.ResumeLayout(false);
            groupBoxSession.PerformLayout();
            tabPageAnalysis.ResumeLayout(false);
            tabAnalysisViews.ResumeLayout(false);
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
            lblMeterStatus.Text = "Meter status: Not connected";
            lblMeterMeasurement.Text = "Last reading: waiting for measure";
            btnMeterDisconnect.Enabled = false;
            btnMeterMeasure.Enabled = false;
            btnMeterCalibrate.Enabled = false;
            lblSequenceStatus.Text = "Sequence: not running";
            btnStopSequence.Enabled = false;
            lstMeasurementLog.Items.Clear();
            UpdateSequenceButtons();
            cmbPatternSource.Items.Clear();
            cmbPatternSource.Items.AddRange(new[]
            {
                "Manual (no automatic pattern)",
                "Internal PGenerator",
                "LG TV (toast prompt)",
                "Android PGenerator (PickleGen Pro)",
                "PickleGen Easy Mode"
            });
            cmbPatternSource.SelectedIndex = 0;

            // Chart defaults
            _cieDiagram.TargetColorSpace = ColorSpace.Rec709;
            _gammaChart.TargetGamma = 2.2;
            _cctChart.TargetCctK = 6504;
        }

        private async void btnConnect_Click(object? sender, EventArgs e)
        {
            _tvIp = txtTvIp.Text.Trim();
            if (string.IsNullOrEmpty(_tvIp))
            {
                MessageBox.Show("Please enter the TV IP address", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var caps = TvBrandCapabilities.For(_selectedBrand);
            if (!caps.SupportsRemoteConnection)
            {
                // Register TV without network connection
                _tvController = new GenericTvController(_selectedBrand, _tvIp);
                await _tvController.ConnectAsync();
                _tvController.OnStatusChange += TvController_OnStatusChange;
                lblStatus.Text = $"{caps.DisplayName} TV registered — manual control via remote";
                lblStatus.ForeColor = DarkTheme.Success;
                btnDisconnect.Enabled = true;
                btnConnect.Enabled = false;
                UpdateAutoCalButtonState();
                return;
            }

            btnConnect.Enabled = false;
            lblStatus.Text = $"Connecting to {caps.DisplayName} TV at {_tvIp}...";

            try
            {
                // Create correct controller based on brand
                _tvController = _selectedBrand == TvBrand.LG
                    ? new LgTvController(_tvIp, chkSecureConnection.Checked)
                    : new GenericTvController(_selectedBrand, _tvIp);

                _tvController.OnStatusChange += TvController_OnStatusChange;
                _tvController.OnDisconnect += TvController_OnDisconnect;

                await _tvController.ConnectAsync();

                if (_tvController.IsConnected && _tvController.IsPaired)
                {
                    lblStatus.Text = $"Connected to {caps.DisplayName} TV at {_tvIp}";
                    lblStatus.ForeColor = DarkTheme.Success;
                    btnDisconnect.Enabled = true;
                    btnConnect.Enabled = false;
                }
                else
                {
                    lblStatus.Text = "Connection failed - check pairing";
                    lblStatus.ForeColor = DarkTheme.Error;
                    btnConnect.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error: {ex.Message}";
                lblStatus.ForeColor = DarkTheme.Error;
                btnConnect.Enabled = true;
            }

            UpdateAutoCalButtonState();
        }

        private async void btnDisconnect_Click(object? sender, EventArgs e)
        {
            if (_tvController != null)
            {
                await _tvController.DisconnectAsync();
                lblStatus.Text = "Disconnected";
                lblStatus.ForeColor = DarkTheme.TextPrimary;
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
            lblStatus.ForeColor = DarkTheme.TextPrimary;
            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            UpdateAutoCalButtonState();
        }

        private void cmbTvBrand_SelectedIndexChanged(object? sender, EventArgs e)
        {
            int idx = cmbTvBrand.SelectedIndex;
            if (idx >= 0 && idx < TvBrandCapabilities.AllBrands.Length)
            {
                _selectedBrand = TvBrandCapabilities.AllBrands[idx];
                var caps = TvBrandCapabilities.For(_selectedBrand);

                // Show/hide LG-specific secure connection checkbox
                chkSecureConnection.Visible = _selectedBrand == TvBrand.LG;

                // Update connect button text
                btnConnect.Text = caps.SupportsRemoteConnection
                    ? $"Connect to {caps.DisplayName}"
                    : $"Register {caps.DisplayName}";

                UpdateAutoCalButtonState();
            }
        }

        private void UpdateAutoCalButtonState()
        {
            var caps = TvBrandCapabilities.For(_selectedBrand);
            bool canAutoCal = caps.SupportsAutoCalWhiteBalance
                && _tvController != null && _tvController.IsConnected;

            btnAutoCal2ptWb.Enabled = canAutoCal;
            btnAutoCal20ptWb.Enabled = canAutoCal;
            btnAutoCalCms.Enabled = caps.SupportsAutoCalCms && _tvController != null && _tvController.IsConnected;
            btnAutoCalFull.Enabled = canAutoCal;
            btnResetWb.Enabled = canAutoCal;
            btnResetCms.Enabled = caps.SupportsAutoCalCms && _tvController != null && _tvController.IsConnected;

            if (!caps.SupportsAutoCalWhiteBalance)
            {
                lblAutoCalStatus.Text = $"AutoCal: Not available for {caps.DisplayName} TVs — measurement only";
            }
        }

        private async void btnApplyPictureMode_Click(object? sender, EventArgs e)
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

        private async void btnApplyColorSettings_Click(object? sender, EventArgs e)
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

        private async void MainForm_LoadAsync(object? sender, EventArgs e)
        {
            await RefreshMetersAsync();

            // Launch calibration wizard at startup
            ShowCalibrationWizard();
        }

        private void ShowCalibrationWizard()
        {
            using var wizard = new Wizard.CalibrationWizard(this);
            var result = wizard.ShowDialog(this);

            if (result == DialogResult.OK)
            {
                ApplyWizardResult(wizard);
            }
        }

        private void ApplyWizardResult(Wizard.CalibrationWizard wizard)
        {
            // Transfer brand selection from wizard
            _selectedBrand = wizard.SelectedBrand;
            for (int i = 0; i < TvBrandCapabilities.AllBrands.Length; i++)
            {
                if (TvBrandCapabilities.AllBrands[i] == _selectedBrand)
                {
                    cmbTvBrand.SelectedIndex = i;
                    break;
                }
            }

            // Transfer PickleGen connection from wizard
            var wizardClient = wizard.GetPickleGenClient();
            if (wizardClient != null && wizardClient.IsConnected)
            {
                if (_pickleGenRemote != null)
                {
                    _ = ShutdownPickleGenRemoteAsync();
                }

                _pickleGenRemote = wizardClient;
                _pickleGenRemote.OnStatusChange += msg => BeginInvoke(new Action(() =>
                {
                    lblPickleGenStatus.Text = $"Status: {msg}";
                    PostSequenceLog($"PickleGen: {msg}");
                }));
                _pickleGenRemote.OnDisconnected += () => BeginInvoke(new Action(() =>
                {
                    UpdatePickleGenUi(false);
                    PostSequenceLog("PickleGen: Connection lost");
                }));

                txtPickleGenIp.Text = _pickleGenRemote.RemoteAddress ?? "";
                UpdatePickleGenUi(true);
                cmbPatternSource.SelectedIndex = 4; // PickleGen Easy mode

                PostSequenceLog($"Wizard connected PickleGen: {_pickleGenRemote.DeviceName}");
            }

            // Transfer TV connection from wizard
            var wizardTv = wizard.GetTvController();
            if (wizardTv != null && wizardTv.IsConnected)
            {
                if (_tvController != null)
                {
                    try { _tvController.DisconnectAsync().GetAwaiter().GetResult(); } catch { }
                }

                _tvController = wizardTv;
                _tvController.OnStatusChange += TvController_OnStatusChange;
                _tvController.OnDisconnect += TvController_OnDisconnect;
                txtTvIp.Text = wizard.GetTvIp();
                lblStatus.Text = $"Connected to {wizard.GetTvIp()}";
                lblStatus.ForeColor = DarkTheme.Success;
                btnConnect.Enabled = false;
                btnDisconnect.Enabled = true;

                PostSequenceLog($"Wizard connected TV: {wizard.GetTvIp()}");
            }
            else
            {
                // Apply wizard TV IP if provided (user can connect manually)
                string wizardTvIp = wizard.GetTvIp();
                if (!string.IsNullOrEmpty(wizardTvIp))
                {
                    txtTvIp.Text = wizardTvIp;
                }
            }

            PostSequenceLog($"Wizard completed — Profile: {wizard.SelectedProfile}, HDR: {wizard.IsHdrMode}, Brand: {_selectedBrand}");
            UpdateAutoCalButtonState();
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _meterCancellation.Cancel();
            _sequenceCancellation?.Cancel();
            _sequenceCancellation?.Dispose();
            try
            {
                if (_pgenServer != null)
                {
                    _pgenServer.StopAsync().GetAwaiter().GetResult();
                    _pgenServer.Dispose();
                    _pgenServer = null;
                }
            }
            catch
            {
                // best effort cleanup
            }
            try
            {
                if (_pgenClient != null)
                {
                    _pgenClient.DisconnectAsync().GetAwaiter().GetResult();
                    _pgenClient = null;
                }
            }
            catch
            {
                // best effort cleanup
            }
            try
            {
                if (_pickleGenRemote != null)
                {
                    _pickleGenRemote.DisposeAsync().AsTask().GetAwaiter().GetResult();
                    _pickleGenRemote = null;
                }
            }
            catch
            {
                // best effort cleanup
            }
            try
            {
                _meterManager.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            catch
            {
                // best effort cleanup
            }
        }

        private async void btnMeterRefresh_Click(object? sender, EventArgs e)
        {
            await RefreshMetersAsync();
        }

        private async void btnMeterConnect_Click(object? sender, EventArgs e)
        {
            if (cmbMeters.SelectedItem is not MeterDescriptor descriptor)
            {
                MessageBox.Show("No meter detected. Refresh and select a device.", "Meter", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ToggleMeterControls(false);

            try
            {
                bool selected = await _meterManager.SelectMeterAsync(descriptor.Id, _meterCancellation.Token);
                if (!selected)
                {
                    lblMeterStatus.Text = "Meter status: selection failed";
                    return;
                }

                var options = new MeterConnectOptions
                {
                    PreferredMode = MeterMeasurementMode.Display,
                    UseHighResolution = chkMeterHighRes.Checked,
                    DisplayType = string.IsNullOrWhiteSpace(txtMeterDisplayType.Text) ? null : txtMeterDisplayType.Text.Trim()
                };

                await _meterManager.ConnectActiveMeterAsync(options, _meterCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                lblMeterStatus.Text = "Meter status: operation cancelled";
            }
            catch (Exception ex)
            {
                lblMeterStatus.Text = $"Meter status: {ex.Message}";
            }
            finally
            {
                ToggleMeterControls(true);
            }
        }

        private async void btnMeterDisconnect_Click(object? sender, EventArgs e)
        {
            ToggleMeterControls(false);
            try
            {
                await _meterManager.DisconnectAsync(_meterCancellation.Token);
            }
            catch (Exception ex)
            {
                lblMeterStatus.Text = $"Meter status: {ex.Message}";
            }
            finally
            {
                ToggleMeterControls(true);
            }
        }

        private async void btnMeterCalibrate_Click(object? sender, EventArgs e)
        {
            ToggleMeterControls(false);
            try
            {
                var request = new MeterCalibrationRequest(MeterMeasurementMode.Display, true);
                var result = await _meterManager.CalibrateAsync(request, _meterCancellation.Token);
                if (result != null && !result.Success)
                {
                    MessageBox.Show(result.Message ?? "Calibration failed.", "Meter", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (OperationCanceledException)
            {
                lblMeterStatus.Text = "Meter status: calibration cancelled";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Meter", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ToggleMeterControls(true);
            }
        }

        private async void btnMeterMeasure_Click(object? sender, EventArgs e)
        {
            ToggleMeterControls(false);
            try
            {
                var request = new MeterMeasureRequest(MeterMeasurementMode.Display, TimeSpan.Zero, 100d, chkMeterAveraging.Checked);
                var result = await _meterManager.MeasureAsync(request, _meterCancellation.Token);
                if (result == null)
                {
                    lblMeterStatus.Text = "Meter status: no active meter";
                }
                else if (!result.Success)
                {
                    MessageBox.Show(result.Message ?? "Measurement failed.", "Meter", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (OperationCanceledException)
            {
                lblMeterStatus.Text = "Meter status: measurement cancelled";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Meter", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ToggleMeterControls(true);
            }
        }

        private async void btnRunGrayscale_Click(object? sender, EventArgs e)
        {
            var sequence = MeasurementSequences.Grayscale10Point(chkMeterAveraging.Checked);
            await RunSequenceAsync(sequence);
        }

        private async void btnRunColorSweep_Click(object? sender, EventArgs e)
        {
            var sequence = MeasurementSequences.PrimarySecondarySweep(chkMeterAveraging.Checked);
            await RunSequenceAsync(sequence);
        }

        private async void btnRunNearBlack_Click(object? sender, EventArgs e)
        {
            var sequence = MeasurementSequences.NearBlack(chkMeterAveraging.Checked);
            await RunSequenceAsync(sequence);
        }

        private async void btnRunNearWhite_Click(object? sender, EventArgs e)
        {
            var sequence = MeasurementSequences.NearWhite(chkMeterAveraging.Checked);
            await RunSequenceAsync(sequence);
        }

        private async void btnRunSaturation_Click(object? sender, EventArgs e)
        {
            var sequence = MeasurementSequences.FullSaturationSweep(5, chkMeterAveraging.Checked);
            await RunSequenceAsync(sequence);
        }

        private async void btnRunMeasureAll_Click(object? sender, EventArgs e)
        {
            var sequence = MeasurementSequences.MeasureEverything(21, chkMeterAveraging.Checked);
            await RunSequenceAsync(sequence);
        }

        private void btnNewSession_Click(object? sender, EventArgs e)
        {
            var colorSpace = cmbTargetColorSpace.SelectedIndex switch
            {
                1 => ColorSpace.DciP3,
                2 => ColorSpace.Bt2020,
                3 => ColorSpace.AdobeRgb,
                _ => ColorSpace.Rec709
            };
            var eotf = cmbTargetEotf.SelectedIndex switch
            {
                1 => EotfType.Srgb,
                2 => EotfType.Bt1886,
                3 => EotfType.PQ,
                4 => EotfType.Hlg,
                5 => EotfType.LStar,
                _ => EotfType.Gamma
            };
            double gamma = double.TryParse(txtTargetGamma.Text, out double g) ? g : 2.2;
            string displayName = string.IsNullOrWhiteSpace(txtDisplayName.Text) ? "My Display" : txtDisplayName.Text.Trim();

            _session = new CalibrationSession(displayName, colorSpace, eotf, gamma);

            // Update chart targets
            _cieDiagram.TargetColorSpace = colorSpace;
            _gammaChart.TargetGamma = gamma;
            _cieDiagram.ClearPoints();
            _gammaChart.SetPoints(Array.Empty<CalibrationPoint>());
            _rgbBalance.SetPoints(Array.Empty<CalibrationPoint>());
            _luminanceChart.SetPoints(Array.Empty<CalibrationPoint>());
            _deltaEChart.SetPoints(Array.Empty<CalibrationPoint>());
            _cctChart.SetPoints(Array.Empty<CalibrationPoint>());
            _dataGrid.ClearPoints();

            lblSessionInfo.Text = $"Session: {displayName} | {colorSpace.Name} | {eotf} γ{gamma:F1}";
            AppendSequenceLog($"New session created: {displayName}");
        }

        // ── AutoCal handlers ──

        private async void btnAutoCal2ptWb_Click(object? sender, EventArgs e)
        {
            await RunAutoCalAsync(engine => engine.Run2PointWhiteBalanceAsync);
        }

        private async void btnAutoCal20ptWb_Click(object? sender, EventArgs e)
        {
            await RunAutoCalAsync(engine => engine.Run20PointWhiteBalanceAsync);
        }

        private async void btnAutoCalCms_Click(object? sender, EventArgs e)
        {
            await RunAutoCalAsync(engine => engine.RunCmsCalibrationAsync);
        }

        private async void btnAutoCalFull_Click(object? sender, EventArgs e)
        {
            if (!ValidateAutoCalPrerequisites()) return;

            var engine = CreateAutoCalEngine();
            _autoCalCancellation = new CancellationTokenSource();
            SetAutoCalRunning(true);

            try
            {
                var results = await engine.RunFullAutoCalAsync(_autoCalCancellation.Token);
                foreach (var r in results)
                    PostSequenceLog($"AutoCal result: {r}");
            }
            catch (OperationCanceledException)
            {
                PostSequenceLog("AutoCal cancelled.");
                lblAutoCalStatus.Text = "AutoCal: Cancelled";
            }
            catch (Exception ex)
            {
                PostSequenceLog($"AutoCal error: {ex.Message}");
                lblAutoCalStatus.Text = $"AutoCal: Error - {ex.Message}";
            }
            finally
            {
                SetAutoCalRunning(false);
                _autoCalCancellation?.Dispose();
                _autoCalCancellation = null;
            }
        }

        private void btnAutoCalStop_Click(object? sender, EventArgs e)
        {
            _autoCalCancellation?.Cancel();
        }

        private async void btnResetWb_Click(object? sender, EventArgs e)
        {
            if (_tvController == null || !_tvController.IsConnected)
            {
                MessageBox.Show("Not connected to TV", "AutoCal", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            await _tvController.ResetWhiteBalanceAsync();
            PostSequenceLog("White balance reset to defaults");
        }

        private async void btnResetCms_Click(object? sender, EventArgs e)
        {
            if (_tvController == null || !_tvController.IsConnected)
            {
                MessageBox.Show("Not connected to TV", "AutoCal", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            await _tvController.ResetCmsAsync();
            PostSequenceLog("CMS reset to defaults");
        }

        private async Task RunAutoCalAsync(Func<AutoCalEngine, Func<CancellationToken, Task<AutoCalResult>>> phaseSelector)
        {
            if (!ValidateAutoCalPrerequisites()) return;

            var engine = CreateAutoCalEngine();
            _autoCalCancellation = new CancellationTokenSource();
            SetAutoCalRunning(true);

            try
            {
                var phase = phaseSelector(engine);
                var result = await phase(_autoCalCancellation.Token);
                PostSequenceLog($"AutoCal result: {result}");
            }
            catch (OperationCanceledException)
            {
                PostSequenceLog("AutoCal cancelled.");
                lblAutoCalStatus.Text = "AutoCal: Cancelled";
            }
            catch (Exception ex)
            {
                PostSequenceLog($"AutoCal error: {ex.Message}");
                lblAutoCalStatus.Text = $"AutoCal: Error - {ex.Message}";
            }
            finally
            {
                SetAutoCalRunning(false);
                _autoCalCancellation?.Dispose();
                _autoCalCancellation = null;
            }
        }

        private bool ValidateAutoCalPrerequisites()
        {
            var caps = TvBrandCapabilities.For(_selectedBrand);
            if (!caps.SupportsAutoCalWhiteBalance)
            {
                MessageBox.Show($"AutoCal is not supported for {caps.DisplayName} TVs.\n\nAutoCal requires a TV brand with remote white balance control (currently LG only).",
                    "AutoCal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (_tvController == null || !_tvController.IsConnected)
            {
                MessageBox.Show($"Connect to the {caps.DisplayName} TV first.", "AutoCal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (_lastMeterState != MeterMeasurementState.Idle)
            {
                MessageBox.Show("Meter must be connected and idle.", "AutoCal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (_patternMode == PatternPlaybackMode.Manual)
            {
                MessageBox.Show("Select a pattern source (PGenerator, Android PGen, PickleGen Easy, or LG TV).", "AutoCal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private AutoCalEngine CreateAutoCalEngine()
        {
            var engine = new AutoCalEngine(
                _tvController!,
                _meterManager,
                async (r, g, b, token) =>
                {
                    var step = new MeasurementStep("AutoCal", r / 2.55, MeterMeasurementMode.Display,
                        TimeSpan.Zero, false, $"AutoCal R{r} G{g} B{b}",
                        MeasurementCategory.Free, r, g, b);
                    await HandleSequencePatternAsync(step, token);
                },
                PostSequenceLog);

            engine.ProgressChanged += OnAutoCalProgress;

            // Set targets from session or defaults
            var colorSpace = _session?.TargetColorSpace ?? ColorSpace.Rec709;
            var gamma = _session?.TargetGamma ?? 2.2;
            engine.TargetColorSpace = colorSpace;
            engine.TargetGamma = gamma;

            return engine;
        }

        private void OnAutoCalProgress(AutoCalProgress progress)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnAutoCalProgress(progress)));
                return;
            }

            lblAutoCalStatus.Text = $"AutoCal: [{progress.Phase}] {progress.Status}";
            int percent = progress.TotalSteps > 0
                ? (int)(progress.CurrentStep * 100.0 / progress.TotalSteps)
                : 0;
            prgAutoCal.Value = Math.Clamp(percent, 0, 100);
        }

        private void SetAutoCalRunning(bool running)
        {
            btnAutoCal2ptWb.Enabled = !running;
            btnAutoCal20ptWb.Enabled = !running;
            btnAutoCalCms.Enabled = !running;
            btnAutoCalFull.Enabled = !running;
            btnAutoCalStop.Enabled = running;
            btnResetWb.Enabled = !running;
            btnResetCms.Enabled = !running;
        }

        // ── Export handlers ──

        private void btnExportCsv_Click(object? sender, EventArgs e)
        {
            if (_session == null || _session.Points.Count == 0)
            {
                MessageBox.Show("No measurements to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"PickleCal_{_session.DisplayName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                Title = "Export Measurements to CSV"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    CsvExporter.Export(_session, dialog.FileName);
                    AppendSequenceLog($"Exported CSV: {dialog.FileName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnExportHtml_Click(object? sender, EventArgs e)
        {
            if (_session == null || _session.Points.Count == 0)
            {
                MessageBox.Show("No measurements to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Filter = "HTML files (*.html)|*.html",
                FileName = $"PickleCal_Report_{_session.DisplayName}_{DateTime.Now:yyyyMMdd_HHmmss}.html",
                Title = "Export Calibration Report"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    HtmlReportExporter.Export(_session, dialog.FileName);
                    AppendSequenceLog($"Exported report: {dialog.FileName}");

                    // Open in default browser
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = dialog.FileName,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnStopSequence_Click(object? sender, EventArgs e)
        {
            if (!_sequenceRunning)
            {
                return;
            }

            _sequenceCancellation?.Cancel();
        }

        private async Task RunSequenceAsync(MeasurementSequence sequence)
        {
            if (_sequenceRunning)
            {
                MessageBox.Show("A sequence is already running.", "Sequences", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_lastMeterState != MeterMeasurementState.Idle)
            {
                MessageBox.Show("Meter must be connected and idle before starting a sequence.", "Sequences", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            EnsureSequenceRunner();

            _sequenceCancellation = new CancellationTokenSource();
            _sequenceRunning = true;
            UpdateSequenceButtons();
            AppendSequenceLog($"Preparing {sequence.Name}...");

            try
            {
                await _sequenceRunner!.RunAsync(sequence, _sequenceCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                AppendSequenceLog("Sequence cancelled.");
                lblSequenceStatus.Text = "Sequence: cancelled";
            }
            catch (Exception ex)
            {
                AppendSequenceLog($"Sequence error: {ex.Message}");
                lblSequenceStatus.Text = "Sequence: error";
            }
            finally
            {
                _sequenceRunning = false;
                _sequenceCancellation?.Dispose();
                _sequenceCancellation = null;
                UpdateSequenceButtons();
            }
        }

        private async void cmbPatternSource_SelectedIndexChanged(object? sender, EventArgs e)
        {
            PatternPlaybackMode newMode = cmbPatternSource.SelectedIndex switch
            {
                1 => PatternPlaybackMode.PGenerator,
                2 => PatternPlaybackMode.LgTv,
                3 => PatternPlaybackMode.AndroidPGen,
                4 => PatternPlaybackMode.PickleGenEasy,
                _ => PatternPlaybackMode.Manual
            };

            if (newMode == _patternMode)
            {
                return;
            }

            _patternMode = newMode;
            await HandlePatternModeChangeAsync(newMode);
        }

        private async Task HandlePatternModeChangeAsync(PatternPlaybackMode mode)
        {
            switch (mode)
            {
                case PatternPlaybackMode.PGenerator:
                    await ShutdownPgenClientAsync();
                    await ShutdownPickleGenRemoteAsync();
                    await EnsurePgenServerAsync();
                    PostSequenceLog("Pattern mode: internal PGenerator");
                    break;
                case PatternPlaybackMode.AndroidPGen:
                    await ShutdownPgenServerAsync();
                    await ShutdownPickleGenRemoteAsync();
                    await EnsurePgenClientAsync();
                    PostSequenceLog("Pattern mode: Android PGenerator (PickleGen Pro)");
                    break;
                case PatternPlaybackMode.PickleGenEasy:
                    await ShutdownPgenServerAsync();
                    await ShutdownPgenClientAsync();
                    PostSequenceLog("Pattern mode: PickleGen Easy — connect via PickleGen tab");
                    break;
                case PatternPlaybackMode.LgTv:
                    await ShutdownPgenServerAsync();
                    await ShutdownPgenClientAsync();
                    await ShutdownPickleGenRemoteAsync();
                    PostSequenceLog("Pattern mode: LG TV toast prompts");
                    break;
                default:
                    await ShutdownPgenServerAsync();
                    await ShutdownPgenClientAsync();
                    await ShutdownPickleGenRemoteAsync();
                    PostSequenceLog("Pattern mode: manual control");
                    break;
            }
        }

        private Task EnsurePgenServerAsync()
        {
            if (_pgenServer != null)
            {
                return Task.CompletedTask;
            }

            var server = new PGenServer();
            server.OnStatusChange += PostSequenceLog;
            server.OnPatternChange += _ => PostSequenceLog("PGenerator emitted pattern");
            _pgenServer = server;

            Task.Run(async () =>
            {
                try
                {
                    await server.StartAsync();
                }
                catch (Exception ex)
                {
                    PostSequenceLog($"PGenerator start failed: {ex.Message}");
                }
            });

            PostSequenceLog("Starting internal PGenerator server...");
            return Task.CompletedTask;
        }

        private async Task ShutdownPgenServerAsync()
        {
            var server = _pgenServer;
            if (server == null)
            {
                return;
            }

            _pgenServer = null;
            try
            {
                await server.StopAsync();
                PostSequenceLog("Internal PGenerator stopped");
            }
            catch (Exception ex)
            {
                PostSequenceLog($"PGenerator stop failed: {ex.Message}");
            }
            finally
            {
                server.Dispose();
            }
        }

        private async Task EnsurePgenClientAsync()
        {
            if (_pgenClient != null && _pgenClient.IsConnected)
            {
                return;
            }

            var client = new PGenClient();
            client.OnStatusChange += PostSequenceLog;
            _pgenClient = client;

            PostSequenceLog("Discovering Android PGenerator...");
            try
            {
                var devices = await client.DiscoverAsync();
                if (devices.Length > 0)
                {
                    await client.ConnectAsync(devices[0].Address);
                    PostSequenceLog($"Connected to Android PGenerator: {devices[0].Name} ({devices[0].Address})");
                }
                else
                {
                    PostSequenceLog("No Android PGenerator found. Enter IP manually or check PickleGen app.");
                }
            }
            catch (Exception ex)
            {
                PostSequenceLog($"Android PGenerator connection failed: {ex.Message}");
            }
        }

        private async Task ShutdownPgenClientAsync()
        {
            var client = _pgenClient;
            if (client == null)
            {
                return;
            }

            _pgenClient = null;
            try
            {
                await client.DisconnectAsync();
                PostSequenceLog("Android PGenerator disconnected");
            }
            catch (Exception ex)
            {
                PostSequenceLog($"Android PGenerator disconnect failed: {ex.Message}");
            }
        }

        private Task HandleSequencePatternAsync(MeasurementStep step, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (!MeasurementPatternFactory.TryCreate(step, out var instruction) || !instruction.IsValid)
            {
                PostSequenceLog($"Prepare pattern manually: {step.PatternDescription ?? step.Name}");
                return Task.CompletedTask;
            }

            string description = instruction.Description.Length > 0 ? instruction.Description : step.Name;

            return _patternMode switch
            {
                PatternPlaybackMode.PGenerator => HandlePatternViaPgenAsync(instruction, description),
                PatternPlaybackMode.AndroidPGen => HandlePatternViaAndroidPgenAsync(instruction, description),
                PatternPlaybackMode.PickleGenEasy => HandlePatternViaPickleGenEasyAsync(instruction, description),
                PatternPlaybackMode.LgTv => HandlePatternViaTvAsync(instruction, description, token),
                _ => HandlePatternManually(description)
            };
        }

        private Task HandlePatternManually(string description)
        {
            PostSequenceLog($"Set pattern manually: {description}");
            return Task.CompletedTask;
        }

        private Task HandlePatternViaPgenAsync(PatternInstruction instruction, string description)
        {
            if (_pgenServer == null)
            {
                PostSequenceLog($"PGenerator unavailable; set manually: {description}");
                return Task.CompletedTask;
            }

            switch (instruction.Kind)
            {
                case PatternKind.FullField:
                    _pgenServer.EmitFullField(instruction.Red, instruction.Green, instruction.Blue);
                    PostSequenceLog($"PGenerator full-field: {description}");
                    break;
                case PatternKind.Window:
                    double percent = instruction.WindowPercent ?? 10d;
                    _pgenServer.EmitWindow(percent, instruction.Red, instruction.Green, instruction.Blue, instruction.BackgroundRed, instruction.BackgroundGreen, instruction.BackgroundBlue);
                    PostSequenceLog($"PGenerator window {percent:F0}%: {description}");
                    break;
                default:
                    PostSequenceLog($"Pattern not supported for PGenerator; manual control required: {description}");
                    break;
            }

            return Task.CompletedTask;
        }

        private async Task HandlePatternViaTvAsync(PatternInstruction instruction, string description, CancellationToken token)
        {
            var controller = _tvController;
            if (controller == null || !controller.IsConnected)
            {
                PostSequenceLog($"LG TV not connected; set manually: {description}");
                return;
            }

            token.ThrowIfCancellationRequested();

            try
            {
                string message = instruction.Kind switch
                {
                    PatternKind.FullField => $"Set pattern: {description} (RGB {instruction.Red},{instruction.Green},{instruction.Blue})",
                    PatternKind.Window => $"Set pattern: {description} ({FormatWindowPercent(instruction.WindowPercent)}RGB {instruction.Red},{instruction.Green},{instruction.Blue})",
                    _ => $"Set pattern: {description}"
                };
                await controller.ShowToastAsync(message);
                PostSequenceLog($"TV prompted for pattern: {description}");
            }
            catch (Exception ex)
            {
                PostSequenceLog($"TV pattern request failed: {ex.Message}");
            }
        }

        private async Task HandlePatternViaAndroidPgenAsync(PatternInstruction instruction, string description)
        {
            var client = _pgenClient;
            if (client == null || !client.IsConnected)
            {
                PostSequenceLog($"Android PGenerator not connected; set manually: {description}");
                return;
            }

            try
            {
                switch (instruction.Kind)
                {
                    case PatternKind.FullField:
                        await client.SendFullFieldAsync(instruction.Red, instruction.Green, instruction.Blue);
                        PostSequenceLog($"Android PGen full-field: {description}");
                        break;
                    case PatternKind.Window:
                        double percent = instruction.WindowPercent ?? 10d;
                        await client.SendWindowAsync(percent, instruction.Red, instruction.Green, instruction.Blue,
                            instruction.BackgroundRed, instruction.BackgroundGreen, instruction.BackgroundBlue);
                        PostSequenceLog($"Android PGen window {percent:F0}%: {description}");
                        break;
                    default:
                        PostSequenceLog($"Pattern not supported for Android PGen; manual control required: {description}");
                        break;
                }
            }
            catch (Exception ex)
            {
                PostSequenceLog($"Android PGen pattern error: {ex.Message}");
            }
        }

        // ---------- PickleGen Easy Mode ----------

        private async Task HandlePatternViaPickleGenEasyAsync(PatternInstruction instruction, string description)
        {
            var remote = _pickleGenRemote;
            if (remote == null || !remote.IsConnected)
            {
                PostSequenceLog($"PickleGen Easy not connected; set manually: {description}");
                return;
            }

            try
            {
                switch (instruction.Kind)
                {
                    case PatternKind.FullField:
                        await remote.SendFullFieldAsync((byte)instruction.Red, (byte)instruction.Green, (byte)instruction.Blue);
                        PostSequenceLog($"PickleGen Easy full-field: {description}");
                        break;
                    case PatternKind.Window:
                        float percent = (float)(instruction.WindowPercent ?? 10d);
                        await remote.SendWindowAsync(percent,
                            (byte)instruction.Red, (byte)instruction.Green, (byte)instruction.Blue,
                            (byte)instruction.BackgroundRed, (byte)instruction.BackgroundGreen, (byte)instruction.BackgroundBlue);
                        PostSequenceLog($"PickleGen Easy window {percent:F0}%: {description}");
                        break;
                    default:
                        PostSequenceLog($"Pattern not supported for PickleGen Easy; manual control required: {description}");
                        break;
                }
            }
            catch (Exception ex)
            {
                PostSequenceLog($"PickleGen Easy pattern error: {ex.Message}");
            }
        }

        private async Task ShutdownPickleGenRemoteAsync()
        {
            var remote = _pickleGenRemote;
            if (remote == null) return;
            _pickleGenRemote = null;

            try
            {
                await remote.DisposeAsync();
                PostSequenceLog("PickleGen Easy disconnected");
            }
            catch (Exception ex)
            {
                PostSequenceLog($"PickleGen Easy disconnect error: {ex.Message}");
            }

            UpdatePickleGenUi(false);
        }

        private void UpdatePickleGenUi(bool connected)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdatePickleGenUi(connected)));
                return;
            }

            btnPickleGenConnect.Enabled = !connected;
            btnPickleGenDiscover.Enabled = !connected;
            txtPickleGenIp.Enabled = !connected;
            btnPickleGenDisconnect.Enabled = connected;
            btnPickleGenApplyMode.Enabled = connected;
            btnPickleGenTestWhite.Enabled = connected;
            btnPickleGenTestBlack.Enabled = connected;

            if (connected && _pickleGenRemote != null)
            {
                lblPickleGenStatus.Text = $"Status: Connected to {_pickleGenRemote.RemoteAddress}";
                lblPickleGenStatus.ForeColor = DarkTheme.Success;
                lblPickleGenDevice.Text = $"Device: {_pickleGenRemote.DeviceName} (v{_pickleGenRemote.ProtocolVersion})";
            }
            else
            {
                lblPickleGenStatus.Text = "Status: Not connected";
                lblPickleGenStatus.ForeColor = DarkTheme.TextSecondary;
                lblPickleGenDevice.Text = "";
            }
        }

        private async void btnPickleGenDiscover_Click(object? sender, EventArgs e)
        {
            btnPickleGenDiscover.Enabled = false;
            lblPickleGenStatus.Text = "Status: Searching...";

            try
            {
                var client = new PickleCalRemoteClient();
                client.OnStatusChange += msg => BeginInvoke(new Action(() =>
                {
                    lblPickleGenStatus.Text = $"Status: {msg}";
                    PostSequenceLog($"PickleGen: {msg}");
                }));

                var devices = await client.DiscoverAsync();
                if (devices.Length > 0)
                {
                    txtPickleGenIp.Text = devices[0].Address.ToString();
                    lblPickleGenStatus.Text = $"Status: Found {devices[0].Name} — click Connect";
                }
                else
                {
                    lblPickleGenStatus.Text = "Status: No devices found. Enter IP manually.";
                }

                await client.DisposeAsync();
            }
            catch (Exception ex)
            {
                lblPickleGenStatus.Text = $"Status: Discovery error: {ex.Message}";
            }

            btnPickleGenDiscover.Enabled = true;
        }

        private async void btnPickleGenConnect_Click(object? sender, EventArgs e)
        {
            string ip = txtPickleGenIp.Text.Trim();
            if (string.IsNullOrEmpty(ip))
            {
                MessageBox.Show("Enter the PickleGen device IP address.", "PickleGen",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnPickleGenConnect.Enabled = false;
            lblPickleGenStatus.Text = "Status: Connecting...";

            try
            {
                await ShutdownPickleGenRemoteAsync();

                var remote = new PickleCalRemoteClient();
                remote.OnStatusChange += msg => BeginInvoke(new Action(() =>
                {
                    lblPickleGenStatus.Text = $"Status: {msg}";
                    PostSequenceLog($"PickleGen: {msg}");
                }));
                remote.OnDisconnected += () => BeginInvoke(new Action(() =>
                {
                    UpdatePickleGenUi(false);
                    PostSequenceLog("PickleGen: Connection lost");
                }));

                await remote.ConnectAsync(ip);
                _pickleGenRemote = remote;
                UpdatePickleGenUi(true);

                // Auto-select PickleGen Easy mode in the pattern source
                if (_patternMode != PatternPlaybackMode.PickleGenEasy)
                {
                    cmbPatternSource.SelectedIndex = 4;
                }

                PostSequenceLog($"PickleGen Easy connected: {remote.DeviceName} at {ip}");
            }
            catch (Exception ex)
            {
                lblPickleGenStatus.Text = $"Status: Connection failed: {ex.Message}";
                btnPickleGenConnect.Enabled = true;
                PostSequenceLog($"PickleGen connection failed: {ex.Message}");
            }
        }

        private async void btnPickleGenDisconnect_Click(object? sender, EventArgs e)
        {
            await ShutdownPickleGenRemoteAsync();
        }

        private async void btnPickleGenApplyMode_Click(object? sender, EventArgs e)
        {
            var remote = _pickleGenRemote;
            if (remote == null || !remote.IsConnected) return;

            try
            {
                if (rdoPickleGenHdr.Checked)
                {
                    await remote.SetupForHdrCalibrationAsync();
                    PostSequenceLog("PickleGen: Configured for HDR10 calibration");
                }
                else
                {
                    await remote.SetupForSdrCalibrationAsync();
                    PostSequenceLog("PickleGen: Configured for SDR calibration");
                }
            }
            catch (Exception ex)
            {
                PostSequenceLog($"PickleGen mode change error: {ex.Message}");
            }
        }

        private async void btnPickleGenTestWhite_Click(object? sender, EventArgs e)
        {
            var remote = _pickleGenRemote;
            if (remote == null || !remote.IsConnected) return;
            try
            {
                await remote.SendWhiteAsync();
                PostSequenceLog("PickleGen: Test white pattern");
            }
            catch (Exception ex)
            {
                PostSequenceLog($"PickleGen test error: {ex.Message}");
            }
        }

        private async void btnPickleGenTestBlack_Click(object? sender, EventArgs e)
        {
            var remote = _pickleGenRemote;
            if (remote == null || !remote.IsConnected) return;
            try
            {
                await remote.SendBlackAsync();
                PostSequenceLog("PickleGen: Test black pattern");
            }
            catch (Exception ex)
            {
                PostSequenceLog($"PickleGen test error: {ex.Message}");
            }
        }

        private void btnLaunchWizard_Click(object? sender, EventArgs e)
        {
            ShowCalibrationWizard();
        }

        private static string FormatWindowPercent(double? value)
        {
            if (!value.HasValue)
            {
                return string.Empty;
            }

            double clamped = Math.Clamp(value.Value, 0d, 100d);
            return $"{clamped:F0}% window, ";
        }

        private void PostSequenceLog(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => AppendSequenceLog(message)));
            }
            else
            {
                AppendSequenceLog(message);
            }
        }

        private void EnsureSequenceRunner()
        {
            if (_sequenceRunner != null)
            {
                return;
            }

            _sequenceRunner = new MeasurementQueueRunner(_meterManager, HandleSequencePatternAsync);
            _sequenceRunner.SequenceStarted += SequenceRunnerOnSequenceStarted;
            _sequenceRunner.StepStarted += SequenceRunnerOnStepStarted;
            _sequenceRunner.StepCompleted += SequenceRunnerOnStepCompleted;
            _sequenceRunner.SequenceCompleted += SequenceRunnerOnSequenceCompleted;
            _sequenceRunner.StepFailed += SequenceRunnerOnStepFailed;
        }

        private void SequenceRunnerOnSequenceStarted(MeasurementSequence sequence)
        {
            lstMeasurementLog.Items.Clear();
            AppendSequenceLog($"Sequence started: {sequence.Name}");
            lblSequenceStatus.Text = $"Sequence: {sequence.Name}";
        }

        private void SequenceRunnerOnStepStarted(MeasurementStep step)
        {
            lblSequenceStatus.Text = $"Running: {step.Name}";
            AppendSequenceLog($"Step started: {step.Name}");
        }

        private void SequenceRunnerOnStepCompleted(MeasurementStep step, MeterMeasurementResult? result)
        {
            if (result != null && result.Success && result.Reading != null)
            {
                var reading = result.Reading;
                var (x, y) = reading.Chromaticity;
                AppendSequenceLog($"Step completed: {step.Name} | Y {reading.Luminance:F2} cd/m², x {x:F4}, y {y:F4}");

                // Feed into CalibrationSession
                RecordMeasurement(step, reading);
            }
            else
            {
                string message = result?.Message ?? "No data";
                AppendSequenceLog($"Step completed with errors: {step.Name} | {message}");
            }
        }

        private void SequenceRunnerOnStepFailed(MeasurementStep step, Exception ex)
        {
            AppendSequenceLog($"Step failed: {step.Name} | {ex.Message}");
            lblSequenceStatus.Text = $"Error on {step.Name}";
        }

        private void SequenceRunnerOnSequenceCompleted(IReadOnlyList<MeasurementStepResult> results)
        {
            int successCount = results.Count(r => r.Success);
            AppendSequenceLog($"Sequence complete: {successCount}/{results.Count} successful");
            lblSequenceStatus.Text = $"Sequence complete ({successCount}/{results.Count})";

            // Refresh all analysis charts
            RefreshAnalysisCharts();
        }

        private void RecordMeasurement(MeasurementStep step, MeterReading reading)
        {
            if (_session == null)
            {
                // Auto-create a default session if none exists
                _session = new CalibrationSession("Auto", ColorSpace.Rec709, EotfType.Gamma, 2.2);
                lblSessionInfo.Text = "Session: Auto | Rec.709 | Gamma γ2.2";
            }

            var xyz = new CieXyz(reading.X, reading.Y, reading.Z);

            switch (step.Category)
            {
                case MeasurementCategory.Grayscale:
                    _session.AddGrayscalePoint(step.Name, step.TargetIre, xyz);
                    break;
                case MeasurementCategory.NearBlack:
                    _session.AddNearBlackPoint(step.Name, step.TargetIre, xyz);
                    break;
                case MeasurementCategory.NearWhite:
                    _session.AddNearWhitePoint(step.Name, step.TargetIre, xyz);
                    break;
                case MeasurementCategory.Primary:
                    _session.AddPrimaryPoint(step.Name, step.TargetIre, xyz);
                    break;
                case MeasurementCategory.Secondary:
                    _session.AddSecondaryPoint(step.Name, step.TargetIre, xyz);
                    break;
                case MeasurementCategory.Saturation:
                    _session.AddSaturationPoint(step.Name, step.TargetIre, xyz);
                    break;
                default:
                    _session.AddFreePoint(step.Name, xyz);
                    break;
            }

            // Update peak/black luminance from 100% and 0% grayscale
            if (step.Category == MeasurementCategory.Grayscale)
            {
                if (step.TargetIre >= 99.5)
                    _session.PeakWhiteLuminance = reading.Luminance;
                else if (step.TargetIre < 0.5)
                    _session.BlackLuminance = reading.Luminance;
            }
        }

        private void RefreshAnalysisCharts()
        {
            if (_session == null)
                return;

            var allPoints = _session.Points.ToList();
            var grayscale = _session.Grayscale.ToList();

            _cieDiagram.SetPoints(allPoints);
            _gammaChart.SetPoints(grayscale);
            _rgbBalance.SetPoints(grayscale);
            _luminanceChart.SetPoints(grayscale);
            _deltaEChart.SetPoints(allPoints);
            _cctChart.SetPoints(grayscale);
            _dataGrid.SetPoints(allPoints);

            // Update session info label
            lblSessionInfo.Text = $"Session: {_session.DisplayName} | " +
                $"Avg ΔE₂₀₀₀: {_session.AverageDeltaE:F2} | " +
                $"Max GS ΔE: {_session.MaxGrayscaleDeltaE:F2} | " +
                $"CR: {(_session.ContrastRatio < 1e6 ? _session.ContrastRatio.ToString("F0") : "∞")}:1 | " +
                $"{allPoints.Count} points";
        }

        private void AppendSequenceLog(string message)
        {
            lstMeasurementLog.Items.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            lstMeasurementLog.TopIndex = lstMeasurementLog.Items.Count - 1;
        }

        private void UpdateSequenceButtons()
        {
            if (btnRunGrayscale == null || btnRunColorSweep == null || btnStopSequence == null)
            {
                return;
            }

            bool meterReady = _lastMeterState == MeterMeasurementState.Idle;
            btnRunGrayscale.Enabled = meterReady && !_sequenceRunning;
            btnRunColorSweep.Enabled = meterReady && !_sequenceRunning;
            btnRunNearBlack.Enabled = meterReady && !_sequenceRunning;
            btnRunNearWhite.Enabled = meterReady && !_sequenceRunning;
            btnRunSaturation.Enabled = meterReady && !_sequenceRunning;
            btnRunMeasureAll.Enabled = meterReady && !_sequenceRunning;
            btnStopSequence.Enabled = _sequenceRunning;
        }

        private async Task RefreshMetersAsync()
        {
            ToggleMeterControls(false);
            try
            {
                lblMeterStatus.Text = "Meter status: scanning...";
                await _meterManager.RefreshMetersAsync(_meterCancellation.Token);
                PopulateMeterList();
                lblMeterStatus.Text = _meterManager.KnownMeters.Count > 0 ? "Meter status: select a device" : "Meter status: no meters found";
            }
            catch (OperationCanceledException)
            {
                lblMeterStatus.Text = "Meter status: scan cancelled";
            }
            catch (Exception ex)
            {
                lblMeterStatus.Text = $"Meter status: {ex.Message}";
            }
            finally
            {
                ToggleMeterControls(true);
            }
        }

        private void PopulateMeterList()
        {
            cmbMeters.BeginUpdate();
            try
            {
                cmbMeters.DataSource = null;
                var meters = _meterManager.KnownMeters.ToList();
                cmbMeters.DataSource = meters;
                cmbMeters.DisplayMember = nameof(MeterDescriptor.DisplayName);
                cmbMeters.ValueMember = nameof(MeterDescriptor.Id);
                if (meters.Count > 0)
                {
                    cmbMeters.SelectedIndex = 0;
                }
            }
            finally
            {
                cmbMeters.EndUpdate();
            }
        }

        private void MeterManagerOnStateChanged(MeterStateChangedEventArgs args)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => MeterManagerOnStateChanged(args)));
                return;
            }

            string meterName = args.Descriptor?.DisplayName ?? "None";
            _lastMeterState = args.State;
            lblMeterStatus.Text = $"Meter status: {args.State} ({meterName})";
            ApplyMeterState();
        }

        private void MeterManagerOnMeasurement(MeterMeasurementResult result)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => MeterManagerOnMeasurement(result)));
                return;
            }

            if (result.Success && result.Reading != null)
            {
                var reading = result.Reading;
                var (x, y) = reading.Chromaticity;
                string cct = reading.CorrelatedColorTemperatureK.HasValue
                    ? $", CCT {reading.CorrelatedColorTemperatureK.Value:F0}K"
                    : string.Empty;
                string deltaE = reading.DeltaE2000.HasValue ? $", ΔE {reading.DeltaE2000.Value:F2}" : string.Empty;
                lblMeterMeasurement.Text = $"Last reading: Y {reading.Luminance:F2} cd/m², x {x:F4}, y {y:F4}{cct}{deltaE}";
            }
            else
            {
                lblMeterMeasurement.Text = $"Last reading: {result.Message ?? "No data"}";
            }
        }

        private void ToggleMeterControls(bool enabled)
        {
            btnMeterRefresh.Enabled = enabled;
            if (!enabled)
            {
                btnMeterConnect.Enabled = false;
                btnMeterMeasure.Enabled = false;
                btnMeterCalibrate.Enabled = false;
                btnMeterDisconnect.Enabled = false;
            }
            else
            {
                ApplyMeterState();
            }
        }

        private void ApplyMeterState()
        {
            btnMeterConnect.Enabled = _meterManager != null && _lastMeterState == MeterMeasurementState.Disconnected && _meterManager.KnownMeters.Count > 0;
            btnMeterMeasure.Enabled = _lastMeterState == MeterMeasurementState.Idle;
            btnMeterCalibrate.Enabled = _lastMeterState == MeterMeasurementState.Idle || _lastMeterState == MeterMeasurementState.Calibrating;
            btnMeterDisconnect.Enabled = _lastMeterState != MeterMeasurementState.Disconnected;
            UpdateSequenceButtons();
        }
    }
}