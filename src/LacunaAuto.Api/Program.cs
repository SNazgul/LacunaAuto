using System.Diagnostics;
using LacunaAuto.Api.Endpoints;
using LacunaAuto.Data.Extensions;
using LacunaAuto.Data.Initialization;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext();
    });

    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        };
    });

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resourceBuilder =>
        {
            var serviceName = builder.Configuration["Observability:ServiceName"] ?? builder.Environment.ApplicationName;
            var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString();

            resourceBuilder
                .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
                .AddAttributes(
                [
                    new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName),
                ]);
        })
        .WithTracing(tracingBuilder =>
        {
            tracingBuilder
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                })
                .AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                });

            var otlpEndpoint = builder.Configuration["Observability:Otlp:Endpoint"];
            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                if (!Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var otlpUri))
                {
                    throw new InvalidOperationException("Observability:Otlp:Endpoint must be a valid absolute URI.");
                }

                tracingBuilder.AddOtlpExporter(options =>
                {
                    options.Endpoint = otlpUri;
                });
            }
        });

    // Add services to the container.
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.AddLacunaAutoData(builder.Configuration);
    }

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {ElapsedMilliseconds} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestId", httpContext.TraceIdentifier);

            var activity = Activity.Current;
            if (activity is not null)
            {
                diagnosticContext.Set("TraceId", activity.TraceId.ToString());
                diagnosticContext.Set("SpanId", activity.SpanId.ToString());
            }
        };
    });

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        await DevelopmentDatabaseInitializer.InitializeAsync(app.Services);
    }

    app.UseHttpsRedirection();

    app.MapPublicListingsEndpoints();

    app.Run();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    Log.Fatal(exception, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program
{
}
