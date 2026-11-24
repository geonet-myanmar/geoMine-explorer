# GeoMine Explorer v1.0

A lightweight, standalone Windows desktop application for **Exploration and Mining Geology**, developed by **GeoNet Myanmar**.

[![Release](https://img.shields.io/github/v/release/geonet-myanmar/geoMine-explorer?color=blue&label=version)](https://github.com/geonet-myanmar/geoMine-explorer/releases)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%2F%2011-lightgrey)](https://github.com/geonet-myanmar/geoMine-explorer)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

---

## 📌 Overview

**GeoMine Explorer** provides field exploration geologists, mine geologists, and resource engineers with an all-in-one, zero-dependency desktop utility to inspect drillhole data, generate graphic strip logs, project cross-sections, compute assay statistics, run downhole length-weighted compositing, solve structural geometry problems, and estimate quick orebody tonnages and grades.

- **Standalone Binary**: `GeoMineExplorer.exe` (~115 KB)
- **Zero Runtime Dependencies**: Runs natively on Windows 10 and Windows 11 without requiring Python, Java, or developer runtimes.
- **Instant Testing**: Preloaded with the *Golden Ridge Au-Cu Exploration Project* dataset (6 drillholes, downhole surveys, lithology logs, 597 assay intervals, and structural orientations).

---

## 🚀 Quick Download & Run

1. Go to the [Releases](https://github.com/geonet-myanmar/geoMine-explorer/releases/tag/v1.0.0) page.
2. Download `GeoMineExplorer-v1.0.0-windows-x64.zip` (or direct `GeoMineExplorer.exe`).
3. Extract and double-click `GeoMineExplorer.exe` to run immediately.

---

## 🛠️ Key Modules & Geological Tools

### 1. Exploration Project Data Explorer
- Multi-table viewer and editor for **Collars**, **Surveys**, **Assays**, **Lithologies**, and **Structural Orientations**.
- Full CSV import and export capabilities.

### 2. Drillhole Graphic Strip Log Viewer
- Vertical depth ruler with tick marks and depth markers.
- Geological lithology column with standard color-coding and rock descriptions (e.g. QVN, POR, BRX, SKN, AND, BAS, OXD).
- Downhole assay grade histogram column with color gradients.
- **Dynamic Mouse Crosshair**: Real-time tooltip tracking depth, rock type, and assay grades under the cursor.
- Vertical zoom scale adjustment (1.0x to 8.0x) and one-click PNG export.

### 3. Plan Map & Cross-Section Visualizer
- **Plan Map View**: Coordinate grid (Easting vs Northing), collar positions, hole labels, and surface trajectory traces. Interactive collar selection synced across workspaces.
- **Cross-Section View**: 3D desurveyed drill traces using the **Minimum Curvature & Balanced Tangential algorithm**. Interactive section azimuth slider (0° to 360°) and corridor slicing tolerance ($\pm\text{m}$).

### 4. Assay Statistics & Cut-off Grade Analysis
- Comprehensive statistics: Count, Min, Max, Mean, Median, Variance, Standard Deviation, Standard Error, Coefficient of Variation (CV), Skewness, and Percentiles ($P_{10}$ to $P_{95}$).
- Interactive cut-off grade slider with instant recalculation of mineralized thickness, mean grade above cut-off, and accumulated Grade-Thickness ($GT$).
- Grade frequency histogram with superimposed cumulative % curve.

### 5. Downhole Length-Weighted Compositing
- Configurable composite interval lengths (e.g. 1m, 2m, 5m, 10m).
- Length-weighted grade accumulation formula:
  $$\text{Composite Grade} = \frac{\sum (L_i \times G_i)}{\sum L_i}$$
- Summary verification ensuring grade conservation between raw assays and composites.
- Direct CSV export of composite tables.

### 6. Structural Geology & Section Calculator
- **Strike/Dip to Dip Direction Converter**: Automatic conversion following Right-Hand Rule (RHR).
- **Apparent Dip on Cross-Sections**:
  $$\tan(\alpha_{\text{app}}) = \tan(\alpha_{\text{true}}) \times |\cos(\theta_{\text{strike}} - \theta_{\text{section}})|$$
- **True Stratigraphic Thickness Calculator**: True thickness derived from core intercept length and orientation vectors.
- **Structural Rose Diagram**: 360° polar compass rose with frequency percentage circles.

### 7. Mine Resource & Tonnage Estimator
- Quick orebody tonnage and contained metal estimation based on deposit footprint area ($m^2$), average true thickness ($m$), bulk density ($t/m^3$), in-situ grade, mining dilution %, and extraction recovery %.
- Outputs in-situ tonnage, mined mill feed, diluted grade, contained metal (Gold troy oz or Copper tonnes), and gross metal value ($ USD).
- Automated $\pm 10\%, \pm 20\%$ sensitivity matrix.

---

## 📂 Sample Datasets

Sample exploration CSV files are provided in the [`sample_data/`](sample_data/) folder:
- `collars.csv`: Drillhole collar coordinates and depths
- `surveys.csv`: Downhole trajectory surveys
- `assays.csv`: 597 exploration assay records (Au, Cu, Fe, Recovery)
- `lithology.csv`: Lithological logging intervals
- `structures.csv`: Structural orientation measurements

---

## 🔨 Building from Source

To compile the standalone executable locally:

```cmd
build.bat
```

Or using the standard C# compiler directly:
```cmd
csc /target:winexe /optimize+ /platform:anycpu /out:GeoMineExplorer.exe /r:System.dll,System.Core.dll,System.Data.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Xml.dll src\Models.cs src\GeoAlgorithms.cs src\CanvasViews.cs src\MainForm.cs src\Program.cs
```

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
Copyright (c) 2026 GeoNet Myanmar.
