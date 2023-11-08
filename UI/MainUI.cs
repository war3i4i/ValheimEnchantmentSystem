using BepInEx.Bootstrap;
using ItemDataManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem.UI;

[VES_Autoload(VES_Autoload.Priority.Normal)]
public static class VES_UI
{
    private static bool IsVisible() => UI && UI.activeSelf;

    private static Action<ItemDrop.ItemData> OnItemSelect;
    private static AudioSource AUsrc;
    private static AudioClip _3sec;
    private static AudioClip _6sec;
    
    private static GameObject UI;
    private static GameObject VFX1;
    private static Sprite Default_QuestionMark;

    private static AudioClip Click;
    private static AudioClip SuccessSound;
    private static AudioClip FailSound;

    private static Transform Item_Transform;
    private static Text Item_Text;
    private static Image Item_Icon;
    private static Image Item_Visual;
    private static Image Item_Trail;

    private static Transform Scroll_Transform;
    private static Text Scroll_Text;
    private static Image Scroll_Icon;
    private static Image Scroll_Visual;
    private static Image Scroll_Trail;

    private static Transform UseBless_Transform;
    private static Image UseBless_Icon;

    private static Transform Start_Transform;
    private static Text Start_Text;

    private static Transform Progress_Transform;
    private static Transform Progress_VFX;
    private static Image Progress_Fill;

    private static float _itemStartX, _scrollStartX;
    private static float _startY;
    private static bool _useBless;
    private static float _fillDistance;
    private static ItemDrop.ItemData _currentItem;
    private static bool _enchantProcessing;
    private static float _enchantTimer;
    private static bool _shouldReselect;

    private static readonly Color VFX_Default_Bless = new Color32(161, 157, 0, 255);
    private static readonly int Speed = Shader.PropertyToID("_Speed");

    private static float TIMER_MAX;
    

    public static ConfigEntry<Duration> EnchantmentAnimationDuration;
    public enum Duration
    {
        _3 = 1,
        _6 = 2
    }
    
