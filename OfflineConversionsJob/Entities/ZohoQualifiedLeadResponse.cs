using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OfflineConversionsJob.Entities;

public class ZohoQualifiedLeadResponse
{
    public List<ZohoQualifiedLead> Data { get; set; }
    public ZohoQualifiedLeadInfo Info { get; set; }
}

public class ZohoQualifiedLeadInfo
{
    [JsonPropertyName("per_page")]
    public int PageSize { get; set; }
    
    public int Count { get; set; }

    [JsonPropertyName("sort_by")]
    public string SortBy { get; set; }
    
    public int Page { get; set; }

    [JsonPropertyName("sort_order")]
    public string SortOrder { get; set; }

    [JsonPropertyName("more_records")]
    public bool MoreRecords { get; set; }
}