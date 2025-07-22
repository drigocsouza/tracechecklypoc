# TraceChecklyPoC

PoC ASP.NET Core Web API instrumentada com OpenTelemetry, enviando traces para o Checkly via OpenTelemetry Collector.

---

## Pacotes necessários

Instale os pacotes NuGet no diretório do projeto:

```sh
dotnet add package OpenTelemetry
dotnet add package OpenTelemetry.Exporter.Console
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
```

---

## Como rodar localmente

1. **Restaure os pacotes:**
   ```sh
   dotnet restore
   ```

2. **Execute o projeto:**
   ```sh
   dotnet run
   ```

3. **Acesse o endpoint de teste:**
   - [https://localhost:5001/ping](https://localhost:5001/ping)  
     Retorna: `pong`
   - [https://localhost:5001/ping?error=true](https://localhost:5001/ping?error=true)  
     Retorna erro HTTP 500 simulado (`"Simulated error for tracing"`) para testar tracing de erros.

---

## OpenTelemetry Collector

O Collector é responsável por:

- Receber spans via OTLP (HTTP ou gRPC)
- Filtrar spans que não tenham o campo `trace_state["checkly"]=true`
- Exportar para o endpoint OTLP do Checkly

**Exemplo de configuração (`otel-collector-config.yaml`):**

```yaml
receivers:
  otlp:
    protocols:
      grpc:
      http:

processors:
  batch:
  filter/checkly:
    error_mode: ignore
    traces:
      span:
        - 'trace_state["checkly"] != "true"'

exporters:
  otlp/checkly:
    endpoint: "otel.eu-west-1.checklyhq.com:4317"
    headers:
      authorization: "${env:CHECKLY_OTEL_API_KEY}"

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [batch, filter/checkly]
      exporters: [otlp/checkly]

debug:
  verbosity: detailed
  use_internal_logger: false
```

> **Importante:**  
> Defina a variável de ambiente `CHECKLY_OTEL_API_KEY` com sua chave de API do Checkly no ambiente onde o Collector roda.

---

## Instrumentação do projeto .NET

No `Program.cs`:

- Adicione um processor customizado para inserir `trace_state["checkly"]=true` em todos os spans:

```csharp
public class ChecklyTraceStateProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        activity.TraceStateString = "checkly=true";
        base.OnStart(activity);
    }
}

// No builder:
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddProcessor(new ChecklyTraceStateProcessor())
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("TraceChecklyPoC"))
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("<ENDEREÇO_DO_COLLECTOR>"); // Exemplo: http://localhost:4317
            })
            .AddConsoleExporter();
    });
```

---

## Exemplo de Controller

No projeto há um endpoint de teste para traces de sucesso e erro:

```csharp
using Microsoft.AspNetCore.Mvc;

namespace TraceChecklyPoC.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PingController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get(bool error = false)
        {
            if (error)
            {                
                return StatusCode(500, "Simulated error for tracing");
            }
            
            return Ok("pong");
        }
    }
}
```

---

## Deploy

1. **Publique o projeto:**
   ```sh
   dotnet publish -c Release -o ./bin/Release/net9.0/publish
   ```

2. **Faça upload do conteúdo da pasta `publish`** para o ambiente de destino (Azure App Service ou outro).

3. **Garanta que a variável de ambiente `CHECKLY_OTEL_API_KEY` está configurada** no painel do serviço onde o Collector está rodando.

---

## Observações

- O serviço estará identificado como `service.name: TraceChecklyPoC`.
- Para visualizar os traces, acesse o painel do Checkly após acessar o endpoint `/ping` (com e sem erro).
- Caso não apareça nada no Checkly, confira:
  - Se os spans possuem `TraceState: checkly=true` nos logs do app.
  - Se o Collector está exportando corretamente e não há erros de autenticação ou conexão.
  - Se o atributo `service.name` está presente no resource.

---

## Referências

- [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/)
- [Checkly Distributed Tracing](https://www.checklyhq.com/docs/observability/distributed-tracing/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)