namespace HireLab.Modules.Positions.Attachments.OrphanSweep;

/// <summary>Settings of the orphan attachment sweep (ADR 0027), bound from <c>OrphanAttachmentSweep</c>.</summary>
internal sealed class OrphanAttachmentSweepOptions
{
    public const string Section = "OrphanAttachmentSweep";

    public const int MaxPageSize = 5000;

    public static readonly TimeSpan MinInterval = TimeSpan.FromMinutes(1);

    public static readonly TimeSpan MinGracePeriod = TimeSpan.FromMinutes(5);

    public bool Enabled { get; set; } = true;

    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>How old a blob without a row must be before it is deleted, so an upload whose row isn't committed yet is kept.</summary>
    public TimeSpan GracePeriod { get; set; } = TimeSpan.FromHours(1);

    /// <summary>How many blobs are listed and checked against the attachment rows at a time.</summary>
    public int PageSize { get; set; } = 500;
}
