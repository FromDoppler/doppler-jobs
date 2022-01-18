using CrossCutting.Notificacion.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Doppler.Notifications.Job.Database
{
    public interface IDopplerRepository
    {
        public Task<IList<UserNotification>> GetUserWithTrialExpiresInDays(int days);
    }
}
