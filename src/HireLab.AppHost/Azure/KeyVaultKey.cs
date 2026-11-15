using Azure.Provisioning;
using Azure.Provisioning.KeyVault;
using Azure.Provisioning.Primitives;

namespace HireLab.AppHost.Azure;

/// <summary>
/// A key in an Azure Key Vault (<c>Microsoft.KeyVault/vaults/keys</c>).
/// Azure.Provisioning.KeyVault has no type for vault keys, so this declares the few properties we set.
/// </summary>
internal sealed class KeyVaultKey(string bicepIdentifier)
    : ProvisionableResource(bicepIdentifier, "Microsoft.KeyVault/vaults/keys", "2024-11-01")
{
    private BicepValue<string>? _name;
    private BicepValue<string>? _keyType;
    private BicepValue<int>? _keySize;
    private BicepValue<string>? _keyUri;
    private ResourceReference<KeyVaultService>? _parent;

    public BicepValue<string> Name
    {
        get { Initialize(); return _name!; }
        set { Initialize(); _name!.Assign(value); }
    }

    public BicepValue<string> KeyType
    {
        get { Initialize(); return _keyType!; }
        set { Initialize(); _keyType!.Assign(value); }
    }

    public BicepValue<int> KeySize
    {
        get { Initialize(); return _keySize!; }
        set { Initialize(); _keySize!.Assign(value); }
    }

    public BicepValue<string> KeyUri
    {
        get { Initialize(); return _keyUri!; }
    }

    public KeyVaultService? Parent
    {
        get { Initialize(); return _parent!.Value; }
        set { Initialize(); _parent!.Value = value; }
    }

    protected override void DefineProvisionableProperties()
    {
        _name = DefineProperty<string>(nameof(Name), ["name"], isRequired: true);
        _keyType = DefineProperty<string>(nameof(KeyType), ["properties", "kty"]);
        _keySize = DefineProperty<int>(nameof(KeySize), ["properties", "keySize"]);
        _keyUri = DefineProperty<string>(nameof(KeyUri), ["properties", "keyUri"], isOutput: true);
        _parent = DefineResource<KeyVaultService>(nameof(Parent), ["parent"], isRequired: true);
    }
}
