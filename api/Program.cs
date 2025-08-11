using api.Middleware;
using application;
using FluentValidation.AspNetCore;
using Hangfire;
using infrastructure;
using infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

namespace api;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/banking-api-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("Starting Banking API");

            var builder = WebApplication.CreateBuilder(args);

            // Add Serilog
            builder.Host.UseSerilog();

            // Add services to the container
            builder.Services.AddControllers();
            builder.Services.AddFluentValidationAutoValidation();

            // Add application and infrastructure layers
            builder.Services.AddApplication(builder.Configuration);
            builder.Services.AddInfrastructure(builder.Configuration);

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = JwtBearerDefaults.AuthenticationScheme
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = JwtBearerDefaults.AuthenticationScheme,
                            },
                            Scheme = "Oauth2",
                            Name = JwtBearerDefaults.AuthenticationScheme,
                            In = ParameterLocation.Header
                        },
                        new List<string>()
                    }
                });
            });

            // CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // Health checks
            builder.Services.AddHealthChecks()
                .AddDbContextCheck<BankingDbContext>();

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowAll");

            // Request logging
            app.UseSerilogRequestLogging();

            // Global exception handling
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHealthChecks("/health");

            app.UseHangfireDashboard("/bg", new DashboardOptions
            {
                DashboardTitle = "Test",
            });

            // Database migration (with error handling)
            try
            {
                using var scope = app.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
                await context.Database.MigrateAsync();
                Log.Information("Database migration completed successfully");
            }
            catch (Exception dbEx)
            {
                Log.Warning(dbEx, "Database migration failed, but application will continue");
                // Don't throw here - let the app start even if DB is not available
            }

            Log.Information("Banking API started successfully. Swagger UI available at: /swagger");
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}