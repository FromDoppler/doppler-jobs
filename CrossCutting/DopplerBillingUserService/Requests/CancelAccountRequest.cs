﻿namespace CrossCutting.DopplerBillingUserService.Requests
{
    public class CancelAccountRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public string ContactSchedule { get; set; }
        public string CancellationReason { get; set; }
    }
}
