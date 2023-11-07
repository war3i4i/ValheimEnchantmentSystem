using ItemDataManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem;

public static class TerminalCommands
{
    [HarmonyPatch(typeof(Terminal),nameof(Terminal.InitTerminal))]
    [ClientOnlyPatch]
    private static class Terminal_InitTerminal_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Terminal __instance)
        {
            new Terminal.ConsoleCommand("setenchant", "", (args) =>
            {
                if(!Utils.IsDebug_Strict) return;
                int level = int.Parse(args[1]);
                ItemDrop.ItemData weapon = Player.m_localPlayer.GetCurrentWeapon();
                if(weapon == null || !weapon.m_dropPrefab) return;
                Enchantment_Core.Enchanted en = weapon.Data().GetOrCreate<Enchantment_Core.Enchanted>();
                en.level = level;
                en.Save();
                Chat.instance.m_hideTimer = 0f;
                Chat.instance.AddString("Enchantment level set to " + level);
                ValheimEnchantmentSystem._thistype.StartCoroutine(Enchantment_Core.FrameSkipEquip(weapon));
            });
            
            new Terminal.ConsoleCommand("setenchantall", "", (args) =>
            {
                if(!Utils.IsDebug_Strict) return;
                int level = int.Parse(args[1]);

                foreach (var item in Player.m_localPlayer.m_inventory.m_inventory.Where(x => SyncedData.GetReqs(x.m_dropPrefab?.name) != null))
                {
                    Enchantment_Core.Enchanted en = item.Data().GetOrCreate<Enchantment_Core.Enchanted>();
                    en.level = level;
                    en.Save();
                }
            });

            new Terminal.ConsoleCommand("enchantment_hotbar", "", (args) =>
            {
                Enchantment_VFX._enableHotbarVisual.Value = !Enchantment_VFX._enableHotbarVisual.Value;
                Enchantment_VFX.UpdateGrid();
                Enchantment_VFX._enableHotbarVisual.ConfigFile.Save();
                Chat.instance.m_hideTimer = 0f;
                Chat.instance.AddString("Enchantment hotbar visual " + (Enchantment_VFX._enableHotbarVisual.Value ? "<color=green>enabled</color>" : "<color=red>disabled</color>"));
            });
            
            new Terminal.ConsoleCommand("enchantment_mainvfx", "", (args) =>
            {
                Enchantment_VFX._enableMainVFX.Value = !Enchantment_VFX._enableMainVFX.Value;
                Enchantment_VFX._enableMainVFX.ConfigFile.Save();
                Chat.instance.m_hideTimer = 0f;
                Chat.instance.AddString("Enchantment Main VFX " + (Enchantment_VFX._enableMainVFX.Value ? "<color=green>enabled</color>" : "<color=red>disabled</color>"));
            });
            
            new Terminal.ConsoleCommand("enchantment_wingsvfx", "", (args) =>
            {
                Enchantment_AdditionalEffects._enableWingsEffects.Value = ! Enchantment_AdditionalEffects._enableWingsEffects.Value;
                Enchantment_AdditionalEffects._enableWingsEffects.ConfigFile.Save();
                Chat.instance.m_hideTimer = 0f;
                Chat.instance.AddString("Enchantment Wings VFX " + (Enchantment_AdditionalEffects._enableWingsEffects.Value ? "<color=green>enabled</color>" : "<color=red>disabled</color>"));
            });
            
            new Terminal.ConsoleCommand("enchantment_auravfx", "", (args) =>
            {
                Enchantment_AdditionalEffects._enableAuraEffects.Value = ! Enchantment_AdditionalEffects._enableAuraEffects.Value;
                Enchantment_AdditionalEffects._enableAuraEffects.ConfigFile.Save();
                Chat.instance.m_hideTimer = 0f;
                Chat.instance.AddString("Enchantment Aura VFX " + (Enchantment_AdditionalEffects._enableAuraEffects.Value ? "<color=green>enabled</color>" : "<color=red>disabled</color>"));
            });
            
        }
    }
}