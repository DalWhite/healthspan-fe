using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Healthspan.Offline;

/// <summary>
/// Client half of the /sync contract (mirrored by the API's
/// ApiContracts/Sync resources).
/// </summary>
public sealed class SyncRequest
{
    public List<SyncOperationPayload> Operations { get; set; } = [];
}

public sealed class SyncOperationPayload
{
    [JsonPropertyName("opId")]
    public Guid OpId { get; set; }

    [JsonPropertyName("entity")]
    public string Entity { get; set; } = "";

    [JsonPropertyName("action")]
    public string Action { get; set; } = "";

    [JsonPropertyName("clientEntityId")]
    public Guid? ClientEntityId { get; set; }

    [JsonPropertyName("payload")]
    public JsonElement Payload { get; set; }
}

public sealed class SyncResponse
{
    [JsonPropertyName("applied")]
    public int Applied { get; set; }

    [JsonPropertyName("deduped")]
    public int Deduped { get; set; }

    [JsonPropertyName("failed")]
    public List<SyncFailure> Failed { get; set; } = [];

    [JsonPropertyName("idMap")]
    public List<SyncIdMap> IdMap { get; set; } = [];
}

public sealed class SyncFailure
{
    [JsonPropertyName("opId")]
    public Guid OpId { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public sealed class SyncIdMap
{
    [JsonPropertyName("clientId")]
    public Guid ClientId { get; set; }

    [JsonPropertyName("serverId")]
    public Guid ServerId { get; set; }
}
