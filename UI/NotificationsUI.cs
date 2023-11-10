using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem.UI;

[VES_Autoload]
public static class Notifications_UI
{
    private const float Duration = 5f;
    private const float FadeDuration = 0.25f;
    
    
    private class Notification
    {
        public string PlayerName;
        public string ItemPrefab;
        public bool Success;
        public int PrevLevel;
        public int Level;
    }

    private static readonly Queue<Notification> _notifications = new();

    private static bool IsVisible() => UI && UI.activeSelf;

    private static GameObject UI;
    private static readonly List<Image> _colorGroup = new();

    private static readonly Color SuccessColor = Color.green;
    private static readonly Color FailColor = Color.red;

    private static Image ItemIcon;
    private static Text ResultText;
    private static Text ItemNameText;
    private static Transform Scaler;

    [Flags]
    public enum Filter
    {
        None = 0, Success = 1, Fail = 2
    }
    
    public static bool HasFlagFast(this Filter value, Filter flag) => (value & flag) == flag;
    public static ConfigEntry<Filter> FilterConfig;
    
    [UsedImplicitly]
    private static void OnInit()
    {
        UI = UnityEngine.Object.Instantiate(
            ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_EnchantmentUI_Notification"));
        UI.name = "kg_EnchantmentUI_Notification";
        UI.SetActive(false);
        UnityEngine.Object.DontDestroyOnLoad(UI);
        
        Scaler = UI.transform.Find("Canvas/Scaler");
        ItemIcon = UI.transform.Find("Canvas/Scaler/NotificationItem/bg/Icon").GetComponent<Image>();
        ResultText = UI.transform.Find("Canvas/Scaler/NotificationText/Result").GetComponent<Text>();
        ItemNameText = UI.transform.Find("Canvas/Scaler/NotificationText/Text").GetComponent<Text>();

        _colorGroup.AddRange(UI.GetComponentsInChildren<Image>(true).Where(t => t.name == "colorcontrol").Select(x => x.GetComponent<Image>()));
        
        FilterConfig = ValheimEnchantmentSystem._thistype.Config.Bind("Notifications", "Filter", Filter.Success, "Filter notifications by type");
    }
    
    
    private static readonly Vector3 OriginalScale = new Vector3(1.25f,1.25f,1f);
    private static float _timer;
    private static float _dequeueTimer = 1f;

    public static void Update()
    {
        _dequeueTimer -= Time.deltaTime;
        if (_dequeueTimer <= 0f)
        {
            _dequeueTimer = 1f;
            if (_notifications.Count > 0 && !IsVisible() && Player.m_localPlayer)
            {
                var notification = _notifications.Dequeue();
                ShowNotification(notification);
            }
        }

        if (!IsVisible()) return;
        _timer += Time.deltaTime;
        switch (_timer)
        {
            case >= Duration:
                Hide();
                return;
            case <= FadeDuration:
                Scaler.localScale = OriginalScale * (_timer / FadeDuration);
                break;
            case >= Duration - FadeDuration:
                Scaler.localScale = OriginalScale * ((Duration - _timer) / FadeDuration);
                break;
            default:
                Scaler.localScale = OriginalScale;
                break;
        }
    }
 
    private static void Hide()
    {
        _timer = 0f;
        Scaler.localScale = Vector3.zero;
        UI.SetActive(false);
    }

    private static void ShowNotification(Notification not)
    {
        switch (not.Success) 
        {
            case true when !FilterConfig.Value.HasFlagFast(Filter.Success):
            case false when !FilterConfig.Value.HasFlagFast(Filter.Fail):
                _dequeueTimer = 0f;
                return;
        }
        
        GameObject item = ZNetScene.instance.GetPrefab(not.ItemPrefab);
        if (!item || item.GetComponent<ItemDrop>() is not { } itemDrop) return;

        string localizedItemName = itemDrop.m_itemData.m_shared.m_name.Localize();


        _timer = 0f;
        Scaler.localScale = OriginalScale;

        ResultText.text = not.Success
            ? "$enchantment_notification_success_topic".Localize()
            : "$enchantment_notification_fail_topic".Localize();
        ResultText.color = not.Success ? SuccessColor : FailColor;
        ItemIcon.sprite = itemDrop.m_itemData.GetIcon();
        
        foreach (var image in _colorGroup)
            image.color = not.Success ? SuccessColor : FailColor;

        string text = not.Success
            ? "$enchantment_notification_success".Localize(not.PlayerName, localizedItemName,
                not.PrevLevel.ToString(), not.Level.ToString())
            : SyncedData.ItemDestroyedOnFailure.Value
                ? "$enchantment_notification_fail_destroyed".Localize(not.PlayerName, localizedItemName)
                : "$enchantment_notification_fail".Localize(not.PlayerName, localizedItemName, not.PrevLevel.ToString(),
                    not.Level.ToString());
        ItemNameText.text = text;
        ItemNameText.color = not.Success ? SuccessColor : FailColor;
        UI.SetActive(true);
    }

    public static void AddNotification(string playerName, string itemPrefab, bool success, int prevLevel, int level) =>
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "kg_Enchantment_GlobalNotification",
            playerName ?? "No Name", itemPrefab ?? "No Prefab", success, prevLevel, level);

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    [ClientOnlyPatch]
    private static class ZNetScene_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ZNetScene __instance)
        {
            ZRoutedRpc.instance.Register("kg_Enchantment_GlobalNotification",
                (long _, string playerName, string itemPrefab, bool success, int prevLevel, int level) =>
                {
                    switch (success)
                    {
                        case true when !FilterConfig.Value.HasFlagFast(Filter.Success):
                        case false when !FilterConfig.Value.HasFlagFast(Filter.Fail):
                            return;
                    }
                    
                    _notifications.Enqueue(new Notification
                    {
                        PlayerName = playerName,
                        ItemPrefab = itemPrefab,
                        Success = success,
                        Level = level,
                        PrevLevel = prevLevel
                    });
                });
        }
    }
}