using Fundo.Application.Persistence;
using Fundo.Infrastructure.ExternalService;
using Fundo.Infrastructure.Outbox;
using Fundo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fundo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<FundoDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Fundo")));
        services.AddScoped<IFundoDbContext>(sp => sp.GetRequiredService<FundoDbContext>());

        services.AddSingleton(TimeProvider.System);

        services.AddHttpClient<IExternalLoanClient, ExternalLoanHttpClient>(client =>
        {
            var baseUrl = configuration["ExternalService:BaseUrl"]
                ?? throw new InvalidOperationException("Missing configuration value: ExternalService:BaseUrl");
            client.BaseAddress = new Uri(baseUrl);
        });

        services.AddHostedService<OutboxDispatcherService>();

        return services;
    }
}