    [UsedImplicitly]
    private static void OnInit()
    {
        EnchantmentAnimationDuration = ValheimEnchantmentSystem._thistype.Config.Bind("Visuals", "EnchantmentAnimationDuration", Duration._3, "Duration of the enchantment animation.");
        _3sec = ValheimEnchantmentSystem._asset.LoadAsset<AudioClip>("kg_EnchantmentSound_Main_3");
        _6sec = ValheimEnchantmentSystem._asset.LoadAsset<AudioClip>("kg_EnchantmentSound_Main_6");
        UI = UnityEngine.Object.Instantiate(ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_EnchantmentUI"));
        VFX1 = ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_EnchantmentUI_VFX1");
        Default_QuestionMark = ValheimEnchantmentSystem._asset.LoadAsset<Sprite>("kg_EnchantmentQuestion");
        Click = ValheimEnchantmentSystem._asset.LoadAsset<AudioClip>("kg_EnchantmentClick");
        SuccessSound = ValheimEnchantmentSystem._asset.LoadAsset<AudioClip>("kg_EnchantmentSound_Success");
        FailSound = ValheimEnchantmentSystem._asset.LoadAsset<AudioClip>("kg_EnchantmentSound_Fail");
        UI.SetActive(false);
        UnityEngine.Object.DontDestroyOnLoad(UI);

        UI.transform.Find("Canvas/Header/Text").GetComponent<Text>().text = "$enchantment_header".Localize();
        
        Item_Transform = UI.transform.Find("Canvas/Background/Item");
        Item_Text = Item_Transform.Find("Text").GetComponent<Text>();
        Item_Icon = Item_Transform.Find("Icon").GetComponent<Image>();
        Item_Visual = Item_Transform.Find("Visual").GetComponent<Image>();
        Item_Trail = Item_Transform.Find("Trail").GetComponent<Image>();

        Scroll_Transform = UI.transform.Find("Canvas/Background/Scroll");
        Scroll_Text = Scroll_Transform.Find("Text").GetComponent<Text>();
        Scroll_Icon = Scroll_Transform.Find("Icon").GetComponent<Image>();
        Scroll_Visual = Scroll_Transform.Find("Visual").GetComponent<Image>();
        Scroll_Trail = Scroll_Transform.Find("Trail").GetComponent<Image>();
        

        UseBless_Transform = UI.transform.Find("Canvas/Background/UseBless");
        UseBless_Icon = UseBless_Transform.Find("Icon").GetComponent<Image>();
        UseBless_Transform.Find("Text").GetComponent<Text>().text = "$enchantment_usebless".Localize();
        UseBless_Transform.Find("Text").GetComponent<Text>().color = Color.yellow;

        Start_Transform = UI.transform.Find("Canvas/Background/Start");
        Start_Text = Start_Transform.Find("Text").GetComponent<Text>();

        Progress_Transform = UI.transform.Find("Canvas/Background/Progress");
        Progress_VFX = Progress_Transform.Find("VFX");
        Progress_Fill = Progress_Transform.Find("Fill").GetComponent<Image>();

        _itemStartX = Item_Transform.GetComponent<RectTransform>().anchoredPosition.x;
        _scrollStartX = Scroll_Transform.GetComponent<RectTransform>().anchoredPosition.x;
        _startY = Start_Transform.GetComponent<RectTransform>().anchoredPosition.y;
        _fillDistance = 300f;
        OnItemSelect += SelectItem;
        UseBless_Transform.GetComponent<Button>().onClick.AddListener(() =>
        {
            PlayClick();
            UseBless_ButtonClick();
        });
        Start_Transform.GetComponent<Button>().onClick.AddListener(() =>
        {
            PlayClick();
            Start_ButtonClick();
        });
        UI.transform.Find("Canvas/Background/Info").GetComponent<Button>().onClick.AddListener(() =>
        {
            PlayClick();
            Info_UI.Show();
        });
        
        Default();
    }

    private static void Start_ButtonClick()
    {
        if (_currentItem == null || !Player.m_localPlayer ||
            !Player.m_localPlayer.m_inventory.ContainsItem(_currentItem))
        {
            Default();
            return;
        }

        if (_shouldReselect)
        {
            bool oldUseBless = _useBless;
            SelectItem(_currentItem);
            if (_useBless != oldUseBless) UseBless_ButtonClick();
            PlayClick();
            return;
        }


        if (_enchantProcessing)
        {
            _enchantProcessing = false;
            _enchantTimer = 0;

            bool oldUseBless = _useBless;
            SelectItem(_currentItem);
            if (_useBless != oldUseBless) UseBless_ButtonClick();
            AUsrc.Stop();
        }
        else
        {
            SyncedData.EnchantmentReqs.req req = _useBless
                ? SyncedData.GetReqs(_currentItem.m_dropPrefab.name).blessed_enchant_prefab
                : SyncedData.GetReqs(_currentItem.m_dropPrefab.name).enchant_prefab;
            if (req == null || !req.IsValid()) return;
            var prefab = ZNetScene.instance.GetPrefab(req.prefab);
            if (!prefab) return;
            if (Utils.CustomCountItemsNoLevel(prefab.name) < req.amount) return;


            _enchantProcessing = true;
            TIMER_MAX = (int)EnchantmentAnimationDuration.Value * 3f;
            _enchantTimer = Input.GetKey(KeyCode.LeftShift) ? 0 : TIMER_MAX;
            Start_Text.text = "$enchantment_cancel".Localize();

            Progress_Transform.gameObject.SetActive(true);
            Progress_Fill.fillAmount = 0f;
            Progress_VFX.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0f);
            Progress_VFX.GetComponent<ParticleSystem>().startColor = _useBless ? VFX_Default_Bless : Color.white;
            Progress_Fill.transform.GetChild(0).GetComponent<Image>().color = _useBless ? Color.yellow : Color.white;
            Item_Visual.color = Color.clear;
            Scroll_Visual.color = Color.clear;
            
            Item_Trail.material.SetFloat(Speed, 1f);
            Scroll_Trail.material.SetFloat(Speed, 1f);

            UseBless_Transform.gameObject.SetActive(false);

            Item_Text.text = "";
            Scroll_Text.text = "";
            
            AUsrc.Stop();
            AUsrc.clip = EnchantmentAnimationDuration.Value == Duration._3 ? _3sec : _6sec;
            if (!Input.GetKey(KeyCode.LeftShift))
                AUsrc.Play();
        }
    }

