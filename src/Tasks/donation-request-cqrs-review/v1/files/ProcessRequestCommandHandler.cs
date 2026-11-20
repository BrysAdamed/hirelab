using Donations.Infrastructure;

namespace Donations.Application;

public class ProcessRequestCommandHandler : ICommandHandler<ProcessRequestCommand>
{
    private ApplicationDbContext _context;

    public ProcessRequestCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task HandleAsync(ProcessRequestCommand command)
    {
        var donationRequest = _context.Requests.Find(command.RequestId);
        if (donationRequest == null)
        {
            throw new Exception($"Donation request {command.RequestId} does not exist");
        }

        donationRequest.IsAccepted = command.Accepted;
        donationRequest.DecisionDate = DateTime.Now;

        if (command.Accepted)
        {
            donationRequest.ApprovedAmmount = command.ApprovedAmount;
            donationRequest.ApprovedCurrency = command.ApprovedCurrency;
        }

        _context.SaveChanges();
    }
}
