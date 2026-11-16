namespace HireLab.Modules.Identity.Auth.Login;

internal sealed record LoginCommand(string Email, string Password)
{
    public override string ToString() => nameof(LoginCommand);
}
