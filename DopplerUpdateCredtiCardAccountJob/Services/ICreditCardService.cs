using Doppler.UpdateCredtiCardAccount.Job.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Doppler.UpdateCredtiCardAccount.Job.Services;

public interface ICreditCardService
{
    Task SendCurrentCCDataToComerica();

    Task<EchoValidationResult> VerifyComericaRequestDelivery(string remoteEchoPath, string echoFileNamePrefix);

    List<AccountUpdaterResponseRecord> ProcessAccountUpdaterResponse(string responseFileContent);
}
