using Donations.Domain;
using Donations.Infrastructure;

namespace Donations.Application;

public class CreateRequestCommandHandler : ICommandHandler<CreateRequestCommand, Guid>
{
    private ApplicationDbContext _context;

    public CreateRequestCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> HandleAsync(CreateRequestCommand command)
    {
        var donationRequest = new DonationRequest();
        donationRequest.Id = new Guid();
        donationRequest.UserLogin = command.UserLogin;
        donationRequest.SubmittedAt = DateTime.Now;
        donationRequest.RequestedAmount = command.RequestedAmount;
        donationRequest.RequestedCurrency = command.RequestedCurrency;

        _context.Requests.Add(donationRequest);
        _context.SaveChanges();

        return donationRequest.Id;
    }
}
