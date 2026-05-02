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
http://localhost:5011/api/experiment-runs
http://localhost:5011/api/experiment-runs/latest/metrics
http://localhost:5011/api/experiment-runs/{runId}/metrics
```

## Verify experiment run metrics

After the full pipeline has run long enough to persist data, open:

```
http://localhost:5011/api/experiment-runs
http://localhost:5011/api/experiment-runs/latest/metrics
```

Use a run id from `/api/experiment-runs` to open:

```
http://localhost:5011/api/experiment-runs/{runId}/metrics
```

Manual PostgreSQL validation:

```bash
docker exec -it traffic-postgres psql -U postgres -d traffic_thesis -c "select id, policy, scenario, started_at_utc, finished_at_utc from experiment_runs order by started_at_utc desc;"
docker exec -it traffic-postgres psql -U postgres -d traffic_thesis -c "select run_id, count(*) as measurement_count, avg(queue_length) as average_queue_length, max(queue_length) as max_queue_length, sum(arrivals) as total_arrivals, sum(departures) as total_departures from traffic_measurements group by run_id order by max(measured_at_utc) desc;"
docker exec -it traffic-postgres psql -U postgres -d traffic_thesis -c "select run_id, count(*) as decision_command_count from signal_decision_commands group by run_id;"
docker exec -it traffic-postgres psql -U postgres -d traffic_thesis -c "select run_id, count(*) as state_snapshot_count from signal_state_snapshots group by run_id;"
```

## Run a controlled experiment

Choose the policy in `src/Traffic.DecisionEngine.Worker/appsettings.json`:

```json
{
  "Policy": {
    "Mode": "LongestQueueFirst"
  }
}
```

Configure the Producer run in `src/Traffic.Producer.Worker/appsettings.json`:

```json
{
  "Simulation": {
    "TickMilliseconds": 1000,
    "RandomSeed": 42,
    "Scenario": "Balanced",
    "DepartureRatePerTick": 3,
    "RunDurationSeconds": 60,
    "StopProducerAfterRun": true,
    "ExperimentName": "Balanced-LQF-60s"
  }
}
```

Start infrastructure and services:

```bash
docker compose up -d
dotnet run --project src/Traffic.Gateway.Api/Traffic.Gateway.Api.csproj
dotnet run --project src/Traffic.DecisionEngine.Worker/Traffic.DecisionEngine.Worker.csproj
dotnet run --project src/Traffic.Producer.Worker/Traffic.Producer.Worker.csproj
```

After the Producer stops, mark the latest run as finished:

```bash
curl -X POST http://localhost:5011/api/experiment-runs/latest/finish
```

Open the latest metrics:

```
http://localhost:5011/api/experiment-runs/latest/metrics
```

## Traffic scenarios

Set the active scenario in `src/Traffic.Producer.Worker/appsettings.json`:

```json
{
  "Simulation": {
    "Scenario": "Balanced"
  }
}
```

Supported scenarios:

- `Balanced`: each signal generates uniformly distributed arrivals from 0 to 3 vehicles per tick.
- `Unbalanced`: the first configured signal in each intersection is treated as high traffic and generates 2 to 6 arrivals per tick; other signals generate 0 to 2 arrivals per tick.

For an unbalanced experiment:

```json
{
  "Simulation": {
    "Scenario": "Unbalanced",
    "RunDurationSeconds": 60,
    "StopProducerAfterRun": true,
    "ExperimentName": "Unbalanced-LQF-60s"
  }
}
```

Unknown scenario names fail at Producer startup with a clear error.

## Signal phases

Each intersection must define phases: named groups of non-conflicting signals that may be Green at the same time. There is no automatic phase generation; every intersection must explicitly list its allowed phases in topology configuration.

Example topology phase configuration:

```json
{
  "Id": "I1",
  "Name": "Intersection 1",
  "Signals": [
    { "Id": "S1", "Name": "North" },
    { "Id": "S2", "Name": "East" },
    { "Id": "S3", "Name": "South" },
    { "Id": "S4", "Name": "West" }
  ],
  "Phases": [
    {
      "Id": "P1",
      "Name": "North/South",
      "GreenSignalIds": ["S1", "S3"]
    },
    {
      "Id": "P2",
      "Name": "East/West",
      "GreenSignalIds": ["S2", "S4"]
    }
  ]
}
```

`FixedTime` cycles through phases in topology order. `LongestQueueFirst` scores each phase by summing the latest queue length for all signals in `GreenSignalIds`, then selects the highest-scoring phase while applying fairness at the phase level.

Gateway applies all selected signals from a command as Green, transitions all selected signals to Yellow together, then returns them to Red. `/api/signal-states` can therefore show multiple Green signals for the same intersection.

## Compare experiment runs

Run one controlled experiment with `Policy:Mode` set to `FixedTime`, then mark it finished and note its run id from:

```
http://localhost:5011/api/experiment-runs
```

Run another controlled experiment with `Policy:Mode` set to `LongestQueueFirst`, then mark it finished and note its run id.

Compare the Fixed-Time baseline against the Longest-Queue-First candidate:

```
http://localhost:5011/api/experiment-runs/compare?baselineRunId=<fixed-time-run-id>&candidateRunId=<lqf-run-id>
```

In the comparison result, a negative `averageQueueLengthDelta` means the candidate had a lower average queue than the baseline. A positive `servedVehiclesDelta` means the candidate served more vehicles.

## PostgreSQL:

```
Host: localhost
Port: 5433
Database: traffic_thesis
Username: postgres
Password: postgres
```

## PostgreSQL migrations

Start PostgreSQL:

```bash
docker compose up -d postgres
```

Apply EF Core migrations:

```bash
dotnet ef database update --project src/Traffic.Infrastructure/Traffic.Infrastructure.csproj --startup-project src/Traffic.Infrastructure/Traffic.Infrastructure.csproj --context TrafficDbContext
```

If your global `dotnet-ef` tool is older than the project EF Core version, update it first:

```bash
dotnet tool update --global dotnet-ef --version 10.0.4
```

Verify tables in PostgreSQL:

```bash
docker exec -it traffic-postgres psql -U postgres -d traffic_thesis -c "\dt"
docker exec -it traffic-postgres psql -U postgres -d traffic_thesis -c "select count(*) from traffic_measurements;"
docker exec -it traffic-postgres psql -U postgres -d traffic_thesis -c "select count(*) from signal_decision_commands;"
docker exec -it traffic-postgres psql -U postgres -d traffic_thesis -c "select count(*) from signal_state_snapshots;"
docker exec -it traffic-postgres psql -U postgres -d traffic_thesis -c "select * from experiment_runs order by started_at_utc desc;"
```

## Configuration

Each executable project uses its own `appsettings.Development.json`.

Copy the example file:

```
copy appsettings.Development.example.json appsettings.Development.json
```
