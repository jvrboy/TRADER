using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BrainSystem.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBrainSystemCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BrainOptions>(configuration.GetSection(BrainOptions.SectionName));
        services.Configure<MemoryOptions>(configuration.GetSection(MemoryOptions.SectionName));
        services.Configure<SecurityOptions>(configuration.GetSection(SecurityOptions.SectionName));
        services.AddSingleton<IMarketDataSource, SyntheticMarketDataSource>();
        services.AddSingleton<IBrainEnsemble, BrainEnsemble>();
        services.AddSingleton<IHierarchicalMemory, HierarchicalMemory>();
        services.AddSingleton<ITool, UtcTimeTool>();
        services.AddSingleton<ITool, UnitConverterTool>();
        services.AddSingleton<ITool, CalculatorTool>();
        services.AddSingleton<ITool, PredictionTool>();
        services.AddSingleton<IToolRegistry, ToolRegistry>();
        services.AddSingleton<IGgufLlmProvider, GgufLlmProvider>();
        services.AddSingleton<AgentService>();
        services.AddHostedService<MemoryConsolidationService>();
        return services;
    }
}