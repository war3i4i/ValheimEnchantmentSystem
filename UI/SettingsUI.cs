using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;
using TMPro;

namespace kg.ValheimEnchantmentSystem.UI;

public static class SettingsUI
{
    private static GameObject CurrentUI;

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    [ClientOnlyPatch]
    static class Menu_Start_Patch
    {
        private static bool firstInit = true;

        [UsedImplicitly]
        private static void Postfix(FejdStartup __instance)
        {
            if (!firstInit) return;
            firstInit = false;
            var settingsPrefab = __instance.m_settingsPrefab;
            var controls = settingsPrefab.transform.Find("panel/TabButtons/Controlls");
            var newButton = UnityEngine.Object.Instantiate(controls);
            newButton.SetParent(controls.parent, false);
            newButton.name = "kg_Enchantment";
            newButton.SetAsLastSibling();
            newButton.GetComponent<RectTransform>().anchoredPosition +=
                new Vector2(0, newButton.GetComponent<RectTransform>().sizeDelta.y);
            newButton.transform.Find("Text").GetComponent<TMP_Text>().text = "$enchantment_enchantment".Localize();
            var tabHandler = settingsPrefab.transform.Find("panel/TabButtons").GetComponent<TabHandler>();
            var page = settingsPrefab.transform.Find("panel/Tabs");
            GameObject newPage = UnityEngine.Object.Instantiate(ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_Enchantments_Settings"));
            Localization.instance.Localize(newPage.transform);
            newPage.transform.SetParent(page);
            newPage.name = "kg_Enchantment";
            newPage.SetActive(false);
            TabHandler.Tab newTab = new TabHandler.Tab
            {
                m_default = false,
                m_button = newButton.GetComponent<Button>(),
                m_page = newPage.GetComponent<RectTransform>()
            };
            tabHandler.m_tabs.Add(newTab);
        }
    }

    [HarmonyPatch(typeof(Settings), nameof(Settings.Awake))]
    [ClientOnlyPatch]
    private static class Settings_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Settings __instance)
        {
            var enTab = __instance.transform.Find("panel/Tabs/kg_Enchantment");
            if (!enTab) return;
            CurrentUI = enTab.gameObject;

            Transform bools = CurrentUI.transform.Find("Background/Bools");
            Transform hotbarVFX = bools.Find("HotbarVFX");
            Transform mainVFX = bools.Find("MainVFX");
            Transform wingsVFX = bools.Find("WingsVFX");
            Transform auraVFX = bools.Find("AuraVFX");
            
            hotbarVFX.Find("Button").GetComponent<Button>().onClick.AddListener(() =>
            {
                VES_UI.PlayClick();
                Enchantment_VFX._enableHotbarVisual.Value = !Enchantment_VFX._enableHotbarVisual.Value;
                Enchantment_VFX.UpdateGrid();
                Enchantment_VFX._enableHotbarVisual.ConfigFile.Save();
                InitValues();
            });
            mainVFX.Find("Button").GetComponent<Button>().onClick.AddListener(() =>
            {
                VES_UI.PlayClick();
                Enchantment_VFX._enableMainVFX.Value = !Enchantment_VFX._enableMainVFX.Value;
                Enchantment_VFX._enableMainVFX.ConfigFile.Save();
                InitValues();
            });
            wingsVFX.Find("Button").GetComponent<Button>().onClick.AddListener(() =>
            {
                VES_UI.PlayClick();
                Enchantment_AdditionalEffects._enableWingsEffects.Value = !Enchantment_AdditionalEffects._enableWingsEffects.Value;
                Enchantment_AdditionalEffects._enableWingsEffects.ConfigFile.Save();
                InitValues();
            });
            auraVFX.Find("Button").GetComponent<Button>().onClick.AddListener(() =>
            {
                VES_UI.PlayClick();
                Enchantment_AdditionalEffects._enableAuraEffects.Value = !Enchantment_AdditionalEffects._enableAuraEffects.Value;
                Enchantment_AdditionalEffects._enableAuraEffects.ConfigFile.Save();
                InitValues();
            });
            
            CurrentUI.transform.Find("Background/DefaultButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                VES_UI.PlayClick();
                Enchantment_VFX._enableHotbarVisual.Value = (bool)Enchantment_VFX._enableHotbarVisual.DefaultValue;
                Enchantment_VFX._enableMainVFX.Value = (bool)Enchantment_VFX._enableMainVFX.DefaultValue;
                Enchantment_AdditionalEffects._enableWingsEffects.Value = (bool)Enchantment_AdditionalEffects._enableWingsEffects.DefaultValue;
                Enchantment_AdditionalEffects._enableAuraEffects.Value = (bool)Enchantment_AdditionalEffects._enableAuraEffects.DefaultValue;
                Enchantment_VFX.UpdateGrid();
                Enchantment_VFX._enableHotbarVisual.ConfigFile.Save();
                InitValues();
            });
            
            InitValues();
        }
    }

    private static void InitValues()
    {
        if (!CurrentUI) return;
        
        Transform bools = CurrentUI.transform.Find("Background/Bools");
        Transform hotbarVFX = bools.Find("HotbarVFX");
        Transform mainVFX = bools.Find("MainVFX");
        Transform wingsVFX = bools.Find("WingsVFX");
        Transform auraVFX = bools.Find("AuraVFX");
        hotbarVFX.Find("Button/Checkmark").gameObject.SetActive(Enchantment_VFX._enableHotbarVisual.Value);
        mainVFX.Find("Button/Checkmark").gameObject.SetActive(Enchantment_VFX._enableMainVFX.Value);
        wingsVFX.Find("Button/Checkmark").gameObject.SetActive(Enchantment_AdditionalEffects._enableWingsEffects.Value);
        auraVFX.Find("Button/Checkmark").gameObject.SetActive(Enchantment_AdditionalEffects._enableAuraEffects.Value);
    }
}