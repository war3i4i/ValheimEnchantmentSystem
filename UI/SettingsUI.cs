using System.Configuration;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;
using TMPro;

namespace kg.ValheimEnchantmentSystem.UI;

public static class SettingsUI
{
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
            GameObject settingsPrefab = __instance.m_settingsPrefab;
            Transform gameplay = settingsPrefab.transform.Find("Panel/TabButtons/Gameplay");
            if (!gameplay) gameplay = settingsPrefab.transform.Find("Panel/TabButtons/Tabs/Gameplay");
            if (!gameplay) return;
            Transform newButton = UnityEngine.Object.Instantiate(gameplay);
            newButton.transform.Find("KeyHint").gameObject.SetActive(false);
            newButton.SetParent(gameplay.parent, false);
            newButton.name = "kg_Enchantment";
            newButton.SetAsLastSibling();
            Transform textTransform = newButton.transform.Find("Label");
            Transform textTransform_Selected = newButton.transform.Find("Selected/LabelSelected");
            if (!textTransform || !textTransform_Selected) return;
            textTransform.GetComponent<TMP_Text>().text = "$enchantment_enchantment".Localize();
            textTransform_Selected.GetComponent<TMP_Text>().text = "$enchantment_enchantment".Localize();
            TabHandler tabHandler = settingsPrefab.transform.Find("Panel/TabButtons").GetComponent<TabHandler>();
            Transform page = settingsPrefab.transform.Find("Panel/TabContent");
            GameObject newPage = UnityEngine.Object.Instantiate(ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_Enchantments_Settings"));
            newPage.AddComponent<VesSettings>();
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
            newPage.transform.localScale *= 1.2f;
        }
    }

    public class VesSettings : Fishlabs.Valheim.SettingsBase
    {
        public override void FixBackButtonNavigation(Button backButton)
        {
        }

        public override void FixOkButtonNavigation(Button okButton)
        {
        }

        private bool _enableHotbarVisual_Internal, _enableMainVFX_Internal, _enableInventoryVisual_Internal;
        private VES_UI.Duration _enchantmentAnimationDuration_Internal;
        private Notifications_UI.Filter _filterConfig_Internal;
        private int _notificationDuration_Internal;

        public override void LoadSettings()
        {
            _enableHotbarVisual_Internal = Enchantment_VFX._enableHotbarVisual.Value;
            _enableMainVFX_Internal = Enchantment_VFX._enableMainVFX.Value;
            _enableInventoryVisual_Internal = Enchantment_VFX._enableInventoryVisual.Value;
            _enchantmentAnimationDuration_Internal = VES_UI._enchantmentAnimationDuration.Value;
            _filterConfig_Internal = Notifications_UI._filterConfig.Value;
            _notificationDuration_Internal = Notifications_UI._duration.Value;

            Transform options = this.transform.Find("Background/options");
            Transform hotbarVFX = options.Find("HotbarVFX");
            Transform mainVFX = options.Find("MainVFX");
            Transform inventoryVFX = options.Find("InventoryVFX");
            Transform enchantSpeed = options.Find("EnchantSpeed");
            Transform notifyFilter = options.Find("NotificationsFilter");
            Transform notifyDuration = options.Find("NotificationsDuration");

            hotbarVFX.Find("Button/Checkmark").gameObject.SetActive(_enableHotbarVisual_Internal);
            mainVFX.Find("Button/Checkmark").gameObject.SetActive(_enableMainVFX_Internal);
            inventoryVFX.Find("Button/Checkmark").gameObject.SetActive(_enableInventoryVisual_Internal);
            enchantSpeed.Find("Button_1/Checkmark").gameObject.SetActive(_enchantmentAnimationDuration_Internal == VES_UI.Duration._1);
            enchantSpeed.Find("Button_3/Checkmark").gameObject.SetActive(_enchantmentAnimationDuration_Internal == VES_UI.Duration._3);
            enchantSpeed.Find("Button_6/Checkmark").gameObject.SetActive(_enchantmentAnimationDuration_Internal == VES_UI.Duration._6);
            notifyFilter.Find("Success/Checkmark").gameObject.SetActive(_filterConfig_Internal.HasFlagFast(Notifications_UI.Filter.Success));
            notifyFilter.Find("Fail/Checkmark").gameObject.SetActive(_filterConfig_Internal.HasFlagFast(Notifications_UI.Filter.Fail));
            
            hotbarVFX.Find("Button").GetComponent<Button>().onClick.AddListener(() =>
            {
                VES_UI.PlayClick();
                _enableHotbarVisual_Internal = !_enableHotbarVisual_Internal;
                hotbarVFX.Find("Button/Checkmark").gameObject.SetActive(_enableHotbarVisual_Internal);
            });
            inventoryVFX.Find("Button").GetComponent<Button>().onClick.AddListener(() =>
            {
                VES_UI.PlayClick();
                _enableInventoryVisual_Internal = !_enableInventoryVisual_Internal;
                inventoryVFX.Find("Button/Checkmark").gameObject.SetActive(_enableInventoryVisual_Internal);
            });
            mainVFX.Find("Button").GetComponent<Button>().onClick.AddListener(() =>
            {
                VES_UI.PlayClick();
                _enableMainVFX_Internal = !_enableMainVFX_Internal;
                mainVFX.Find("Button/Checkmark").gameObject.SetActive(_enableMainVFX_Internal);
            });
            enchantSpeed.Find("Button_1").GetComponent<Button>().onClick.AddListener(() =>
            {
                VES_UI.PlayClick();
                _enchantmentAnimationDuration_Internal = VES_UI.Duration._1; 
                enchantSpeed.Find("Button_1/Checkmark").gameObject.SetActive(true);
                enchantSpeed.Find("Button_3/Checkmark").gameObject.SetActive(false);
                enchantSpeed.Find("Button_6/Checkmark").gameObject.SetActive(false);
            });
            enchantSpeed.Find("Button_3").GetComponent<Button>().onClick.AddListener(() =>
            {
                VES_UI.PlayClick();
                _enchantmentAnimationDuration_Internal = VES_UI.Duration._3;
                enchantSpeed.Find("Button_1/Checkmark").gameObject.SetActive(false);
                enchantSpeed.Find("Button_3/Checkmark").gameObject.SetActive(true);
                enchantSpeed.Find("Button_6/Checkmark").gameObject.SetActive(false);
            });
            enchantSpeed.Find("Button_6").GetComponent<Button>().onClick.AddListener(() =>
            {
                VES_UI.PlayClick();
                _enchantmentAnimationDuration_Internal = VES_UI.Duration._6;
                enchantSpeed.Find("Button_1/Checkmark").gameObject.SetActive(false);
                enchantSpeed.Find("Button_3/Checkmark").gameObject.SetActive(false);
                enchantSpeed.Find("Button_6/Checkmark").gameObject.SetActive(true);
            });
            notifyFilter.Find("Success").GetComponent<Button>().onClick.AddListener(() =>
            {
                VES_UI.PlayClick();
                _filterConfig_Internal ^= Notifications_UI.Filter.Success;
                notifyFilter.Find("Success/Checkmark").gameObject.SetActive(_filterConfig_Internal.HasFlagFast(Notifications_UI.Filter.Success));
            });
            notifyFilter.Find("Fail").GetComponent<Button>().onClick.AddListener(() =>
            {
                VES_UI.PlayClick();
                _filterConfig_Internal ^= Notifications_UI.Filter.Fail;
                notifyFilter.Find("Fail/Checkmark").gameObject.SetActive(_filterConfig_Internal.HasFlagFast(Notifications_UI.Filter.Fail));
            });
            notifyDuration.Find("Slider").GetComponent<Slider>().onValueChanged.AddListener((val) =>
            {
                int currentVal = (int)val;
                _notificationDuration_Internal = currentVal;
                notifyDuration.Find("text").GetComponent<Text>().text = currentVal + "s";
            });
            notifyDuration.Find("Slider").GetComponent<Slider>().value = _notificationDuration_Internal;
            notifyDuration.Find("text").GetComponent<Text>().text = _notificationDuration_Internal + "s";
        }

        public override void SaveSettings()
        {
            Enchantment_VFX._enableHotbarVisual.Value = _enableHotbarVisual_Internal;
            Enchantment_VFX._enableMainVFX.Value = _enableMainVFX_Internal;
            Enchantment_VFX._enableInventoryVisual.Value = _enableInventoryVisual_Internal;
            VES_UI._enchantmentAnimationDuration.Value = _enchantmentAnimationDuration_Internal;
            Notifications_UI._filterConfig.Value = _filterConfig_Internal;
            Notifications_UI._duration.Value = _notificationDuration_Internal;
            Enchantment_VFX.UpdateGrid();
            Enchantment_VFX._enableHotbarVisual.ConfigFile.Save();
        }
    }
}