    public static void Update()
    {
        if (!IsVisible()) return;
        if (!Player.m_localPlayer)
        {
            Hide();
            return;
        }
            
        if (Input.GetKeyDown(KeyCode.Escape) && !Info_UI.IsVisible())
        {
            ValheimEnchantmentSystem._thistype.DelayedInvoke(Hide, 1);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab) && !_enchantProcessing && !_shouldReselect)
        {
            PlayClick();
            UseBless_ButtonClick();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            Start_ButtonClick();
            return;
        }

        if (!_enchantProcessing) return;
        if (_currentItem == null || !Player.m_localPlayer.m_inventory.ContainsItem(_currentItem))
        {
            Hide();
            return;
        }

        _enchantTimer -= Time.deltaTime;

        if (_enchantTimer <= 0)
        {
            RectTransform Item_Rect = Item_Transform.GetComponent<RectTransform>();
            Item_Rect.anchoredPosition = new Vector2(0, 0);
            Item_Transform.localScale = new Vector3(1.4f, 1.4f, 1f);

            _enchantProcessing = false;
            _enchantTimer = 0;
            Scroll_Transform.gameObject.SetActive(false);
            Progress_Transform.gameObject.SetActive(false);

            Enchantment_Core.Enchanted en = _currentItem.Data().GetOrCreate<Enchantment_Core.Enchanted>();
            bool enchanted = en.Enchant(_useBless, out string msg);

            Item_Text.text = msg;
            Item_Text.color = enchanted ? Color.green : Color.red;
            Item_Visual.color = enchanted ? Color.green : Color.red;
            Color c = SyncedData.GetColor(en, out _, true).IncreaseColorLight().ToColorAlpha();
            Item_Trail.color = c;
            var uifx = UnityEngine.Object.Instantiate(VFX1, Item_Transform.transform);
            uifx.GetComponent<ParticleSystem>().startColor = enchanted ? Color.green : Color.red;
            AUsrc.PlayOneShot(enchanted ? SuccessSound : FailSound);
            Item_Trail.material.SetFloat(Speed, 0.5f);
            Scroll_Trail.material.SetFloat(Speed, 0.5f);

            _shouldReselect = true;
            Start_Transform.gameObject.SetActive(true);
            Start_Text.text = "$enchantment_ok".Localize();
        }
        else
        {
            Progress_Fill.fillAmount = 1f - (_enchantTimer / TIMER_MAX);
            Progress_VFX.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(_fillDistance * Progress_Fill.fillAmount, 0f);

            if (_useBless)
            {
                Item_Visual.color = new Color(1f, 1f, 0f, Progress_Fill.fillAmount);
                Scroll_Visual.color = new Color(1f, 1f, 0f, Progress_Fill.fillAmount);
            }
            else
            {
                Item_Visual.color = new Color(1f, 1f, 1f, Progress_Fill.fillAmount);
                Scroll_Visual.color = new Color(1f, 1f, 1f, Progress_Fill.fillAmount);
            }

            RectTransform Item_Rect = Item_Transform.GetComponent<RectTransform>();
            Item_Rect.anchoredPosition =
                new Vector2(Mathf.Lerp(_itemStartX, 0f, Progress_Fill.fillAmount),
                    Mathf.Lerp(_startY, 0f, Progress_Fill.fillAmount));
            RectTransform Scroll_Rect = Scroll_Transform.GetComponent<RectTransform>();
            Scroll_Rect.anchoredPosition =
                new Vector2(Mathf.Lerp(_scrollStartX, 0f, Progress_Fill.fillAmount),
                    Mathf.Lerp(_startY, 0f, Progress_Fill.fillAmount));
                
            Item_Transform.localScale = new Vector3(1f + Progress_Fill.fillAmount * 0.4f, 1f + Progress_Fill.fillAmount * 0.4f, 1f);
            Scroll_Transform.localScale = new Vector3(1f + Progress_Fill.fillAmount * 0.4f, 1f + Progress_Fill.fillAmount * 0.4f, 1f);
        }
    }

    private static void Default()
    {
        AUsrc?.Stop();
        _currentItem = null;
        _enchantProcessing = false;
        _enchantTimer = 0;
        _shouldReselect = false;

        RectTransform Item_Rect = Item_Transform.GetComponent<RectTransform>();
        Item_Rect.anchoredPosition = new Vector2(0, 0);
        Item_Transform.gameObject.SetActive(true);
        Item_Transform.localScale = new Vector3(1.4f,1.4f,1f);
        Item_Text.text = "$enchantment_selectanitem".Localize();
        Item_Text.color = Color.white;
        Item_Icon.sprite = Default_QuestionMark;
        Item_Visual.color = Color.clear;
        Item_Trail.gameObject.SetActive(false);
        Item_Trail.color = new Color(1f, 1f, 1f, 0.8f);
        Item_Trail.material.SetFloat(Speed, 0.5f);

        RectTransform Scroll_Rect = Scroll_Transform.GetComponent<RectTransform>();
        Scroll_Rect.anchoredPosition = new Vector2(_scrollStartX, _startY);
        Scroll_Transform.gameObject.SetActive(false);
        Scroll_Transform.localScale = Vector3.one;
        Scroll_Text.text = "$enchantment_noenchantitems".Localize();
        Scroll_Text.color = Color.red;
        Scroll_Icon.sprite = Default_QuestionMark;
        Scroll_Visual.color = Color.clear;
        Scroll_Trail.gameObject.SetActive(false);
        Scroll_Trail.color = new Color(1f, 1f, 1f, 0.8f);
        Scroll_Trail.material.SetFloat(Speed, 0.5f);

        UseBless_Transform.gameObject.SetActive(false);
        UseBless_Icon.gameObject.SetActive(false);
        _useBless = false;

        Start_Transform.gameObject.SetActive(false);

        Progress_Transform.gameObject.SetActive(false);
        Progress_VFX.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0f);
        Progress_Fill.fillAmount = 0f;
        Progress_Fill.transform.GetChild(0).GetComponent<Image>().color = Color.clear;
        Progress_VFX.GetComponent<ParticleSystem>().startColor = Color.clear;
    }

    private static void UseBless_ButtonClick()
    {
        if (_currentItem == null) return;
        _useBless = !_useBless;
        UseBless_Icon.gameObject.SetActive(_useBless);

        var reqs = SyncedData.GetReqs(_currentItem.m_dropPrefab?.name);

        SyncedData.EnchantmentReqs.req req = _useBless ? reqs.blessed_enchant_prefab : reqs.enchant_prefab;
        if (req.IsValid())
        {
            var enchant_item = ZNetScene.instance.GetPrefab(req.prefab);
            Scroll_Text.text = enchant_item.GetComponent<ItemDrop>().m_itemData.m_shared.m_name.Localize() + " <color=yellow>x" + req.amount + "</color>";
            Scroll_Text.color = Utils.CustomCountItemsNoLevel(req.prefab) >= req.amount ? Color.white : Color.red;
            Scroll_Icon.sprite = enchant_item.GetComponent<ItemDrop>().m_itemData.GetIcon();
            Scroll_Trail.gameObject.SetActive(true);
            Scroll_Trail.color = _useBless ? new Color(1f,1f,0f,0.8f) : new Color(1f, 1f, 1f, 0.8f);
        }
        else
        {
            Scroll_Text.text = "$enchantment_noenchantitems".Localize();
            Scroll_Text.color = Color.red;
            Scroll_Icon.sprite = Default_QuestionMark;
            Scroll_Trail.gameObject.SetActive(false);
            Scroll_Trail.color = new Color(1f, 1f, 1f, 0.8f);
        }
    }

    private static void SelectItem(ItemDrop.ItemData item)
    {
        if (!IsVisible() || _enchantProcessing || item == null) return;
        Default();
        InventoryGui.instance.SetupDragItem(null, null, 1);
        if (!Player.m_localPlayer.m_inventory.ContainsItem(item)) return;
        var reqs = SyncedData.GetReqs(item.m_dropPrefab?.name);
        if (reqs == null) return;

        int enchantSkillLvl = (int)Player.m_localPlayer.GetSkillLevel(Enchantment_Skill.SkillType_Enchantment);
        if (enchantSkillLvl < reqs.required_skill) return;
        
        Enchantment_Core.Enchanted en = item.Data().Get<Enchantment_Core.Enchanted>();
        if(en && en!.GetEnchantmentChance() <= 0) return;

        _currentItem = item;

        RectTransform Item_Rect = Item_Transform.GetComponent<RectTransform>();
        Item_Rect.anchoredPosition = new Vector2(_itemStartX, _startY);
        Item_Transform.gameObject.SetActive(true);
        Item_Transform.localScale = Vector3.one;
        string itemName = item.m_shared.m_name.Localize();
        Item_Trail.gameObject.SetActive(true);
        if (en)
        {
            string c = SyncedData.GetColor(en, out _, true).IncreaseColorLight();
            itemName += $" (<color={c.IncreaseColorLight()}>+{en.level}</color>)";
            Item_Trail.color = c.ToColorAlpha();
        }
        else
        {
            itemName += " (<color=#FFFFFF>+0</color>)";
            Item_Trail.color = new Color(1f, 1f, 1f, 0.8f);
        }

        Item_Text.text = itemName;
        Item_Icon.sprite = item.GetIcon();

        UseBless_Transform.gameObject.SetActive(true);
        UseBless_Icon.gameObject.SetActive(false);

        RectTransform Scroll_Rect = Scroll_Transform.GetComponent<RectTransform>();
        Scroll_Rect.anchoredPosition = new Vector2(_scrollStartX, _startY);
        Scroll_Transform.gameObject.SetActive(true);
        Scroll_Trail.gameObject.SetActive(true);
        if (reqs.enchant_prefab.IsValid())
        {
            var enchant_item = ZNetScene.instance.GetPrefab(reqs.enchant_prefab.prefab);
            Scroll_Text.text = enchant_item.GetComponent<ItemDrop>().m_itemData.m_shared.m_name.Localize() + " <color=yellow>x" + reqs.enchant_prefab.amount + "</color>";
            Scroll_Icon.sprite = enchant_item.GetComponent<ItemDrop>().m_itemData.GetIcon();
            Scroll_Text.color = Utils.CustomCountItemsNoLevel(reqs.enchant_prefab.prefab) >= reqs.enchant_prefab.amount ? Color.white : Color.red;
            Start_Transform.gameObject.SetActive(true);
            Start_Text.text = "$enchantment_enchant".Localize();
            Scroll_Trail.color = new Color(1f, 1f, 1f, 0.8f);
        }
        else
        {
            Scroll_Text.text = "$enchantment_noenchantitems".Localize();
            Scroll_Text.color = Color.red;
            Scroll_Icon.sprite = Default_QuestionMark;
            Scroll_Trail.gameObject.SetActive(false);
            Scroll_Trail.color = new Color(1f, 1f, 1f, 0.8f);
        }
    }

    private static void Show()
    {
        Default();
        if (!InventoryGui.IsVisible())
            InventoryGui.instance.Show(null);
        UI.SetActive(true);
    }

    private static void Hide()
    {
        UI.SetActive(false);
        Default();
    }

    public static void PlayClick() => AUsrc.PlayOneShot(Click);

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

    [HarmonyPatch(typeof(AudioMan), nameof(AudioMan.Awake))]
    [ClientOnlyPatch]
    private static class AudioMan_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(AudioMan __instance)
        {
            var SFXgroup = __instance.m_masterMixer.FindMatchingGroups("SFX")[0];
            AUsrc = Chainloader.ManagerObject.AddComponent<AudioSource>();
            AUsrc.reverbZoneMix = 0;
            AUsrc.spatialBlend = 0;
            AUsrc.bypassListenerEffects = true;
            AUsrc.bypassEffects = true;
            AUsrc.volume = 1f;
            AUsrc.outputAudioMixerGroup = SFXgroup;

            foreach (var asset in ValheimEnchantmentSystem._asset.LoadAllAssets<GameObject>())
                foreach (AudioSource audioSource in asset.GetComponentsInChildren<AudioSource>(true))
                    audioSource.outputAudioMixerGroup = SFXgroup;
        }
    }
 
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupDragItem))]
    [ClientOnlyPatch]
    static class InventoryGui_SetupDragItem_Patch
    {
        [UsedImplicitly]
        private static void Postfix(InventoryGui __instance)
        {
            if (__instance.m_dragGo && __instance.m_dragItem != null)
            {
                OnItemSelect(__instance.m_dragItem);
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    [ClientOnlyPatch]
    private static class InventoryGui_Awake_Patch
    {
        public static Button _enchantmentButton;
        public static GameObject _enchantmentBackground;

        [UsedImplicitly]
        private static void Postfix(InventoryGui __instance)
        {
            _enchantmentBackground = UnityEngine.Object.Instantiate(__instance.m_repairPanel.gameObject,
                __instance.m_repairPanel.transform.parent);
            RectTransform rectTransform = _enchantmentBackground.GetComponent<RectTransform>();
            rectTransform.anchoredPosition += new Vector2(0, 74);
            _enchantmentBackground.transform.SetAsFirstSibling();
            _enchantmentButton = UnityEngine.Object
                .Instantiate(__instance.m_repairButton.gameObject, __instance.m_repairButton.transform.parent)
                .GetComponent<Button>();
            _enchantmentButton.name = "enchantment_menu";
            _enchantmentButton.onClick.RemoveAllListeners();
            _enchantmentButton.onClick.AddListener(() =>
            {
                if (IsVisible()) Hide();
                else Show();
                PlayClick();
            });
            _enchantmentButton.GetComponent<UITooltip>().m_text = "$enchantment_menu".Localize();
            RectTransform rect = _enchantmentButton.GetComponent<RectTransform>();
            rect.anchoredPosition += new Vector2(0, 74);
            _enchantmentButton.transform.Find("Glow").gameObject.SetActive(false);
            _enchantmentButton.gameObject.SetActive(true);
            _enchantmentButton.transform.Find("Image").GetComponent<Image>().sprite =
                ValheimEnchantmentSystem._asset.LoadAsset<Sprite>("kg_Enchantment_Icon");
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    [ClientOnlyPatch]
    private static class InventoryGui_Show_Patch
    {
        [UsedImplicitly]
        private static void Postfix(InventoryGui __instance)
        {
            if (!Player.m_localPlayer) return;
            if (Player.m_localPlayer.GetCurrentCraftingStation() || (__instance.m_currentContainer && __instance.m_currentContainer.m_nview != Player.m_localPlayer.m_nview))
            {
                InventoryGui_Awake_Patch._enchantmentBackground.gameObject.SetActive(false);
                InventoryGui_Awake_Patch._enchantmentButton.gameObject.SetActive(false);
                return;
            }

            InventoryGui_Awake_Patch._enchantmentBackground.gameObject.SetActive(true);
            InventoryGui_Awake_Patch._enchantmentButton.gameObject.SetActive(true);
        }
    }
    
    [HarmonyPatch(typeof(InventoryGui),nameof(InventoryGui.Hide))]
    [ClientOnlyPatch]
    private static class InventoryGui_Hide_Patch
    {
        [UsedImplicitly]
        private static bool Prefix() => !IsVisible();
    }
}