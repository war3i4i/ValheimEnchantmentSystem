using ItemDataManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;
using TMPro;
using Object = UnityEngine.Object;

namespace kg.ValheimEnchantmentSystem;

[VES_Autoload(VES_Autoload.Priority.Normal)]
public static class Enchantment_VFX 
{
    private static GameObject HOTBAR_PART;
    private static readonly int TintColor = Shader.PropertyToID("_TintColor");

    private static readonly List<float> INTENSITY = new List<float> { 220f, 80f, 1000f, 10f };

    public static readonly List<Material> VFXs = new List<Material>();

    public static ConfigEntry<bool> _enableHotbarVisual;
    public static ConfigEntry<bool> _enableInventoryVisual;
    public static ConfigEntry<bool> _enableMainVFX;
    
    [UsedImplicitly]
    private static void OnInit()
    {
        VFXs.Add(ValheimEnchantmentSystem._asset.LoadAsset<Material>("Enchantment_VFX_Mat1"));
        VFXs.Add(ValheimEnchantmentSystem._asset.LoadAsset<Material>("Enchantment_VFX_Mat2"));
        VFXs.Add(ValheimEnchantmentSystem._asset.LoadAsset<Material>("Enchantment_VFX_Mat3")); 
        VFXs.Add(ValheimEnchantmentSystem._asset.LoadAsset<Material>("Enchantment_VFX_Mat4"));
        HOTBAR_PART = ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("Enchantment_HotbarPart");
        _enableHotbarVisual = ValheimEnchantmentSystem._thistype.Config.Bind("Visual", "EnableHotbarVisual", true, "Enable hotbar visual");
        _enableInventoryVisual = ValheimEnchantmentSystem._thistype.Config.Bind("Visual", "EnableInventoryVisual", true, "Enable inventory visual");
        _enableMainVFX = ValheimEnchantmentSystem._thistype.Config.Bind("Visual", "EnableMainVFX", true, "Enable main VFX");
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.SetupVisEquipment))]
    [ClientOnlyPatch]
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
            InsertColor(p, "VES_leftitemColor", GetEnchantmentColor(p!.m_leftItem, out int variantli), variantli);
            InsertColor(p, "VES_rightitemColor", GetEnchantmentColor(p!.m_rightItem, out int variantri), variantri);
            InsertColor(p, "VES_leftbackitemColor", GetEnchantmentColor(p!.m_hiddenLeftItem, out int variantlib), variantlib); 
            InsertColor(p, "VES_rightbackitemColor", GetEnchantmentColor(p!.m_hiddenRightItem, out int variantrib), variantrib);
            InsertColor(p, "VES_chestitemColor", GetEnchantmentColor(p!.m_chestItem, out int variantrch), variantrch);
            InsertColor(p, "VES_legsitemColor", GetEnchantmentColor(p!.m_legItem, out int variantrle), variantrle);
            InsertColor(p, "VES_helmetitemColor", GetEnchantmentColor(p!.m_helmetItem, out int variantrhe), variantrhe);
            InsertColor(p, "VES_shoulderitemColor", GetEnchantmentColor(p!.m_shoulderItem, out int variantrsh), variantrsh);
        }
    }
    
    private static void AttachMeshEffect(GameObject item, Color c, int variant, bool isArmor = false)
    {
        if (!_enableMainVFX.Value) return;
        if (!isArmor)
        {
            Light l = item.AddComponent<Light>();
            if (l)
            {
                l.type = LightType.Point;
                l.color = c;
                l.intensity *= 2.5f * c.a;
                l.range = 9f;
            }
        }
     
        List<Renderer> renderers = item.GetComponentsInChildren<SkinnedMeshRenderer>(true).Cast<Renderer>().Concat(item.GetComponentsInChildren<MeshRenderer>(true)).ToList();
        foreach (var renderer in renderers)
        {
            List<Material> list = renderer.sharedMaterials.ToList();
            list.Add(VFXs[variant]);
            renderer.sharedMaterials = list.ToArray();
        }
        foreach (var material in renderers.SelectMany(m => m.materials))
                if (material.name.Contains("Enchantment_VFX_Mat"))
                    material.SetColor(TintColor, isArmor ? c : c * INTENSITY[variant]);
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetLeftHandEquipped))]
    [ClientOnlyPatch]
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
            if (!__instance.m_leftItemInstance) return;
            string leftColor = __instance.m_nview.m_zdo.GetString("VES_leftitemColor");
            if (string.IsNullOrEmpty(leftColor)) return;
            Color c = leftColor.ToColorAlpha();
            int variant = __instance.m_nview.m_zdo.GetInt("VES_leftitemColor_variant");
            AttachMeshEffect(__instance.m_leftItemInstance, c, variant);
        }
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetRightHandEquipped))]
    [ClientOnlyPatch]
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
            if (!__instance.m_rightItemInstance) return;
            string rightColor = __instance.m_nview.m_zdo.GetString("VES_rightitemColor");
            if (string.IsNullOrEmpty(rightColor)) return;
            Color c = rightColor.ToColorAlpha();
            int variant = __instance.m_nview.m_zdo.GetInt("VES_rightitemColor_variant");
            AttachMeshEffect(__instance.m_rightItemInstance, c, variant);
        }
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetBackEquipped))]
    [ClientOnlyPatch]
    private static class MockLeftBack
    {
        private static bool Transfer;

        [UsedImplicitly]
        private static void Prefix(VisEquipment __instance, int leftItem, int rightItem, int leftVariant)
        {
            if (__instance.m_currentLeftBackItemHash != leftItem || __instance.m_currentRightBackItemHash != rightItem)
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
                string leftColor = __instance.m_nview.m_zdo.GetString("VES_leftbackitemColor");
                if (!string.IsNullOrEmpty(leftColor))
                {
                    Color c = leftColor.ToColorAlpha();
                    int variant = __instance.m_nview.m_zdo.GetInt("VES_leftbackitemColor_variant");
                    AttachMeshEffect(__instance.m_leftBackItemInstance, c, variant);
                }
            }

            if (__instance.m_rightBackItemInstance)
            {
                string rightColor = __instance.m_nview.m_zdo.GetString("VES_rightbackitemColor");
                if (!string.IsNullOrEmpty(rightColor))
                {
                    Color c = rightColor.ToColorAlpha();
                    int variant = __instance.m_nview.m_zdo.GetInt("VES_rightbackitemColor_variant");
                    AttachMeshEffect(__instance.m_rightBackItemInstance, c, variant);
                }
            }
        }
    }

    
    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetChestEquipped))]
    [ClientOnlyPatch]
    private static class MockChest
    {
        private static bool Transfer;

        [UsedImplicitly]
        static void Prefix(VisEquipment __instance, int hash)
        {
            if (__instance.m_currentChestItemHash != hash && SyncedData.AllowVFXArmor.Value)
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
            if (__instance.m_chestItemInstances == null) return;
            string chestColor = __instance.m_nview.m_zdo.GetString("VES_chestitemColor");
            if (string.IsNullOrEmpty(chestColor)) return;
            Color c = chestColor.ToColorAlpha();
            int variant = __instance.m_nview.m_zdo.GetInt("VES_chestitemColor_variant");
            foreach (var itemInstance in __instance.m_chestItemInstances)
            {
                AttachMeshEffect(itemInstance, c, variant, true);
            }
        }
    }
    
    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetLegEquipped))]
    [ClientOnlyPatch]
    private static class MockLegs
    {
        private static bool Transfer;

        [UsedImplicitly]
        static void Prefix(VisEquipment __instance, int hash)
        {
            if (__instance.m_currentLegItemHash != hash && SyncedData.AllowVFXArmor.Value)
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
            if (__instance.m_legItemInstances == null) return;
            string legsColor = __instance.m_nview.m_zdo.GetString("VES_legsitemColor");
            if (string.IsNullOrEmpty(legsColor)) return;
            Color c = legsColor.ToColorAlpha();
            int variant = __instance.m_nview.m_zdo.GetInt("VES_legsitemColor_variant");
            foreach (var itemInstance in __instance.m_legItemInstances)
            {
                AttachMeshEffect(itemInstance, c, variant, true);
            }
        }
    }
    
    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetShoulderEquipped))]
    [ClientOnlyPatch]
    private static class MockShoulder
    {
        private static bool Transfer;

        [UsedImplicitly]
        static void Prefix(VisEquipment __instance, int hash)
        {
            if (__instance.m_currentShoulderItemHash != hash && SyncedData.AllowVFXArmor.Value)
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
            if (__instance.m_shoulderItemInstances == null) return;
            string shoulderColor = __instance.m_nview.m_zdo.GetString("VES_shoulderitemColor");
            if (string.IsNullOrEmpty(shoulderColor)) return;
            Color c = shoulderColor.ToColorAlpha();
            int variant = __instance.m_nview.m_zdo.GetInt("VES_shoulderitemColor_variant");
            foreach (var itemInstance in __instance.m_shoulderItemInstances)
            {
                AttachMeshEffect(itemInstance, c, variant, true);
            }
        }
    }
    
    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetHelmetEquipped))]
    [ClientOnlyPatch]
    private static class MockHelmet
    {
        private static bool Transfer;

        [UsedImplicitly]
        static void Prefix(VisEquipment __instance, int hash)
        {
            if (__instance.m_currentHelmetItemHash != hash && SyncedData.AllowVFXArmor.Value)
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
            if (__instance.m_helmetItemInstance == null) return;
            string helmetColor = __instance.m_nview.m_zdo.GetString("VES_helmetitemColor");
            if (string.IsNullOrEmpty(helmetColor)) return;
            Color c = helmetColor.ToColorAlpha();
            int variant = __instance.m_nview.m_zdo.GetInt("VES_helmetitemColor_variant");
            AttachMeshEffect(__instance.m_helmetItemInstance, c, variant, true);
        }
    }
    
    [HarmonyPatch(typeof(ItemDrop),nameof(ItemDrop.Start))]
    [ClientOnlyPatch]
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
    [ClientOnlyPatch]
    private static class ItemStand_SetVisualItem_Patch
    {
        [UsedImplicitly]
        private static void Prefix(ItemStand __instance,out bool __state, string itemName, int variant)
        {
            if (__instance.m_visualName == itemName && __instance.m_visualVariant == variant)
            {
                __state = false;
            }
            else
            {
                __state = true;
            }
        }
        
        [UsedImplicitly]
        private static void Postfix(ItemStand __instance, bool __state)
        {
            if(!__state) return;
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
    [ClientOnlyPatch]
    private static class FejdStartup_Awake_Patch
    {
        private static bool done;

        [UsedImplicitly]
        private static void Postfix(FejdStartup __instance)
        {
            if (done) return;
            done = true;
            if (__instance.transform.Find("StartGame/Panel/JoinPanel/serverCount")?.GetComponent<TextMeshProUGUI>() is not { } vanilla) return;
            var tmp = HOTBAR_PART.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            tmp.font = vanilla.font;
            AccessTools.Field(typeof(TextMeshProUGUI), "m_canvasRenderer").SetValue(tmp, tmp.GetComponent<CanvasRenderer>());
            tmp.outlineWidth = 0.15f;
        }
    }


    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.Awake))]
    [ClientOnlyPatch]
    public static class InventoryGrid_Awake_Patch
    {
        private static HashSet<GameObject> firsttime = new();
        
        [UsedImplicitly]
        public static void Postfix(InventoryGrid __instance)
        {
            if (!__instance.m_elementPrefab) return;
            if (firsttime.Contains(__instance.m_elementPrefab)) return;
            firsttime.Add(__instance.m_elementPrefab);
            Transform transform = __instance.m_elementPrefab.transform;
            GameObject newIcon = Object.Instantiate(HOTBAR_PART);
            newIcon!.transform.SetParent(transform);
            newIcon.name = "VES_Level";
            newIcon.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            newIcon.gameObject.SetActive(false);
        }
    }
    
    [HarmonyPatch(typeof(Hud),nameof(Hud.Awake))]
    [ClientOnlyPatch]
    private static class Hud_Awake_Patch
    {
        private static bool firsttime;
        public static HotkeyBar barRef;
        
        [UsedImplicitly]
        private static void Postfix(Hud __instance)
        {
            HotkeyBar bar = __instance.m_rootObject.transform.Find("HotKeyBar")?.GetComponent<HotkeyBar>();
            barRef = bar;
            if (!bar || firsttime) return;
            firsttime = true;
            Transform transform = bar.m_elementPrefab.transform;
            GameObject newIcon = Object.Instantiate(HOTBAR_PART);
            newIcon!.transform.SetParent(transform);
            newIcon.name = "VES_Level";
            newIcon.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            newIcon.gameObject.SetActive(false);
        }
    }
    
    [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons))]
    [ClientOnlyPatch]
    private static class HotkeyBar_UpdateIcons_Patch
    {
        public static int _needUpdateFrame = -1;

        [UsedImplicitly] 
        public static void Postfix(HotkeyBar __instance)
        {
            if (__instance != Hud_Awake_Patch.barRef) return;
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
                    Color c = SyncedData.GetColor(en, out _, true).ToColorAlpha().IncreaseColorLight();
                    ves.transform.GetChild(0).GetComponent<TMP_Text>().text = "+" + en!.level;
                    ves.transform.GetChild(0).GetComponent<TMP_Text>().color = c;
                    ves.transform.GetChild(1).GetComponent<Image>().color = c;
                    ves.transform.GetChild(1).gameObject.SetActive(_enableHotbarVisual.Value);
                }
                else
                {
                    ves.gameObject.SetActive(false);
                }
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
    [ClientOnlyPatch]
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
                    Color c = SyncedData.GetColor(en, out _, true).ToColorAlpha().IncreaseColorLight();
                    ves.transform.GetChild(0).GetComponent<TMP_Text>().text = "+" + en!.level;
                    ves.transform.GetChild(0).GetComponent<TMP_Text>().color = c;
                    ves.transform.GetChild(1).GetComponent<Image>().color = c;
                    ves.transform.GetChild(1).gameObject.SetActive(_enableInventoryVisual.Value);
                }
                else
                {
                    ves.gameObject.SetActive(false);
                }
            }
        }
    }

    public static void UpdateGrid()
    {
        HotkeyBar_UpdateIcons_Patch._needUpdateFrame = Time.frameCount + 1;
        InventoryGrid_UpdateGui_Patch._needUpdateFrame = Time.frameCount + 1;
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.Changed))]
    [ClientOnlyPatch]
    private static class EventsToUpdate
    {
        [UsedImplicitly]
        private static void Postfix(Inventory __instance)
        {
            if(__instance != Player.m_localPlayer?.m_inventory) return;
            UpdateGrid();
        }
    }
    [HarmonyPatch]
    [ClientOnlyPatch]
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
            UpdateGrid();
        }
    }
    [HarmonyPatch]
    [ClientOnlyPatch]
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
            UpdateGrid();
        }
    }
}