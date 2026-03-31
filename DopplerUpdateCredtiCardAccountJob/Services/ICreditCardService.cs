using System.Threading.Tasks;

namespace Doppler.UpdateCredtiCardAccount.Job.Services;

public interface ICreditCardService
{
    Task SendCurrentCCDataToComerica();
}
