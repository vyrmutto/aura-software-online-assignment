using System.Text;
using ClinicPOS.API.Middleware;
using ClinicPOS.API.Seeder;
using ClinicPOS.Application.Appointments.Services;
using ClinicPOS.Application.Branches.Services;
using ClinicPOS.Application.Interfaces;
using ClinicPOS.Application.Patients.Services;
using ClinicPOS.Application.Users.Services;
using ClinicPOS.Domain.Interfaces;
using ClinicPOS.Infrastructure.Auth;
using ClinicPOS.Infrastructure.Cache;
using ClinicPOS.Infrastructure.Data;
using ClinicPOS.Infrastructure.Messaging;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

// Redis
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(redisConnection + ",abortConnect=false")));
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// RabbitMQ
builder.Services.AddSingleton<IEventPublisher, RabbitMqPublisher>();

// Auth
var jwtKey = builder.Configuration["Jwt:Key"] ?? "SuperSecretKeyForClinicPOS2026!!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ClinicPOS";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ClinicPOS";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CanCreatePatient", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.Claims.Any(c => c.Type == "role" && (c.Value == "Admin" || c.Value == "User"))))
    .AddPolicy("CanCreateAppointment", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.Claims.Any(c => c.Type == "role" && (c.Value == "Admin" || c.Value == "User"))))
    .AddPolicy("AdminOnly", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.Claims.Any(c => c.Type == "role" && c.Value == "Admin")));

// Tenant context
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

// Services
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PatientService>();
builder.Services.AddScoped<AppointmentService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<BranchService>();

// Validators
builder.Services.AddValidatorsFromAssemblyContaining<ClinicPOS.Application.Patients.Validators.CreatePatientValidator>();

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                builder.Configuration["Cors:Origins"] ?? "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddOpenApi();

var app = builder.Build();

// Run migrations and seed on startup (skip for Testing environment - handled by test factory)
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DataSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantContextMiddleware>();
app.MapControllers();

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
