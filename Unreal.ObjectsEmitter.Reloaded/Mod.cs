using System.Diagnostics;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using UE.Toolkit.Interfaces;
using Unreal.ObjectsEmitter.Interfaces;
using Unreal.ObjectsEmitter.Reloaded.Configuration;
using Unreal.ObjectsEmitter.Reloaded.Template;
using IDataTables = Unreal.ObjectsEmitter.Interfaces.IDataTables;

namespace Unreal.ObjectsEmitter.Reloaded;

public class Mod : ModBase, IExports
{
    private readonly IModLoader modLoader;
    private readonly IReloadedHooks? hooks;
    private readonly ILogger log;
    private readonly IMod owner;

    private Config config;
    private readonly IModConfig modConfig;

    private readonly ObjectEmitterShim objEmitShim;

    public Mod(ModContext context)
    {
        this.modLoader = context.ModLoader;
        this.hooks = context.Hooks!;
        this.log = context.Logger;
        this.owner = context.Owner;
        this.config = context.Configuration;
        this.modConfig = context.ModConfig;

        Project.Init(this.modConfig, this.modLoader, this.log);
        Log.LogLevel = this.config.LogLevel;

#if DEBUG
        Debugger.Launch();
#endif

        modLoader.GetController<IUnrealMemory>().TryGetTarget(out var mem);
        modLoader.GetController<IUnrealObjects>().TryGetTarget(out var objs);
        modLoader.GetController<IUnrealNames>().TryGetTarget(out var names);
        modLoader.GetController<UE.Toolkit.Interfaces.IDataTables>().TryGetTarget(out var dt);
        
        objEmitShim = new(mem!, objs!, names!, dt!);
        this.modLoader.AddOrReplaceController<IUnreal>(this.owner, this.objEmitShim);
        this.modLoader.AddOrReplaceController<IDataTables>(this.owner, this.objEmitShim);
        this.modLoader.AddOrReplaceController<IUObjects>(this.owner, this.objEmitShim);

        this.ApplyConfig();
        Project.Start();
    }

    private void ApplyConfig() => Log.LogLevel = this.config.LogLevel;

    #region Standard Overrides
    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        config = configuration;
        log.WriteLine($"[{modConfig.ModId}] Config Updated: Applying");
        this.ApplyConfig();
    }

    public Type[] GetTypes() => [typeof(IUnreal), typeof(IDataTables), typeof(IUObjects)];
    #endregion

    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}