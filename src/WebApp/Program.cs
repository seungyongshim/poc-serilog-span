using System.Diagnostics;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Enrichers.Span;
using OpenTelemetry.Trace;
using Serilog.Templates;

using var activitySource = new ActivitySource("1");

ActivitySource.AddActivityListener(new ActivityListener
{
    ShouldListenTo = s => true,
    SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivitySamplingResult.AllData,
    Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivitySamplingResult.AllData,
});

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.Configure(builder =>
{
    builder.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.Baggage;
});
builder.Services.AddSerilog((sp, config) =>
{
    //config.Enrich.WithSpan(new() { IncludeBaggage = true });
    config.WriteTo.Console(new ExpressionTemplate("{ { TraceId: @tr, SpanId: @sp, ..@p} }\n"));
});
builder.Services.AddOpenTelemetry().WithTracing(builder =>
{
    builder.SetSampler(new AlwaysOnSampler());
});

var app = builder.Build();

using var activity = activitySource.StartActivity("Main");
activity?.SetBaggage("Hello", "World");

app.Services.GetRequiredService<ILogger<Program>>().LogInformation("Hello World!");


app.Run();
