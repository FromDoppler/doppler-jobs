using CrossCutting.DopplerSapService.Enum;
using System.Collections.Generic;

namespace CrossCutting.DopplerSapService.Entities
{
    public class AdditionalService
    {
        public int? ConversationQty { get; set; }
        public int? PrintQty { get; set; }
        public double Charge { get; set; }
        public double PlanFee { get; set; }
        public IList<Pack> Packs { get; set; }
        public AdditionalServiceTypeEnum Type { get; set; }
        public int ExtraPeriodMonth { get; set; }
        public int ExtraPeriodYear { get; set; }
        public int ExtraQty { get; set; }
        public double ExtraFee { get; set; }
        public double ExtraFeePerUnit { get; set; }
        public bool IsCustom { get; set; }
        public string UserEmail { get; set; }
        public int? Quantity { get; set; }
    }
}
