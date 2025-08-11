using application.Contracts.Managers;
using application.Contracts.Repos;
using application.Contracts.Response;
using application.Contracts.Services;
using Hangfire;
using Hangfire.Redis.StackExchange;
using infrastructure.Data;
using infrastructure.Managers;
using infrastructure.Repositories;
using infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;
using System.Text;

namespace infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BankingDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), npgsqlOptions =>
            {
                // Configure command timeout
                npgsqlOptions.CommandTimeout(30);
            });

            // Configure service provider caching
            options.EnableServiceProviderCaching();
        });

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Redis
        IConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(configuration["ConnectionStrings:Redis"]!);
        services.AddStackExchangeRedisCache(options =>
            options.ConnectionMultiplexerFactory = () => Task.FromResult(connectionMultiplexer));
        services.AddSingleton<IConnectionMultiplexer>(connectionMultiplexer);

        // Hangifre
        services.AddHangfireServer();
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseRedisStorage(connectionMultiplexer)
            .UseFilter(new AutomaticRetryAttribute
            {
                Attempts = 1,
                LogEvents = true,
                OnAttemptsExceeded = AttemptsExceededAction.Fail,
                DelaysInSeconds = new int[] { 1, 2, 3 },
            }));

        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IPaymentService, PaystackPaymentService>();
        services.AddScoped<IAccountNumberGenerator, AccountNumberGenerator>();
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<ITokenService, TokenService>();

        services.AddScoped<ITransactionManager, TransactionManager>();
        services.AddScoped<IAccountManager, AccountManager>();
        services.AddScoped<IUserManager, UserManager>();

        // Payment Services
        services.AddHttpClient("paystackAPI", paystack =>
        {
            paystack.BaseAddress = new Uri(configuration["Paystack:Uri"]!);
            paystack.DefaultRequestHeaders.Add("Authorization", "Bearer " + configuration["Paystack:SecretKey"]);
            paystack.DefaultRequestHeaders.Add("accept", "application/json");
        });

        // Logging
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        // JWT Authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["JWT:Issuer"],
                    ValidAudience = configuration["JWT:Issuer"], // Use same as issuer for simplicity
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                        configuration["JWT:Key"] ?? throw new InvalidOperationException("JWT Key not configured")))
                };
            });

        return services;
    }
}