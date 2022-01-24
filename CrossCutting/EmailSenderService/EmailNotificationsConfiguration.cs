using System.Collections.Generic;

namespace CrossCutting.EmailSenderService
{
    public class EmailNotificationsConfiguration
    {
        public string UrlEmailImagesBase { get; set; }

        public Dictionary<string, string> FreeTrialExpiresIn7DaysNotificationsTemplateId { get; set; }

        public Dictionary<string, string> FreeTrialExpiresTodayNotificationsTemplateId { get; set; }

        public Dictionary<string, string> FreeTrialExpiredNotificationsTemplateId { get; set; }
    }
}
