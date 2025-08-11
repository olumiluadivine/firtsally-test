using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using infrastructure.Data;

namespace test.Fixtures;

public class BankingApiWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<BankingDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<BankingDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryBankingTestDb");
            });

            // Override configuration for testing
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"JWT:Key", "super-secret-key-for-testing-that-is-long-enough"},
                    {"JWT:Issuer", "BankingAPITest"},
                    {"Security:EncryptionKey", "test-encryption-key-32-characters"},
                    {"ConnectionStrings:Redis", "localhost:6379"},
                    {"Paystack:SecretKey", "sk_test_dummy_key"},
                    {"Paystack:PublicKey", "pk_test_dummy_key"},
                    {"Paystack:Uri", "https://api.paystack.co"}
                })
                .Build();

            services.AddSingleton<IConfiguration>(configuration);
        });

        builder.UseEnvironment("Testing");
    }
}