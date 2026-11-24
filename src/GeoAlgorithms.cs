using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoMineExplorer
{
    public static class DesurveyingEngine
    {
        private const double DegToRad = Math.PI / 180.0;
        private const double RadToDeg = 180.0 / Math.PI;

        /// <summary>
        /// Calculates 3D desurveyed trace points along a drillhole using the Balanced Tangential / Minimum Curvature method.
        /// </summary>
        public static List<DesurveyedPoint> CalculateTrajectory(Collar collar, List<Survey> surveys)
        {
            var points = new List<DesurveyedPoint>();
            if (collar == null) return points;

            // Start point at collar (depth = 0)
            points.Add(new DesurveyedPoint(collar.HoleId, 0.0, collar.Easting, collar.Northing, collar.Elevation, 0.0));

            // Filter and sort surveys for this hole
            var holeSurveys = (surveys ?? new List<Survey>())
                .Where(s => s.HoleId.Equals(collar.HoleId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.Depth)
                .ToList();

            // If no survey at depth 0, add collar orientation
            if (!holeSurveys.Any(s => Math.Abs(s.Depth) < 0.01))
            {
                holeSurveys.Insert(0, new Survey(collar.HoleId, 0.0, collar.Dip, collar.Azimuth));
            }

            // Ensure EOH is surveyed
            if (holeSurveys.Last().Depth < collar.TotalDepth)
            {
                var last = holeSurveys.Last();
                holeSurveys.Add(new Survey(collar.HoleId, collar.TotalDepth, last.Dip, last.Azimuth));
            }

            // Interpolate points at 5-meter increments or at survey stations
            double currentX = collar.Easting;
            double currentY = collar.Northing;
            double currentZ = collar.Elevation;
            double currentTVD = 0.0;

            for (int i = 0; i < holeSurveys.Count - 1; i++)
            {
                var s1 = holeSurveys[i];
                var s2 = holeSurveys[i + 1];

                double d1 = s1.Depth;
                double d2 = s2.Depth;
                double dL = d2 - d1;
                if (dL <= 0) continue;

                // Geological conventions:
                // Dip is measured negative downwards from horizontal: -90 (vertical down), 0 (horizontal)
                // In drilling trigonometry, inclination from vertical (theta): theta = 90 - (-dip) = 90 + dip
                // Or inclination i1 = -dip1 * DegToRad, i2 = -dip2 * DegToRad (where positive is downwards from horizontal)
                double inc1 = -s1.Dip * DegToRad;
                double inc2 = -s2.Dip * DegToRad;
                double az1 = s1.Azimuth * DegToRad;
                double az2 = s2.Azimuth * DegToRad;

                // Balanced Tangential / Minimum Curvature calculation
                double cosDogLeg = Math.Cos(inc2 - inc1) - (Math.Sin(inc1) * Math.Sin(inc2) * (1.0 - Math.Cos(az2 - az1)));
                cosDogLeg = Math.Max(-1.0, Math.Min(1.0, cosDogLeg));
                double dogLeg = Math.Acos(cosDogLeg);

                double ratioFactor = 1.0;
                if (Math.Abs(dogLeg) > 1e-6)
                {
                    ratioFactor = (2.0 / dogLeg) * Math.Tan(dogLeg / 2.0);
                }

                // Sub-stepping for smooth 3D display (every 5m)
                int steps = Math.Max(1, (int)Math.Ceiling(dL / 5.0));
                double stepL = dL / steps;

                for (int step = 1; step <= steps; step++)
                {
                    double frac = (double)step / steps;
                    double subDepth = d1 + frac * dL;

                    // Interpolate inclination and azimuth
                    double curInc = inc1 + frac * (inc2 - inc1);
                    double curAz = az1 + frac * (az2 - az1);

                    // Direction cosines:
                    // dx = dL * cos(inclination from horizontal) * sin(azimuth)
                    // dy = dL * cos(inclination from horizontal) * cos(azimuth)
                    // dz = -dL * sin(inclination from horizontal) (downwards)
                    double dx = stepL * Math.Cos(curInc) * Math.Sin(curAz) * ratioFactor;
                    double dy = stepL * Math.Cos(curInc) * Math.Cos(curAz) * ratioFactor;
                    double dz = -stepL * Math.Sin(curInc) * ratioFactor;

                    currentX += dx;
                    currentY += dy;
                    currentZ += dz;
                    currentTVD += Math.Abs(dz);

                    points.Add(new DesurveyedPoint(collar.HoleId, Math.Round(subDepth, 2), Math.Round(currentX, 2), Math.Round(currentY, 2), Math.Round(currentZ, 2), Math.Round(currentTVD, 2)));
                }
            }

            return points;
        }
    }

    public static class CompositingEngine
    {
        /// <summary>
        /// Calculates length-weighted downhole composites.
        /// </summary>
        public static List<CompositeRecord> RunCompositing(List<AssayRecord> assays, double compositeLength = 2.0, double minLengthFraction = 0.5)
        {
            var results = new List<CompositeRecord>();
            if (assays == null || !assays.Any() || compositeLength <= 0) return results;

            var holeGroups = assays.GroupBy(a => a.HoleId);
            double minAcceptableLength = compositeLength * minLengthFraction;

            foreach (var group in holeGroups)
            {
                string holeId = group.Key;
                var sorted = group.OrderBy(a => a.FromDepth).ToList();
                if (!sorted.Any()) continue;

                double holeStart = sorted.First().FromDepth;
                double holeEnd = sorted.Max(a => a.ToDepth);

                for (double compFrom = holeStart; compFrom < holeEnd; compFrom += compositeLength)
                {
                    double compTo = compFrom + compositeLength;
                    if (compTo > holeEnd) compTo = holeEnd;

                    double totalLength = 0.0;
                    double weightedAu = 0.0;
                    double weightedCu = 0.0;
                    int count = 0;

                    foreach (var a in sorted)
                    {
                        // Check overlap
                        double overlapFrom = Math.Max(compFrom, a.FromDepth);
                        double overlapTo = Math.Min(compTo, a.ToDepth);
                        double overlap = overlapTo - overlapFrom;

                        if (overlap > 1e-4)
                        {
                            totalLength += overlap;
                            weightedAu += overlap * a.Au_gpt;
                            weightedCu += overlap * a.Cu_pct;
                            count++;
                        }
                    }

                    // Only keep composite if it meets minimum acceptable length
                    if (totalLength >= minAcceptableLength && totalLength > 0)
                    {
                        double finalAu = weightedAu / totalLength;
                        double finalCu = weightedCu / totalLength;
                        results.Add(new CompositeRecord(holeId, compFrom, compFrom + totalLength, finalAu, finalCu, count));
                    }
                }
            }

            return results;
        }
    }

    public class BasicStats
    {
        public int Count { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Mean { get; set; }
        public double Median { get; set; }
        public double Variance { get; set; }
        public double StdDev { get; set; }
        public double StdErr { get; set; }
        public double CV { get; set; } // Coefficient of Variation (SD / Mean)
        public double Skewness { get; set; }
        public double P10 { get; set; }
        public double P25 { get; set; }
        public double P75 { get; set; }
        public double P90 { get; set; }
        public double P95 { get; set; }
    }

    public class HistogramBin
    {
        public double MinGrade { get; set; }
        public double MaxGrade { get; set; }
        public int Frequency { get; set; }
        public double RelativeFrequencyPct { get; set; }
        public double CumulativePct { get; set; }
        public double MidPoint { get { return (MinGrade + MaxGrade) / 2.0; } }
    }

    public class CutoffEvaluation
    {
        public double Cutoff { get; set; }
        public int SamplesAbove { get; set; }
        public double SamplesAbovePct { get; set; }
        public double TotalLengthAbove { get; set; }
        public double TotalLengthAbovePct { get; set; }
        public double MeanGradeAbove { get; set; }
        public double GradeThickness { get; set; } // GT accumulation = sum(Length * Grade)
    }

    public static class StatisticsEngine
    {
        public static BasicStats CalculateStats(List<double> values)
        {
            var stats = new BasicStats();
            if (values == null || !values.Any()) return stats;

            var clean = values.Where(v => !double.IsNaN(v) && !double.IsInfinity(v)).OrderBy(v => v).ToList();
            int n = clean.Count;
            if (n == 0) return stats;

            stats.Count = n;
            stats.Min = clean[0];
            stats.Max = clean[n - 1];
            stats.Mean = clean.Average();

            // Median
            if (n % 2 == 1)
                stats.Median = clean[n / 2];
            else
                stats.Median = (clean[(n / 2) - 1] + clean[n / 2]) / 2.0;

            // Variance & StdDev
            double sumSqDiff = clean.Sum(v => Math.Pow(v - stats.Mean, 2));
            stats.Variance = n > 1 ? sumSqDiff / (n - 1) : 0.0;
            stats.StdDev = Math.Sqrt(stats.Variance);
            stats.StdErr = n > 0 ? stats.StdDev / Math.Sqrt(n) : 0.0;
            stats.CV = stats.Mean > 1e-6 ? stats.StdDev / stats.Mean : 0.0;

            // Skewness
            if (n > 2 && stats.StdDev > 1e-6)
            {
                double sumCubed = clean.Sum(v => Math.Pow((v - stats.Mean) / stats.StdDev, 3));
                stats.Skewness = (n / ((double)(n - 1) * (n - 2))) * sumCubed;
            }

            // Percentiles
            stats.P10 = GetPercentile(clean, 0.10);
            stats.P25 = GetPercentile(clean, 0.25);
            stats.P75 = GetPercentile(clean, 0.75);
            stats.P90 = GetPercentile(clean, 0.90);
            stats.P95 = GetPercentile(clean, 0.95);

            return stats;
        }

        private static double GetPercentile(List<double> sorted, double percentile)
        {
            if (!sorted.Any()) return 0.0;
            if (percentile <= 0.0) return sorted.First();
            if (percentile >= 1.0) return sorted.Last();

            double pos = (sorted.Count - 1) * percentile;
            int lower = (int)pos;
            int upper = Math.Min(lower + 1, sorted.Count - 1);
            double frac = pos - lower;
            return sorted[lower] + frac * (sorted[upper] - sorted[lower]);
        }

        public static List<HistogramBin> CalculateHistogram(List<double> values, int numBins = 15)
        {
            var bins = new List<HistogramBin>();
            if (values == null || !values.Any()) return bins;

            double min = values.Min();
            double max = values.Max();
            if (Math.Abs(max - min) < 1e-6) max = min + 1.0;

            double binWidth = (max - min) / numBins;
            for (int i = 0; i < numBins; i++)
            {
                bins.Add(new HistogramBin
                {
                    MinGrade = min + i * binWidth,
                    MaxGrade = min + (i + 1) * binWidth,
                    Frequency = 0
                });
            }

            foreach (var v in values)
            {
                int idx = (int)((v - min) / binWidth);
                if (idx < 0) idx = 0;
                if (idx >= numBins) idx = numBins - 1;
                bins[idx].Frequency++;
            }

            int cumulative = 0;
            int total = values.Count;
            foreach (var b in bins)
            {
                cumulative += b.Frequency;
                b.RelativeFrequencyPct = total > 0 ? (b.Frequency * 100.0 / total) : 0.0;
                b.CumulativePct = total > 0 ? (cumulative * 100.0 / total) : 0.0;
            }

            return bins;
        }

        public static CutoffEvaluation EvaluateCutoff(List<AssayRecord> assays, double cutoff, string element = "Au")
        {
            var eval = new CutoffEvaluation { Cutoff = cutoff };
            if (assays == null || !assays.Any()) return eval;

            double totalHoleLength = assays.Sum(a => a.Length);
            int totalCount = assays.Count;

            var filtered = assays.Where(a =>
            {
                double g = element == "Cu" ? a.Cu_pct : a.Au_gpt;
                return g >= cutoff;
            }).ToList();

            eval.SamplesAbove = filtered.Count;
            eval.SamplesAbovePct = totalCount > 0 ? (filtered.Count * 100.0 / totalCount) : 0.0;
            eval.TotalLengthAbove = filtered.Sum(a => a.Length);
            eval.TotalLengthAbovePct = totalHoleLength > 0 ? (eval.TotalLengthAbove * 100.0 / totalHoleLength) : 0.0;

            if (eval.TotalLengthAbove > 0)
            {
                double weightedSum = filtered.Sum(a =>
                {
                    double g = element == "Cu" ? a.Cu_pct : a.Au_gpt;
                    return a.Length * g;
                });
                eval.MeanGradeAbove = weightedSum / eval.TotalLengthAbove;
                eval.GradeThickness = weightedSum;
            }

            return eval;
        }

        public static List<CutoffEvaluation> GenerateGradeTonnageCurve(List<AssayRecord> assays, string element = "Au", int steps = 20)
        {
            var curve = new List<CutoffEvaluation>();
            if (assays == null || !assays.Any()) return curve;

            double maxGrade = assays.Max(a => element == "Cu" ? a.Cu_pct : a.Au_gpt);
            double step = maxGrade / steps;
            if (step < 0.01) step = 0.01;

            for (double c = 0; c <= maxGrade; c += step)
            {
                curve.Add(EvaluateCutoff(assays, Math.Round(c, 2), element));
            }

            return curve;
        }
    }

    public static class StructuralEngine
    {
        private const double DegToRad = Math.PI / 180.0;
        private const double RadToDeg = 180.0 / Math.PI;

        /// <summary>
        /// Converts Strike (Right Hand Rule) to Dip Direction.
        /// </summary>
        public static double StrikeToDipDirection(double strike)
        {
            return (strike + 90.0) % 360.0;
        }

        /// <summary>
        /// Converts Dip Direction to Strike (Right Hand Rule).
        /// </summary>
        public static double DipDirectionToStrike(double dipDirection)
        {
            return (dipDirection - 90.0 + 360.0) % 360.0;
        }

        /// <summary>
        /// Calculates Apparent Dip on a cross-section line.
        /// tan(apparent_dip) = tan(true_dip) * |cos(strike - section_bearing)|
        /// </summary>
        public static double CalculateApparentDip(double trueDip, double strike, double sectionBearing)
        {
            double deltaAngle = (strike - sectionBearing) * DegToRad;
            double trueDipRad = trueDip * DegToRad;

            double tanApp = Math.Tan(trueDipRad) * Math.Abs(Math.Cos(deltaAngle));
            return Math.Atan(tanApp) * RadToDeg;
        }

        /// <summary>
        /// Calculates True Stratigraphic Thickness from drillhole intersection.
        /// True Thickness = Intersected Length * |sin(angle between hole vector and plane)|
        /// </summary>
        public static double CalculateTrueThickness(double apparentThickness, double holeDip, double holeAzimuth, double planeDip, double planeStrike)
        {
            double hdRad = -holeDip * DegToRad; // downward inclination
            double haRad = holeAzimuth * DegToRad;
            double pdRad = planeDip * DegToRad;
            double psRad = planeStrike * DegToRad;

            // Unit vector along drillhole
            double hx = Math.Cos(hdRad) * Math.Sin(haRad);
            double hy = Math.Cos(hdRad) * Math.Cos(haRad);
            double hz = -Math.Sin(hdRad);

            // Pole (normal vector) to plane:
            // Dip direction is (Strike + 90)
            double dipDirRad = (planeStrike + 90.0) * DegToRad;
            double nx = Math.Sin(pdRad) * Math.Sin(dipDirRad);
            double ny = Math.Sin(pdRad) * Math.Cos(dipDirRad);
            double nz = Math.Cos(pdRad);

            // Dot product gives cos(theta between hole and normal) = sin(angle with plane)
            double dot = Math.Abs(hx * nx + hy * ny + hz * nz);
            return apparentThickness * dot;
        }

        /// <summary>
        /// Bins planar structural strikes or dip directions into polar rose sectors.
        /// </summary>
        public static int[] BinRoseData(List<double> azimuths, int numSectors = 24)
        {
            var bins = new int[numSectors];
            double sectorSize = 360.0 / numSectors;

            foreach (var az in azimuths)
            {
                double norm = (az % 360.0 + 360.0) % 360.0;
                int idx = (int)(norm / sectorSize);
                if (idx >= 0 && idx < numSectors)
                {
                    bins[idx]++;
                }
            }

            return bins;
        }
    }

    public class MiningEstimateResult
    {
        public double InSituVolume_m3 { get; set; }
        public double InSituTonnage_t { get; set; }
        public double MinedTonnage_t { get; set; }
        public double DilutedGrade { get; set; }
        public double RecoveredTonnage_t { get; set; }
        public double ContainedMetalUnits { get; set; }
        public string MetalUnitName { get; set; }
        public double GrossMetalValueUSD { get; set; }
    }

    public static class MiningEstimator
    {
        public static MiningEstimateResult CalculateTonnageAndGrade(
            double area_m2,
            double thickness_m,
            double bulkDensity_t_m3,
            double grade,
            double dilutionPct,
            double miningRecoveryPct,
            string commodity = "Au (Gold)",
            double metalPriceUSD = 2400.0) // Gold in $/oz, Cu in $/tonne
        {
            var res = new MiningEstimateResult();

            // 1. In-situ Volume and Tonnage
            res.InSituVolume_m3 = area_m2 * thickness_m;
            res.InSituTonnage_t = res.InSituVolume_m3 * bulkDensity_t_m3;

            // 2. Mining Dilution
            // Diluted Tonnage = In-situ Tonnage * (1 + Dilution%)
            // Diluted Grade = In-situ Grade / (1 + Dilution%)
            double dilutionFactor = 1.0 + (dilutionPct / 100.0);
            res.MinedTonnage_t = res.InSituTonnage_t * dilutionFactor;
            res.DilutedGrade = dilutionFactor > 0 ? grade / dilutionFactor : grade;

            // 3. Mining Recovery / Mill Feed
            res.RecoveredTonnage_t = res.MinedTonnage_t * (miningRecoveryPct / 100.0);

            // 4. Contained Metal & Value
            if (commodity.Contains("Au") || commodity.Contains("Gold"))
            {
                // Grade is g/t. Contained grams = Recovered tonnes * Grade.
                // 1 Troy Ounce = 31.1034768 grams
                double grams = res.RecoveredTonnage_t * res.DilutedGrade;
                res.ContainedMetalUnits = grams / 31.1034768;
                res.MetalUnitName = "Troy Ounces Au";
                res.GrossMetalValueUSD = res.ContainedMetalUnits * metalPriceUSD;
            }
            else // Cu (Copper) or Base Metal (%)
            {
                // Grade is %. Contained tonnes of metal = Recovered tonnes * (Grade / 100)
                res.ContainedMetalUnits = res.RecoveredTonnage_t * (res.DilutedGrade / 100.0);
                res.MetalUnitName = "Tonnes Cu Metal";
                res.GrossMetalValueUSD = res.ContainedMetalUnits * metalPriceUSD;
            }

            return res;
        }
    }
}
