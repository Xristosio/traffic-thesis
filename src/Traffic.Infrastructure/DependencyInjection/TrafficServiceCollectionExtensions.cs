using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Traffic.Application.Simulation;
using Traffic.Application.Topology;
using Traffic.Contracts.Configuration;
using Traffic.Domain.Topology;

namespace Traffic.Infrastructure.DependencyInjection;

public static class TrafficServiceCollectionExtensions
{
    public static IServiceCollection AddTrafficConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSettings<KafkaSettings>(configuration, KafkaSettings.SectionName);
        services.AddSettings<DatabaseSettings>(configuration, DatabaseSettings.SectionName);
        services.AddSettings<SimulationSettings>(configuration, SimulationSettings.SectionName);
        services.AddSettings<PolicySettings>(configuration, PolicySettings.SectionName);
        services.AddSettings<TopologySettings>(configuration, TopologySettings.SectionName);

        return services;
    }

    public static IServiceCollection AddTrafficTopology(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ITopologyBuilder, TopologyBuilder>();
        services.AddSingleton(serviceProvider =>
        {
            var settings = serviceProvider.GetRequiredService<TopologySettings>();
            var builder = serviceProvider.GetRequiredService<ITopologyBuilder>();

            return builder.Build(settings);
        });

        return services;
    }

    public static IServiceCollection AddTrafficProducerSimulation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IProducerSimulationService, ProducerSimulationService>();

        return services;
    }

    private static void AddSettings<TSettings>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TSettings : class, new()
    {
        services
            .AddOptions<TSettings>()
            .Bind(configuration.GetSection(sectionName));

        services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<TSettings>>().Value);
    }
}
