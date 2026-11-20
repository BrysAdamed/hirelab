namespace Donations.Application;

public class CreateRequestCommand : ICommand<Guid>
{
    public string UserLogin { get; set; }
    public double RequestedAmount { get; set; }
    public string RequestedCurrency { get; set; }
}
