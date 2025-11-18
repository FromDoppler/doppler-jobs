using System;
using System.Text.Json.Serialization;

namespace OfflineConversionsJob.Entities;

public class ZohoQualifiedLead
{
    [JsonPropertyName("GCLID")]
    public string GoogleClickId { get; set; }
    
    [JsonPropertyName("Created_Time")]
    public DateTimeOffset ConversionDateTime { get; set; }
    
    [JsonPropertyName("Score")]
    public double ConversionValue { get; set; }
    
    [JsonPropertyName("Currency")]
    public string CurrencyCode { get; set; }
    
    [JsonPropertyName("id")]
    public string LeadId { get; set; }
    
    [JsonPropertyName("GoogleAdProcessed")]
    public bool GoogleAdProcessed { get; set; }
}