using System;
using System.Windows.Forms;

namespace FPTester
{
    internal static class Program
    {
        /// <summary>
        /// [STAThread] is required — COM/USB device drivers expect STA.
        /// x86 build target is required — ftrScanAPI.dll is 32-bit.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Catch exceptions thrown from UI event handlers
            Application.ThreadException += (_, e) => ShowError(e.Exception);

            // Catch unobserved Task exceptions — in .NET 10 these crash the
            // process by default if not handled here.
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                e.SetObserved(); // prevent process crash
                ShowError(e.Exception.InnerException ?? e.Exception);
            };

            try
            {
                var server = new WebSocketServer();
                server.StartAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private static void ShowError(Exception ex)
        {
            if (ex is DllNotFoundException || ex is System.IO.FileNotFoundException)
            {
                MessageBox.Show(
                    "ftrScanAPI.dll could not be found.\n\n" +
                    "It should have been copied next to FPTester.exe automatically " +
                    "during the build (see FPTester.csproj <Content> block).\n\n" +
                    "If running the .exe directly without building, manually copy:\n" +
                    @"...\FS6x_Enrollment_Kit_2025.11.06\ftrScanAPI.dll" +
                    "\ninto the same folder as FPTester.exe.\n\n" +
                    $"Raw error: {ex.Message}",
                    "DLL Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (ex is BadImageFormatException)
            {
                MessageBox.Show(
                    "Bitness mismatch — ftrScanAPI.dll from the Enrollment Kit\n" +
                    "must be the 32-bit version, matching FPTester's x86 build target.\n\n" +
                    "Make sure you copied the DLL from the root of the Enrollment Kit\n" +
                    "folder (NOT from any x64 subfolder).\n\n" +
                    $"Raw error: {ex.Message}",
                    "Bitness Mismatch", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show(
                    $"Unexpected error:\n\n{ex.Message}\n\n{ex.StackTrace}",
                    "FPTester Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
