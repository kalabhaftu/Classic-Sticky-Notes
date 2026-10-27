using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace ClassicStickyNotes.Services
{
    public static class Logger
    {
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClassicStickyNotes");

        private static readonly string LogFilePath = Path.Combine(LogDirectory, "app.log");
        private static readonly object LogLock = new object();

        static Logger()
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }
            }
            catch { }
        }

        public static string GetLogPath() => LogFilePath;

        public static void InitializeGlobalExceptionHandling()
        {
            // 1. UI Thread unhandled exceptions
            Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;

            // 2. Non-UI / background threads
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;

            // 3. Unobserved task exceptions
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            LogInfo("=== Application Started ===");
        }

        public static void LogInfo(string message)
        {
            WriteToFile("INFO", message);
        }

        public static void LogWarning(string message)
        {
            WriteToFile("WARN", message);
        }

        public static void LogError(string context, Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Context: {context}");
            sb.AppendLine($"Exception: {ex.GetType().FullName}: {ex.Message}");
            sb.AppendLine($"StackTrace:\n{ex.StackTrace}");

            var inner = ex.InnerException;
            while (inner != null)
            {
                sb.AppendLine($"Inner Exception: {inner.GetType().FullName}: {inner.Message}");
                sb.AppendLine($"Inner StackTrace:\n{inner.StackTrace}");
                inner = inner.InnerException;
            }

            WriteToFile("ERROR", sb.ToString().TrimEnd());
        }

        private static void WriteToFile(string level, string text)
        {
            lock (LogLock)
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string entry = $"[{timestamp}] [{level}] {text}{Environment.NewLine}";
                    File.AppendAllText(LogFilePath, entry, Encoding.UTF8);
                }
                catch { }
            }
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogError("UI Dispatcher Unhandled Exception", e.Exception);

            // Prevent crash if possible
            e.Handled = true;

            ShowCrashDialog("Application Error", e.Exception.Message, e.Exception.ToString());
        }

        private static void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogError($"AppDomain Fatal Exception (IsTerminating: {e.IsTerminating})", ex);
                ShowCrashDialog("Fatal Error", ex.Message, ex.ToString());
            }
            else
            {
                WriteToFile("FATAL", $"Non-exception object: {e.ExceptionObject}");
            }
        }

        private static void OnUnobservedTaskException(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
        {
            LogError("Unobserved Task Exception", e.Exception);
            e.SetObserved();
        }

        private static void ShowCrashDialog(string title, string summary, string fullDetails)
        {
            try
            {
                // Run on dispatcher if available
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    var msg = $"{summary}\n\nA crash log was saved to:\n{LogFilePath}\n\nWould you like to copy the error details to your clipboard?";
                    var result = MessageBox.Show(msg, title, MessageBoxButton.YesNo, MessageBoxImage.Error);
                    if (result == MessageBoxResult.Yes)
                    {
                        Clipboard.SetText(fullDetails);
                    }
                });
            }
            catch { }
        }
    }
}
