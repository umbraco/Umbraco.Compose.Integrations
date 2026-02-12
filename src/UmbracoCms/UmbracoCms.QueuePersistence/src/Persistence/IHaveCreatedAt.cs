namespace Umbraco.Compose.Integrations.UmbracoCms.QueuePersistence.Persistence;

/// <summary>Marker interface for DTOs that have a <see cref="CreatedAt"/> timestamp.</summary>
public interface IHaveCreatedAt
{
    /// <summary>Gets or sets the UTC timestamp when the record was created.</summary>
    DateTime CreatedAt { get; set; }
}
