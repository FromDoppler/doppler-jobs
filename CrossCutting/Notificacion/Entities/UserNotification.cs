using System;

namespace CrossCutting.Notificacion.Entities
{
    public class UserNotification
    { 
        public DateTime TrialExpirationDate { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Language { get; set; }
    }
}
