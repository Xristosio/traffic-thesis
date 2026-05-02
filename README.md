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
