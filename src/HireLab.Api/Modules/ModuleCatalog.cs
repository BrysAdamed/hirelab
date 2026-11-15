using HireLab.Modules.Evaluation;
using HireLab.Modules.Identity;
using HireLab.Modules.Positions;
using HireLab.Modules.Sessions;
using HireLab.Modules.TaskBank;
using HireLab.SharedKernel.Modules;

namespace HireLab.Api.Modules;

internal static class ModuleCatalog
{
    public static IReadOnlyList<IModule> All { get; } =
    [
        new IdentityModule(),
        new PositionsModule(),
        new TaskBankModule(),
        new SessionsModule(),
        new EvaluationModule(),
    ];
}
