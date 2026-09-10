using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ReconFlow.Api.Auth;
using ReconFlow.Api.Middleware;
using ReconFlow.Application.Abstractions;
using ReconFlow.Application.Services;
using ReconFlow.Core.Services;
using ReconFlow.Infrastructure.Auth;
using ReconFlow.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddCors(options => options.AddPolicy("ui", policy => policy
    .WithOrigins(builder.Configuration["FrontendUrl"] ?? "http://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod()));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");
if (jwt.Key.Length < 32) throw new InvalidOperationException("JWT key must be at least 32 characters.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});
builder.Services.AddAuthorization();
builder.Services.AddDbContext<ReconFlowDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IReconFlowDbContext>(provider => provider.GetRequiredService<ReconFlowDbContext>());
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<ReconciliationService>();
builder.Services.AddScoped<CsvPaymentImportService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IPasswordService, AspNetPasswordService>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<DemoDataSeeder>();
builder.Services.AddSingleton<IMatchingRulesEngine, MatchingRulesEngine>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("ui");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<ReconFlowDbContext>();
    await database.Database.MigrateAsync();
    if (app.Environment.IsDevelopment())
        await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync();
}

app.Run();

public partial class Program;
