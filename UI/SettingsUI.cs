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

            Transform options = CurrentUI.transform.Find("Background/options");
            Transform hotbarVFX = options.Find("HotbarVFX");
            Transform mainVFX = options.Find("MainVFX");
            Transform wingsVFX = options.Find("WingsVFX");
            Transform auraVFX = options.Find("AuraVFX");
            Transform inventoryVFX = options.Find("InventoryVFX");
            Transform enchantSpeed = options.Find("EnchantSpeed");
            Transform notifyFilter = options.Find("NotificationsFilter");
            Transform notifyDuration = options.Find("NotificationsDuration");
            
            hotbarVFX.Find("Button").GetComponent<Button>().onClick.AddListener(() =>
            {
                VES_UI.PlayClick();
                Enchantment_VFX._enableHotbarVisual.Value = !Enchantment_VFX._enableHotbarVisual.Value;
                Enchantment_VFX.UpdateGrid();
                Enchantment_VFX._enableHotbarVisual.ConfigFile.Save();
                InitValues();
            });
            inventoryVFX.Find("Button").GetComponent<Button>().onClick.AddListener(() =>
            {
                VES_UI.PlayClick();
                Enchantment_VFX._enableInventoryVisual.Value = !Enchantment_VFX._enableInventoryVisual.Value;
                Enchantment_VFX.UpdateGrid();
                Enchantment_VFX._enableInventoryVisual.ConfigFile.Save();
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
            enchantSpeed.Find("Button_1").GetComponent<Button>().onClick.AddListener(() =>
            {
                VES_UI.PlayClick();
                VES_UI.EnchantmentAnimationDuration.Value = VES_UI.Duration._1;
                VES_UI.EnchantmentAnimationDuration.ConfigFile.Save();
                InitValues();
            });
            enchantSpeed.Find("Button_3").GetComponent<Button>().onClick.AddListener(() =>
            {
                VES_UI.PlayClick();
                VES_UI.EnchantmentAnimationDuration.Value = VES_UI.Duration._3;
                VES_UI.EnchantmentAnimationDuration.ConfigFile.Save();
                InitValues();
            });
            enchantSpeed.Find("Button_6").GetComponent<Button>().onClick.AddListener(() =>
            {
                VES_UI.PlayClick();
                VES_UI.EnchantmentAnimationDuration.Value = VES_UI.Duration._6;
                VES_UI.EnchantmentAnimationDuration.ConfigFile.Save();
                InitValues();
            });
            notifyFilter.Find("Success").GetComponent<Button>().onClick.AddListener(() =>
            {
                VES_UI.PlayClick();
                Notifications_UI.FilterConfig.Value ^= Notifications_UI.Filter.Success;
                Notifications_UI.FilterConfig.ConfigFile.Save();
                InitValues();
            });
            notifyFilter.Find("Fail").GetComponent<Button>().onClick.AddListener(() =>
            {
                VES_UI.PlayClick();
                Notifications_UI.FilterConfig.Value ^= Notifications_UI.Filter.Fail;
                Notifications_UI.FilterConfig.ConfigFile.Save();
                InitValues();
            });
            notifyDuration.Find("Slider").GetComponent<Slider>().onValueChanged.AddListener((val) =>
            {
                int currentVal = (int)val;
                Notifications_UI.Duration.Value = currentVal;
                Notifications_UI.Duration.ConfigFile.Save();
                notifyDuration.Find("text").GetComponent<Text>().text = currentVal + "s";
            });
            notifyDuration.Find("Slider").GetComponent<Slider>().value = Notifications_UI.Duration.Value;
            notifyDuration.Find("text").GetComponent<Text>().text = Notifications_UI.Duration.Value + "s";
            
            InitValues();
        }
    }

    private static void InitValues()
    {
        if (!CurrentUI) return;
        
        Transform options = CurrentUI.transform.Find("Background/options");
        Transform hotbarVFX = options.Find("HotbarVFX");
        Transform mainVFX = options.Find("MainVFX");
        Transform wingsVFX = options.Find("WingsVFX"); 
        Transform auraVFX = options.Find("AuraVFX");
        Transform inventoryVFX = options.Find("InventoryVFX"); 
        Transform enchantSpeed = options.Find("EnchantSpeed");
        Transform notifyFilter = options.Find("NotificationsFilter");
        hotbarVFX.Find("Button/Checkmark").gameObject.SetActive(Enchantment_VFX._enableHotbarVisual.Value);
        mainVFX.Find("Button/Checkmark").gameObject.SetActive(Enchantment_VFX._enableMainVFX.Value);
        wingsVFX.Find("Button/Checkmark").gameObject.SetActive(Enchantment_AdditionalEffects._enableWingsEffects.Value);
        auraVFX.Find("Button/Checkmark").gameObject.SetActive(Enchantment_AdditionalEffects._enableAuraEffects.Value);
        inventoryVFX.Find("Button/Checkmark").gameObject.SetActive(Enchantment_VFX._enableInventoryVisual.Value);
        enchantSpeed.Find("Button_1/Checkmark").gameObject.SetActive(VES_UI.EnchantmentAnimationDuration.Value == VES_UI.Duration._1);
        enchantSpeed.Find("Button_3/Checkmark").gameObject.SetActive(VES_UI.EnchantmentAnimationDuration.Value == VES_UI.Duration._3);
        enchantSpeed.Find("Button_6/Checkmark").gameObject.SetActive(VES_UI.EnchantmentAnimationDuration.Value == VES_UI.Duration._6);
        notifyFilter.Find("Success/Checkmark").gameObject.SetActive(Notifications_UI.FilterConfig.Value.HasFlagFast(Notifications_UI.Filter.Success));
        notifyFilter.Find("Fail/Checkmark").gameObject.SetActive(Notifications_UI.FilterConfig.Value.HasFlagFast(Notifications_UI.Filter.Fail));
    }
}