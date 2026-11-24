using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;

namespace GeoMineExplorer
{
    public class Collar
    {
        public string HoleId { get; set; }
        public double Easting { get; set; }   // X
        public double Northing { get; set; }  // Y
        public double Elevation { get; set; } // Z (RL)
        public double TotalDepth { get; set; }
        public double Dip { get; set; }       // Incline from horizontal (-90 to +90)
        public double Azimuth { get; set; }   // Bearing (0 to 360)
        public string Project { get; set; }

        public Collar() { }
        public Collar(string holeId, double easting, double northing, double elevation, double totalDepth, double dip, double azimuth, string project = "Golden Ridge")
        {
            HoleId = holeId;
            Easting = easting;
            Northing = northing;
            Elevation = elevation;
            TotalDepth = totalDepth;
            Dip = dip;
            Azimuth = azimuth;
            Project = project;
        }
    }

    public class Survey
    {
        public string HoleId { get; set; }
        public double Depth { get; set; }
        public double Dip { get; set; }
        public double Azimuth { get; set; }

        public Survey() { }
        public Survey(string holeId, double depth, double dip, double azimuth)
        {
            HoleId = holeId;
            Depth = depth;
            Dip = dip;
            Azimuth = azimuth;
        }
    }

    public class DesurveyedPoint
    {
        public string HoleId { get; set; }
        public double Depth { get; set; }
        public double Easting { get; set; }
        public double Northing { get; set; }
        public double Elevation { get; set; }
        public double TVD { get; set; } // True Vertical Depth

        public DesurveyedPoint(string holeId, double depth, double easting, double northing, double elevation, double tvd)
        {
            HoleId = holeId;
            Depth = depth;
            Easting = easting;
            Northing = northing;
            Elevation = elevation;
            TVD = tvd;
        }
    }

    public class AssayRecord
    {
        public string SampleId { get; set; }
        public string HoleId { get; set; }
        public double FromDepth { get; set; }
        public double ToDepth { get; set; }
        public double Length { get { return Math.Round(Math.Max(0, ToDepth - FromDepth), 2); } }
        public double Au_gpt { get; set; }     // Gold (g/t)
        public double Cu_pct { get; set; }     // Copper (%)
        public double Fe_pct { get; set; }     // Iron (%)
        public double RecoveryPct { get; set; } // Core Recovery %

        public AssayRecord() { }
        public AssayRecord(string sampleId, string holeId, double fromDepth, double toDepth, double au, double cu, double fe, double recovery = 95.0)
        {
            SampleId = sampleId;
            HoleId = holeId;
            FromDepth = fromDepth;
            ToDepth = toDepth;
            Au_gpt = au;
            Cu_pct = cu;
            Fe_pct = fe;
            RecoveryPct = recovery;
        }
    }

    public class LithologyRecord
    {
        public string HoleId { get; set; }
        public double FromDepth { get; set; }
        public double ToDepth { get; set; }
        public double Length { get { return Math.Round(Math.Max(0, ToDepth - FromDepth), 2); } }
        public string RockCode { get; set; }
        public string Description { get; set; }
        public string ColorHex { get; set; }

        public LithologyRecord() { }
        public LithologyRecord(string holeId, double fromDepth, double toDepth, string rockCode, string description, string colorHex = "")
        {
            HoleId = holeId;
            FromDepth = fromDepth;
            ToDepth = toDepth;
            RockCode = rockCode;
            Description = description;
            ColorHex = string.IsNullOrEmpty(colorHex) ? GetDefaultColor(rockCode) : colorHex;
        }

        public static string GetDefaultColor(string code)
        {
            switch (code?.ToUpper()?.Trim())
            {
                case "QVN": return "#FFF275"; // Quartz Vein (Golden Yellow)
                case "POR": return "#FF8A65"; // Porphyry (Salmon/Coral)
                case "BAS": return "#2E7D32"; // Basalt (Dark Green)
                case "AND": return "#689F38"; // Andesite (Olive Green)
                case "SHL": return "#424242"; // Shale (Dark Gray/Charcoal)
                case "SKN": return "#8D6E63"; // Skarn (Brownish)
                case "GRN": return "#BA68C8"; // Granite (Lavender/Pink)
                case "BRX": return "#FFB74D"; // Breccia (Warm Amber)
                case "SST": return "#FFE082"; // Sandstone (Light Tan)
                case "LIM": return "#90CAF9"; // Limestone (Sky Blue)
                case "OXD": return "#D32F2F"; // Oxide / Gossan (Rust Red)
                default: return "#BDBDBD";    // Undifferentiated (Gray)
            }
        }
    }

