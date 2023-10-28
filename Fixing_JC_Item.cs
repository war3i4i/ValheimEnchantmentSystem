using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem;

[VES_Autoload]
public static class Fixing_JC_Item
{
    [UsedImplicitly]
    private static void OnInit()
    {
        Jewelcrafting.API.OnItemMirrored(OnItemMirror);
    }

    private static bool OnItemMirror(ItemDrop.ItemData item)
    {
        const string key = "kg.ValheimEnchantmentSystem#kg.ValheimEnchantmentSystem.Enchantment_Core+Enchanted";
        if (!SyncedData.AllowJewelcraftingMirrorCopyEnchant.Value)
        {
            item.m_customData.Remove(key);
        }

        return true;
    }
    
}