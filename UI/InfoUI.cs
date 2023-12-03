using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem.UI;

[VES_Autoload(VES_Autoload.Priority.Normal)]
public static class Info_UI
{
    public static bool IsVisible() => UI && UI.activeSelf;

    private static GameObject UI;
    private static GameObject Element;

    private enum Category
    {
        Reqs,
        Stats,
        Chances
    }

    private static readonly GameObject[] _categories = new GameObject[3];
    private static InputField _search;
    private static Transform Content;
    private static Category _currentCategory = Category.Reqs;


    [UsedImplicitly]
    private static void OnInit()
    {
        UI = UnityEngine.Object.Instantiate(
            ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_EnchantmentUI_Info"));
        Element = ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_EnchantmentUI_Info_Element");
        UI.SetActive(false);
        UnityEngine.Object.DontDestroyOnLoad(UI);

        _search = UI.transform.Find("Canvas/Background/Search").GetComponent<InputField>();
        _search.onValueChanged.AddListener(OnSearch);
        _categories[0] = UI.transform.Find("Canvas/Background/Categories/Reqs").gameObject;
        _categories[1] = UI.transform.Find("Canvas/Background/Categories/Stats").gameObject;
        _categories[2] = UI.transform.Find("Canvas/Background/Categories/Chances").gameObject;
        Content = UI.transform.Find("Canvas/Background/Scroll View/Viewport/Content");
        for (int i = 0; i < _categories.Length; i++)
        {
            Category category = (Category)i;
            _categories[i].GetComponent<Button>().onClick.AddListener(() =>
            {
                VES_UI.PlayClick();
                SelectCategory(category);
                CreateElements();
            });
        }

        Localization.instance.Localize(UI.transform);
        Default();
    }

    private static void Default()
    {
        foreach (Transform transform in Content)
            UnityEngine.Object.Destroy(transform.gameObject);
        SelectCategory(Category.Reqs);
        _search.text = "";
    }

    private static void OnSearch(string _)
    {
        CreateElements();
    }

    private static void SelectCategory(Category cat)
    {
        _currentCategory = cat;
        foreach (GameObject category in _categories)
            category.transform.Find("Text").GetComponent<Text>().color = Color.white;
        _categories[(int)cat].transform.Find("Text").GetComponent<Text>().color = Color.yellow;
    }

    private static void CreateElements()
    {
        foreach (Transform transform in Content)
            UnityEngine.Object.Destroy(transform.gameObject);

        switch (_currentCategory)
        {
            case Category.Reqs:
                LoadReqs();
                break;
            case Category.Stats:
                LoadStats();
                break;
            case Category.Chances:
                LoadChances();
                break;
        }
        ForceCanvas();
    }

    private static bool HasAny(IEnumerable<string> list, string search, out string found)
    {
        search = search.ToLower().Replace(" ", "");
        foreach (string prefab in list)
        {
            if (prefab.ToLower().Replace(" ", "").Contains(search))
            {
                found = prefab;
                return true;
            }

            GameObject tryFind = ZNetScene.instance.GetPrefab(prefab);
            if (!tryFind || tryFind.GetComponent<ItemDrop>() is not { } item) continue;
            bool check = item.m_itemData.m_shared.m_name.Localize().Contains(search);
            if (check)
            {
                found = prefab;
                return true;
            }
        }

        found = null;
        return false;
    }

    private static string GenerateReqsText(SyncedData.EnchantmentReqs reqs)
    {
        string result = $"• $enchantment_canbeenchantedwith:";
        if (reqs.enchant_prefab.IsValid())
        {
            string mainName = ZNetScene.instance.GetPrefab(reqs.enchant_prefab.prefab).GetComponent<ItemDrop>()
                .m_itemData.m_shared.m_name;
            int val1 = reqs.enchant_prefab.amount;
            result += $"\n<color=yellow>• {mainName} x{val1}</color>";
        }

        if (reqs.blessed_enchant_prefab.IsValid())
        {
            string blessName = ZNetScene.instance.GetPrefab(reqs.blessed_enchant_prefab.prefab)
                .GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
            int val2 = reqs.blessed_enchant_prefab.amount;
            result += $"\n<color=yellow>• {blessName} x{val2}</color>";
        }

        if (reqs.required_skill > 0)
        {
            result += "\n<color=yellow>• $enchantment_requiresskilllevel</color>".Localize(
                reqs.required_skill.ToString());
        }

        return result.Localize();
    }

    private static string GenerateStatsText(Dictionary<int, SyncedData.Stat_Data> stats)
    {
        string result = "";
        foreach (KeyValuePair<int, SyncedData.Stat_Data> stat in stats.OrderBy(x => x.Key))
        {
            result += $"<color=yellow>• lvl{stat.Key}:</color>";
            result += stat.Value.Info_Description();
        }

        return result.Localize();
    }

    private static string GenerateChancesText(Dictionary<int, SyncedData.Chance_Data> chances)
    {
        string result = "";
        foreach (KeyValuePair<int, SyncedData.Chance_Data> chance in chances.OrderBy(x => x.Key))
        {
            string success = $"{chance.Value.success}";
            string destroy = chance.Value.destroy > 0 ? $", $enchantment_destroychance: {chance.Value.destroy}%".Localize() : "";
            result += $"<color=yellow>• lvl{chance.Key}:</color> {success}%{destroy}\n";
        }
        return result;
    }

    private static GameObject CreateElementWithText(IEnumerable<string> prefabs, string text, string found,
        string additionalText = null)
    {
        if (!Player.m_localPlayer) return null;
        GameObject element = UnityEngine.Object.Instantiate(Element, Content);
        GameObject toInstantiate = element.transform.Find("Items/Icon").gameObject;

        if (prefabs != null)
        {
            foreach (string prefab in prefabs)
            {
                GameObject tryFind = ZNetScene.instance.GetPrefab(prefab);
                if (!tryFind || tryFind.GetComponent<ItemDrop>() is not { } item) continue;
                if (!Player.m_localPlayer.m_knownRecipes.Contains(item.m_itemData.m_shared.m_name) && !Player.m_localPlayer.m_knownMaterial.Contains(item.m_itemData.m_shared.m_name)) continue;
                GameObject icon = UnityEngine.Object.Instantiate(toInstantiate, element.transform.Find("Items"));
                icon.SetActive(true);
                icon.transform.Find("Icon").GetComponent<Image>().sprite = item.m_itemData.GetIcon();
                icon.GetComponent<UITooltip>().m_topic = item.m_itemData.m_shared.m_name.Localize();
                icon.GetComponent<UITooltip>().m_text = item.m_itemData.GetTooltip();

                if (found == prefab)
                    icon.transform.Find("border").GetComponent<Image>().color = Color.green;
            }
        }
        else
        {
            element.transform.Find("Items").gameObject.SetActive(false);
        }

        if (additionalText != null)
        {
            element.transform.Find("ANY").gameObject.SetActive(true);
            element.transform.Find("ANY").GetComponent<Text>().text = additionalText;
        }

        if (additionalText == null && element.transform.Find("Items").childCount <= 1)
        {
            UnityEngine.Object.Destroy(element);
            return null;
        }

        element.transform.Find("Info").GetComponent<Text>().text = text;
        element.transform.Find("Open").GetComponent<Button>().onClick.AddListener(() =>
        {
            VES_UI.PlayClick();
            bool isActive = element.transform.Find("Info").gameObject.activeSelf;
            isActive = !isActive;
            element.transform.Find("Info").gameObject.SetActive(isActive);
            if (isActive)
            {
                element.transform.Find("Open/open").gameObject.SetActive(false);
                element.transform.Find("Open/close").gameObject.SetActive(true);
                element.transform.Find("Info").GetComponent<Text>().gameObject.SetActive(true);
            }
            else
            {
                element.transform.Find("Open/open").gameObject.SetActive(true);
                element.transform.Find("Open/close").gameObject.SetActive(false);
                element.transform.Find("Info").GetComponent<Text>().gameObject.SetActive(false);
            }

            ForceCanvas();
        });

        return element;
    }

    private static void LoadReqs()
    {
        List<SyncedData.EnchantmentReqs> target = SyncedData.Synced_EnchantmentReqs.Value;
        List<GameObject> _fittersUpdate = new();

        foreach (SyncedData.EnchantmentReqs req in target)
        {
            string found = null;
            if (!string.IsNullOrWhiteSpace(_search.text) && !HasAny(req.Items, _search.text, out found)) continue;
            GameObject element = CreateElementWithText(req.Items, GenerateReqsText(req), found);
            if (element) _fittersUpdate.Add(element);
        }
        ForceCanvas();
        _fittersUpdate.ForEach(x => x.transform.Find("Info").gameObject.SetActive(false));
    }

    private static void LoadStats()
    {
        List<GameObject> _fittersUpdate = new();

        if (string.IsNullOrWhiteSpace(_search.text))
        {
            GameObject defaultWeapons = CreateElementWithText(null,
                GenerateStatsText(SyncedData.Synced_EnchantmentStats_Weapons.Value), null, "$enchantment_defaultstats_weapon".Localize());
            _fittersUpdate.Add(defaultWeapons);
            GameObject defaultArmor = CreateElementWithText(null,
                GenerateStatsText(SyncedData.Synced_EnchantmentStats_Armor.Value), null, "$enchantment_defaultstats_armor".Localize());
            _fittersUpdate.Add(defaultArmor);
        }

        List<SyncedData.OverrideStats> target = SyncedData.Overrides_EnchantmentStats.Value;
        foreach (SyncedData.OverrideStats stat in target)
        {
            string found = null;
            if (!string.IsNullOrWhiteSpace(_search.text) && !HasAny(stat.Items, _search.text, out found)) continue;
            GameObject element = CreateElementWithText(stat.Items, GenerateStatsText(stat.Stats), found);
            if (element) _fittersUpdate.Add(element);
        } 
        ForceCanvas();
        _fittersUpdate.ForEach(x => x.transform.Find("Info").gameObject.SetActive(false));
    }

    private static void LoadChances()
    {
        List<GameObject> _fittersUpdate = new();
        
        if (string.IsNullOrWhiteSpace(_search.text))
        {
            GameObject defaultChances = CreateElementWithText(null, GenerateChancesText(SyncedData.Synced_EnchantmentChances.Value), null, "$enchantment_defaultchances".Localize());
            _fittersUpdate.Add(defaultChances);
        }
        
        List<SyncedData.OverrideChances> target = SyncedData.Overrides_EnchantmentChances.Value;
        
        foreach (SyncedData.OverrideChances chance in target)
        {
            string found = null;
            if (!string.IsNullOrWhiteSpace(_search.text) && !HasAny(chance.Items, _search.text, out found)) continue;
            GameObject element = CreateElementWithText(chance.Items, GenerateChancesText(chance.Chances), found);
            if (element) _fittersUpdate.Add(element);
        }
        ForceCanvas();
        _fittersUpdate.ForEach(x => x.transform.Find("Info").gameObject.SetActive(false));
    }

    public static void Show()
    {
        Default();
        UI.SetActive(true);
        CreateElements();
    }

    private static void Hide()
    {
        UI.SetActive(false);
        Default();
    }

    public static void Update()
    {
        if (!IsVisible()) return;
        if (!Player.m_localPlayer)
        {
            Hide();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
        }
    }

    private static void ForceCanvas()
    {
        List<ContentSizeFitter> allFitters = UI.GetComponentsInChildren<ContentSizeFitter>(true).ToList();
        Canvas.ForceUpdateCanvases();
        allFitters.ForEach(filter => filter.enabled = false);
        allFitters.ForEach(filter => filter.enabled = true);
    }

    [HarmonyPatch(typeof(TextInput), nameof(TextInput.IsVisible))]
    [ClientOnlyPatch]
    private static class TextInput_IsVisible_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ref bool __result) => __result |= IsVisible();
    }

    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.IsVisible))]
    [ClientOnlyPatch]
    private static class StoreGui_IsVisible_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ref bool __result) => __result |= IsVisible();
    } 

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    [ClientOnlyPatch]
    private static class InventoryGui_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(InventoryGui __instance)
        {
            foreach (UITooltip uiTooltip in Element.GetComponentsInChildren<UITooltip>(true))
            {
                uiTooltip.m_tooltipPrefab = __instance.m_playerGrid.m_elementPrefab.GetComponent<UITooltip>().m_tooltipPrefab;
            }
        }
    }
}