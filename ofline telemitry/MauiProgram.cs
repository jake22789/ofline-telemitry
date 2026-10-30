using Azure.Monitor.OpenTelemetry.Exporter;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Syncfusion.Maui.Toolkit.Hosting;
using System.Diagnostics;

namespace ofline_telemitry
{
    public static class MauiProgram
    {
        public static readonly ActivitySource ActivitySource = new("OflineTelemitry");

        // Replace with your Application Insights connection string
        private const string AppInsightsConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://westus3-1.in.applicationinsights.azure.com/";

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureSyncfusionToolkit()
                .ConfigureMauiHandlers(handlers =>
                {
#if WINDOWS
    				Microsoft.Maui.Controls.Handlers.Items.CollectionViewHandler.Mapper.AppendToMapping("KeyboardAccessibleCollectionView", (handler, view) =>
    				{
    					handler.PlatformView.SingleSelectionFollowsFocus = false;
    				});

    				Microsoft.Maui.Handlers.ContentViewHandler.Mapper.AppendToMapping(nameof(Pages.Controls.CategoryChart), (handler, view) =>
    				{
    					if (view is Pages.Controls.CategoryChart && handler.PlatformView is Microsoft.Maui.Platform.ContentPanel contentPanel)
    					{
    						contentPanel.IsTabStop = true;
    					}
    				});
#endif
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                    fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
                });

#if DEBUG
    		builder.Logging.AddDebug();
    		builder.Services.AddLogging(configure => configure.AddDebug());
#endif

            builder.Services.AddSingleton<ProjectRepository>();
            builder.Services.AddSingleton<TaskRepository>();
            builder.Services.AddSingleton<CategoryRepository>();
            builder.Services.AddSingleton<TagRepository>();
            builder.Services.AddSingleton<SeedDataService>();
            builder.Services.AddSingleton<ModalErrorHandler>();
            builder.Services.AddSingleton<MainPageModel>();
            builder.Services.AddSingleton<ProjectListPageModel>();
            builder.Services.AddSingleton<ManageMetaPageModel>();

            builder.Services.AddTransientWithShellRoute<ProjectDetailPage, ProjectDetailPageModel>("project");
            builder.Services.AddTransientWithShellRoute<TaskDetailPage, TaskDetailPageModel>("task");

            // 1. Save failed uploads to disk. The exporter's default folder isn't writable on phones.
            var telemetryDirectory = Path.Combine(FileSystem.Current.AppDataDirectory, "telemetry");
            Directory.CreateDirectory(telemetryDirectory);

            void ConfigureAzure(AzureMonitorExporterOptions o)
            {
                o.ConnectionString = AppInsightsConnectionString;
                // Own subfolder so Azure and OTLP don't share one storage size limit
                o.StorageDirectory = Path.Combine(telemetryDirectory, "azure");
            }

            // Register the Azure exporter per signal. UseAzureMonitorExporter() never attached it to the
            // trace pipeline here, so nothing was sent or saved to disk.
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.AddAzureMonitorLogExporter(ConfigureAzure);
            });
            var otel = builder.Services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService("OflineTelemitry"))
                .WithTracing(tracing => tracing
                    .AddSource(ActivitySource.Name)
                    .AddAzureMonitorTraceExporter(ConfigureAzure));

            // When started from the AppHost, Aspire passes the dashboard's address in OTEL_EXPORTER_OTLP_ENDPOINT
            if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            {
                // Experimental OTLP option: save failed batches to disk and retry, like the Azure exporter
                builder.Configuration["OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY"] = "disk";
                builder.Configuration["OTEL_DOTNET_EXPERIMENTAL_OTLP_DISK_RETRY_DIRECTORY_PATH"] = Path.Combine(telemetryDirectory, "otlp");

                otel.UseOtlpExporter();
            }

            var app = builder.Build();

            // OpenTelemetry starts when its providers are created, and MAUI doesn't create them for us
            var tracerProvider = app.Services.GetRequiredService<TracerProvider>();
            var loggerProvider = app.Services.GetRequiredService<LoggerProvider>();

            // 2. When the device comes back online, send what's buffered right away
            Connectivity.Current.ConnectivityChanged += (_, e) =>
            {
                if (e.NetworkAccess == NetworkAccess.Internet)
                    Task.Run(() =>
                    {
                        tracerProvider.ForceFlush();
                        loggerProvider.ForceFlush();
                    });
            };

            return app;
        }
    }
}
