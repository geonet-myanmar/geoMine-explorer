using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GeoMineExplorer
{
    public class MainForm : Form
    {
        // Data Stores
        private List<Collar> _collars = new List<Collar>();
        private List<Survey> _surveys = new List<Survey>();
        private List<AssayRecord> _assays = new List<AssayRecord>();
        private List<LithologyRecord> _lithologies = new List<LithologyRecord>();
        private List<StructuralRecord> _structures = new List<StructuralRecord>();
        private List<CompositeRecord> _composites = new List<CompositeRecord>();

        // Controls
        private TabControl _mainTabControl;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusLabel;
        private ToolStripStatusLabel _statsSummaryLabel;

        // Tab 1: Data Grid Views
        private DataGridView _gridCollars;
        private DataGridView _gridSurveys;
        private DataGridView _gridAssays;
        private DataGridView _gridLithology;
        private DataGridView _gridStructures;

        // Tab 2: Strip Log
        private ComboBox _comboStripHole;
        private ComboBox _comboStripElement;
        private TrackBar _sliderStripScale;
        private StripLogPanel _stripLogPanel;

        // Tab 3: Plan & Section
        private PlanAndSectionPanel _planSectionPanel;
        private RadioButton _rbPlanView;
        private RadioButton _rbSectionView;
        private TrackBar _sliderSectionAzimuth;
        private Label _lblSectionAzimuth;
        private NumericUpDown _numCorridor;

        // Tab 4: Statistics & Cut-off
        private ComboBox _comboStatsElement;
        private DataGridView _gridStats;
        private NumericUpDown _numCutoff;
        private TrackBar _sliderCutoff;
        private Label _lblCutoffResultSamples;
        private Label _lblCutoffResultLength;
        private Label _lblCutoffResultGrade;
        private Label _lblCutoffResultGT;
        private HistogramChartPanel _histogramPanel;

        // Tab 5: Compositing
        private NumericUpDown _numCompLength;
        private NumericUpDown _numCompMinFraction;
        private DataGridView _gridComposites;
        private Label _lblCompSummary;

        // Tab 6: Structural & Geometry
        private TextBox _txtCalcStrike;
        private TextBox _txtCalcDip;
        private Label _lblCalcDipDir;
        private TextBox _txtAppTrueDip;
        private TextBox _txtAppStrike;
        private TextBox _txtAppSectionBearing;
        private Label _lblAppResult;
        private TextBox _txtThickAppLength;
        private TextBox _txtThickHoleDip;
        private TextBox _txtThickHoleAz;
        private TextBox _txtThickPlaneDip;
        private TextBox _txtThickPlaneStrike;
        private Label _lblThickResult;
        private RoseDiagramPanel _rosePanel;

        // Tab 7: Resource Estimator
        private NumericUpDown _numResArea;
        private NumericUpDown _numResThickness;
        private NumericUpDown _numResDensity;
        private NumericUpDown _numResGrade;
        private NumericUpDown _numResDilution;
        private NumericUpDown _numResRecovery;
        private NumericUpDown _numResPrice;
        private ComboBox _comboResCommodity;
        private Label _lblResInSituTonnage;
        private Label _lblResMinedTonnage;
        private Label _lblResDilutedGrade;
        private Label _lblResContainedMetal;
        private Label _lblResGrossValue;
        private DataGridView _gridSensitivity;

        public MainForm()
        {
            InitializeComponent();
            LoadSampleProject(); // Automatically populate with realistic exploration project data on launch!
        }

        private void InitializeComponent()
        {
            this.Text = "GeoMine Explorer - Geological Exploration & Mining Geology Suite v1.0";
            this.Size = new Size(1280, 840);
            this.MinimumSize = new Size(1024, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);
            this.BackColor = Color.FromArgb(245, 247, 250);

            // Menu Bar & Tool Strip
            var toolStrip = CreateMainToolStrip();
            this.Controls.Add(toolStrip);

            // Status Bar
            _statusStrip = new StatusStrip { BackColor = Color.FromArgb(241, 245, 249), SizingGrip = true };
            _statusLabel = new ToolStripStatusLabel { Text = "Ready", Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            _statsSummaryLabel = new ToolStripStatusLabel { Text = "", BorderSides = ToolStripStatusLabelBorderSides.Left, BorderStyle = Border3DStyle.Etched };
            _statusStrip.Items.AddRange(new ToolStripItem[] { _statusLabel, _statsSummaryLabel });
            this.Controls.Add(_statusStrip);

            // Main Tab Control
            _mainTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(12, 6),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
            };

            // Build Individual Tabs
            _mainTabControl.TabPages.Add(BuildDataTab());
            _mainTabControl.TabPages.Add(BuildStripLogTab());
            _mainTabControl.TabPages.Add(BuildPlanAndSectionTab());
            _mainTabControl.TabPages.Add(BuildAssayStatsTab());
            _mainTabControl.TabPages.Add(BuildCompositingTab());
            _mainTabControl.TabPages.Add(BuildStructuralTab());
            _mainTabControl.TabPages.Add(BuildMiningEstimatorTab());

            this.Controls.Add(_mainTabControl);
            _mainTabControl.BringToFront();
        }

        private ToolStrip CreateMainToolStrip()
        {
            var ts = new ToolStrip
            {
                GripStyle = ToolStripGripStyle.Hidden,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                Padding = new Padding(6, 4, 6, 4),
                RenderMode = ToolStripRenderMode.System,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular)
            };

            var btnSample = new ToolStripButton("★ Load Sample Exploration Project (Golden Ridge Au-Cu)", null, (s, e) => LoadSampleProject())
            {
                ForeColor = Color.FromArgb(254, 240, 138),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                Padding = new Padding(4, 2, 8, 2)
            };

            var btnImport = new ToolStripDropDownButton("Import CSV Data", null,
                new ToolStripMenuItem("Import Collars CSV...", null, (s, e) => ImportCSV("collars")),
                new ToolStripMenuItem("Import Downhole Surveys CSV...", null, (s, e) => ImportCSV("surveys")),
                new ToolStripMenuItem("Import Assay Intervals CSV...", null, (s, e) => ImportCSV("assays")),
                new ToolStripMenuItem("Import Lithology Logs CSV...", null, (s, e) => ImportCSV("lithology")),
                new ToolStripMenuItem("Import Structural Orientations CSV...", null, (s, e) => ImportCSV("structures"))
            ) { ForeColor = Color.White };

            var btnExport = new ToolStripDropDownButton("Export Data & Reports", null,
                new ToolStripMenuItem("Export Collars to CSV...", null, (s, e) => ExportDataToCSV("collars")),
                new ToolStripMenuItem("Export Assays to CSV...", null, (s, e) => ExportDataToCSV("assays")),
                new ToolStripMenuItem("Export Composites to CSV...", null, (s, e) => ExportDataToCSV("composites")),
                new ToolStripMenuItem("Export Resource Sensitivity Summary...", null, (s, e) => ExportDataToCSV("sensitivity")),
                new ToolStripSeparator(),
                new ToolStripMenuItem("Export Active Visual as High-Res PNG Image...", null, (s, e) => ExportActiveVisualImage())
            ) { ForeColor = Color.White };

            var btnAbout = new ToolStripButton("About & Geology Guide", null, (s, e) => ShowAboutDialog())
            {
                ForeColor = Color.FromArgb(191, 219, 254),
                Alignment = ToolStripItemAlignment.Right
            };

            ts.Items.AddRange(new ToolStripItem[] { btnSample, new ToolStripSeparator(), btnImport, btnExport, btnAbout });
            return ts;
        }

        #region Tab 1: Project Data Explorer
        private TabPage BuildDataTab()
        {
            var tab = new TabPage("Project Data Explorer");
            var innerTabs = new TabControl { Dock = DockStyle.Fill };

            // Collars SubTab
            var tabCollar = new TabPage("Collars (" + _collars.Count + ")");
            _gridCollars = CreateStyledGrid();
            tabCollar.Controls.Add(_gridCollars);

            // Surveys SubTab
            var tabSurvey = new TabPage("Surveys (" + _surveys.Count + ")");
            _gridSurveys = CreateStyledGrid();
            tabSurvey.Controls.Add(_gridSurveys);

            // Assays SubTab
            var tabAssay = new TabPage("Assays (" + _assays.Count + ")");
            _gridAssays = CreateStyledGrid();
            tabAssay.Controls.Add(_gridAssays);

            // Lithology SubTab
            var tabLith = new TabPage("Lithology (" + _lithologies.Count + ")");
            _gridLithology = CreateStyledGrid();
            tabLith.Controls.Add(_gridLithology);

            // Structures SubTab
            var tabStruct = new TabPage("Structures (" + _structures.Count + ")");
            _gridStructures = CreateStyledGrid();
            tabStruct.Controls.Add(_gridStructures);

            innerTabs.TabPages.AddRange(new TabPage[] { tabCollar, tabSurvey, tabAssay, tabLith, tabStruct });
            tab.Controls.Add(innerTabs);
            return tab;
        }
        #endregion

        #region Tab 2: Drillhole Graphic Strip Log
        private TabPage BuildStripLogTab()
        {
            var tab = new TabPage("Drillhole Graphic Strip Log");

            // Top Control Panel
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.FromArgb(241, 245, 249), Padding = new Padding(10, 6, 10, 6) };

            var lblHole = new Label { Text = "Select Drillhole:", AutoSize = true, Location = new Point(12, 12), Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            _comboStripHole = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(120, 9), Width = 110 };
            _comboStripHole.SelectedIndexChanged += (s, e) => UpdateStripLogView();

            var lblElem = new Label { Text = "Assay Element:", AutoSize = true, Location = new Point(250, 12), Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            _comboStripElement = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(350, 9), Width = 110 };
            _comboStripElement.Items.AddRange(new object[] { "Gold Au (g/t)", "Copper Cu (%)" });
            _comboStripElement.SelectedIndex = 0;
            _comboStripElement.SelectedIndexChanged += (s, e) => UpdateStripLogView();

            var lblScale = new Label { Text = "Vertical Zoom:", AutoSize = true, Location = new Point(485, 12), Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            _sliderStripScale = new TrackBar { Minimum = 1, Maximum = 8, Value = 3, Location = new Point(585, 6), Width = 120, TickStyle = TickStyle.None };
            _sliderStripScale.ValueChanged += (s, e) =>
            {
                if (_stripLogPanel != null) _stripLogPanel.SetScale(_sliderStripScale.Value);
            };

            var btnExportLog = new Button { Text = "Save Log Image (PNG)", Location = new Point(730, 8), Width = 150, Height = 28, BackColor = Color.White };
            btnExportLog.Click += (s, e) => ExportControlImage(_stripLogPanel, "Drillhole_StripLog");

            topPanel.Controls.AddRange(new Control[] { lblHole, _comboStripHole, lblElem, _comboStripElement, lblScale, _sliderStripScale, btnExportLog });

            // Split Container: Left is StripLogPanel, Right is Lithology Legend
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 720 };

            _stripLogPanel = new StripLogPanel { Dock = DockStyle.Fill };
            split.Panel1.Controls.Add(_stripLogPanel);

            // Right Panel: Lithology Legend & Description Guide
            var pnlLegend = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12), AutoScroll = true };
            var lblLegendTitle = new Label { Text = "Lithology Legend & Stratigraphic Guide", Font = new Font("Segoe UI", 10.0f, FontStyle.Bold), Dock = DockStyle.Top, Height = 30 };
            pnlLegend.Controls.Add(lblLegendTitle);

            var lithList = new[]
            {
                new { Code = "OXD", Name = "Oxide / Gossan", Color = "#D32F2F", Desc = "Ferruginous capping, weathered limonitic saprolite" },
                new { Code = "QVN", Name = "Quartz Vein", Color = "#FFF275", Desc = "Auriferous sheeted quartz-sulfide veins (Au-rich)" },
                new { Code = "POR", Name = "Porphyry Dyke", Color = "#FF8A65", Desc = "Quartz-feldspar porphyry, stockwork alteration" },
                new { Code = "BRX", Name = "Hydrothermal Breccia", Color = "#FFB74D", Desc = "Explosion breccia with chalcopyrite matrix" },
                new { Code = "SKN", Name = "Skarn / Calc-Silicate", Color = "#8D6E63", Desc = "Garnet-pyroxene contact skarn, high magnetite" },
                new { Code = "AND", Name = "Andesite Volcanics", Color = "#689F38", Desc = "Host porphyritic andesite, propylitic alteration" },
                new { Code = "BAS", Name = "Basalt Footwall", Color = "#2E7D32", Desc = "Fine-grained amygdaloidal volcanic basement" },
                new { Code = "SHL", Name = "Carbonaceous Shale", Color = "#424242", Desc = "Dark graphitic/pyritic marine sediments" },
                new { Code = "LIM", Name = "Limestone", Color = "#90CAF9", Desc = "Bioclastic limestone protolith" }
            };

            int yOffset = 35;
            foreach (var item in lithList)
            {
                var pBox = new Panel { Size = new Size(24, 20), Location = new Point(14, yOffset), BackColor = ColorTranslator.FromHtml(item.Color), BorderStyle = BorderStyle.FixedSingle };
                var pLabel = new Label
                {
                    Text = item.Code + " - " + item.Name + "\n" + item.Desc,
                    Location = new Point(46, yOffset),
                    Size = new Size(380, 36),
                    Font = new Font("Segoe UI", 8.2f)
                };
                pnlLegend.Controls.AddRange(new Control[] { pBox, pLabel });
                yOffset += 42;
            }

            split.Panel2.Controls.Add(pnlLegend);

            tab.Controls.AddRange(new Control[] { split, topPanel });
            return tab;
        }
        #endregion

        #region Tab 3: Plan Map & Cross-Section
        private TabPage BuildPlanAndSectionTab()
        {
            var tab = new TabPage("Plan Map & Cross-Section");

            var topPanel = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.FromArgb(241, 245, 249), Padding = new Padding(10, 6, 10, 6) };

            _rbPlanView = new RadioButton { Text = "Plan Map (Collar Traces)", Checked = true, AutoSize = true, Location = new Point(15, 12), Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            _rbSectionView = new RadioButton { Text = "Cross-Section Projection", AutoSize = true, Location = new Point(195, 12), Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };

            _rbPlanView.CheckedChanged += (s, e) =>
            {
                if (_rbPlanView.Checked) _planSectionPanel.SetViewMode(PlanAndSectionPanel.ViewMode.PlanMap);
            };
            _rbSectionView.CheckedChanged += (s, e) =>
            {
                if (_rbSectionView.Checked) _planSectionPanel.SetViewMode(PlanAndSectionPanel.ViewMode.CrossSection);
            };

            _lblSectionAzimuth = new Label { Text = "Section Azimuth: 090° (E-W)", AutoSize = true, Location = new Point(390, 12) };
            _sliderSectionAzimuth = new TrackBar { Minimum = 0, Maximum = 360, Value = 90, TickFrequency = 45, Location = new Point(540, 6), Width = 150, TickStyle = TickStyle.None };
            _sliderSectionAzimuth.ValueChanged += (s, e) =>
            {
                _lblSectionAzimuth.Text = string.Format("Section Azimuth: {0:000}°", _sliderSectionAzimuth.Value);
                _planSectionPanel.SetSectionParameters(_sliderSectionAzimuth.Value, (double)_numCorridor.Value);
            };

            var lblCorridor = new Label { Text = "Corridor (±m):", AutoSize = true, Location = new Point(710, 12) };
            _numCorridor = new NumericUpDown { Minimum = 10, Maximum = 500, Value = 80, Location = new Point(795, 9), Width = 60 };
            _numCorridor.ValueChanged += (s, e) =>
            {
                _planSectionPanel.SetSectionParameters(_sliderSectionAzimuth.Value, (double)_numCorridor.Value);
            };

            var btnExportMap = new Button { Text = "Save View (PNG)", Location = new Point(880, 8), Width = 130, Height = 28, BackColor = Color.White };
            btnExportMap.Click += (s, e) => ExportControlImage(_planSectionPanel, _rbPlanView.Checked ? "Collar_PlanMap" : "CrossSection");

            topPanel.Controls.AddRange(new Control[] { _rbPlanView, _rbSectionView, _lblSectionAzimuth, _sliderSectionAzimuth, lblCorridor, _numCorridor, btnExportMap });

            _planSectionPanel = new PlanAndSectionPanel { Dock = DockStyle.Fill };
            _planSectionPanel.CollarSelected += (c) =>
            {
                if (c != null && _comboStripHole.Items.Contains(c.HoleId))
                {
                    _comboStripHole.SelectedItem = c.HoleId;
                }
            };

            tab.Controls.AddRange(new Control[] { _planSectionPanel, topPanel });
            return tab;
        }
        #endregion

        #region Tab 4: Assay Statistics & Cut-off
        private TabPage BuildAssayStatsTab()
        {
            var tab = new TabPage("Assay Stats & Cut-off Analysis");

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 450 };

            // Left Panel: Stats Table & Cut-off parameters
            var leftPnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = Color.White };

            var lblElem = new Label { Text = "Element to Analyze:", AutoSize = true, Location = new Point(12, 14), Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            _comboStatsElement = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(140, 10), Width = 140 };
            _comboStatsElement.Items.AddRange(new object[] { "Gold Au (g/t)", "Copper Cu (%)", "Iron Fe (%)" });
            _comboStatsElement.SelectedIndex = 0;
            _comboStatsElement.SelectedIndexChanged += (s, e) => RecalculateStats();

            _gridStats = CreateStyledGrid();
            _gridStats.Location = new Point(12, 45);
            _gridStats.Size = new Size(420, 290);
            _gridStats.Columns.Add("Metric", "Statistical Parameter");
            _gridStats.Columns.Add("Value", "Calculated Value");
            _gridStats.Columns[0].Width = 200;
            _gridStats.Columns[1].Width = 180;

            // Cut-off evaluation group
            var grpCutoff = new GroupBox
            {
                Text = "Cut-off Grade & Ore-Waste Threshold Evaluation",
                Location = new Point(12, 345),
                Size = new Size(420, 260),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold)
            };

            var lblCO = new Label { Text = "Cut-off Grade Threshold:", Location = new Point(15, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Regular) };
            _numCutoff = new NumericUpDown { DecimalPlaces = 2, Increment = 0.1M, Minimum = 0.0M, Maximum = 20.0M, Value = 0.5M, Location = new Point(175, 22), Width = 70, Font = new Font("Segoe UI", 8.5f) };
            _numCutoff.ValueChanged += (s, e) =>
            {
                int sliderVal = (int)(_numCutoff.Value * 10);
                if (sliderVal <= _sliderCutoff.Maximum) _sliderCutoff.Value = sliderVal;
                UpdateCutoffDisplay();
            };

            _sliderCutoff = new TrackBar { Minimum = 0, Maximum = 50, Value = 5, Location = new Point(255, 18), Width = 150, TickStyle = TickStyle.None };
            _sliderCutoff.ValueChanged += (s, e) =>
            {
                _numCutoff.Value = (decimal)(_sliderCutoff.Value / 10.0);
            };

            _lblCutoffResultSamples = new Label { Location = new Point(15, 60), Size = new Size(390, 22), Font = new Font("Segoe UI", 9.0f, FontStyle.Regular), Text = "Samples Above Cut-off: -" };
            _lblCutoffResultLength = new Label { Location = new Point(15, 90), Size = new Size(390, 22), Font = new Font("Segoe UI", 9.0f, FontStyle.Regular), Text = "Mineralized Core Length: -" };
            _lblCutoffResultGrade = new Label { Location = new Point(15, 120), Size = new Size(390, 22), Font = new Font("Segoe UI", 9.0f, FontStyle.Bold), ForeColor = Color.FromArgb(194, 65, 12), Text = "Weighted Mean Grade: -" };
            _lblCutoffResultGT = new Label { Location = new Point(15, 150), Size = new Size(390, 22), Font = new Font("Segoe UI", 9.0f, FontStyle.Regular), Text = "Accumulated Grade-Thickness (GT): -" };

            var btnExportStats = new Button { Text = "Export Cut-off Report (CSV)", Location = new Point(15, 195), Width = 210, Height = 32, BackColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            btnExportStats.Click += (s, e) => ExportCutoffReport();

            grpCutoff.Controls.AddRange(new Control[] { lblCO, _numCutoff, _sliderCutoff, _lblCutoffResultSamples, _lblCutoffResultLength, _lblCutoffResultGrade, _lblCutoffResultGT, btnExportStats });

            leftPnl.Controls.AddRange(new Control[] { lblElem, _comboStatsElement, _gridStats, grpCutoff });
            split.Panel1.Controls.Add(leftPnl);

            // Right Panel: Visual Histogram & Curve
            _histogramPanel = new HistogramChartPanel { Dock = DockStyle.Fill };
            split.Panel2.Controls.Add(_histogramPanel);

            tab.Controls.Add(split);
            return tab;
        }
        #endregion

        #region Tab 5: Downhole Compositing
        private TabPage BuildCompositingTab()
        {
            var tab = new TabPage("Downhole Compositing");

            var topPanel = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Color.FromArgb(241, 245, 249), Padding = new Padding(12) };

            var lblLen = new Label { Text = "Composite Length (m):", Location = new Point(12, 16), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            _numCompLength = new NumericUpDown { DecimalPlaces = 1, Increment = 0.5M, Minimum = 0.5M, Maximum = 20.0M, Value = 2.0M, Location = new Point(155, 13), Width = 65 };

            var lblMin = new Label { Text = "Min Interval Threshold (%):", Location = new Point(240, 16), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            _numCompMinFraction = new NumericUpDown { Minimum = 10, Maximum = 100, Value = 50, Location = new Point(415, 13), Width = 55 };

            var btnRunComp = new Button { Text = "▶ Run Length-Weighted Compositing", Location = new Point(490, 10), Width = 250, Height = 32, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            btnRunComp.Click += (s, e) => ExecuteCompositing();

            var btnExportComp = new Button { Text = "Export Composites CSV", Location = new Point(760, 10), Width = 180, Height = 32, BackColor = Color.White };
            btnExportComp.Click += (s, e) => ExportDataToCSV("composites");

            _lblCompSummary = new Label { Location = new Point(12, 44), AutoSize = true, ForeColor = Color.FromArgb(71, 85, 105), Font = new Font("Segoe UI", 8.5f, FontStyle.Italic), Text = "Ready to composite." };

            topPanel.Controls.AddRange(new Control[] { lblLen, _numCompLength, lblMin, _numCompMinFraction, btnRunComp, btnExportComp, _lblCompSummary });

            _gridComposites = CreateStyledGrid();
            tab.Controls.AddRange(new Control[] { _gridComposites, topPanel });
            return tab;
        }
        #endregion

        #region Tab 6: Structural & Section Geometry
        private TabPage BuildStructuralTab()
        {
            var tab = new TabPage("Structural & Section Geometry");

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 500 };

            var leftPnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), AutoScroll = true, BackColor = Color.White };

            // 1. Strike/Dip to Dip Direction Converter
            var grpConv = new GroupBox { Text = "1. Planar Strike & Dip to Dip Direction Converter (Right-Hand Rule)", Location = new Point(10, 10), Size = new Size(470, 110), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            grpConv.Controls.Add(new Label { Text = "Strike (° 0-360):", Location = new Point(15, 25), AutoSize = true, Font = new Font("Segoe UI", 8.2f) });
            _txtCalcStrike = new TextBox { Text = "000", Location = new Point(115, 22), Width = 60 };
            grpConv.Controls.Add(_txtCalcStrike);

            grpConv.Controls.Add(new Label { Text = "Dip (° 0-90):", Location = new Point(190, 25), AutoSize = true, Font = new Font("Segoe UI", 8.2f) });
            _txtCalcDip = new TextBox { Text = "45", Location = new Point(265, 22), Width = 60 };
            grpConv.Controls.Add(_txtCalcDip);

            var btnCalcConv = new Button { Text = "Convert", Location = new Point(340, 20), Width = 80, Height = 26, BackColor = Color.FromArgb(241, 245, 249) };
            btnCalcConv.Click += (s, e) =>
            {
                double st, dp;
                if (double.TryParse(_txtCalcStrike.Text, out st) && double.TryParse(_txtCalcDip.Text, out dp))
                {
                    double dd = StructuralEngine.StrikeToDipDirection(st);
                    _lblCalcDipDir.Text = string.Format("Result: Dip / Dip Direction = {0:00}° / {1:000}° (RHR)", dp, dd);
                }
            };
            grpConv.Controls.Add(btnCalcConv);
            _lblCalcDipDir = new Label { Text = "Result: Dip / Dip Direction = 45° / 090° (RHR)", Location = new Point(15, 62), AutoSize = true, ForeColor = Color.FromArgb(37, 99, 235), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            grpConv.Controls.Add(_lblCalcDipDir);
            leftPnl.Controls.Add(grpConv);

            // 2. Apparent Dip on Cross-Section
            var grpApp = new GroupBox { Text = "2. Apparent Dip on Cross-Section Calculator", Location = new Point(10, 130), Size = new Size(470, 160), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            grpApp.Controls.Add(new Label { Text = "True Dip (°):", Location = new Point(15, 25), AutoSize = true, Font = new Font("Segoe UI", 8.2f) });
            _txtAppTrueDip = new TextBox { Text = "60", Location = new Point(140, 22), Width = 60 };
            grpApp.Controls.Add(_txtAppTrueDip);

            grpApp.Controls.Add(new Label { Text = "Planar Strike (°):", Location = new Point(15, 55), AutoSize = true, Font = new Font("Segoe UI", 8.2f) });
            _txtAppStrike = new TextBox { Text = "000", Location = new Point(140, 52), Width = 60 };
            grpApp.Controls.Add(_txtAppStrike);

            grpApp.Controls.Add(new Label { Text = "Section Bearing (°):", Location = new Point(15, 85), AutoSize = true, Font = new Font("Segoe UI", 8.2f) });
            _txtAppSectionBearing = new TextBox { Text = "090", Location = new Point(140, 82), Width = 60 };
            grpApp.Controls.Add(_txtAppSectionBearing);

            var btnCalcApp = new Button { Text = "Calculate Apparent Dip", Location = new Point(220, 50), Width = 180, Height = 30, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White };
            btnCalcApp.Click += (s, e) =>
            {
                double td, ps, sb;
                if (double.TryParse(_txtAppTrueDip.Text, out td) && double.TryParse(_txtAppStrike.Text, out ps) && double.TryParse(_txtAppSectionBearing.Text, out sb))
                {
                    double app = StructuralEngine.CalculateApparentDip(td, ps, sb);
                    _lblAppResult.Text = string.Format("Calculated Apparent Dip on Section: {0:F1}°", app);
                }
            };
            grpApp.Controls.Add(btnCalcApp);
            _lblAppResult = new Label { Text = "Calculated Apparent Dip on Section: 60.0°", Location = new Point(15, 120), AutoSize = true, ForeColor = Color.FromArgb(194, 65, 12), Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            grpApp.Controls.Add(_lblAppResult);
            leftPnl.Controls.Add(grpApp);

            // 3. True Stratigraphic Thickness
            var grpThick = new GroupBox { Text = "3. True Stratigraphic Thickness from Drillhole Core", Location = new Point(10, 300), Size = new Size(470, 240), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            grpThick.Controls.Add(new Label { Text = "Drill Intercept Length (m):", Location = new Point(15, 25), AutoSize = true, Font = new Font("Segoe UI", 8.2f) });
            _txtThickAppLength = new TextBox { Text = "25.0", Location = new Point(180, 22), Width = 60 };
            grpThick.Controls.Add(_txtThickAppLength);

            grpThick.Controls.Add(new Label { Text = "Drillhole Dip (e.g. -60°):", Location = new Point(15, 55), AutoSize = true, Font = new Font("Segoe UI", 8.2f) });
            _txtThickHoleDip = new TextBox { Text = "-60", Location = new Point(180, 52), Width = 60 };
            grpThick.Controls.Add(_txtThickHoleDip);

            grpThick.Controls.Add(new Label { Text = "Drillhole Azimuth (°):", Location = new Point(15, 85), AutoSize = true, Font = new Font("Segoe UI", 8.2f) });
            _txtThickHoleAz = new TextBox { Text = "090", Location = new Point(180, 82), Width = 60 };
            grpThick.Controls.Add(_txtThickHoleAz);

            grpThick.Controls.Add(new Label { Text = "Bedding / Vein Dip (°):", Location = new Point(15, 115), AutoSize = true, Font = new Font("Segoe UI", 8.2f) });
            _txtThickPlaneDip = new TextBox { Text = "70", Location = new Point(180, 112), Width = 60 };
            grpThick.Controls.Add(_txtThickPlaneDip);

            grpThick.Controls.Add(new Label { Text = "Bedding / Vein Strike (°):", Location = new Point(15, 145), AutoSize = true, Font = new Font("Segoe UI", 8.2f) });
            _txtThickPlaneStrike = new TextBox { Text = "000", Location = new Point(180, 142), Width = 60 };
            grpThick.Controls.Add(_txtThickPlaneStrike);

            var btnCalcThick = new Button { Text = "Calculate True Thickness", Location = new Point(260, 80), Width = 180, Height = 32, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White };
            btnCalcThick.Click += (s, e) =>
            {
                double appL, hd, ha, pd, ps;
                if (double.TryParse(_txtThickAppLength.Text, out appL) && double.TryParse(_txtThickHoleDip.Text, out hd) &&
                    double.TryParse(_txtThickHoleAz.Text, out ha) && double.TryParse(_txtThickPlaneDip.Text, out pd) && double.TryParse(_txtThickPlaneStrike.Text, out ps))
                {
                    double tt = StructuralEngine.CalculateTrueThickness(appL, hd, ha, pd, ps);
                    _lblThickResult.Text = string.Format("True Stratigraphic Thickness: {0:F2} m  (Ratio: {1:P1})", tt, appL > 0 ? tt / appL : 0);
                }
            };
            grpThick.Controls.Add(btnCalcThick);
            _lblThickResult = new Label { Text = "True Stratigraphic Thickness: 21.65 m  (Ratio: 86.6%)", Location = new Point(15, 190), AutoSize = true, ForeColor = Color.FromArgb(22, 101, 52), Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            grpThick.Controls.Add(_lblThickResult);
            leftPnl.Controls.Add(grpThick);

            split.Panel1.Controls.Add(leftPnl);

            // Right Panel: Rose Diagram Visualizer
            _rosePanel = new RoseDiagramPanel { Dock = DockStyle.Fill };
            split.Panel2.Controls.Add(_rosePanel);

            tab.Controls.Add(split);
            return tab;
        }
        #endregion

        #region Tab 7: Mine Resource & Tonnage Estimator
        private TabPage BuildMiningEstimatorTab()
        {
            var tab = new TabPage("Mine Resource & Tonnage Estimator");

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 460 };

            var leftPnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14), BackColor = Color.White, AutoScroll = true };

            var lblTitle = new Label { Text = "Quick Orebody Tonnage & Contained Metal Estimator", Font = new Font("Segoe UI", 10.0f, FontStyle.Bold), Dock = DockStyle.Top, Height = 30 };
            leftPnl.Controls.Add(lblTitle);

            int y = 40;
            // Area
            leftPnl.Controls.Add(new Label { Text = "Mineralized Footprint Area (m²):", Location = new Point(15, y + 4), AutoSize = true });
            _numResArea = new NumericUpDown { Maximum = 10000000, Minimum = 100, Value = 35000, Location = new Point(250, y), Width = 110 };
            _numResArea.ValueChanged += (s, e) => RecalculateMiningEstimate();
            leftPnl.Controls.Add(_numResArea);
            y += 35;

            // Thickness
            leftPnl.Controls.Add(new Label { Text = "Average True Thickness (m):", Location = new Point(15, y + 4), AutoSize = true });
            _numResThickness = new NumericUpDown { DecimalPlaces = 1, Maximum = 500, Minimum = 0.5M, Value = 18.0M, Location = new Point(250, y), Width = 110 };
            _numResThickness.ValueChanged += (s, e) => RecalculateMiningEstimate();
            leftPnl.Controls.Add(_numResThickness);
            y += 35;

            // Density
            leftPnl.Controls.Add(new Label { Text = "Bulk Density / Specific Gravity (t/m³):", Location = new Point(15, y + 4), AutoSize = true });
            _numResDensity = new NumericUpDown { DecimalPlaces = 2, Maximum = 6.0M, Minimum = 1.0M, Value = 2.75M, Location = new Point(250, y), Width = 110 };
            _numResDensity.ValueChanged += (s, e) => RecalculateMiningEstimate();
            leftPnl.Controls.Add(_numResDensity);
            y += 35;

            // In-situ Grade
            leftPnl.Controls.Add(new Label { Text = "In-situ Average Grade (g/t or %):", Location = new Point(15, y + 4), AutoSize = true });
            _numResGrade = new NumericUpDown { DecimalPlaces = 2, Maximum = 100.0M, Minimum = 0.01M, Value = 2.25M, Location = new Point(250, y), Width = 110 };
            _numResGrade.ValueChanged += (s, e) => RecalculateMiningEstimate();
            leftPnl.Controls.Add(_numResGrade);
            y += 35;

            // Dilution
            leftPnl.Controls.Add(new Label { Text = "Mining Dilution (%):", Location = new Point(15, y + 4), AutoSize = true });
            _numResDilution = new NumericUpDown { DecimalPlaces = 1, Maximum = 50.0M, Minimum = 0.0M, Value = 8.0M, Location = new Point(250, y), Width = 110 };
            _numResDilution.ValueChanged += (s, e) => RecalculateMiningEstimate();
            leftPnl.Controls.Add(_numResDilution);
            y += 35;

            // Recovery
            leftPnl.Controls.Add(new Label { Text = "Mining Recovery / Ore Extraction (%):", Location = new Point(15, y + 4), AutoSize = true });
            _numResRecovery = new NumericUpDown { DecimalPlaces = 1, Maximum = 100.0M, Minimum = 10.0M, Value = 95.0M, Location = new Point(250, y), Width = 110 };
            _numResRecovery.ValueChanged += (s, e) => RecalculateMiningEstimate();
            leftPnl.Controls.Add(_numResRecovery);
            y += 35;

            // Commodity
            leftPnl.Controls.Add(new Label { Text = "Commodity Type:", Location = new Point(15, y + 4), AutoSize = true });
            _comboResCommodity = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(250, y), Width = 140 };
            _comboResCommodity.Items.AddRange(new object[] { "Au (Gold) - $/troy oz", "Cu (Copper) - $/tonne" });
            _comboResCommodity.SelectedIndex = 0;
            _comboResCommodity.SelectedIndexChanged += (s, e) =>
            {
                if (_comboResCommodity.SelectedIndex == 0) _numResPrice.Value = 2400.0M;
                else _numResPrice.Value = 9200.0M;
                RecalculateMiningEstimate();
            };
            leftPnl.Controls.Add(_comboResCommodity);
            y += 35;

            // Price
            leftPnl.Controls.Add(new Label { Text = "Commodity Unit Price (USD):", Location = new Point(15, y + 4), AutoSize = true });
            _numResPrice = new NumericUpDown { Maximum = 500000, Minimum = 1, Value = 2400.0M, Location = new Point(250, y), Width = 110 };
            _numResPrice.ValueChanged += (s, e) => RecalculateMiningEstimate();
            leftPnl.Controls.Add(_numResPrice);
            y += 45;

            // Results Display Panel
            var grpRes = new GroupBox { Text = "Estimation Results Summary", Location = new Point(15, y), Size = new Size(420, 210), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            _lblResInSituTonnage = new Label { Text = "In-Situ Tonnage: -", Location = new Point(15, 25), Size = new Size(390, 22), Font = new Font("Segoe UI", 8.5f) };
            _lblResMinedTonnage = new Label { Text = "Mined Mill Feed: -", Location = new Point(15, 55), Size = new Size(390, 22), Font = new Font("Segoe UI", 8.5f) };
            _lblResDilutedGrade = new Label { Text = "Diluted Grade: -", Location = new Point(15, 85), Size = new Size(390, 22), Font = new Font("Segoe UI", 8.5f) };
            _lblResContainedMetal = new Label { Text = "Contained Metal: -", Location = new Point(15, 115), Size = new Size(390, 22), ForeColor = Color.FromArgb(194, 65, 12), Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
            _lblResGrossValue = new Label { Text = "Gross Metal Value: -", Location = new Point(15, 145), Size = new Size(390, 22), ForeColor = Color.FromArgb(22, 101, 52), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };

            grpRes.Controls.AddRange(new Control[] { _lblResInSituTonnage, _lblResMinedTonnage, _lblResDilutedGrade, _lblResContainedMetal, _lblResGrossValue });
            leftPnl.Controls.Add(grpRes);

            split.Panel1.Controls.Add(leftPnl);

            // Right Panel: Sensitivity Matrix Table
            var rightPnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = Color.FromArgb(250, 252, 255) };
            var lblSensTitle = new Label { Text = "Resource Sensitivity Matrix (Grade vs Tonnage Variations)", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Dock = DockStyle.Top, Height = 30 };
            _gridSensitivity = CreateStyledGrid();
            _gridSensitivity.Dock = DockStyle.Fill;
            rightPnl.Controls.AddRange(new Control[] { _gridSensitivity, lblSensTitle });
            split.Panel2.Controls.Add(rightPnl);

            tab.Controls.Add(split);
            return tab;
        }
        #endregion

        #region Helpers & Data Loading
        private DataGridView CreateStyledGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false
            };
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            grid.ColumnHeadersHeight = 30;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 8.5f);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            grid.DefaultCellStyle.SelectionForeColor = Color.Black;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            return grid;
        }

        private void LoadSampleProject()
        {
            _collars = SampleDataGenerator.GetSampleCollars();
            _surveys = SampleDataGenerator.GetSampleSurveys();
            _lithologies = SampleDataGenerator.GetSampleLithologies();
            _assays = SampleDataGenerator.GetSampleAssays();
            _structures = SampleDataGenerator.GetSampleStructures();

            // Refresh UI grids and views
            RefreshAllViews();

            _statusLabel.Text = "Sample Exploration Project: 'Golden Ridge Au-Cu Project' loaded successfully.";
            UpdateStatusSummary();
        }

        private void RefreshAllViews()
        {
            // 1. Refresh Data Tab Grids
            _gridCollars.DataSource = null;
            _gridCollars.DataSource = _collars.Select(c => new
            {
                c.HoleId,
                Easting_X = c.Easting,
                Northing_Y = c.Northing,
                Elevation_Z = c.Elevation,
                TotalDepth_m = c.TotalDepth,
                c.Dip,
                c.Azimuth,
                c.Project
            }).ToList();

            _gridSurveys.DataSource = null;
            _gridSurveys.DataSource = _surveys.Select(s => new
            {
                s.HoleId,
                Depth_m = s.Depth,
                s.Dip,
                s.Azimuth
            }).ToList();

            _gridAssays.DataSource = null;
            _gridAssays.DataSource = _assays.Select(a => new
            {
                a.SampleId,
                a.HoleId,
                From_m = a.FromDepth,
                To_m = a.ToDepth,
                Length_m = a.Length,
                Au_gpt = a.Au_gpt,
                Cu_pct = a.Cu_pct,
                Fe_pct = a.Fe_pct,
                Recovery_pct = a.RecoveryPct
            }).ToList();

            _gridLithology.DataSource = null;
            _gridLithology.DataSource = _lithologies.Select(l => new
            {
                l.HoleId,
                From_m = l.FromDepth,
                To_m = l.ToDepth,
                Length_m = l.Length,
                l.RockCode,
                l.Description
            }).ToList();

            _gridStructures.DataSource = null;
            _gridStructures.DataSource = _structures.Select(s => new
            {
                s.FeatureId,
                s.HoleId,
                Depth_m = s.Depth,
                s.FeatureType,
                Dip_deg = s.Dip,
                DipDirection_deg = s.DipDirection,
                Strike_deg = Math.Round(s.Strike, 1)
            }).ToList();

            // 2. Refresh Strip Log Combos
            _comboStripHole.Items.Clear();
            foreach (var c in _collars)
            {
                _comboStripHole.Items.Add(c.HoleId);
            }
            if (_comboStripHole.Items.Count > 0) _comboStripHole.SelectedIndex = 0;

            // 3. Refresh Plan and Section
            _planSectionPanel.SetData(_collars, _surveys, _lithologies, _assays);

            // 4. Refresh Stats
            RecalculateStats();

            // 5. Run Initial Compositing
            ExecuteCompositing();

            // 6. Refresh Rose Diagram
            if (_structures.Any())
            {
                var strikes = _structures.Select(s => s.Strike).ToList();
                _rosePanel.SetData(strikes, "Structural Rose Diagram (Planar Strikes)");
            }

            // 7. Refresh Mining Estimate
            RecalculateMiningEstimate();
        }

        private void UpdateStripLogView()
        {
            if (_comboStripHole.SelectedItem == null) return;
            string holeId = _comboStripHole.SelectedItem.ToString();
            var collar = _collars.FirstOrDefault(c => c.HoleId == holeId);
            string elem = _comboStripElement.SelectedIndex == 1 ? "Cu" : "Au";

            double scale = _sliderStripScale != null ? _sliderStripScale.Value : 3.0;
            _stripLogPanel.SetData(collar, _lithologies, _assays, elem, scale);
        }

        private void RecalculateStats()
        {
            if (!_assays.Any()) return;
            string elemName = _comboStatsElement.SelectedIndex == 1 ? "Cu" : (_comboStatsElement.SelectedIndex == 2 ? "Fe" : "Au");

            List<double> values = _assays.Select(a =>
                elemName == "Cu" ? a.Cu_pct : (elemName == "Fe" ? a.Fe_pct : a.Au_gpt)
            ).ToList();

            var stats = StatisticsEngine.CalculateStats(values);

            _gridStats.Rows.Clear();
            _gridStats.Rows.Add("Sample Count (N)", stats.Count.ToString());
            _gridStats.Rows.Add("Minimum Grade", string.Format("{0:F3}", stats.Min));
            _gridStats.Rows.Add("Maximum Grade", string.Format("{0:F3}", stats.Max));
            _gridStats.Rows.Add("Mean (Average) Grade", string.Format("{0:F3}", stats.Mean));
            _gridStats.Rows.Add("Median Grade (P50)", string.Format("{0:F3}", stats.Median));
            _gridStats.Rows.Add("Variance", string.Format("{0:F4}", stats.Variance));
            _gridStats.Rows.Add("Standard Deviation", string.Format("{0:F3}", stats.StdDev));
            _gridStats.Rows.Add("Standard Error", string.Format("{0:F4}", stats.StdErr));
            _gridStats.Rows.Add("Coefficient of Variation (CV)", string.Format("{0:F3}", stats.CV));
            _gridStats.Rows.Add("Skewness", string.Format("{0:F3}", stats.Skewness));
            _gridStats.Rows.Add("10th Percentile (P10)", string.Format("{0:F3}", stats.P10));
            _gridStats.Rows.Add("25th Percentile (P25)", string.Format("{0:F3}", stats.P25));
            _gridStats.Rows.Add("75th Percentile (P75)", string.Format("{0:F3}", stats.P75));
            _gridStats.Rows.Add("90th Percentile (P90)", string.Format("{0:F3}", stats.P90));
            _gridStats.Rows.Add("95th Percentile (P95)", string.Format("{0:F3}", stats.P95));

            // Histogram
            var bins = StatisticsEngine.CalculateHistogram(values, 16);
            _histogramPanel.SetData(bins, (double)_numCutoff.Value, _comboStatsElement.SelectedItem.ToString());

            UpdateCutoffDisplay();
        }

        private void UpdateCutoffDisplay()
        {
            if (!_assays.Any()) return;
            string elem = _comboStatsElement.SelectedIndex == 1 ? "Cu" : "Au";
            double co = (double)_numCutoff.Value;

            var eval = StatisticsEngine.EvaluateCutoff(_assays, co, elem);
            string unit = elem == "Cu" ? "%" : "g/t";

            _lblCutoffResultSamples.Text = string.Format("Samples Above Cut-off: {0} of {1} ({2:F1}%)", eval.SamplesAbove, _assays.Count, eval.SamplesAbovePct);
            _lblCutoffResultLength.Text = string.Format("Mineralized Core Length: {0:F1} m ({1:F1}% of drilled core)", eval.TotalLengthAbove, eval.TotalLengthAbovePct);
            _lblCutoffResultGrade.Text = string.Format("Weighted Mean Grade: {0:F3} {1}", eval.MeanGradeAbove, unit);
            _lblCutoffResultGT.Text = string.Format("Accumulated Grade-Thickness (GT): {0:F1} m·{1}", eval.GradeThickness, unit);

            // Re-render histogram with new cutoff line
            string elemName = _comboStatsElement.SelectedIndex == 1 ? "Cu" : (_comboStatsElement.SelectedIndex == 2 ? "Fe" : "Au");
            List<double> values = _assays.Select(a =>
                elemName == "Cu" ? a.Cu_pct : (elemName == "Fe" ? a.Fe_pct : a.Au_gpt)
            ).ToList();
            var bins = StatisticsEngine.CalculateHistogram(values, 16);
            _histogramPanel.SetData(bins, co, _comboStatsElement.SelectedItem.ToString());
        }

        private void ExecuteCompositing()
        {
            if (!_assays.Any()) return;

            double compLen = (double)_numCompLength.Value;
            double minFrac = (double)_numCompMinFraction.Value / 100.0;

            _composites = CompositingEngine.RunCompositing(_assays, compLen, minFrac);

            _gridComposites.DataSource = null;
            _gridComposites.DataSource = _composites.Select(c => new
            {
                c.HoleId,
                From_m = c.FromDepth,
                To_m = c.ToDepth,
                Length_m = c.Length,
                Comp_Au_gpt = c.Au_gpt,
                Comp_Cu_pct = c.Cu_pct,
                SampleCount = c.IntervalCount
            }).ToList();

            double rawMeanAu = _assays.Average(a => a.Au_gpt);
            double compMeanAu = _composites.Any() ? _composites.Average(c => c.Au_gpt) : 0;
            _lblCompSummary.Text = string.Format("Generated {0} composites ({1:0.#}m intervals). Original Raw Mean Au: {2:F2} g/t  vs  Composite Mean Au: {3:F2} g/t (Grade Preserved).",
                _composites.Count, compLen, rawMeanAu, compMeanAu);
        }

        private void RecalculateMiningEstimate()
        {
            double area = (double)_numResArea.Value;
            double thick = (double)_numResThickness.Value;
            double dens = (double)_numResDensity.Value;
            double grade = (double)_numResGrade.Value;
            double dil = (double)_numResDilution.Value;
            double rec = (double)_numResRecovery.Value;
            double price = (double)_numResPrice.Value;
            string comm = _comboResCommodity.SelectedItem.ToString();

            var res = MiningEstimator.CalculateTonnageAndGrade(area, thick, dens, grade, dil, rec, comm, price);

            _lblResInSituTonnage.Text = string.Format("In-Situ Tonnage: {0:N0} tonnes (Volume: {1:N0} m³)", res.InSituTonnage_t, res.InSituVolume_m3);
            _lblResMinedTonnage.Text = string.Format("Mined Mill Feed: {0:N0} tonnes (Recovered: {1:N0} tonnes)", res.MinedTonnage_t, res.RecoveredTonnage_t);
            _lblResDilutedGrade.Text = string.Format("Diluted Run-of-Mine Grade: {0:F2} (In-situ: {1:F2})", res.DilutedGrade, grade);
            _lblResContainedMetal.Text = string.Format("Contained Recovered Metal: {0:N1} {1}", res.ContainedMetalUnits, res.MetalUnitName);
            _lblResGrossValue.Text = string.Format("Gross Recovered Metal Value: ${0:N0} USD", res.GrossMetalValueUSD);

            // Populate Sensitivity Table
            var factors = new[] { -0.20, -0.10, 0.0, 0.10, 0.20 };
            var table = new System.Data.DataTable();
            table.Columns.Add("Scenario", typeof(string));
            table.Columns.Add("Grade Factor", typeof(string));
            table.Columns.Add("Estimated Grade", typeof(string));
            table.Columns.Add("Recovered Tonnes", typeof(string));
            table.Columns.Add("Contained Metal", typeof(string));
            table.Columns.Add("Gross Value ($ USD)", typeof(string));

            foreach (var f in factors)
            {
                double testG = grade * (1.0 + f);
                var subRes = MiningEstimator.CalculateTonnageAndGrade(area, thick, dens, testG, dil, rec, comm, price);
                string scName = f == 0 ? "★ Base Case" : string.Format("{0:+0%;-0%}", f);
                table.Rows.Add(
                    scName,
                    string.Format("{0:+0%;-0%}", f),
                    string.Format("{0:F2}", testG),
                    string.Format("{0:N0} t", subRes.RecoveredTonnage_t),
                    string.Format("{0:N0} {1}", subRes.ContainedMetalUnits, comm.Contains("Au") ? "oz" : "t"),
                    string.Format("${0:N0}", subRes.GrossMetalValueUSD)
                );
            }

            _gridSensitivity.DataSource = table;
        }

        private void UpdateStatusSummary()
        {
            int nHoles = _collars.Count;
            int nAssays = _assays.Count;
            double totLen = _assays.Sum(a => a.Length);
            double avgAu = _assays.Any() ? _assays.Average(a => a.Au_gpt) : 0;
            double avgCu = _assays.Any() ? _assays.Average(a => a.Cu_pct) : 0;

            _statsSummaryLabel.Text = string.Format("  Project: Golden Ridge  |  {0} Drillholes  |  {1} Assays ({2:N0}m drilled)  |  Mean Au: {3:F2} g/t  |  Mean Cu: {4:F2}%  ",
                nHoles, nAssays, totLen, avgAu, avgCu);
        }
        #endregion

        #region Import / Export & Dialogs
        private void ImportCSV(string tableType)
        {
            using (var ofd = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = "Import " + tableType.ToUpper() + " CSV Data"
            })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var lines = File.ReadAllLines(ofd.FileName);
                        if (lines.Length < 2)
                        {
                            MessageBox.Show("Selected CSV file has no data rows.", "Import Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        int imported = 0;
                        if (tableType == "collars")
                        {
                            var list = new List<Collar>();
                            for (int i = 1; i < lines.Length; i++)
                            {
                                var parts = lines[i].Split(new[] { ',', '\t', ';' });
                                if (parts.Length >= 5)
                                {
                                    list.Add(new Collar(
                                        parts[0].Trim(),
                                        double.Parse(parts[1], CultureInfo.InvariantCulture),
                                        double.Parse(parts[2], CultureInfo.InvariantCulture),
                                        double.Parse(parts[3], CultureInfo.InvariantCulture),
                                        double.Parse(parts[4], CultureInfo.InvariantCulture),
                                        parts.Length > 5 ? double.Parse(parts[5], CultureInfo.InvariantCulture) : -90.0,
                                        parts.Length > 6 ? double.Parse(parts[6], CultureInfo.InvariantCulture) : 0.0
                                    ));
                                    imported++;
                                }
                            }
                            _collars = list;
                        }
                        else if (tableType == "assays")
                        {
                            var list = new List<AssayRecord>();
                            for (int i = 1; i < lines.Length; i++)
                            {
                                var parts = lines[i].Split(new[] { ',', '\t', ';' });
                                if (parts.Length >= 5)
                                {
                                    list.Add(new AssayRecord(
                                        "S" + (1000 + i),
                                        parts[0].Trim(),
                                        double.Parse(parts[1], CultureInfo.InvariantCulture),
                                        double.Parse(parts[2], CultureInfo.InvariantCulture),
                                        double.Parse(parts[3], CultureInfo.InvariantCulture),
                                        parts.Length > 4 ? double.Parse(parts[4], CultureInfo.InvariantCulture) : 0.0,
                                        parts.Length > 5 ? double.Parse(parts[5], CultureInfo.InvariantCulture) : 0.0
                                    ));
                                    imported++;
                                }
                            }
                            _assays = list;
                        }

                        RefreshAllViews();
                        MessageBox.Show(string.Format("Successfully imported {0} records from {1}!", imported, Path.GetFileName(ofd.FileName)), "Import Succeeded", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error parsing CSV: " + ex.Message, "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ExportDataToCSV(string tableType)
        {
            using (var sfd = new SaveFileDialog
            {
                Filter = "CSV File (*.csv)|*.csv",
                FileName = tableType + "_export_" + DateTime.Now.ToString("yyyyMMdd") + ".csv",
                Title = "Export " + tableType + " Data to CSV"
            })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var sb = new StringBuilder();
                        if (tableType == "collars")
                        {
                            sb.AppendLine("HoleId,Easting,Northing,Elevation,TotalDepth,Dip,Azimuth,Project");
                            foreach (var c in _collars)
                                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5},{6},{7}", c.HoleId, c.Easting, c.Northing, c.Elevation, c.TotalDepth, c.Dip, c.Azimuth, c.Project));
                        }
                        else if (tableType == "assays")
                        {
                            sb.AppendLine("SampleId,HoleId,From,To,Length,Au_gpt,Cu_pct,Fe_pct,Recovery");
                            foreach (var a in _assays)
                                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5},{6},{7},{8}", a.SampleId, a.HoleId, a.FromDepth, a.ToDepth, a.Length, a.Au_gpt, a.Cu_pct, a.Fe_pct, a.RecoveryPct));
                        }
                        else if (tableType == "composites")
                        {
                            sb.AppendLine("HoleId,From,To,Length,Comp_Au_gpt,Comp_Cu_pct,IntervalCount");
                            foreach (var c in _composites)
                                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5},{6}", c.HoleId, c.FromDepth, c.ToDepth, c.Length, c.Au_gpt, c.Cu_pct, c.IntervalCount));
                        }
                        else if (tableType == "sensitivity")
                        {
                            sb.AppendLine("Scenario,GradeFactor,EstimatedGrade,RecoveredTonnes,ContainedMetal,GrossValueUSD");
                            for (int r = 0; r < _gridSensitivity.Rows.Count; r++)
                            {
                                var row = _gridSensitivity.Rows[r];
                                sb.AppendLine(string.Format("{0},{1},{2},{3},{4},{5}", row.Cells[0].Value, row.Cells[1].Value, row.Cells[2].Value, row.Cells[3].Value, row.Cells[4].Value, row.Cells[5].Value));
                            }
                        }

                        File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                        MessageBox.Show("Data successfully exported to:\n" + sfd.FileName, "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error exporting CSV: " + ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ExportCutoffReport()
        {
            using (var sfd = new SaveFileDialog
            {
                Filter = "CSV File (*.csv)|*.csv",
                FileName = "Cutoff_Grade_Tonnage_Report_" + DateTime.Now.ToString("yyyyMMdd") + ".csv",
                Title = "Export Grade-Tonnage Cut-off Report"
            })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string elem = _comboStatsElement.SelectedIndex == 1 ? "Cu" : "Au";
                        var curve = StatisticsEngine.GenerateGradeTonnageCurve(_assays, elem, 25);

                        var sb = new StringBuilder();
                        sb.AppendLine(string.Format("Cutoff_{0},SamplesAbove,SamplesAbovePct,CoreLengthAbove_m,LengthPct,MeanGradeAbove,GradeThickness_GT", elem));
                        foreach (var c in curve)
                        {
                            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F2},{1},{2:F1},{3:F1},{4:F1},{5:F3},{6:F1}",
                                c.Cutoff, c.SamplesAbove, c.SamplesAbovePct, c.TotalLengthAbove, c.TotalLengthAbovePct, c.MeanGradeAbove, c.GradeThickness));
                        }
                        File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                        MessageBox.Show("Report exported successfully to:\n" + sfd.FileName, "Report Exported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error exporting report: " + ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ExportControlImage(Control ctrl, string defaultName)
        {
            using (var sfd = new SaveFileDialog
            {
                Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg",
                FileName = defaultName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png",
                Title = "Save Visual Image"
            })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (var bmp = new Bitmap(ctrl.Width, ctrl.Height))
                        {
                            ctrl.DrawToBitmap(bmp, new Rectangle(0, 0, ctrl.Width, ctrl.Height));
                            bmp.Save(sfd.FileName, ImageFormat.Png);
                        }
                        MessageBox.Show("Image saved successfully to:\n" + sfd.FileName, "Image Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error saving image: " + ex.Message, "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ExportActiveVisualImage()
        {
            if (_mainTabControl.SelectedIndex == 1) ExportControlImage(_stripLogPanel, "Drillhole_StripLog");
            else if (_mainTabControl.SelectedIndex == 2) ExportControlImage(_planSectionPanel, "PlanOrSectionMap");
            else if (_mainTabControl.SelectedIndex == 3) ExportControlImage(_histogramPanel, "Assay_Histogram");
            else if (_mainTabControl.SelectedIndex == 5) ExportControlImage(_rosePanel, "Structural_RoseDiagram");
            else MessageBox.Show("Please switch to a graphical visual tab (Strip Log, Plan/Section, Histogram, or Rose Diagram) to export an image.", "Export Image", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowAboutDialog()
        {
            string msg = "GeoMine Explorer v1.0\n" +
                         "Exploration & Mining Geology Desktop Suite\n\n" +
                         "Core Features:\n" +
                         "• Drillhole Data Management (Collar, Survey, Assay, Lithology, Structure)\n" +
                         "• Desurveying Engine (Minimum Curvature & Balanced Tangential Algorithm)\n" +
                         "• Interactive Graphic Strip Log with depth crosshairs & hover tooltip\n" +
                         "• 2D Plan Map & Cross-Section Viewer with lithology downhole traces\n" +
                         "• Assay Statistics & Grade-Tonnage Cut-off Analysis Engine\n" +
                         "• Downhole Length-Weighted Compositing\n" +
                         "• Structural Geology: Strike/Dip to DipDir, Apparent Dip, True Thickness & Rose Diagram\n" +
                         "• Quick Mining Resource & Tonnage Estimator with Sensitivity Matrix\n\n" +
                         "Built as a lightweight, native, standalone Windows application.";

            MessageBox.Show(msg, "About GeoMine Explorer", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion
    }
}
