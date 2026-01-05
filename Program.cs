using System;
using System.Windows.Forms;
using Linage.GUI;

namespace Linage
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainWindow());
            }
            catch (Exception ex)
            {
                // Unwrap InnerException if this is a TargetInvocationException
                var realException = ex;
                if (ex.InnerException != null)
                {
                    realException = ex.InnerException;
                }

                MessageBox.Show(
                    $"Application failed to start.\n\n" +
                    $"Error: {realException.Message}\n\n" +
                    $"Details: {realException.GetType().Name}\n\n" +
                    $"Please ensure:\n" +
                    $"1. SQL Server is running\n" +
                    $"2. Connection string is configured in App.config\n" +
                    $"3. Database can be accessed\n\n" +
                    $"Stack Trace: {realException.StackTrace}",
                    "Fatal Startup Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                Environment.Exit(1);
            }
        }
    }
}
