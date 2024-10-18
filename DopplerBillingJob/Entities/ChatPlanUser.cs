namespace Doppler.Billing.Job.Entities
{
    public class ChatPlanUser
    {
        public int IdUser { get; set; }
        public int IdBillingCredit { get; set; }
        public int IdChatPlan {  get; set; }
        public decimal AdditionalConversation {  get; set; }
        public decimal Fee { get; set; }
        public int ConversationQty {  get; set; }
        public bool IsCustom { get; set; }
    }
}
