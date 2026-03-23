using System;
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
        AppPaths.EnsureCreated();

        var runtime = new AppRuntime();
        ConfigureGlobalExceptionHandling(runtime);

        runtime.InitializeAsync().GetAwaiter().GetResult();

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
}
