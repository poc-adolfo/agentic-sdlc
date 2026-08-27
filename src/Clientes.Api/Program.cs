using Clientes.Api.Apis;
using Serilog;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
var builder=WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx,log)=>log.WriteTo.Console());
builder.Services.AddSingleton<IStore,MemoryStore>();
builder.Services.AddEndpointsApiExplorer(); builder.Services.AddSwaggerGen();
builder.Services.AddOpenTelemetry().WithTracing(t=>t.AddAspNetCoreInstrumentation().AddConsoleExporter()).WithMetrics(m=>m.AddAspNetCoreInstrumentation().AddConsoleExporter());
var app=builder.Build(); app.UseSerilogRequestLogging(); app.UseSwagger(); app.UseSwaggerUI(); app.MapClienteEndpoints(); app.Run();
public partial class Program { }
