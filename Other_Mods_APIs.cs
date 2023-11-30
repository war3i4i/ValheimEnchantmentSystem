using Backpacks;
using ItemDataManager;

namespace kg.ValheimEnchantmentSystem;

public static class Other_Mods_APIs
{
    public static void ApplyAPIs(Enchantment_Core.Enchanted item)
    {
        //backpack API
        if (item.Item.Data("org.bepinex.plugins.backpacks") is {} fD && fD.Get<ItemContainer>() is {} iC)
        {
            Vector2i size = iC.GetDefaultContainerSize();
            var stats = item.Stats;
            int addRowX = stats.API_backpacks_additionalrow_x;
            int addRowY = stats.API_backpacks_additionalrow_y;
            if (addRowX <= 0 && addRowY <= 0)
            {
                iC.Resize(size);
                return;
            }
            size.x += addRowX;
            size.y += addRowY;
            iC.Resize(size);
        }
    }

    public static bool CanEnchant(ItemDrop.ItemData item, out string msg)
    {
        msg = "";
        
        if (item.Data("org.bepinex.plugins.backpacks") is {} fD && fD.Get<ItemContainer>() is {} iC && iC.Inventory.m_inventory.Count != 0)
        {
            msg = "Can only enchant empty backpacks";
            return false;
        }
        
        return true;
    }
}