    public class CompositeRecord
    {
        public string HoleId { get; set; }
        public double FromDepth { get; set; }
        public double ToDepth { get; set; }
        public double Length { get { return Math.Round(Math.Max(0, ToDepth - FromDepth), 2); } }
        public double Au_gpt { get; set; }
        public double Cu_pct { get; set; }
        public int IntervalCount { get; set; }

        public CompositeRecord(string holeId, double fromDepth, double toDepth, double au, double cu, int count)
        {
            HoleId = holeId;
            FromDepth = fromDepth;
            ToDepth = toDepth;
            Au_gpt = Math.Round(au, 3);
            Cu_pct = Math.Round(cu, 3);
            IntervalCount = count;
        }
    }

    public class StructuralRecord
    {
        public string FeatureId { get; set; }
        public string HoleId { get; set; }
        public double Depth { get; set; }
        public string FeatureType { get; set; } // Bedding, Joint, Vein, Fault, Foliation
        public double Dip { get; set; }         // 0 - 90
        public double DipDirection { get; set; } // 0 - 360
        public double Strike
        {
            get
            {
                // Right hand rule strike = (DipDirection - 90 + 360) % 360
                return (DipDirection - 90.0 + 360.0) % 360.0;
            }
        }

        public StructuralRecord() { }
        public StructuralRecord(string id, string holeId, double depth, string type, double dip, double dipDir)
        {
            FeatureId = id;
            HoleId = holeId;
            Depth = depth;
            FeatureType = type;
            Dip = dip;
            DipDirection = dipDir;
        }
    }

    public static class SampleDataGenerator
    {
        public static List<Collar> GetSampleCollars()
        {
            return new List<Collar>
            {
                new Collar("DDH01", 521400.0, 7834200.0, 1250.0, 180.0, -60.0, 090.0, "Golden Ridge"),
                new Collar("DDH02", 521480.0, 7834205.0, 1242.0, 200.0, -65.0, 090.0, "Golden Ridge"),
                new Collar("DDH03", 521560.0, 7834210.0, 1235.0, 175.0, -60.0, 090.0, "Golden Ridge"),
                new Collar("DDH04", 521405.0, 7834300.0, 1255.0, 210.0, -70.0, 085.0, "Golden Ridge"),
                new Collar("DDH05", 521490.0, 7834305.0, 1246.0, 190.0, -60.0, 090.0, "Golden Ridge"),
                new Collar("DDH06", 521570.0, 7834310.0, 1238.0, 220.0, -65.0, 095.0, "Golden Ridge")
            };
        }

        public static List<Survey> GetSampleSurveys()
        {
            return new List<Survey>
            {
                new Survey("DDH01", 0.0, -60.0, 090.0),
                new Survey("DDH01", 60.0, -59.5, 090.5),
                new Survey("DDH01", 120.0, -58.8, 091.2),
                new Survey("DDH01", 180.0, -58.0, 092.0),

                new Survey("DDH02", 0.0, -65.0, 090.0),
                new Survey("DDH02", 100.0, -64.2, 091.0),
                new Survey("DDH02", 200.0, -63.5, 091.8),

                new Survey("DDH03", 0.0, -60.0, 090.0),
                new Survey("DDH03", 90.0, -59.0, 090.4),
                new Survey("DDH03", 175.0, -58.2, 091.0),

                new Survey("DDH04", 0.0, -70.0, 085.0),
                new Survey("DDH04", 100.0, -69.1, 085.8),
                new Survey("DDH04", 210.0, -68.0, 086.5),

                new Survey("DDH05", 0.0, -60.0, 090.0),
                new Survey("DDH05", 100.0, -59.2, 090.8),
                new Survey("DDH05", 190.0, -58.5, 091.5),

                new Survey("DDH06", 0.0, -65.0, 095.0),
                new Survey("DDH06", 110.0, -64.0, 095.8),
                new Survey("DDH06", 220.0, -63.0, 096.5)
            };
        }

