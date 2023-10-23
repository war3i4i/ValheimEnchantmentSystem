using HarmonyLib;
using ItemDataManager;
using JetBrains.Annotations;

namespace kg.ValheimEnchantmentSystem;

public static class TerminalCommands
{
    [HarmonyPatch(typeof(Terminal),nameof(Terminal.InitTerminal))]
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
                Enchantment.EnchantedItem en = weapon.Data().GetOrCreate<Enchantment.EnchantedItem>();
                en.level = level;
                en.Save();
                Chat.instance.m_hideTimer = 0f;
                Chat.instance.AddString("Enchantment level set to " + level);
                ValheimEnchantmentSystem._thistype.StartCoroutine(Enchantment.FrameSkipEquip(weapon));
            });
        }
    }
}