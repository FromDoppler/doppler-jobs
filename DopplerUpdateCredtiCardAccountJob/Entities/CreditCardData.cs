namespace Doppler.UpdateCredtiCardAccount.Job.Entities;

public class CreditCardData
{
    public long ChainCode { get; set; }
    public string MerchantNumber { get; set; }
    public string Token { get; set; }
    public string ExpiryDate { get; set; }
}