        public static List<LithologyRecord> GetSampleLithologies()
        {
            var list = new List<LithologyRecord>();

            // DDH01
            list.Add(new LithologyRecord("DDH01", 0.0, 18.5, "OXD", "Ferruginous soil and oxidized capping gossan"));
            list.Add(new LithologyRecord("DDH01", 18.5, 54.0, "AND", "Porphyritic andesite with propylitic alteration"));
            list.Add(new LithologyRecord("DDH01", 54.0, 82.5, "POR", "Quartz-feldspar porphyry, quartz stockwork"));
            list.Add(new LithologyRecord("DDH01", 82.5, 102.0, "QVN", "Massive quartz vein with chalcopyrite and pyrite"));
            list.Add(new LithologyRecord("DDH01", 102.0, 145.0, "BRX", "Hydrothermal mineralized breccia, tourmaline matrix"));
            list.Add(new LithologyRecord("DDH01", 145.0, 180.0, "BAS", "Fine-grained volcanic basalt footwall"));

            // DDH02
            list.Add(new LithologyRecord("DDH02", 0.0, 15.0, "OXD", "Oxidized saprolite and weathered cap"));
            list.Add(new LithologyRecord("DDH02", 15.0, 68.0, "AND", "Unaltered to weakly sericitized andesite"));
            list.Add(new LithologyRecord("DDH02", 68.0, 95.0, "POR", "Quartz-feldspar porphyry dyke with disseminated pyrite"));
            list.Add(new LithologyRecord("DDH02", 95.0, 128.0, "QVN", "High-grade auriferous quartz-sulfide vein system"));
            list.Add(new LithologyRecord("DDH02", 128.0, 165.0, "SKN", "Garnet-pyroxene skarn with retrograde epidote"));
            list.Add(new LithologyRecord("DDH02", 165.0, 200.0, "BAS", "Amygdaloidal basalt host rock"));

            // DDH03
            list.Add(new LithologyRecord("DDH03", 0.0, 12.0, "OXD", "Limonitic clay and gravel overburden"));
            list.Add(new LithologyRecord("DDH03", 12.0, 58.0, "AND", "Andesite flow breccia"));
            list.Add(new LithologyRecord("DDH03", 58.0, 92.0, "POR", "Hydrothermally altered porphyry with chalcopyrite"));
            list.Add(new LithologyRecord("DDH03", 92.0, 114.0, "QVN", "Quartz-carbonate vein with visible gold specks"));
            list.Add(new LithologyRecord("DDH03", 114.0, 175.0, "BAS", "Basalt flows, chlorite-carbonate altered"));

            // DDH04
            list.Add(new LithologyRecord("DDH04", 0.0, 22.0, "OXD", "Ferruginous gossanous clay cap"));
            list.Add(new LithologyRecord("DDH04", 22.0, 75.0, "AND", "Porphyritic andesite host"));
            list.Add(new LithologyRecord("DDH04", 75.0, 110.0, "POR", "Potassic-altered quartz monzonite porphyry"));
            list.Add(new LithologyRecord("DDH04", 110.0, 140.0, "BRX", "Hydrothermal explosion breccia with matrix sulfides"));
            list.Add(new LithologyRecord("DDH04", 140.0, 178.0, "QVN", "Sheeted quartz vein swarm"));
            list.Add(new LithologyRecord("DDH04", 178.0, 210.0, "BAS", "Basalt basement unit"));

            // DDH05
            list.Add(new LithologyRecord("DDH05", 0.0, 16.0, "OXD", "Weathered saprolite"));
            list.Add(new LithologyRecord("DDH05", 16.0, 62.0, "AND", "Andesite flow unit"));
            list.Add(new LithologyRecord("DDH05", 62.0, 98.0, "POR", "Quartz-feldspar porphyry with chalcopyrite veinlets"));
            list.Add(new LithologyRecord("DDH05", 98.0, 125.0, "QVN", "High-grade Au quartz vein"));
            list.Add(new LithologyRecord("DDH05", 125.0, 158.0, "SKN", "Magnetite-actinolite skarn"));
            list.Add(new LithologyRecord("DDH05", 158.0, 190.0, "BAS", "Basalt basement"));

            // DDH06
            list.Add(new LithologyRecord("DDH06", 0.0, 20.0, "OXD", "Gravel cap and oxide zone"));
            list.Add(new LithologyRecord("DDH06", 20.0, 80.0, "AND", "Porphyritic andesite"));
            list.Add(new LithologyRecord("DDH06", 80.0, 118.0, "POR", "Silicified porphyry intruding volcanics"));
            list.Add(new LithologyRecord("DDH06", 118.0, 152.0, "BRX", "Sulfide-matrix breccia pipe"));
            list.Add(new LithologyRecord("DDH06", 152.0, 185.0, "QVN", "Quartz-veined contact zone"));
            list.Add(new LithologyRecord("DDH06", 185.0, 220.0, "BAS", "Basalt volcanic sequence"));

            return list;
        }

