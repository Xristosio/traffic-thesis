using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Traffic.Application.Measurements;
using Traffic.Application.Messaging;
using Traffic.Application.Policies;
using Traffic.Application.SignalStates;
using Traffic.Application.Simulation;
using Traffic.Application.Topology;
using Traffic.Contracts.Configuration;
using Traffic.Contracts.Messages;
using Traffic.Domain.Topology;
using Traffic.Infrastructure.Messaging;

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

    public static IServiceCollection AddTrafficMeasurementPublishing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IMessagePublisher<TrafficMeasurement>, KafkaTrafficMeasurementPublisher>();

        return services;
    }

    public static IServiceCollection AddTrafficMeasurementConsuming(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ILatestMeasurementStore, LatestMeasurementStore>();
        services.AddSingleton<IMessageConsumer<TrafficMeasurement>, KafkaTrafficMeasurementConsumer>();

        return services;
    }

    public static IServiceCollection AddTrafficDecisionCommandPublishing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ISignalDecisionCommandScheduler, FixedTimeSignalDecisionCommandScheduler>();
        services.AddSingleton<IMessagePublisher<SignalDecisionCommand>, KafkaSignalDecisionCommandPublisher>();

        return services;
    }

    public static IServiceCollection AddTrafficGatewaySignalControl(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ISignalStateStore, SignalStateStore>();
        services.AddSingleton<IMessageConsumer<SignalDecisionCommand>, KafkaSignalDecisionCommandConsumer>();
        services.AddSingleton<IMessageConsumer<TrafficMeasurement>, KafkaGatewayTrafficMeasurementConsumer>();
        services.AddSingleton<IMessagePublisher<SignalStateSnapshot>, KafkaSignalStateSnapshotPublisher>();

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
