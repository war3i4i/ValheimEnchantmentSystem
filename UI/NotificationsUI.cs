using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem.UI;

[VES_Autoload]
public static class Notifications_UI
{
    public static ConfigEntry<string> _successWebhook;
    public static ConfigEntry<string> _failWebhook;
    public static ConfigEntry<int> _duration;
    private const float FadeDuration = 0.25f;


    private class Notification
    {
        public string PlayerName;
        public string ItemPrefab;
        public int Type;
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
    private static Image Outline;

    [Flags]
    public enum Filter
    {
        None = 0, Success = 1, Fail = 2
    }

    public static bool HasFlagFast(this Filter value, Filter flag) => (value & flag) == flag;
    public static ConfigEntry<Filter> _filterConfig;

    [UsedImplicitly]
    private static void OnInit()
    {
        _successWebhook = ValheimEnchantmentSystem.SyncedConfig.Bind("Notifications", "SuccessWebhook", "", "Discord webhook for notifications");
        _failWebhook = ValheimEnchantmentSystem.SyncedConfig.Bind("Notifications", "FailWebhook", "", "Discord webhook for notifications");
        if (ValheimEnchantmentSystem.NoGraphics) return;
        _filterConfig = ValheimEnchantmentSystem._thistype.Config.Bind("Notifications", "Filter", Filter.Success, "Filter notifications by type");
        _duration = ValheimEnchantmentSystem._thistype.Config.Bind("Notifications", "Duration", 5, "Duration of notification");

        UI = UnityEngine.Object.Instantiate(ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_EnchantmentUI_Notification"));
        UI.name = "kg_EnchantmentUI_Notification";
        UI.SetActive(false);
        UnityEngine.Object.DontDestroyOnLoad(UI);

        Scaler = UI.transform.Find("Canvas/Scaler");
        ItemIcon = UI.transform.Find("Canvas/Scaler/NotificationItem/bg/Icon").GetComponent<Image>();
        ResultText = UI.transform.Find("Canvas/Scaler/NotificationText/Result").GetComponent<Text>();
        ItemNameText = UI.transform.Find("Canvas/Scaler/NotificationText/Text").GetComponent<Text>();
        Outline = UI.transform.Find("Canvas/Scaler/NotificationItem/outline").GetComponent<Image>();
        _colorGroup.AddRange(UI.GetComponentsInChildren<Image>(true).Where(t => t.name == "colorcontrol").Select(x => x.GetComponent<Image>()));
    }


    private static readonly Vector3 OriginalScale = new Vector3(1.25f, 1.25f, 1f);
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
                Notification notification = _notifications.Dequeue();
                ShowNotification(notification);
            }
        }

        if (!IsVisible()) return;
        _timer += Time.deltaTime;

        int duration = _duration.Value;

