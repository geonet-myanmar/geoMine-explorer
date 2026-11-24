using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace GeoMineExplorer
{
    public class StripLogPanel : UserControl
    {
        private Collar _collar;
        private List<LithologyRecord> _lithologies = new List<LithologyRecord>();
        private List<AssayRecord> _assays = new List<AssayRecord>();
        private string _element = "Au"; // "Au" or "Cu"
        private double _scaleY = 3.5; // pixels per meter
        private double _hoverDepth = -1.0;
        private string _hoverInfo = "";

        public StripLogPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.BackColor = Color.FromArgb(250, 252, 255);
            this.AutoScroll = true;

            this.MouseMove += StripLogPanel_MouseMove;
            this.MouseLeave += (s, e) => { _hoverDepth = -1.0; Invalidate(); };
        }

        public void SetData(Collar collar, List<LithologyRecord> liths, List<AssayRecord> assays, string element = "Au", double scaleY = 3.5)
        {
            _collar = collar;
            _lithologies = liths ?? new List<LithologyRecord>();
            _assays = assays ?? new List<AssayRecord>();
            _element = element;
            _scaleY = scaleY;

            UpdateScrollSize();
            Invalidate();
        }

        public void SetScale(double scaleY)
        {
            _scaleY = Math.Max(1.0, Math.Min(10.0, scaleY));
            UpdateScrollSize();
            Invalidate();
        }

        private void UpdateScrollSize()
        {
            double maxDepth = _collar != null ? _collar.TotalDepth : 200.0;
            if (_assays.Any()) maxDepth = Math.Max(maxDepth, _assays.Max(a => a.ToDepth));
            if (_lithologies.Any()) maxDepth = Math.Max(maxDepth, _lithologies.Max(l => l.ToDepth));

            int totalHeight = (int)(maxDepth * _scaleY) + 120;
            this.AutoScrollMinSize = new Size(580, totalHeight);
        }

        private void StripLogPanel_MouseMove(object sender, MouseEventArgs e)
        {
            int scrollY = -this.AutoScrollPosition.Y;
            int topMargin = 50;
            double depth = (e.Y + scrollY - topMargin) / _scaleY;

            if (_collar != null && depth >= 0 && depth <= _collar.TotalDepth)
            {
                _hoverDepth = depth;
                var curLith = _lithologies.FirstOrDefault(l => depth >= l.FromDepth && depth <= l.ToDepth);
                var curAssay = _assays.FirstOrDefault(a => depth >= a.FromDepth && depth <= a.ToDepth);

                string lithStr = curLith != null ? (curLith.RockCode + " (" + curLith.Description + ")") : "No Lith";
                string assayStr = curAssay != null ? string.Format("Au: {0:F2} g/t, Cu: {1:F2}%, Fe: {2:F1}%", curAssay.Au_gpt, curAssay.Cu_pct, curAssay.Fe_pct) : "Unassayed";

                _hoverInfo = string.Format("Depth: {0:F1} m | Lithology: {1} | {2}", depth, lithStr, assayStr);
            }
            else
            {
                _hoverDepth = -1.0;
                _hoverInfo = "";
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int scrollX = this.AutoScrollPosition.X;
            int scrollY = this.AutoScrollPosition.Y;

            g.TranslateTransform(scrollX, scrollY);

            int topMargin = 50;
            int rulerX = 20;
            int rulerW = 60;
            int lithX = rulerX + rulerW + 10;
            int lithW = 140;
            int assayX = lithX + lithW + 15;
            int assayW = 280;

            if (_collar == null)
            {
                using (var brush = new SolidBrush(Color.DimGray))
                using (var font = new Font("Segoe UI", 11, FontStyle.Italic))
                {
                    g.DrawString("Please select a drillhole from the dropdown above to view graphic strip log.", font, brush, 30, 80);
                }
                return;
            }

            double eoh = _collar.TotalDepth;
            int logHeight = (int)(eoh * _scaleY);

            // 1. Draw Headers
            using (var headerFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var subFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (var headerBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
            using (var borderPen = new Pen(Color.FromArgb(203, 213, 225), 1))
            {
                // Title
                string holeTitle = string.Format("Drillhole: {0}  |  EOH: {1:F1} m  |  Dip: {2:F1}°  |  Azimuth: {3:F1}°",
                    _collar.HoleId, _collar.TotalDepth, _collar.Dip, _collar.Azimuth);
                g.DrawString(holeTitle, headerFont, headerBrush, rulerX, 12);

                // Column headers
                g.FillRectangle(new SolidBrush(Color.FromArgb(241, 245, 249)), rulerX, topMargin - 26, rulerW, 24);
                g.DrawRectangle(borderPen, rulerX, topMargin - 26, rulerW, 24);
                g.DrawString("Depth (m)", subFont, headerBrush, rulerX + 5, topMargin - 22);

                g.FillRectangle(new SolidBrush(Color.FromArgb(241, 245, 249)), lithX, topMargin - 26, lithW, 24);
                g.DrawRectangle(borderPen, lithX, topMargin - 26, lithW, 24);
                g.DrawString("Lithology", subFont, headerBrush, lithX + 40, topMargin - 22);

                g.FillRectangle(new SolidBrush(Color.FromArgb(241, 245, 249)), assayX, topMargin - 26, assayW, 24);
                g.DrawRectangle(borderPen, assayX, topMargin - 26, assayW, 24);
                string assayHeader = _element == "Cu" ? "Assay: Copper Cu (%)" : "Assay: Gold Au (g/t)";
                g.DrawString(assayHeader, subFont, headerBrush, assayX + 40, topMargin - 22);

                // Background borders for columns
                g.DrawRectangle(borderPen, rulerX, topMargin, rulerW, logHeight);
                g.DrawRectangle(borderPen, lithX, topMargin, lithW, logHeight);
                g.DrawRectangle(borderPen, assayX, topMargin, assayW, logHeight);
            }

            // 2. Draw Depth Ruler
            using (var penMinor = new Pen(Color.FromArgb(203, 213, 225), 1))
            using (var penMajor = new Pen(Color.FromArgb(100, 116, 139), 1.5f))
            using (var fontRuler = new Font("Segoe UI", 7.5f))
            using (var brushText = new SolidBrush(Color.FromArgb(71, 85, 105)))
            {
                for (double d = 0; d <= eoh; d += 1.0)
                {
                    int y = topMargin + (int)(d * _scaleY);
                    if (d % 10.0 == 0)
                    {
                        g.DrawLine(penMajor, rulerX + rulerW - 14, y, rulerX + rulerW, y);
                        g.DrawString(string.Format("{0:0}m", d), fontRuler, brushText, rulerX + 4, y - 6);
                        // Grid lines across columns
                        using (var gridPen = new Pen(Color.FromArgb(241, 245, 249), 1))
                        {
                            g.DrawLine(gridPen, lithX, y, lithX + lithW, y);
                            g.DrawLine(gridPen, assayX, y, assayX + assayW, y);
                        }
                    }
                    else if (d % 5.0 == 0)
                    {
                        g.DrawLine(penMajor, rulerX + rulerW - 8, y, rulerX + rulerW, y);
                    }
                    else
                    {
                        g.DrawLine(penMinor, rulerX + rulerW - 4, y, rulerX + rulerW, y);
                    }
                }
            }

            // 3. Draw Lithology Column
            using (var lithFont = new Font("Segoe UI", 8.0f, FontStyle.Bold))
            using (var descFont = new Font("Segoe UI", 7.0f))
            {
                foreach (var lith in _lithologies.Where(l => l.HoleId == _collar.HoleId))
                {
                    int y1 = topMargin + (int)(lith.FromDepth * _scaleY);
                    int y2 = topMargin + (int)(lith.ToDepth * _scaleY);
                    int h = Math.Max(1, y2 - y1);

                    Color c = ColorTranslator.FromHtml(lith.ColorHex);
                    using (var b = new SolidBrush(c))
                    {
                        g.FillRectangle(b, lithX, y1, lithW, h);
                    }
                    using (var p = new Pen(Color.FromArgb(100, 0, 0, 0), 1))
                    {
                        g.DrawRectangle(p, lithX, y1, lithW, h);
                    }

                    // Text labels if interval has enough height
                    if (h >= 14)
                    {
                        Color textC = (c.R * 0.299 + c.G * 0.587 + c.B * 0.114) > 150 ? Color.Black : Color.White;
                        using (var tb = new SolidBrush(textC))
                        {
                            string lbl = lith.RockCode + (h >= 24 ? " (" + Math.Round(lith.Length, 1) + "m)" : "");
                            g.DrawString(lbl, lithFont, tb, lithX + 6, y1 + 2);
                        }
                    }
                }
            }

            // 4. Draw Assay Bars Column
            double maxGrade = _element == "Cu" ? 3.0 : 10.0;
            if (_assays.Any())
            {
                double actualMax = _assays.Max(a => _element == "Cu" ? a.Cu_pct : a.Au_gpt);
                if (actualMax > maxGrade) maxGrade = actualMax;
            }

            // Draw grade grid lines (e.g. 2, 4, 6, 8 g/t)
            using (var penGrid = new Pen(Color.FromArgb(226, 232, 240), 1) { DashStyle = DashStyle.Dot })
            using (var fontGrid = new Font("Segoe UI", 7.0f))
            using (var brushGrid = new SolidBrush(Color.FromArgb(148, 163, 184)))
            {
                int ticks = 5;
                for (int t = 1; t <= ticks; t++)
                {
                    double gVal = (maxGrade / ticks) * t;
                    int gx = assayX + (int)((gVal / maxGrade) * (assayW - 40));
                    g.DrawLine(penGrid, gx, topMargin, gx, topMargin + logHeight);
                    g.DrawString(string.Format("{0:0.#}", gVal), fontGrid, brushGrid, gx - 8, topMargin - 12);
                }
            }

            using (var fontBar = new Font("Segoe UI", 7.0f))
            using (var penBarBorder = new Pen(Color.FromArgb(80, 0, 0, 0), 1))
            {
                foreach (var a in _assays.Where(x => x.HoleId == _collar.HoleId))
                {
                    int y1 = topMargin + (int)(a.FromDepth * _scaleY);
                    int y2 = topMargin + (int)(a.ToDepth * _scaleY);
                    int h = Math.Max(1, y2 - y1);

                    double grade = _element == "Cu" ? a.Cu_pct : a.Au_gpt;
                    int barWidth = (int)((grade / maxGrade) * (assayW - 40));
                    barWidth = Math.Max(2, Math.Min(assayW - 40, barWidth));

                    // Grade color ramp
                    Color barColor = GetGradeColor(grade, _element);

                    using (var b = new SolidBrush(barColor))
                    {
                        g.FillRectangle(b, assayX, y1, barWidth, h);
                    }
                    g.DrawRectangle(penBarBorder, assayX, y1, barWidth, h);

                    // Print grade text next to bar
                    if (h >= 10 && barWidth > 10)
                    {
                        using (var textB = new SolidBrush(Color.FromArgb(30, 41, 59)))
                        {
                            g.DrawString(string.Format("{0:0.##}", grade), fontBar, textB, assayX + barWidth + 3, y1 + (h / 2) - 5);
                        }
                    }
                }
            }

            // 5. Draw Interactive Hover Cursor & Tooltip
            if (_hoverDepth >= 0 && _hoverDepth <= eoh)
            {
                int hoverY = topMargin + (int)(_hoverDepth * _scaleY);
                using (var crosshairPen = new Pen(Color.FromArgb(220, 20, 60), 1.5f) { DashStyle = DashStyle.Dash })
                {
                    g.DrawLine(crosshairPen, rulerX, hoverY, assayX + assayW, hoverY);
                }

                // Tooltip bubble
                using (var tipFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                using (var tipBg = new SolidBrush(Color.FromArgb(230, 15, 23, 42)))
                using (var tipBorder = new Pen(Color.FromArgb(220, 20, 60), 1))
                using (var tipTextBrush = new SolidBrush(Color.White))
                {
                    SizeF sz = g.MeasureString(_hoverInfo, tipFont);
                    int tipX = rulerX + 80;
                    int tipY = Math.Max(topMargin, hoverY - (int)sz.Height - 8);
                    g.FillRectangle(tipBg, tipX, tipY, sz.Width + 12, sz.Height + 6);
                    g.DrawRectangle(tipBorder, tipX, tipY, sz.Width + 12, sz.Height + 6);
                    g.DrawString(_hoverInfo, tipFont, tipTextBrush, tipX + 6, tipY + 3);
                }
            }
        }

        private Color GetGradeColor(double grade, string elem)
        {
            if (elem == "Cu")
            {
                if (grade < 0.1) return Color.FromArgb(179, 229, 252);
                if (grade < 0.4) return Color.FromArgb(79, 195, 247);
                if (grade < 1.0) return Color.FromArgb(129, 199, 132);
                if (grade < 2.0) return Color.FromArgb(255, 183, 77);
                return Color.FromArgb(229, 57, 53); // High grade Cu
            }
            else // Au
            {
                if (grade < 0.2) return Color.FromArgb(224, 224, 224);
                if (grade < 0.8) return Color.FromArgb(174, 213, 129);
                if (grade < 2.0) return Color.FromArgb(255, 213, 79);
                if (grade < 5.0) return Color.FromArgb(255, 138, 101);
                return Color.FromArgb(211, 47, 47); // Bonanza Au
            }
        }
    }

    public class PlanAndSectionPanel : UserControl
    {
        private List<Collar> _collars = new List<Collar>();
        private List<LithologyRecord> _lithologies = new List<LithologyRecord>();
        private List<AssayRecord> _assays = new List<AssayRecord>();
        private Dictionary<string, List<DesurveyedPoint>> _trajectories = new Dictionary<string, List<DesurveyedPoint>>();

        public enum ViewMode { PlanMap, CrossSection }
        private ViewMode _mode = ViewMode.PlanMap;

        private Collar _selectedCollar = null;
        private double _sectionAzimuth = 90.0; // East-West section
        private double _corridorWidth = 100.0; // Corridor tolerance in meters

        public event Action<Collar> CollarSelected;

        public PlanAndSectionPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.MouseClick += PlanAndSectionPanel_MouseClick;
        }

        public void SetData(List<Collar> collars, List<Survey> surveys, List<LithologyRecord> liths, List<AssayRecord> assays)
        {
            _collars = collars ?? new List<Collar>();
            _lithologies = liths ?? new List<LithologyRecord>();
            _assays = assays ?? new List<AssayRecord>();

            // Calculate trajectories
            _trajectories.Clear();
            foreach (var c in _collars)
            {
                _trajectories[c.HoleId] = DesurveyingEngine.CalculateTrajectory(c, surveys);
            }

            if (_selectedCollar == null && _collars.Any())
            {
                _selectedCollar = _collars.First();
            }

            Invalidate();
        }

        public void SetViewMode(ViewMode mode)
        {
            _mode = mode;
            Invalidate();
        }

        public void SetSectionParameters(double azimuth, double corridor)
        {
            _sectionAzimuth = azimuth;
            _corridorWidth = corridor;
            Invalidate();
        }

        public void SelectCollar(string holeId)
        {
            _selectedCollar = _collars.FirstOrDefault(c => c.HoleId.Equals(holeId, StringComparison.OrdinalIgnoreCase));
            Invalidate();
        }

        private void PlanAndSectionPanel_MouseClick(object sender, MouseEventArgs e)
        {
            if (_mode == ViewMode.PlanMap && _collars.Any())
            {
                // Find clicked collar
                int margin = 60;
                int drawW = Width - 2 * margin;
                int drawH = Height - 2 * margin;

                double minX = _collars.Min(c => c.Easting);
                double maxX = _collars.Max(c => c.Easting);
                double minY = _collars.Min(c => c.Northing);
                double maxY = _collars.Max(c => c.Northing);

                double spanX = Math.Max(20.0, maxX - minX);
                double spanY = Math.Max(20.0, maxY - minY);

                foreach (var c in _collars)
                {
                    float px = margin + (float)((c.Easting - minX) / spanX * drawW);
                    float py = Height - margin - (float)((c.Northing - minY) / spanY * drawH);

                    double dist = Math.Sqrt(Math.Pow(e.X - px, 2) + Math.Pow(e.Y - py, 2));
                    if (dist < 14.0)
                    {
                        _selectedCollar = c;
                        CollarSelected?.Invoke(c);
                        Invalidate();
                        break;
                    }
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_collars == null || !_collars.Any())
            {
                using (var brush = new SolidBrush(Color.Gray))
                using (var font = new Font("Segoe UI", 11, FontStyle.Italic))
                {
                    g.DrawString("No drillhole data loaded. Load sample data or import CSV.", font, brush, 40, 40);
                }
                return;
            }

            if (_mode == ViewMode.PlanMap)
            {
                DrawPlanMap(g);
            }
            else
            {
                DrawCrossSection(g);
            }
        }

        private void DrawPlanMap(Graphics g)
        {
            int margin = 70;
            int drawW = Width - 2 * margin;
            int drawH = Height - 2 * margin;

            if (drawW <= 10 || drawH <= 10) return;

            double minX = _collars.Min(c => c.Easting) - 30;
            double maxX = _collars.Max(c => c.Easting) + 30;
            double minY = _collars.Min(c => c.Northing) - 30;
            double maxY = _collars.Max(c => c.Northing) + 30;

            double spanX = Math.Max(40.0, maxX - minX);
            double spanY = Math.Max(40.0, maxY - minY);

            // Draw coordinate grid & border
            using (var borderPen = new Pen(Color.FromArgb(148, 163, 184), 1.5f))
            using (var gridPen = new Pen(Color.FromArgb(226, 232, 240), 1) { DashStyle = DashStyle.Dash })
            using (var font = new Font("Segoe UI", 8.0f))
            using (var textBrush = new SolidBrush(Color.FromArgb(71, 85, 105)))
            {
                g.DrawRectangle(borderPen, margin, margin, drawW, drawH);

                // Grid intervals
                for (int i = 0; i <= 4; i++)
                {
                    float gx = margin + (drawW * i / 4.0f);
                    float gy = margin + (drawH * i / 4.0f);

                    double curEasting = minX + (spanX * i / 4.0);
                    double curNorthing = maxY - (spanY * i / 4.0);

                    g.DrawLine(gridPen, gx, margin, gx, margin + drawH);
                    g.DrawLine(gridPen, margin, gy, margin + drawW, gy);

                    // Labels
                    g.DrawString(string.Format("{0:0} E", curEasting), font, textBrush, gx - 20, margin + drawH + 8);
                    g.DrawString(string.Format("{0:0} N", curNorthing), font, textBrush, 4, gy - 7);
                }
            }

            // Draw North Arrow
            using (var naPen = new Pen(Color.FromArgb(30, 41, 59), 2))
            using (var naFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
            using (var naBrush = new SolidBrush(Color.FromArgb(220, 38, 38)))
            {
                int naX = Width - margin - 35;
                int naY = margin + 35;
                g.DrawLine(naPen, naX, naY, naX, naY - 24);
                PointF[] arrow = { new PointF(naX, naY - 30), new PointF(naX - 6, naY - 18), new PointF(naX + 6, naY - 18) };
                g.FillPolygon(naBrush, arrow);
                g.DrawString("N", naFont, naBrush, naX - 5, naY - 45);
            }

            // Draw Drillholes and their surface projections
            using (var tracePen = new Pen(Color.FromArgb(100, 116, 139), 1.5f))
            using (var fontLabel = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            {
                foreach (var c in _collars)
                {
                    float px = margin + (float)((c.Easting - minX) / spanX * drawW);
                    float py = Height - margin - (float)((c.Northing - minY) / spanY * drawH);

                    // Draw 2D trajectory projection trace
                    if (_trajectories.ContainsKey(c.HoleId))
                    {
                        var pts = _trajectories[c.HoleId];
                        if (pts.Count > 1)
                        {
                            var gPts = pts.Select(p => new PointF(
                                margin + (float)((p.Easting - minX) / spanX * drawW),
                                Height - margin - (float)((p.Northing - minY) / spanY * drawH)
                            )).ToArray();
                            g.DrawLines(tracePen, gPts);
                        }
                    }

                    // Collar symbol
                    bool isSelected = (_selectedCollar != null && _selectedCollar.HoleId == c.HoleId);
                    int r = isSelected ? 8 : 6;
                    Color col = isSelected ? Color.FromArgb(220, 38, 38) : Color.FromArgb(37, 99, 235);

                    using (var b = new SolidBrush(col))
                    {
                        g.FillEllipse(b, px - r, py - r, 2 * r, 2 * r);
                    }
                    using (var p = new Pen(Color.White, 1.5f))
                    {
                        g.DrawEllipse(p, px - r, py - r, 2 * r, 2 * r);
                    }

                    // Label
                    using (var tb = new SolidBrush(isSelected ? Color.FromArgb(185, 28, 28) : Color.FromArgb(15, 23, 42)))
                    {
                        g.DrawString(c.HoleId + string.Format(" ({0:0}m)", c.TotalDepth), fontLabel, tb, px + 8, py - 6);
                    }
                }
            }

            // Legend / Title
            using (var titleFont = new Font("Segoe UI", 10.0f, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString("PLAN MAP - Drillhole Collar Locations & Surface Traces", titleFont, titleBrush, margin, 15);
            }
        }

        private void DrawCrossSection(Graphics g)
        {
            int margin = 70;
            int drawW = Width - 2 * margin;
            int drawH = Height - 2 * margin;

            if (drawW <= 10 || drawH <= 10) return;

            // Project 3D points onto cross section plane along azimuth _sectionAzimuth
            double azRad = _sectionAzimuth * Math.PI / 180.0;
            double normX = Math.Cos(azRad);
            double normY = -Math.Sin(azRad);

            // Origin reference
            double refX = _collars.Average(c => c.Easting);
            double refY = _collars.Average(c => c.Northing);

            // Compute section distances (Distance along azimuth, Distance perpendicular to section)
            // U = dx * sin(az) + dy * cos(az)
            // V = -dx * cos(az) + dy * sin(az)
            var projectedCollars = new List<Tuple<Collar, double, double>>();
            foreach (var c in _collars)
            {
                double dx = c.Easting - refX;
                double dy = c.Northing - refY;
                double u = dx * Math.Sin(azRad) + dy * Math.Cos(azRad);
                double v = -dx * Math.Cos(azRad) + dy * Math.Sin(azRad);
                projectedCollars.Add(new Tuple<Collar, double, double>(c, u, v));
            }

            // Filter holes within corridor
            var visibleCollars = projectedCollars.Where(t => Math.Abs(t.Item3) <= _corridorWidth).Select(t => t.Item1).ToList();
            if (!visibleCollars.Any()) visibleCollars = _collars;

            double minU = -120.0;
            double maxU = 200.0;
            double minZ = 1000.0;
            double maxZ = 1300.0;

            if (visibleCollars.Any())
            {
                var allSectionPts = new List<Tuple<double, double>>();
                foreach (var c in visibleCollars)
                {
                    if (_trajectories.ContainsKey(c.HoleId))
                    {
                        foreach (var p in _trajectories[c.HoleId])
                        {
                            double dx = p.Easting - refX;
                            double dy = p.Northing - refY;
                            double u = dx * Math.Sin(azRad) + dy * Math.Cos(azRad);
                            allSectionPts.Add(new Tuple<double, double>(u, p.Elevation));
                        }
                    }
                }

                if (allSectionPts.Any())
                {
                    minU = allSectionPts.Min(p => p.Item1) - 40;
                    maxU = allSectionPts.Max(p => p.Item1) + 40;
                    minZ = allSectionPts.Min(p => p.Item2) - 40;
                    maxZ = allSectionPts.Max(p => p.Item2) + 40;
                }
            }

            double spanU = Math.Max(50.0, maxU - minU);
            double spanZ = Math.Max(50.0, maxZ - minZ);

            // Draw Grid & Elevation lines
            using (var borderPen = new Pen(Color.FromArgb(148, 163, 184), 1.5f))
            using (var gridPen = new Pen(Color.FromArgb(226, 232, 240), 1) { DashStyle = DashStyle.Dash })
            using (var font = new Font("Segoe UI", 8.0f))
            using (var textBrush = new SolidBrush(Color.FromArgb(71, 85, 105)))
            {
                g.DrawRectangle(borderPen, margin, margin, drawW, drawH);

                for (int i = 0; i <= 5; i++)
                {
                    float gx = margin + (drawW * i / 5.0f);
                    float gy = margin + (drawH * i / 5.0f);

                    double curU = minU + (spanU * i / 5.0);
                    double curZ = maxZ - (spanZ * i / 5.0);

                    g.DrawLine(gridPen, gx, margin, gx, margin + drawH);
                    g.DrawLine(gridPen, margin, gy, margin + drawW, gy);

                    g.DrawString(string.Format("{0:0}m", curU), font, textBrush, gx - 14, margin + drawH + 8);
                    g.DrawString(string.Format("{0:0}m RL", curZ), font, textBrush, 4, gy - 7);
                }
            }

            // Draw Section Title
            using (var titleFont = new Font("Segoe UI", 10.0f, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                string title = string.Format("CROSS-SECTION (Azimuth: {0:F0}° | Corridor: ±{1:F0}m)", _sectionAzimuth, _corridorWidth);
                g.DrawString(title, titleFont, titleBrush, margin, 15);
            }

            // Draw Drillholes in Section View with Lithology colors
            using (var labelFont = new Font("Segoe UI", 8.0f, FontStyle.Bold))
            {
                foreach (var c in visibleCollars)
                {
                    if (!_trajectories.ContainsKey(c.HoleId)) continue;
                    var pts = _trajectories[c.HoleId];
                    if (pts.Count < 2) continue;

                    // Draw trace segments
                    for (int i = 0; i < pts.Count - 1; i++)
                    {
                        var p1 = pts[i];
                        var p2 = pts[i + 1];

                        double u1 = (p1.Easting - refX) * Math.Sin(azRad) + (p1.Northing - refY) * Math.Cos(azRad);
                        double u2 = (p2.Easting - refX) * Math.Sin(azRad) + (p2.Northing - refY) * Math.Cos(azRad);

                        float x1 = margin + (float)((u1 - minU) / spanU * drawW);
                        float y1 = margin + (float)((maxZ - p1.Elevation) / spanZ * drawH);
                        float x2 = margin + (float)((u2 - minU) / spanU * drawW);
                        float y2 = margin + (float)((maxZ - p2.Elevation) / spanZ * drawH);

                        double midD = (p1.Depth + p2.Depth) / 2.0;
                        var lith = _lithologies.FirstOrDefault(l => l.HoleId == c.HoleId && midD >= l.FromDepth && midD <= l.ToDepth);
                        Color segColor = lith != null ? ColorTranslator.FromHtml(lith.ColorHex) : Color.DarkGray;

                        using (var segPen = new Pen(segColor, 4.0f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                        {
                            g.DrawLine(segPen, x1, y1, x2, y2);
                        }
                    }

                    // Collar point & label
                    var topP = pts.First();
                    double topU = (topP.Easting - refX) * Math.Sin(azRad) + (topP.Northing - refY) * Math.Cos(azRad);
                    float cx = margin + (float)((topU - minU) / spanU * drawW);
                    float cy = margin + (float)((maxZ - topP.Elevation) / spanZ * drawH);

                    using (var cb = new SolidBrush(Color.FromArgb(30, 41, 59)))
                    {
                        g.FillEllipse(cb, cx - 4, cy - 4, 8, 8);
                        g.DrawString(c.HoleId, labelFont, cb, cx - 12, cy - 16);
                    }
                }
            }
        }
    }

    public class HistogramChartPanel : UserControl
    {
        private List<HistogramBin> _bins = new List<HistogramBin>();
        private double _cutoff = 0.5;
        private string _element = "Au (g/t)";

        public HistogramChartPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.BackColor = Color.FromArgb(250, 252, 255);
        }

        public void SetData(List<HistogramBin> bins, double cutoff, string element = "Au (g/t)")
        {
            _bins = bins ?? new List<HistogramBin>();
            _cutoff = cutoff;
            _element = element;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_bins == null || !_bins.Any())
            {
                using (var brush = new SolidBrush(Color.Gray))
                using (var font = new Font("Segoe UI", 10, FontStyle.Italic))
                {
                    g.DrawString("No data available for histogram.", font, brush, 30, 30);
                }
                return;
            }

            int marginL = 55;
            int marginR = 50;
            int marginT = 40;
            int marginB = 55;

            int drawW = Width - marginL - marginR;
            int drawH = Height - marginT - marginB;
            if (drawW <= 10 || drawH <= 10) return;

            int maxFreq = Math.Max(1, _bins.Max(b => b.Frequency));
            int numBins = _bins.Count;
            float barW = (float)drawW / numBins;

            // Draw Axes & Background lines
            using (var axisPen = new Pen(Color.FromArgb(148, 163, 184), 1.5f))
            using (var gridPen = new Pen(Color.FromArgb(226, 232, 240), 1) { DashStyle = DashStyle.Dash })
            using (var font = new Font("Segoe UI", 8.0f))
            using (var textBrush = new SolidBrush(Color.FromArgb(71, 85, 105)))
            {
                g.DrawRectangle(axisPen, marginL, marginT, drawW, drawH);

                // Y-axis grid lines (Frequency)
                int yTicks = 5;
                for (int i = 0; i <= yTicks; i++)
                {
                    float y = marginT + drawH - (float)drawH * i / yTicks;
                    int fVal = (int)(maxFreq * (double)i / yTicks);
                    double cumVal = (100.0 * i) / yTicks;

                    g.DrawLine(gridPen, marginL, y, marginL + drawW, y);
                    g.DrawString(fVal.ToString(), font, textBrush, marginL - 30, y - 6);
                    g.DrawString(string.Format("{0:0}%", cumVal), font, textBrush, marginL + drawW + 6, y - 6);
                }
            }

            // Draw Bars
            using (var barBorderPen = new Pen(Color.FromArgb(30, 41, 59), 1))
            using (var fontBar = new Font("Segoe UI", 7.0f))
            using (var fontLabel = new Font("Segoe UI", 7.5f))
            using (var textBrush = new SolidBrush(Color.FromArgb(71, 85, 105)))
            {
                for (int i = 0; i < numBins; i++)
                {
                    var b = _bins[i];
                    float x = marginL + i * barW;
                    float h = (float)drawH * b.Frequency / maxFreq;
                    float y = marginT + drawH - h;

                    Color fillC = (b.MinGrade >= _cutoff) ? Color.FromArgb(249, 115, 22) : Color.FromArgb(59, 130, 246);
                    using (var sb = new SolidBrush(fillC))
                    {
                        g.FillRectangle(sb, x + 1, y, barW - 2, h);
                    }
                    g.DrawRectangle(barBorderPen, x + 1, y, barW - 2, h);

                    // X-axis bin labels (every alternate bin to avoid crowding)
                    if (i % 2 == 0 || i == numBins - 1)
                    {
                        g.DrawString(string.Format("{0:0.##}", b.MinGrade), fontLabel, textBrush, x - 4, marginT + drawH + 8);
                    }
                }
            }

            // Draw Cumulative % Curve
            using (var curvePen = new Pen(Color.FromArgb(220, 38, 38), 2.0f))
            {
                var curvePts = new List<PointF>();
                for (int i = 0; i < numBins; i++)
                {
                    float cx = marginL + (i + 0.5f) * barW;
                    float cy = marginT + drawH - (float)drawH * (float)(_bins[i].CumulativePct / 100.0);
                    curvePts.Add(new PointF(cx, cy));
                }
                if (curvePts.Count > 1)
                {
                    g.DrawLines(curvePen, curvePts.ToArray());
                }
            }

            // Title
            using (var titleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var tb = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString("Grade Frequency Distribution & Cumulative % Curve (" + _element + ")", titleFont, tb, marginL, 12);
            }
        }
    }

    public class RoseDiagramPanel : UserControl
    {
        private int[] _sectors = new int[24];
        private string _title = "Structural Strike / Trend Rose Diagram";

        public RoseDiagramPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.BackColor = Color.FromArgb(250, 252, 255);
        }

        public void SetData(List<double> orientations, string title = "Structural Rose Diagram (Strike / Trend)")
        {
            _sectors = StructuralEngine.BinRoseData(orientations, 24);
            _title = title;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int size = Math.Min(Width, Height) - 60;
            if (size <= 20) return;

            float cx = Width / 2.0f;
            float cy = Height / 2.0f + 10;
            float r = size / 2.0f;

            // Draw Title
            using (var titleFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
            using (var tb = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString(_title, titleFont, tb, 20, 10);
            }

            int maxCount = _sectors != null && _sectors.Any() ? Math.Max(1, _sectors.Max()) : 1;

            // Concentric circles (25%, 50%, 75%, 100%)
            using (var circlePen = new Pen(Color.FromArgb(203, 213, 225), 1) { DashStyle = DashStyle.Dash })
            using (var font = new Font("Segoe UI", 7.0f))
            using (var textBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
            {
                for (int i = 1; i <= 4; i++)
                {
                    float curR = r * i / 4.0f;
                    g.DrawEllipse(circlePen, cx - curR, cy - curR, 2 * curR, 2 * curR);
                    g.DrawString(string.Format("{0}%", i * 25), font, textBrush, cx + curR - 18, cy + 2);
                }
            }

            // Draw Sectors
            if (_sectors != null && _sectors.Length > 0)
            {
                int n = _sectors.Length;
                float sectorAngle = 360.0f / n;

                for (int i = 0; i < n; i++)
                {
                    int count = _sectors[i];
                    if (count <= 0) continue;

                    float sectorR = r * ((float)count / maxCount);
                    float startAngle = (i * sectorAngle) - 90.0f; // 0 deg is North (top)

                    Color wedgeColor = Color.FromArgb(180, 249, 115, 22);
                    using (var sb = new SolidBrush(wedgeColor))
                    using (var sp = new Pen(Color.FromArgb(194, 65, 12), 1))
                    {
                        g.FillPie(sb, cx - sectorR, cy - sectorR, 2 * sectorR, 2 * sectorR, startAngle, sectorAngle);
                        g.DrawPie(sp, cx - sectorR, cy - sectorR, 2 * sectorR, 2 * sectorR, startAngle, sectorAngle);
                    }
                }
            }

            // Cardinal Axes & Labels
            using (var axisPen = new Pen(Color.FromArgb(100, 116, 139), 1.5f))
            using (var cardFont = new Font("Segoe UI", 9.0f, FontStyle.Bold))
            using (var cardBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawLine(axisPen, cx, cy - r - 5, cx, cy + r + 5);
                g.DrawLine(axisPen, cx - r - 5, cy, cx + r + 5, cy);

                g.DrawString("N", cardFont, cardBrush, cx - 6, cy - r - 22);
                g.DrawString("S", cardFont, cardBrush, cx - 5, cy + r + 6);
                g.DrawString("E", cardFont, cardBrush, cx + r + 8, cy - 7);
                g.DrawString("W", cardFont, cardBrush, cx - r - 22, cy - 7);
            }
        }
    }
}
