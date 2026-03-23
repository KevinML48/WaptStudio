using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using WaptStudio.App.Bootstrap;
using WaptStudio.App.Forms;
using WaptStudio.Core.Configuration;

namespace WaptStudio.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var runtime = new AppRuntime();
        ConfigureGlobalExceptionHandling(runtime);

        try
        {
            AppPaths.EnsureCreated();
            runtime.InitializeAsync().GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            ShowStartupFailure(exception);
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.Run(new MainForm(runtime));
    }

    private static void ConfigureGlobalExceptionHandling(AppRuntime runtime)
    {
        Application.ThreadException += async (_, eventArgs) => await HandleExceptionAsync(runtime, eventArgs.Exception).ConfigureAwait(false);
        AppDomain.CurrentDomain.UnhandledException += async (_, eventArgs) =>
        {
            if (eventArgs.ExceptionObject is Exception exception)
            {
                await HandleExceptionAsync(runtime, exception).ConfigureAwait(false);
            }
        };

        TaskScheduler.UnobservedTaskException += async (_, eventArgs) =>
        {
            eventArgs.SetObserved();
            await HandleExceptionAsync(runtime, eventArgs.Exception).ConfigureAwait(false);
        };
    }

    private static async Task HandleExceptionAsync(AppRuntime runtime, Exception exception)
    {
        await runtime.LogService.LogErrorAsync("Exception non geree.", exception).ConfigureAwait(false);

        if (Application.MessageLoop)
        {
            MessageBox.Show(
                text: exception.Message,
                caption: "Erreur WaptStudio",
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
        }
    }

    private static void ShowStartupFailure(Exception exception)
    {
        var message = string.Join(
            Environment.NewLine,
            "Le demarrage de WaptStudio a echoue.",
            $"Base locale: {AppPaths.BaseDirectory}",
            $"Base SQLite attendue: {AppPaths.HistoryDatabasePath}",
            $"Erreur: {exception.Message}",
            string.Empty,
            "Verifiez les droits d'ecriture dans %LOCALAPPDATA% et la presence du runtime .NET/SQLite.");

        try
        {
            Directory.CreateDirectory(AppPaths.BaseDirectory);
            File.WriteAllText(Path.Combine(AppPaths.BaseDirectory, "startup-error.log"), $"[{DateTimeOffset.Now:O}] {exception}{Environment.NewLine}");
        }
        catch
        {
        }

        MessageBox.Show(message, "Erreur de demarrage WaptStudio", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
