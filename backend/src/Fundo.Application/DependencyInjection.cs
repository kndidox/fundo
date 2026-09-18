using Fundo.Application.Applications;
using Fundo.Application.Persistence;
using Fundo.Domain.Rules;
using Fundo.Domain.Rules.DataDriven;
using Microsoft.Extensions.DependencyInjection;

namespace Fundo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Every deny rule — including the two required by the spec — is a rule_definitions row
        // (seeded via EF Core HasData, see RuleDefinitionConfiguration). Resolved once per
        // request scope, so a newly inserted row applies immediately, no deploy needed.
        //
        // A rule that needs real logic beyond "field, operator, value" (aggregates, calling
        // another service, ...) would be a plain C# IApplicationRule class concatenated onto
        // this same query, e.g. `.Concat([new MyComplexRule(...)])` — RuleEngine itself never
        // needs to change either way.
        services.AddScoped<IEnumerable<IApplicationRule>>(sp =>
            sp.GetRequiredService<IFundoDbContext>().RuleDefinitions
                .Where(r => r.IsActive)
                .OrderBy(r => r.Priority)
                .AsEnumerable()
                .Select(definition => (IApplicationRule)new DataDrivenRule(definition)));
        services.AddScoped<RuleEngine>();

        services.AddScoped<SubmitApplicationUseCase>();

        return services;
    }
}
