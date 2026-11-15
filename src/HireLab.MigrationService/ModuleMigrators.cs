using HireLab.Modules.Evaluation;
using HireLab.Modules.Identity;
using HireLab.Modules.Positions;
using HireLab.Modules.Sessions;
using HireLab.Modules.TaskBank;
using HireLab.SharedKernel.Modules;

namespace HireLab.MigrationService;

internal static class ModuleMigrators
{
    public static IReadOnlyList<IModuleMigrator> All { get; } =
    [
        new IdentityModule(),
        new PositionsModule(),
        new TaskBankModule(),
        new SessionsModule(),
        new EvaluationModule(),
    ];
}
