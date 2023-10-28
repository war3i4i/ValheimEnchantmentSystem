﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using ItemDataManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace kg.ValheimEnchantmentSystem;

public static class Enchantment_VFX
{
    private static GameObject HOTBAR_PART;
    private static readonly int TintColor = Shader.PropertyToID("_TintColor");

    private static readonly List<float> INTENSITY = new List<float>
    {
        220f,
        220f,
        1000f,
        10f
    };

    public static readonly List<Material> VFXs = new List<Material>();

    public static void Init()
    {
        VFXs.Add(ValheimEnchantmentSystem._asset.LoadAsset<Material>("Enchantment_VFX_Mat1"));
        VFXs.Add(ValheimEnchantmentSystem._asset.LoadAsset<Material>("Enchantment_VFX_Mat2"));
        VFXs.Add(ValheimEnchantmentSystem._asset.LoadAsset<Material>("Enchantment_VFX_Mat3")); 
        VFXs.Add(ValheimEnchantmentSystem._asset.LoadAsset<Material>("Enchantment_VFX_Mat4"));
        HOTBAR_PART = ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("Enchantment_HotbarPart");
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.SetupVisEquipment))]
    [HarmonyPriority(-10000)]
    private static class Humanoid_SetupVisEquipment_Patch
    {
        private static void InsertColor(Player p, string key, string color, int variant)
        {
            p.m_nview.m_zdo.Set(key, color);
            p.m_nview.m_zdo.Set(key + "_variant", variant);
        }

        private static string GetEnchantmentColor(ItemDrop.ItemData item, out int variant, bool trimAlpha = false)
        {
            variant = 0;
            if (item?.Data().Get<Enchantment_Core.Enchanted>() is { level: > 0 } en)
            {
                return SyncedData.GetColor(en, out variant, trimAlpha);
            }

            return "";
        }

        [HarmonyPriority(-6000)]
        public static void Postfix(Humanoid __instance, VisEquipment visEq, bool isRagdoll)
        {
            Player p = ZNetScene.instance ? Player.m_localPlayer : __instance as Player;
            bool zns = ZNetScene.instance;
            if (__instance != p || isRagdoll || !zns) return;
            InsertColor(p, "VEX_leftitemColor", GetEnchantmentColor(p!.m_leftItem, out int variantli), variantli);
            InsertColor(p, "VEX_rightitemColor", GetEnchantmentColor(p!.m_rightItem, out int variantri), variantri);
            InsertColor(p, "VEX_leftbackitemColor", GetEnchantmentColor(p!.m_hiddenLeftItem, out int variantlib), variantlib); 
            InsertColor(p, "VEX_rightbackitemColor", GetEnchantmentColor(p!.m_hiddenRightItem, out int variantrib), variantrib);
        }
    }
    
    private static void AttachMeshEffect(GameObject item, Color c, int variant)
    {
        Light l = item.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = c;
        l.intensity *= 2.5f * c.a;
        l.range = 9f;
        List<Renderer> renderers = item.GetComponentsInChildren<SkinnedMeshRenderer>(true).Cast<Renderer>().Concat(item.GetComponentsInChildren<MeshRenderer>(true)).ToList();
        foreach (var renderer in renderers)
        {
            List<Material> list = renderer.sharedMaterials.ToList();
            list.Add(VFXs[variant]);
            renderer.sharedMaterials = list.ToArray();
        }
        foreach (var material in renderers.SelectMany(m => m.materials))
                if (material.name.Contains("Enchantment_VFX_Mat"))
                    material.SetColor(TintColor, c * INTENSITY[variant]);
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetLeftHandEquipped))]
    private static class MockLeft
    {
        private static bool Transfer;

        [UsedImplicitly]
        static void Prefix(VisEquipment __instance, int hash)
        {
            if (__instance.m_currentLeftItemHash != hash)
            {
                Transfer = true;
            }
        }

        [UsedImplicitly]
        private static void Postfix(VisEquipment __instance)
        {
            if (!Transfer) return;
            Transfer = false;

            if (!__instance.m_nview || __instance.m_nview.m_zdo == null) return;
            if (__instance.m_leftItemInstance)
            {
                string leftColor = __instance.m_nview.m_zdo.GetString("VEX_leftitemColor");
                if (!string.IsNullOrEmpty(leftColor))
                {
                    Color c = leftColor.ToColorAlpha();
                    int variant = __instance.m_nview.m_zdo.GetInt("VEX_leftitemColor_variant");
                    AttachMeshEffect(__instance.m_leftItemInstance, c, variant);
                }
            }
        }
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetRightHandEquipped))]
    private static class MockRight
    {
        private static bool Transfer;

        [UsedImplicitly]
        static void Prefix(VisEquipment __instance, int hash)
        {
            if (__instance.m_currentRightItemHash != hash)
            {
                Transfer = true;
            }
        }

        [UsedImplicitly]
        private static void Postfix(VisEquipment __instance)
        {
            if (!Transfer) return;
            Transfer = false;

            if (!__instance.m_nview || __instance.m_nview.m_zdo == null) return;
            if (__instance.m_rightItemInstance)
            {
                string rightColor = __instance.m_nview.m_zdo.GetString("VEX_rightitemColor");
                if (!string.IsNullOrEmpty(rightColor))
                {
                    Color c = rightColor.ToColorAlpha();
                    int variant = __instance.m_nview.m_zdo.GetInt("VEX_rightitemColor_variant");
                    AttachMeshEffect(__instance.m_rightItemInstance, c, variant);
                }
            }
        }
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetLeftBackItem))]
    private static class MockLeftBack
    {
        private static bool Transfer;

        [UsedImplicitly]
        static void Prefix(VisEquipment __instance, string name)
        {
            if (__instance.m_leftBackItem != name)
            {
                Transfer = true;
            }
        }

        [UsedImplicitly]
        private static void Postfix(VisEquipment __instance)
        {
            if (!Transfer) return;
            Transfer = false;

            if (!__instance.m_nview || __instance.m_nview.m_zdo == null) return;
            if (__instance.m_leftBackItemInstance)
            {
                string leftColor = __instance.m_nview.m_zdo.GetString("VEX_leftbackitemColor");
                if (!string.IsNullOrEmpty(leftColor))
                {
                    Color c = leftColor.ToColorAlpha();
                    int variant = __instance.m_nview.m_zdo.GetInt("VEX_leftbackitemColor_variant");
                    AttachMeshEffect(__instance.m_leftBackItemInstance, c, variant);
                }
            }
        }
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetRightBackItem))]
    private static class MockRightBack
    {
        private static bool Transfer;

        [UsedImplicitly]
        static void Prefix(VisEquipment __instance, string name)
        {
            if (__instance.m_rightBackItem != name)
            {
                Transfer = true;
            }
        }

        [UsedImplicitly]
        private static void Postfix(VisEquipment __instance)
        {
            if (!Transfer) return;
            Transfer = false;
            if (!__instance.m_nview || __instance.m_nview.m_zdo == null) return;
            if (__instance.m_rightBackItemInstance)
            {
                string leftColor = __instance.m_nview.m_zdo.GetString("VEX_rightbackitemColor");
                if (!string.IsNullOrEmpty(leftColor))
                {
                    Color c = leftColor.ToColorAlpha();
                    int variant = __instance.m_nview.m_zdo.GetInt("VEX_rightbackitemColor_variant");
                    AttachMeshEffect(__instance.m_rightBackItemInstance, c, variant);
                }
            }
        }
    }
    
    [HarmonyPatch(typeof(ItemDrop),nameof(ItemDrop.Start))]
    private static class ItemDrop_Start_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ItemDrop __instance)
        {
            if(__instance.m_itemData.Data()?.Get<Enchantment_Core.Enchanted>() is not { level: > 0 } en) return;
            string prefabName = global::Utils.GetPrefabName(__instance.gameObject);
            string color = SyncedData.GetColor(prefabName, en.level, out int variant, false);
            AttachMeshEffect(__instance.gameObject, color.ToColorAlpha(), variant);
        }
    }
    
    [HarmonyPatch(typeof(ItemStand),nameof(ItemStand.SetVisualItem))]
    private static class ItemStand_SetVisualItem_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ItemStand __instance)
        {
            var visualItem = __instance.m_visualItem;
            if (!visualItem) return;
            
            string itemPrefab = __instance.m_nview.GetZDO().GetString(ZDOVars.s_item);
            GameObject prefab = ZNetScene.instance.GetPrefab(itemPrefab);
            if(!prefab) return;
            ItemDrop.ItemData itemData = prefab.GetComponent<ItemDrop>().m_itemData.Clone();
            ItemDrop.LoadFromZDO(itemData, __instance.m_nview.m_zdo);
            if(itemData.Data()?.Get<Enchantment_Core.Enchanted>() is not {level: > 0} en) return;
            string color = SyncedData.GetColor(itemPrefab, en.level, out int variant, false);
            AttachMeshEffect(visualItem, color.ToColorAlpha(), variant);
        }
    }
    

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    private static class FejdStartup_Awake_Patch
    {
        private static bool done;

        [UsedImplicitly]
        private static void Postfix(FejdStartup __instance)
        {
            if (done) return;
            done = true;
            if (__instance.transform.Find("StartGame/Panel/JoinPanel/serverCount")
                    ?.GetComponent<TextMeshProUGUI>() is not { } vanilla) return;
            var tmp = HOTBAR_PART.GetComponent<TextMeshProUGUI>();
            tmp.font = vanilla.font;
            AccessTools.Field(typeof(TextMeshProUGUI), "m_canvasRenderer")
                .SetValue(tmp, tmp.GetComponent<CanvasRenderer>());
            tmp.outlineWidth = 0.15f;
        }
    }


    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.Awake))]
    public static class InventoryGrid_Awake_Patch
    {
        [UsedImplicitly]
        public static void Postfix(InventoryGrid __instance)
        {
            if (!__instance.m_elementPrefab) return;
            Transform transform = __instance.m_elementPrefab.transform;
            GameObject newIcon = Object.Instantiate(HOTBAR_PART);
            newIcon!.transform.SetParent(transform);
            newIcon.name = "VES_Level";
            newIcon.GetComponent<RectTransform>().anchoredPosition = new Vector2(-20, 0);
            newIcon.gameObject.SetActive(false);
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    private static class Game_Awake_Patch_Transmog
    {
        [UsedImplicitly] private static void Postfix() => HotkeyBar_UpdateIcons_Patch.FirstInit = false;
    }


    [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons))]
    private static class HotkeyBar_UpdateIcons_Patch
    {
        public static bool FirstInit;
        public static int _needUpdateFrame = -1;

        [UsedImplicitly]
        public static void Postfix(HotkeyBar __instance)
        {
            if (__instance.gameObject.name != "HotKeyBar") return;
            if (!FirstInit)
            {
                FirstInit = true;
                Transform transform = __instance.m_elementPrefab.transform;
                GameObject newIcon = Object.Instantiate(HOTBAR_PART);
                newIcon!.transform.SetParent(transform);
                newIcon.name = "VES_Level";
                newIcon.GetComponent<RectTransform>().anchoredPosition = new Vector2(-20, 0);
                newIcon.gameObject.SetActive(false);
            }
            if (!Player.m_localPlayer || Player.m_localPlayer.IsDead()) return;
            if (_needUpdateFrame != Time.frameCount) return;
            foreach (HotkeyBar.ElementData element in __instance.m_elements.Where(element => !element.m_used))
            {
                element.m_go.transform.Find("VES_Level").gameObject.SetActive(false);
            }

            foreach (var itemData in __instance.m_items)
            {
                HotkeyBar.ElementData element = __instance.m_elements[itemData.m_gridPos.x];
                Transform ves = element.m_go.transform.Find("VES_Level");
                Enchantment_Core.Enchanted en = itemData.Data().Get<Enchantment_Core.Enchanted>();
                if (en && en!.level > 0)
                {
                    ves.gameObject.SetActive(true);
                    string color = SyncedData.GetColor(en, out _, true).IncreaseColorLight();
                    ves.GetComponent<TMP_Text>().text = $"<color={color}>+" + en!.level + "</color>";
                }
                else
                {
                    ves.gameObject.SetActive(false);
                }
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
    private static class InventoryGrid_UpdateGui_Patch
    {
        public static int _needUpdateFrame = -1;

        [UsedImplicitly]
        public static void Postfix(InventoryGrid __instance)
        {
            if (_needUpdateFrame != Time.frameCount) return;
            int width = __instance.m_inventory.GetWidth();
            foreach (InventoryGrid.Element element in __instance.m_elements)
            {
                if (!element.m_used) element.m_go.transform.Find("VES_Level").gameObject.SetActive(false);
            }

            foreach (ItemDrop.ItemData itemData in __instance.m_inventory.GetAllItems())
            {
                InventoryGrid.Element element = __instance.GetElement(itemData.m_gridPos.x, itemData.m_gridPos.y, width);
                Transform ves = element.m_go.transform.Find("VES_Level");
                Enchantment_Core.Enchanted en = itemData.Data().Get<Enchantment_Core.Enchanted>();
                if (en && en.level > 0)
                {
                    ves.gameObject.SetActive(true);
                    string color = SyncedData.GetColor(en, out _, true).IncreaseColorLight();
                    ves.GetComponent<TMP_Text>().text = $"<color={color}>+" + en!.level + "</color>";
                }
                else
                {
                    ves.gameObject.SetActive(false);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.Changed))]
    private static class EventsToUpdate
    {
        [UsedImplicitly]
        private static void Postfix(Inventory __instance)
        {
            if(__instance != Player.m_localPlayer?.m_inventory) return;
            HotkeyBar_UpdateIcons_Patch._needUpdateFrame = Time.frameCount + 1;
            InventoryGrid_UpdateGui_Patch._needUpdateFrame = Time.frameCount + 1;
        }
    }
    [HarmonyPatch]
    private static class EventsToUpdate_Bulk1
    {
        [UsedImplicitly]
        private static IEnumerable<MethodInfo> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Humanoid), nameof(Humanoid.EquipItem));
            yield return AccessTools.Method(typeof(Humanoid), nameof(Humanoid.UnequipItem));
        }
        [UsedImplicitly]
        private static void Postfix(Humanoid __instance)
        {
            if(__instance != Player.m_localPlayer) return;
            HotkeyBar_UpdateIcons_Patch._needUpdateFrame = Time.frameCount + 1;
            InventoryGrid_UpdateGui_Patch._needUpdateFrame = Time.frameCount + 1;
        }
    }
    [HarmonyPatch]
    private static class EventsToUpdate_Bulk2
    {
        [UsedImplicitly]
        private static IEnumerable<MethodInfo> TargetMethods()
        {
            yield return AccessTools.Method(typeof(InventoryGui), nameof(InventoryGui.Show));
        }
        [UsedImplicitly]
        private static void Postfix()
        {
            HotkeyBar_UpdateIcons_Patch._needUpdateFrame = Time.frameCount + 1;
            InventoryGrid_UpdateGui_Patch._needUpdateFrame = Time.frameCount + 1;
        }
    }
}