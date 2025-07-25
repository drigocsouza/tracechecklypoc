using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using TraceChecklyPoC.ChecklyTraceStateProcessor;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Logging.AddConsole();
builder.Logging.AddFilter("OpenTelemetry", LogLevel.Debug);

// Registra o processor customizado como singleton (opcional, mas recomendado)
builder.Services.AddSingleton<BaseProcessor<Activity>, ChecklyTraceStateProcessor>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("TraceChecklyPoC"))
            .AddAspNetCoreInstrumentation()
            // Chama o processor customizado
            .AddProcessor<ChecklyTraceStateProcessor>()
            .AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri("http://otel.eu-west-1.checklyhq.com/v1/traces");
                otlpOptions.Headers = "authorization=Bearer ot_461f7f9bcfed4e809597df214c522db4";
                otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            })
            .AddConsoleExporter();
    });

var app = builder.Build();

app.MapControllers();

app.Run();

