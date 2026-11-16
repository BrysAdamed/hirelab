using Microsoft.Extensions.Options;

namespace HireLab.Modules.Positions.Attachments.OrphanSweep;

internal sealed class OrphanAttachmentSweepOptionsValidator : IValidateOptions<OrphanAttachmentSweepOptions>
{
    private const string Section = OrphanAttachmentSweepOptions.Section;

    public ValidateOptionsResult Validate(string? name, OrphanAttachmentSweepOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        if (options.Interval < OrphanAttachmentSweepOptions.MinInterval)
        {
            failures.Add($"{Section}:{nameof(options.Interval)} must be at least {OrphanAttachmentSweepOptions.MinInterval}.");
        }

        if (options.GracePeriod < OrphanAttachmentSweepOptions.MinGracePeriod)
        {
            failures.Add(
                $"{Section}:{nameof(options.GracePeriod)} must be at least {OrphanAttachmentSweepOptions.MinGracePeriod}, so an upload whose row isn't saved yet keeps its blob.");
        }

        if (options.PageSize is < 1 or > OrphanAttachmentSweepOptions.MaxPageSize)
        {
            failures.Add($"{Section}:{nameof(options.PageSize)} must be between 1 and {OrphanAttachmentSweepOptions.MaxPageSize}.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
