﻿using CrossCutting.DopplerSapService.Entities;
using System.Collections.Generic;

namespace Doppler.Billing.Job.Entities
{
    public class UserBilling
    {
        public int Id { get; set; }
        public int PlanType { get; set; }
        public int CreditsOrSubscribersQuantity { get; set; }
        public bool IsCustomPlan { get; set; }
        public bool IsPlanUpgrade { get; set; }
        public int? Currency { get; set; }
        public int? Periodicity { get; set; }
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public double PlanFee { get; set; }
        public int? Discount { get; set; }
        public int? ExtraEmails { get; set; }
        public double? ExtraEmailsFeePerUnit { get; set; }
        public int ExtraEmailsPeriodMonth { get; set; }
        public int ExtraEmailsPeriodYear { get; set; }
        public double ExtraEmailsFee { get; set; }
        public string PurchaseOrder { get; set; }
        public string FiscalID { get; set; }
        public int BillingSystemId { get; set; } = 9;
        public double LandingsAmount { get; set; }
        public double ConversationsAmount { get; set; }
        public string ConversationsExtra { get; set; }
        public string ConversationsExtraMonth { get; set; }
        public string ConversationsExtraAmount { get; set; }
        public double PrintsAmount { get; set; }
        public string PrintsExtra { get; set; }
        public string PrintsExtraMonth { get; set; }
        public string PrintsExtraAmount { get; set; }
        public double PushNotificationsAmount { get; set; }
        public string PushNotificationsExtra { get; set; }
        public string PushNotificationsExtraMonth { get; set; }
        public string PushNotificationsExtraAmount { get; set; }
    }
}
