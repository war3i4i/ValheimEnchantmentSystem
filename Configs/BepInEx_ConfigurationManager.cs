using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem.Configs;

[VES_Autoload]
public static class BepInEx_ConfigurationManager
{
    private static Type activator;

    [UsedImplicitly]
    private static void OnInit()
    {
        Type find = Type.GetType("ConfigurationManager.SettingSearcher, ConfigurationManager");
        if (find == null) return;
        MethodInfo method = AccessTools.Method(find, "GetPluginConfig");
        if (method == null) return;
        activator = Type.GetType("ConfigurationManager.ConfigSettingEntry, ConfigurationManager");
        if (activator == null) return;
        ValheimEnchantmentSystem.Harmony.Patch(method, postfix: new HarmonyMethod(typeof(BepInEx_ConfigurationManager), nameof(Modify)));
    }

    private static void Modify(BaseUnityPlugin plugin, ref IEnumerable<object> __result)
    {
        if (plugin != ValheimEnchantmentSystem._thistype) return;
        IEnumerable<object> syncedConfig = ValheimEnchantmentSystem.SyncedConfig.Select(kvp => Activator.CreateInstance(activator, kvp.Value, plugin));
        IEnumerable<object> itemConfig = ValheimEnchantmentSystem.ItemConfig.Select(kvp => Activator.CreateInstance(activator, kvp.Value, plugin));
        __result = syncedConfig.Concat(itemConfig);
    }
}