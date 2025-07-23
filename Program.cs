using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Logging.AddConsole();
builder.Logging.AddFilter("OpenTelemetry", LogLevel.Debug);

// Adiciona o processor customizado para inserir trace_state "checkly=true"
builder.Services.AddSingleton<BaseProcessor<Activity>, ChecklyTraceStateProcessor>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("TraceChecklyPoC"))
            .AddAspNetCoreInstrumentation()
            .AddProcessor<ChecklyTraceStateProcessor>()
            .AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri("http://otel-collector.livelywater-b0572b59.eastus.azurecontainerapps.io:4317");
                otlpOptions.Headers = "authorization=ot_461f7f9bcfed4e809597df214c522db4";
                otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            })
            .AddConsoleExporter();
    });

var app = builder.Build();

app.MapControllers();

app.Run();

public class ChecklyTraceStateProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        if (activity == null)
            return;

        // Adiciona "checkly=true" ao trace state, preservando outros valores
        var currentState = activity.TraceStateString;
        if (string.IsNullOrEmpty(currentState))
        {
            activity.TraceStateString = "checkly=true";
        }
        else if (!currentState.Contains("checkly=true"))
        {
            activity.TraceStateString = currentState + ",checkly=true";
        }

        base.OnStart(activity);
    }
}