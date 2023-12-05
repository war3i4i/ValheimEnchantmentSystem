using Backpacks;
using ItemDataManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem;

[VES_Autoload]
public static class Other_Mods_APIs
{
    public static bool AUGA;

    public static void Start()
    {
        AUGA = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("Auga");
    }
    
    [UsedImplicitly]
    private static void OnInit()
    {
        if (ValheimEnchantmentSystem.NoGraphics) return;
        API.EquippedBackpackUpdate += Backpacks_API_EquippedBackpackUpdate;
        Jewelcrafting.API.OnItemMirrored(OnItemMirror);
    }

    private static void Backpacks_API_EquippedBackpackUpdate(Player obj)
    {
        if (!obj.m_nview.IsValid()) return;
        string color = obj.m_nview.m_zdo.GetString("API_Backpacks_Color", "");
        if (string.IsNullOrEmpty(color)) return;
        List<GameObject> visuals = API.GetBackpackVisual(obj);
        if (visuals == null || visuals.Count == 0) return;
        Color c = color.ToColorAlpha();
        int variant = obj.m_nview.m_zdo.GetInt("API_Backpacks_Color_variant");
        visuals.ForEach(v => Enchantment_VFX.AttachMeshEffect(v, c, variant, true));
    }

    [HarmonyPatch(typeof(Humanoid),nameof(Humanoid.EquipItem))]
    [ClientOnlyPatch]
    private static class Humanoid_EquipItem_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, bool __result)
        {
            if (item == null) return;
            if (__result && item == API.GetEquippedBackpack())
            {
                if (item.Data().Get<Enchantment_Core.Enchanted>() is { level: > 0 } en)
                {
                    var color = SyncedData.GetColor(en, out int variant, false);
                    __instance.m_nview.m_zdo.Set("API_Backpacks_Color", color);
                    __instance.m_nview.m_zdo.Set("API_Backpacks_Color_variant", variant);
                }
                else
                {
                    __instance.m_nview.m_zdo.Set("API_Backpacks_Color", "");
                }
            }
        }
    }
    [HarmonyPatch(typeof(Humanoid),nameof(Humanoid.UnequipItem))]
    [ClientOnlyPatch]
    private static class Humanoid_UnequipItem_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Humanoid __instance, ItemDrop.ItemData item)
        {
            if (item == null) return;
            if (item == API.GetEquippedBackpack())
            {
                __instance.m_nview.m_zdo.Set("API_Backpacks_Color", "");
            }
        }
    }
    
    public static void ApplyAPIs(Enchantment_Core.Enchanted item)
    {
        Backpacks_API(item);
    }

    public static void ApplyAPIs_Upgraded(Enchantment_Core.Enchanted item)
    {
        Backpacks_API(item);
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
    
    private static bool OnItemMirror(ItemDrop.ItemData item)
    {
        const string key = "kg.ValheimEnchantmentSystem#kg.ValheimEnchantmentSystem.Enchantment_Core+Enchanted";
        if (!SyncedData.AllowJewelcraftingMirrorCopyEnchant.Value)
        {
            item.m_customData.Remove(key);
        }

        return true;
    }

    private static void Backpacks_API(Enchantment_Core.Enchanted item)
    {
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
}