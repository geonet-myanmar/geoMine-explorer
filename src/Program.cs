using System;
using System.Windows.Forms;

namespace GeoMineExplorer
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.ThreadException += (s, e) =>
            {
                MessageBox.Show("An unexpected error occurred:\n" + e.Exception.Message + "\n\nStack trace:\n" + e.Exception.StackTrace,
                    "GeoMine Explorer Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                string msg = ex != null ? ex.Message : "Unknown fatal error";
                MessageBox.Show("Fatal Application Error:\n" + msg, "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            Application.Run(new MainForm());
        }
    }
}
