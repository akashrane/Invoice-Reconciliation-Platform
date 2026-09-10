using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using ReconFlow.Api.Middleware;
using ReconFlow.Application.Abstractions;
using ReconFlow.Application.Services;
using ReconFlow.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddCors(options => options.AddPolicy("ui", policy => policy
    .WithOrigins(builder.Configuration["FrontendUrl"] ?? "http://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod()));
builder.Services.AddDbContext<ReconFlowDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IReconFlowDbContext>(provider => provider.GetRequiredService<ReconFlowDbContext>());
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<InvoiceService>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("ui");
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program;
