# Traffic Thesis

Dynamic traffic light management simulation using .NET, Apache Kafka and PostgreSQL.

## Services

- Traffic.Producer.Worker
- Traffic.DecisionEngine.Worker
- Traffic.Gateway.Api

## Infrastructure

- Apache Kafka
- Kafka UI
- PostgreSQL

## Local startup

```bash
docker compose up -d
dotnet build
```

## Kafka UI:

```
http://localhost:8085
```

## Verify measurement publishing

Start the local infrastructure:

```bash
docker compose up -d
```

Run `Traffic.Producer.Worker`, then open:

```bash
dotnet run --project src/Traffic.Producer.Worker/Traffic.Producer.Worker.csproj
```

Open Kafka UI:

```
http://localhost:8085
```

In Kafka UI, check the `traffic.measurements` topic for produced `TrafficMeasurement` JSON messages.

## Verify Fixed-Time command publishing

Start the local infrastructure:

```bash
docker compose up -d
```

Run `Traffic.Producer.Worker`:

```bash
dotnet run --project src/Traffic.Producer.Worker/Traffic.Producer.Worker.csproj
```

Run `Traffic.DecisionEngine.Worker` in another terminal:

```bash
dotnet run --project src/Traffic.DecisionEngine.Worker/Traffic.DecisionEngine.Worker.csproj
```

Open Kafka UI:

```
http://localhost:8085
```

In Kafka UI, check the `traffic.commands` topic for produced `SignalDecisionCommand` JSON messages.

## PostgreSQL:

```
Host: localhost
Port: 5433
Database: traffic_thesis
Username: postgres
Password: postgres
```

## Configuration

Each executable project uses its own `appsettings.Development.json`.

Copy the example file:

```
copy appsettings.Development.example.json appsettings.Development.json
```
