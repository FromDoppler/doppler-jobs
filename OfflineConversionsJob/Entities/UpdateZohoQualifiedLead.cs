using System.Text.Json.Serialization;

namespace OfflineConversionsJob.Entities;

public class UpdateZohoQualifiedLead
{
    [JsonPropertyName("id")]
    public string LeadId { get; set; }
    
    [JsonPropertyName("GoogleAdProcessed")]
    public bool GoogleAdProcessed { get; set; }
}