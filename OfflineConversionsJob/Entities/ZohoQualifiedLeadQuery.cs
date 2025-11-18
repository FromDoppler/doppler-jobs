using System.Collections.Generic;

namespace OfflineConversionsJob.Entities;

public class ZohoQualifiedLeadQuery
{
    public List<string> Fields { get; set; } = new();
    public int PageSize { get; set; }
    public int Page { get; set; }
    public string FromCreatedTime { get; set; }
    public int MinScore { get; set; }
    public string SortBy { get; set; }
    public string SortOrder { get; set; }
    public bool GoogleAdProcessed { get; set; }

    public string AsQueryString()
    {
        var fieldsJoined = string.Join(",", Fields);
        var criteria = $"(Created_Time:greater_equal:{FromCreatedTime})and(Score:greater_equal:{MinScore})and(GCLID:not_equal:null)and(GoogleAdProcessed:equals:{GoogleAdProcessed.ToString().ToLower()})";
        return $"fields={fieldsJoined}&per_page={PageSize}&page={Page}&criteria={criteria}&sort_by={SortBy}&sort_order={SortOrder}";
    }
}