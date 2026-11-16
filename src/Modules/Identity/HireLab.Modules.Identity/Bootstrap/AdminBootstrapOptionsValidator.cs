using System.Net.Mail;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HireLab.Modules.Identity.Bootstrap;

internal sealed class AdminBootstrapOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<AdminBootstrapOptions>, IValidateOptions<StaffAppOptions>
{
    private const string AdminEmailSetting = $"{AdminBootstrapOptions.Section}:{nameof(AdminBootstrapOptions.Email)}";
    private const string PublicBaseUrlSetting = $"{StaffAppOptions.Section}:{nameof(StaffAppOptions.PublicBaseUrl)}";

    public ValidateOptionsResult Validate(string? name, AdminBootstrapOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Email))
        {
            if (!environment.IsDevelopment())
            {
                failures.Add($"{AdminEmailSetting} is required outside Development: the email the first admin logs in with.");
            }
        }
        else if (!MailAddress.TryCreate(options.Email.Trim(), out _))
        {
            failures.Add($"{AdminEmailSetting} is not a valid email address.");
        }

        if (string.IsNullOrWhiteSpace(options.DisplayName))
        {
            failures.Add($"{AdminBootstrapOptions.Section}:{nameof(AdminBootstrapOptions.DisplayName)} must not be empty.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    public ValidateOptionsResult Validate(string? name, StaffAppOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.PublicBaseUrl switch
        {
            null when environment.IsDevelopment() => ValidateOptionsResult.Success,
            null => ValidateOptionsResult.Fail(
                $"{PublicBaseUrlSetting} is required outside Development: the origin of the staff app, for example https://hirelab.example.com."),
            { IsAbsoluteUri: true, Scheme: "https" or "http", AbsolutePath: "/", Query: "", Fragment: "" } => ValidateOptionsResult.Success,
            _ => ValidateOptionsResult.Fail(
                $"{PublicBaseUrlSetting} must be an absolute http or https origin without a path, for example https://hirelab.example.com."),
        };
    }
}
