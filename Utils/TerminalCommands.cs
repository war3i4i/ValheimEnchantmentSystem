using HarmonyLib;
using ItemDataManager;
using JetBrains.Annotations;
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
        }
    }
}