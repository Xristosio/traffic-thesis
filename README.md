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

## Switch Decision Engine policy

Set `Policy:Mode` in `src/Traffic.DecisionEngine.Worker/appsettings.json` or `appsettings.Development.json`:

```json
{
  "Policy": {
    "Mode": "FixedTime"
  }
}
```

or:

```json
{
  "Policy": {
    "Mode": "LongestQueueFirst"
  }
}
```

Run Producer and Decision Engine, then open Kafka UI:

```
http://localhost:8085
```

Check `traffic.commands` for `SignalDecisionCommand` messages. With `LongestQueueFirst`, command payloads use `"policy": "LongestQueueFirst"` and select signals based on the latest queues per intersection.

## Verify Gateway state publishing

Start the local infrastructure, then run these services:

```bash
dotnet run --project src/Traffic.Producer.Worker/Traffic.Producer.Worker.csproj
dotnet run --project src/Traffic.DecisionEngine.Worker/Traffic.DecisionEngine.Worker.csproj
dotnet run --project src/Traffic.Gateway.Api/Traffic.Gateway.Api.csproj
```

Open Kafka UI:

```
http://localhost:8085
```

Check the `traffic.state` topic for produced `SignalStateSnapshot` JSON messages.

## Verify Gateway read API

Start the local infrastructure, then run the full pipeline:

```bash
docker compose up -d
dotnet run --project src/Traffic.Producer.Worker/Traffic.Producer.Worker.csproj
dotnet run --project src/Traffic.DecisionEngine.Worker/Traffic.DecisionEngine.Worker.csproj
dotnet run --project src/Traffic.Gateway.Api/Traffic.Gateway.Api.csproj
```

Open the Gateway API endpoints:

```
http://localhost:5011/api/topology
http://localhost:5011/api/signal-states
```

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
