using HireLab.Modules.TaskBank.Domain;
using HireLab.SharedKernel.Results;
using HireLab.TaskTypes.Abstractions;

namespace HireLab.Modules.TaskBank.Seeds;

/// <summary>
/// Runs a seed package through the one task version validation (ADR 0029) for its task type. A package of an unknown
/// task type or with invalid details throws <see cref="SeedPackageException"/> listing every broken rule by field.
/// </summary>
internal sealed class SeedPackageValidator(ITaskTypeRegistry taskTypes, TaskVersionValidator validator)
{
    public ValidatedTaskVersion Validate(SeedTaskPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);

        if (taskTypes.Find(package.TaskTypeKey) is null)
        {
            throw new SeedPackageException(
                $"Seed package {package.Name} uses the task type '{package.TaskTypeKey}', which isn't registered.");
        }

        var result = validator.Validate(package.TaskTypeKey, package.Details);
        if (result.IsFailure)
        {
            throw new SeedPackageException($"Seed package {package.Name} is invalid: {Describe(result.Error!)}");
        }

        return result.Value;
    }

    private static string Describe(Error error) =>
        error is ValidationError validation
            ? string.Join("; ", validation.Errors.Select(field => $"{field.Key}: {string.Join(" ", field.Value)}"))
            : error.Description;
}
