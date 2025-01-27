using System;
using System.Threading.Tasks;

namespace CrossCutting.DopplerPopUpHubService
{
    public interface IDopplerPopUpHubService
    {
        public Task<int> GetImpressionsByUserIdAndPeriodAsync(int userId, string email, DateTime dateFrom, DateTime dateTo);
    }
}
