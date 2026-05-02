namespace Traffic.Contracts.Configuration;

public sealed class DatabaseSettings
{
    public const string SectionName = "Database";

    public string ConnectionString { get; set; } = string.Empty;
}
