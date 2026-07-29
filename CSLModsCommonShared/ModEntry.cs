using CSLModsCommon.Manager;
using ICities;

namespace CSLModsCommon;

public abstract class ModEntry<TManager> : IUserMod
    where TManager : ModManagerBase, new() {
    private TManager _modManager;
    protected TManager ModManager => _modManager ??= CreateManager();

    public virtual string Name => ModManager.GetModEntryName();
    public virtual string Description => ModManager.GetModEntryDescription();

    protected virtual TManager CreateManager() => Domain.DefaultDomain.GetOrCreateManager<TManager>();

    public virtual void OnEnabled() => ModManager.OnEnabled();

    public virtual void OnDisabled() => ModManager.OnDisabled();

    public virtual void OnSettingsUI(UIHelperBase helper) => ModManager.OnSettingsUI(helper);
}