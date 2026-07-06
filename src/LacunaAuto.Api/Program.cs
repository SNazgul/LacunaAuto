using System.Diagnostics;
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
    }

    app.UseHttpsRedirection();

    var summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
