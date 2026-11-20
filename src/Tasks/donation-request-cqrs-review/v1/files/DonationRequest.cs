namespace Donations.Domain;

public class DonationRequest
{
    public Guid Id { get; set; }

    public string UserLogin { get; set; }
    public DateTime SubmittedAt { get; set; }
    public double RequestedAmount { get; set; }
    public string RequestedCurrency { get; set; }

    public bool? IsAccepted { get; set; }
    public DateTime? DecisionDate { get; set; }
    public double? ApprovedAmmount { get; set; }
    public string? ApprovedCurrency { get; set; }
}
