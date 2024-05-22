using CrossCutting.DopplerSapService.Enum;
using System.Collections.Generic;

namespace CrossCutting.DopplerSapService.Entities
{
    public class AdditionalService
    {
        public int? ConversationQty { get; set; }
        public double Charge { get; set; }
        public IList<Pack> Packs { get; set; }
        public AdditionalServiceTypeEnum Type { get; set; }
    }
}
