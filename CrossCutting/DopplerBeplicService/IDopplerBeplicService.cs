using System;
using System.Threading.Tasks;

namespace CrossCutting.DopplerPopUpHubService
{
    public interface IDopplerBeplicService
    {
        public Task<int> GetConversationsByUserIdAndPeriodAsync(int userId, DateTime dateFrom, DateTime dateTo);
    }
}