        if (_timer >= duration)
            Hide();
        else if (_timer <= FadeDuration)
            Scaler.localScale = OriginalScale * (_timer / FadeDuration);
        else if (_timer >= duration - FadeDuration)
            Scaler.localScale = OriginalScale * ((duration - _timer) / FadeDuration);
        else
            Scaler.localScale = OriginalScale;
    }

    private static void Hide()
    {
        _timer = 0f;
        Scaler.localScale = Vector3.zero;
        UI.SetActive(false);
    }

    private static void ShowNotification(Notification not)
    {
        NotificationItemResult type = (NotificationItemResult)not.Type;

        switch (type)
        {
            case NotificationItemResult.Success when !_filterConfig.Value.HasFlagFast(Filter.Success):
                _dequeueTimer = 0f;
                return;
            case NotificationItemResult.LevelDecrease or NotificationItemResult.Destroyed when !_filterConfig.Value.HasFlagFast(Filter.Fail):
                _dequeueTimer = 0f;
                return;
        }


        GameObject item = ZNetScene.instance.GetPrefab(not.ItemPrefab);
        if (!item || item.GetComponent<ItemDrop>() is not { } itemDrop) return;

        string localizedItemName = itemDrop.m_itemData.m_shared.m_name.Localize();


        _timer = 0f;
        Scaler.localScale = OriginalScale;

        ResultText.text = type switch
        {
            NotificationItemResult.Success => "$enchantment_notification_success_topic".Localize(),
            NotificationItemResult.LevelDecrease or NotificationItemResult.Destroyed => "$enchantment_notification_fail_topic".Localize(),
            _ => ""
        };
        ResultText.color = type is NotificationItemResult.Success ? SuccessColor : FailColor;
        ItemIcon.sprite = itemDrop.m_itemData.GetIcon();

        foreach (Image image in _colorGroup)
            image.color = type is NotificationItemResult.Success ? SuccessColor : FailColor;

        string text = type switch
        {
            NotificationItemResult.Success => "$enchantment_notification_success".Localize(not.PlayerName, localizedItemName,
                not.PrevLevel.ToString(), not.Level.ToString()),
            NotificationItemResult.LevelDecrease => "$enchantment_notification_fail".Localize(not.PlayerName, localizedItemName, not.PrevLevel.ToString(),
                not.Level.ToString()),
            NotificationItemResult.Destroyed => "$enchantment_notification_fail_destroyed".Localize(not.PlayerName, localizedItemName, not.PrevLevel.ToString()),
            _ => ""
        };
        ItemNameText.text = text;
        ItemNameText.color = type is NotificationItemResult.Success ? SuccessColor : FailColor;
        Outline.color = SyncedData.GetColor(not.ItemPrefab, not.Level, out _, true, type is NotificationItemResult.Success ? "#00FF00" : "#FF0000").IncreaseColorLight().ToColorAlpha();
        UI.SetActive(true);
    }

    public enum NotificationItemResult { Success, LevelDecrease, Destroyed }

    public static void AddNotification(string playerName, string itemPrefab, int type, int prevLevel, int level)
    {
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "kg_Enchantment_GlobalNotification",
            playerName ?? "No Name", itemPrefab ?? "No Prefab", type, prevLevel, level);
        
        ZRoutedRpc.instance.InvokeRoutedRPC("kg_Enchantment_GlobalNotification_Discord",
            playerName ?? "No Name", itemPrefab ?? "No Prefab", type, prevLevel, level);
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    [ClientOnlyPatch]
    private static class ZNetScene_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ZNetScene __instance)
        {
            ZRoutedRpc.instance.Register("kg_Enchantment_GlobalNotification",
                (long _, string playerName, string itemPrefab, int type, int prevLevel, int level) =>
                {
                    switch ((NotificationItemResult)type)
                    {
                        case NotificationItemResult.Success when !_filterConfig.Value.HasFlagFast(Filter.Success):
                            return;
                        case NotificationItemResult.LevelDecrease or NotificationItemResult.Destroyed when !_filterConfig.Value.HasFlagFast(Filter.Fail):
                            return;
                    }

                    _notifications.Enqueue(new Notification
                    {
                        PlayerName = playerName,
                        ItemPrefab = itemPrefab,
                        Type = type,
                        Level = level,
                        PrevLevel = prevLevel
                    });
                });
        }
    }
    
    [HarmonyPatch(typeof(ZNetScene),nameof(ZNetScene.Awake))]
    private static class ZNetScene_Awake_Patch_Both
    {
        [UsedImplicitly]
        private static void Postfix(ZNetScene __instance)
        {
            ZRoutedRpc.instance.Register("kg_Enchantment_GlobalNotification_Discord",
                (long _, string playerName, string itemPrefab, int type, int prevLevel, int level) =>
                {
                    if (ZNet.instance.IsServer())
                    {
                        string link = (NotificationItemResult)type is NotificationItemResult.Success ? _successWebhook.Value : _failWebhook.Value;
                        if (!string.IsNullOrEmpty(link))
                        {
                            string localizedItemName = itemPrefab;
                            if (ZNetScene.instance.GetPrefab(itemPrefab)?.GetComponent<ItemDrop>() is { } itemDrop) localizedItemName = itemDrop.m_itemData.m_shared.m_name.Localize();
                            string text = (NotificationItemResult)type switch
                            {
                                NotificationItemResult.Success => "$enchantment_notification_success".Localize(playerName, localizedItemName, prevLevel.ToString(), level.ToString()),
                                NotificationItemResult.LevelDecrease => "$enchantment_notification_fail".Localize(playerName, localizedItemName, prevLevel.ToString(), level.ToString()),
                                NotificationItemResult.Destroyed => "$enchantment_notification_fail_destroyed".Localize(playerName, localizedItemName, prevLevel.ToString()),
                                _ => ""
                            };
                            DiscordWebhook.TrySend(link, text);
                        }
                    }
                });
        }
    }
}