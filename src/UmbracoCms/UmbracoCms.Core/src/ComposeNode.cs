using System.Text.Json.Serialization;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core;

/// <summary>
/// Represents a node in the Umbraco Compose
/// </summary>
/// <param name="Id">The ID of the node</param>
[JsonConverter(typeof(ComposeNodeJsonSerializer))]
public sealed record ComposeNode(string Id);
