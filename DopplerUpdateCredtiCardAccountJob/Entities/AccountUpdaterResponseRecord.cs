namespace Doppler.UpdateCredtiCardAccount.Job.Entities;

public enum ResponseAction
{
    NoChange,
    UpdateTokenAndExpiry,
    UpdateExpiry,
    ContactCardholder
}

public class AccountUpdaterResponseRecord
{
    public string MerchantNumber { get; set; }
    public string OldToken { get; set; }
    public string OldExpiry { get; set; }
    public string NewToken { get; set; }
    public string NewExpiry { get; set; }
    public string ResponseCode { get; set; }
    public ResponseAction Action { get; set; }
}
