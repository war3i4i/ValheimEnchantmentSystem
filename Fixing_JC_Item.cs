using kg.ValheimEnchantmentSystem.Configs;

namespace kg.ValheimEnchantmentSystem;

public static class Fixing_JC_Item
{
    public static void Fix()
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