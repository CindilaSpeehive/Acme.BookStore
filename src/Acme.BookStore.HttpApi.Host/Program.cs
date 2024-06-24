using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using DevExpress.AspNetCore;
using DevExpress.AspNetCore.Reporting;

namespace Acme.BookStore;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Async(c => c.File("Logs/logs.txt"))
            .WriteTo.Async(c => c.Console())
            .CreateLogger();

        try
        {
            Log.Information("Starting Acme.BookStore.HttpApi.Host.");
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.AddAppSettingsSecretsJson()
                .UseAutofac()
                .UseSerilog();


            

            builder.Services.AddDevExpressControls();
            builder.Services.AddMvc();
            builder.Services.ConfigureReportingServices(configurator => {
                if (builder.Environment.IsDevelopment())
                {
                    configurator.UseDevelopmentMode();
                }
                configurator.ConfigureReportDesigner(designerConfigurator => {
                });
                configurator.ConfigureWebDocumentViewer(viewerConfigurator => {
                    // Use cache for document generation and export.
                    // This setting is necessary in asynchronous mode and when a report has interactive or drill down features.
                    viewerConfigurator.UseCachedReportSourceBuilder();
                });
            });
            await builder.AddApplicationAsync<BookStoreHttpApiHostModule>();
            var app = builder.Build();

            app.UseDevExpressControls();

            await app.InitializeApplicationAsync();
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            if (ex is HostAbortedException)
            {
                throw;
            }

            Log.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
