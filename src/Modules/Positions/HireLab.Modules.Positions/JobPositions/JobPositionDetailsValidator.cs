using FluentValidation;
using HireLab.Modules.Positions.Domain;

namespace HireLab.Modules.Positions.JobPositions;

internal sealed class JobPositionDetailsValidator : AbstractValidator<JobPositionDetails>
{
    public JobPositionDetailsValidator()
    {
        RuleFor(details => details.Title)
            .Must(title => !string.IsNullOrWhiteSpace(title)).WithMessage("Enter a title.")
            .MaximumLength(JobPositionDetails.TitleMaxLength);
        RuleFor(details => details.Requirements)
            .Must(requirements => !string.IsNullOrWhiteSpace(requirements)).WithMessage("Enter the requirements.")
            .MaximumLength(JobPositionDetails.SectionMaxLength);
        RuleFor(details => details.Description).MaximumLength(JobPositionDetails.SectionMaxLength);
        RuleFor(details => details.NiceToHaves).MaximumLength(JobPositionDetails.SectionMaxLength);
        RuleFor(details => details.InterviewerNotes).MaximumLength(JobPositionDetails.SectionMaxLength);
    }
}