        public static List<AssayRecord> GetSampleAssays()
        {
            var list = new List<AssayRecord>();
            int sampleNum = 1001;

            var liths = GetSampleLithologies();
            var rand = new Random(42);

            foreach (var lith in liths)
            {
                double step = 2.0; // 2m sample intervals
                for (double d = lith.FromDepth; d < lith.ToDepth; d += step)
                {
                    double to = Math.Min(d + step, lith.ToDepth);
                    double au = 0.05;
                    double cu = 0.02;
                    double fe = 3.5;

                    // Base grades on rock type
                    if (lith.RockCode == "QVN")
                    {
                        au = 2.5 + rand.NextDouble() * 8.5; // High Au (2.5 - 11.0 g/t)
                        cu = 0.4 + rand.NextDouble() * 1.8; // Cu (0.4 - 2.2 %)
                        fe = 5.0 + rand.NextDouble() * 4.0;
                    }
                    else if (lith.RockCode == "BRX")
                    {
                        au = 1.2 + rand.NextDouble() * 3.8;
                        cu = 0.6 + rand.NextDouble() * 1.4;
                        fe = 6.5 + rand.NextDouble() * 5.0;
                    }
                    else if (lith.RockCode == "POR")
                    {
                        au = 0.4 + rand.NextDouble() * 1.2;
                        cu = 0.3 + rand.NextDouble() * 0.9;
                        fe = 4.2 + rand.NextDouble() * 2.5;
                    }
                    else if (lith.RockCode == "SKN")
                    {
                        au = 0.3 + rand.NextDouble() * 0.9;
                        cu = 0.5 + rand.NextDouble() * 2.1;
                        fe = 12.0 + rand.NextDouble() * 10.0; // High iron skarn
                    }
                    else if (lith.RockCode == "OXD")
                    {
                        au = 0.2 + rand.NextDouble() * 0.8;
                        cu = 0.08 + rand.NextDouble() * 0.15;
                        fe = 8.0 + rand.NextDouble() * 6.0;
                    }
                    else // AND, BAS
                    {
                        au = 0.02 + rand.NextDouble() * 0.15;
                        cu = 0.01 + rand.NextDouble() * 0.06;
                        fe = 3.0 + rand.NextDouble() * 2.0;
                    }

                    double recovery = 92.0 + rand.NextDouble() * 7.5;
                    list.Add(new AssayRecord("S" + sampleNum++, lith.HoleId, d, to, Math.Round(au, 2), Math.Round(cu, 2), Math.Round(fe, 1), Math.Round(recovery, 1)));
                }
            }

            return list;
        }

        public static List<StructuralRecord> GetSampleStructures()
        {
            return new List<StructuralRecord>
            {
                new StructuralRecord("ST01", "DDH01", 35.5, "Joint", 45.0, 110.0),
                new StructuralRecord("ST02", "DDH01", 72.0, "Vein", 65.0, 085.0),
                new StructuralRecord("ST03", "DDH01", 95.0, "Vein", 70.0, 090.0),
                new StructuralRecord("ST04", "DDH01", 115.0, "Fault", 78.0, 095.0),
                new StructuralRecord("ST05", "DDH02", 42.0, "Joint", 50.0, 105.0),
                new StructuralRecord("ST06", "DDH02", 102.0, "Vein", 68.0, 088.0),
                new StructuralRecord("ST07", "DDH02", 140.0, "Bedding", 35.0, 270.0),
                new StructuralRecord("ST08", "DDH03", 64.0, "Joint", 48.0, 115.0),
                new StructuralRecord("ST09", "DDH03", 98.0, "Vein", 72.0, 092.0),
                new StructuralRecord("ST10", "DDH04", 88.0, "Foliation", 60.0, 090.0),
                new StructuralRecord("ST11", "DDH04", 125.0, "Fault", 82.0, 100.0),
                new StructuralRecord("ST12", "DDH05", 105.0, "Vein", 70.0, 090.0),
                new StructuralRecord("ST13", "DDH06", 132.0, "Vein", 66.0, 086.0),
                new StructuralRecord("ST14", "DDH06", 160.0, "Joint", 52.0, 108.0)
            };
        }
    }
}
