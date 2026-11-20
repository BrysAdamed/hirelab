namespace Donations.Application;

public class ProcessRequestCommand : ICommand
{
    public Guid RequestId { get; set; }
    public bool Accepted { get; set; }
    public double? ApprovedAmount { get; set; }
    public string? ApprovedCurrency { get; set; }
}
