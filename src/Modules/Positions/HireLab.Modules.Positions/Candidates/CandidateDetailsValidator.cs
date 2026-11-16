using System.Text.RegularExpressions;
using FluentValidation;
using HireLab.Modules.Positions.Domain;

namespace HireLab.Modules.Positions.Candidates;

internal sealed partial class CandidateDetailsValidator : AbstractValidator<CandidateDetails>
{
    public CandidateDetailsValidator()
    {
        RuleFor(details => details.FullName)
            .Must(fullName => !string.IsNullOrWhiteSpace(fullName)).WithMessage("Enter the candidate's full name.")
            .MaximumLength(CandidateDetails.FullNameMaxLength);
        RuleFor(details => details.Email)
            .MaximumLength(CandidateDetails.EmailMaxLength)
            .EmailAddress().WithMessage("Enter a valid email address.")
            .When(details => !string.IsNullOrWhiteSpace(details.Email));
        RuleFor(details => details.Phone)
            .MaximumLength(CandidateDetails.PhoneMaxLength)
            .Matches(PhonePattern()).WithMessage("Enter a phone number with digits, spaces and + ( ) - . / only.")
            .When(details => !string.IsNullOrWhiteSpace(details.Phone));
        RuleFor(details => details.Links)
            .NotNull()
            .Must(links => links.Count <= CandidateDetails.MaxLinks)
            .WithMessage($"Add at most {CandidateDetails.MaxLinks} profile links.");
        RuleForEach(details => details.Links)
            .MaximumLength(CandidateDetails.LinkMaxLength)
            .Must(CandidateDetails.IsProfileLink).WithMessage("Enter each profile link as a full http or https address.");
    }

    [GeneratedRegex(@"^\s*[+(]?[0-9][0-9 ()./+-]*\s*$")]
    private static partial Regex PhonePattern();
}
