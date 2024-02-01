using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WinForms;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        using var host = Program.CreateHostBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<WebApiStartup>())
            .Build();
        using var serviceScope = host.Services.CreateScope();
        var services = serviceScope.ServiceProvider;

        // Run Self-Hosted Web API.
        host.RunAsync();

        var mainForm = services.GetRequiredService<Form1>();
        Application.Run(mainForm);
        if (!mainForm.IsDisposed)
        {
            mainForm.Dispose();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
                // TODO: add services.
                services
                    .AddTransient<Form1>());
}