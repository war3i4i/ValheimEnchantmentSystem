using System.Text;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;
using ServerSync;
using ISP_Auto;

namespace kg.ValheimEnchantmentSystem.Configs;

[VES_Autoload(VES_Autoload.Priority.First)]
public static class SyncedData
{
    private static FileSystemWatcher FSW;
    private static string YAML_Chances;
    private static string YAML_Stats_Weapons;
    private static string YAML_Stats_Armor;
    private static string YAML_Colors;
    private static string YAML_Reqs;

    private static string Directory_Overrides_Chances;
    private static string Directory_Overrides_Stats;
    private static string Directory_Overrides_Colors;
    private static string Directory_Reqs;
    

    private static readonly Dictionary<string, Action> FSW_Mapper = new();
    
    [UsedImplicitly]
    private static void OnInit()
    {
        SafetyLevel = ValheimEnchantmentSystem.config("Enchantment", "SafetyLevel", 3,
            "The level until which enchantments won't destroy the item. Set to 0 to disable.");
        DropEnchantmentOnUpgrade = ValheimEnchantmentSystem.config("Enchantment", "DropEnchantmentOnUpgrade", false, "Drop enchantment on item upgrade.");
        ItemFailureType = ValheimEnchantmentSystem.config("Enchantment", "ItemFailureType", ItemDesctructionTypeEnum.LevelDecrease, "LevelDecrease will remove one level on fail, Destroy will destroy item on fail, Combined will use yaml destroy chance and success chance.");
        AllowJewelcraftingMirrorCopyEnchant = ValheimEnchantmentSystem.config("Enchantment", "AllowJewelcraftingMirrorCopyEnchant", false, "Allow jewelcrafting to copy enchantment from one item to another using mirror.");
        AdditionalEnchantmentChancePerLevel = ValheimEnchantmentSystem.config("Enchantment", "AdditionalEnchantmentChancePerLevel", 0.06f, "Additional enchantment chance per level of Enchantment skill.");
        AllowVFXArmor = ValheimEnchantmentSystem.config("Enchantment", "AllowVFXArmor", false, "Allow VFX on armor.");
        EnchantmentEnableNotifications = ValheimEnchantmentSystem.config("Notifications", "EnchantmentEnableNotifications", true, "Enable enchantment notifications.");
        EnchantmentNotificationMinLevel = ValheimEnchantmentSystem.config("Notifications", "EnchantmentNotificationMinLevel", 6, "The minimum level of enchantment to show notification.");

        YAML_Stats_Weapons = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "EnchantmentStats_Weapons.yml");
        YAML_Stats_Armor = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "EnchantmentStats_Armor.yml");
        YAML_Colors = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "EnchantmentColors.yml");
        YAML_Reqs = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "EnchantmentReqs.yml");
        YAML_Chances = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "EnchantmentChancesV2.yml");
        Directory_Reqs = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "AdditionalEnchantmentReqs");
        Directory_Overrides_Chances = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "AdditionalOverrides_EnchantmentChances");
        Directory_Overrides_Stats = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "AdditionalOverrides_EnchantmentStats");
        Directory_Overrides_Colors = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "AdditionalOverrides_EnchantmentColors");
        
        if (!Directory.Exists(Directory_Reqs))
            Directory.CreateDirectory(Directory_Reqs);
        if (!Directory.Exists(Directory_Overrides_Chances))
            Directory.CreateDirectory(Directory_Overrides_Chances);
        if (!Directory.Exists(Directory_Overrides_Stats))
            Directory.CreateDirectory(Directory_Overrides_Stats);
        if (!Directory.Exists(Directory_Overrides_Colors))
            Directory.CreateDirectory(Directory_Overrides_Colors);
        

        if (!File.Exists(YAML_Chances))
            YAML_Chances.WriteFile(Defaults.YAML_Chances);
        if (!File.Exists(YAML_Stats_Weapons))
            YAML_Stats_Weapons.WriteFile(Defaults.YAML_Stats_Weapons);
        if (!File.Exists(YAML_Stats_Armor))
            YAML_Stats_Armor.WriteFile(Defaults.YAML_Stats_Armor);
        if (!File.Exists(YAML_Colors))
            YAML_Colors.WriteFile(Defaults.YAML_Colors);
        if (!File.Exists(YAML_Reqs))
            YAML_Reqs.WriteFile(Defaults.YAML_Reqs);

        Synced_EnchantmentChances.ValueChanged += ResetInventory;
        Synced_EnchantmentStats_Weapons.ValueChanged += ResetInventory;
        Synced_EnchantmentStats_Armor.ValueChanged += ResetInventory;
        Synced_EnchantmentColors.ValueChanged += ResetInventory;
        Synced_EnchantmentReqs.ValueChanged += ResetInventory;
        Overrides_EnchantmentChances.ValueChanged += ResetInventory;
        Overrides_EnchantmentStats.ValueChanged += ResetInventory;
        Overrides_EnchantmentColors.ValueChanged += ResetInventory;
        
        Overrides_EnchantmentChances.ValueChanged += OptimizeChances;
        Overrides_EnchantmentStats.ValueChanged += OptimizeStats;
        Overrides_EnchantmentColors.ValueChanged += OptimizeColors;
        
        Synced_EnchantmentChances.Value = YAML_Chances.FromYAML<Dictionary<int, Chance_Data>>();
        Synced_EnchantmentStats_Weapons.Value = YAML_Stats_Weapons.FromYAML<Dictionary<int, Stat_Data>>();
        Synced_EnchantmentStats_Armor.Value = YAML_Stats_Armor.FromYAML<Dictionary<int, Stat_Data>>();
        Synced_EnchantmentColors.Value = YAML_Colors.FromYAML<Dictionary<int, VFX_Data>>();
        ReadReqs();
        ReadOverrideChances();
        ReadOverrideStats();
        ReadOverrideColors();
        OptimizeChances();
        OptimizeStats();
        OptimizeColors();
        
        FSW_Mapper.Add(YAML_Chances, () => Synced_EnchantmentChances.Value = YAML_Chances.FromYAML<Dictionary<int, Chance_Data>>());
        FSW_Mapper.Add(YAML_Stats_Weapons, () => Synced_EnchantmentStats_Weapons.Value = YAML_Stats_Weapons.FromYAML<Dictionary<int, Stat_Data>>());
        FSW_Mapper.Add(YAML_Stats_Armor, () => Synced_EnchantmentStats_Armor.Value = YAML_Stats_Armor.FromYAML<Dictionary<int, Stat_Data>>());
        FSW_Mapper.Add(YAML_Colors, () => Synced_EnchantmentColors.Value = YAML_Colors.FromYAML<Dictionary<int, VFX_Data>>());
        FSW_Mapper.Add(YAML_Reqs, ReadReqs);
        FSW_Mapper.Add(ValheimEnchantmentSystem.SyncedConfig.ConfigFilePath, () => ValheimEnchantmentSystem.SyncedConfig.Reload());
        FSW_Mapper.Add(ValheimEnchantmentSystem.ItemConfig.ConfigFilePath, () => ValheimEnchantmentSystem.ItemConfig.Reload());
        FSW_Mapper.Add(Directory_Reqs, ReadReqs);
        FSW_Mapper.Add(Directory_Overrides_Chances, ReadOverrideChances);
        FSW_Mapper.Add(Directory_Overrides_Stats, ReadOverrideStats);
        FSW_Mapper.Add(Directory_Overrides_Colors, ReadOverrideColors);
        FSW = new FileSystemWatcher(ValheimEnchantmentSystem.ConfigFolder)
        {
            EnableRaisingEvents = true,
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite,
            SynchronizingObject = ThreadingHelper.SynchronizingObject
        };
        FSW.Changed += ConfigChanged;
    }
    private static void OptimizeChances()
    {
        OPTIMIZED_Overrides_EnchantmentChances.Clear();
        foreach (Defaults.OverrideChances chance in Overrides_EnchantmentChances.Value)
            foreach (string entry in chance.Items)
                 OPTIMIZED_Overrides_EnchantmentChances[entry] = chance.Chances;
    }
    private static void OptimizeColors()
    {
        OPTIMIZED_Overrides_EnchantmentColors.Clear();
        foreach (Defaults.OverrideColors chance in Overrides_EnchantmentColors.Value)
            foreach (string entry in chance.Items)
                OPTIMIZED_Overrides_EnchantmentColors[entry] = chance.Colors;
    }
    private static void OptimizeStats()
    {
        OPTIMIZED_Overrides_EnchantmentStats.Clear();
        foreach (Defaults.OverrideStats chance in Overrides_EnchantmentStats.Value)
            foreach (string entry in chance.Items)
                OPTIMIZED_Overrides_EnchantmentStats[entry] = chance.Stats;
    }
    private static void ReadReqs()
    {
        List<EnchantmentReqs> result = new();
        if(YAML_Reqs.FromYAML<List<EnchantmentReqs>>() is { } yamlData)
            result.AddRange(yamlData);
        
        foreach (string file in Directory.GetFiles(Directory_Reqs, "*.yml", SearchOption.TopDirectoryOnly))
            if (file.FromYAML<List<EnchantmentReqs>>() is { } data)
                result.AddRange(data);
        
        Synced_EnchantmentReqs.Value = result;
    }
    private static void ReadOverrideChances()
    {
        List<Defaults.OverrideChances> result = new();

        foreach (string file in Directory.GetFiles(Directory_Overrides_Chances, "*.yml", SearchOption.TopDirectoryOnly))
            if (file.FromYAML<List<Defaults.OverrideChances>>() is {} data)
                result.AddRange(data);

        Overrides_EnchantmentChances.Value = result;
    }
    private static void ReadOverrideStats()
    {
        List<Defaults.OverrideStats> result = new();

        foreach (string file in Directory.GetFiles(Directory_Overrides_Stats, "*.yml", SearchOption.TopDirectoryOnly))
            if (file.FromYAML<List<Defaults.OverrideStats>>() is {} data)
                result.AddRange(data);

        Overrides_EnchantmentStats.Value = result;
    }
    private static void ReadOverrideColors()
    {
        List<Defaults.OverrideColors> result = new();

        foreach (string file in Directory.GetFiles(Directory_Overrides_Colors, "*.yml", SearchOption.TopDirectoryOnly))
            if (file.FromYAML<List<Defaults.OverrideColors>>() is {} data)
                result.AddRange(data);

        Overrides_EnchantmentColors.Value = result;
    }
    
    private static void ResetInventory()
    {
        Enchantment_VFX.UpdateGrid();
        Enchantment_AdditionalEffects.UpdateVFXs();
    }

    private static DateTime LastConfigChange = DateTime.Now;
    private static void ConfigChanged(object sender, FileSystemEventArgs e)
    {
        if (!Game.instance || !ZNet.instance || !ZNet.instance.IsServer()) return;
        if (e.ChangeType != WatcherChangeTypes.Changed) return;
        string extention = Path.GetExtension(e.FullPath);
        if (extention != ".yml" && extention != ".cfg") return;
        if (FSW_Mapper.TryGetValue(e.FullPath, out Action action))
        {
            if (DateTime.Now - LastConfigChange < TimeSpan.FromSeconds(3)) return;
            LastConfigChange = DateTime.Now;
            try
            {
                Utils.print($"Reloading config {e.FullPath}");
                action.Invoke();
            } 
            catch (Exception ex)
            {
                Utils.print($"Error while reloading config {e.FullPath}: {ex}", ConsoleColor.Red); 
            }
            return;
        }
        string folder = Path.GetDirectoryName(e.FullPath);
        if (folder == null) return;
        if (FSW_Mapper.TryGetValue(folder, out action))
        {
            if (DateTime.Now - LastConfigChange < TimeSpan.FromSeconds(3)) return;
            LastConfigChange = DateTime.Now;
            try
            {
                Utils.print($"Reloading config {e.FullPath}");
                action.Invoke();
            }
            catch (Exception ex)
            {
                Utils.print($"Error while reloading config {e.FullPath}: {ex}", ConsoleColor.Red);
            }
        }
    }

    public static string GetColor(Enchantment_Core.Enchanted en, out int variant, bool trimApha) =>
        GetColor(en.Item.m_dropPrefab?.name, en.level, out variant, trimApha);

    public static string GetColor(string dropPrefab, int level, out int variant, bool trimApha, string defaultValue = "#00000000")
    {
        variant = 0;
        if (level == 0) return trimApha ? defaultValue.Substring(0,7) : defaultValue;
        if (dropPrefab != null && OPTIMIZED_Overrides_EnchantmentColors.TryGetValue(dropPrefab, out Dictionary<int, VFX_Data> overriden))
        {
            if (overriden.TryGetValue(level, out VFX_Data overrideVfxData))
            {
                string result = overrideVfxData.color;
                if (trimApha) result = result.Substring(0, 7);
                variant = Mathf.Clamp(overrideVfxData.variant, 0, Enchantment_VFX.VFXs.Count - 1);
                return result;
            }
        }
        
        if (Synced_EnchantmentColors.Value.TryGetValue(level, out VFX_Data vfxData))
        {
            string result = vfxData.color;
            if (trimApha) result = result.Substring(0, 7);
            variant = Mathf.Clamp(vfxData.variant - 1, 0, Enchantment_VFX.VFXs.Count - 1);
            return result;
        }

        return trimApha ? defaultValue.Substring(0,7) : defaultValue;
    }

    public static Enchantment_AdditionalEffects.AdditionalEffectsModule GetAdditionalEffects(Enchantment_Core.Enchanted en)
    {
        if (en.level == 0) return null;
        string dropPrefab = en.Item.m_dropPrefab?.name;
        if (dropPrefab != null && OPTIMIZED_Overrides_EnchantmentColors.TryGetValue(dropPrefab, out Dictionary<int, VFX_Data> overriden))
        {
            return overriden.TryGetValue(en.level, out VFX_Data overrideChance) ? overrideChance.additionaleffects : null;
        }
        
        return Synced_EnchantmentColors.Value.TryGetValue(en.level, out VFX_Data vfxData) ? vfxData.additionaleffects : null;
    }

    public static Chance_Data GetEnchantmentChance(Enchantment_Core.Enchanted en)
        => GetEnchantmentChance(en.Item.m_dropPrefab?.name, en.level);

    private static Chance_Data GetEnchantmentChance(string dropPrefab, int level)
    {
        if (level == 0) return new Chance_Data() { success = 100 };
        if (dropPrefab != null && OPTIMIZED_Overrides_EnchantmentChances.TryGetValue(dropPrefab, out Dictionary<int, Chance_Data> overriden))
        {
            if (overriden.TryGetValue(level, out Chance_Data overrideChance))
                return overrideChance;
        }

        return Synced_EnchantmentChances.Value.TryGetValue(level, out Chance_Data chance) ? chance : new Chance_Data() { success = 0 };
    }

    public static Stat_Data GetStatIncrease(Enchantment_Core.Enchanted en)
    {
        if (en.level == 0) return null;
        string dropPrefab = en.Item.m_dropPrefab?.name;
        if (dropPrefab != null && OPTIMIZED_Overrides_EnchantmentStats.TryGetValue(dropPrefab, out Dictionary<int, Stat_Data> overriden))
        {
            return overriden.TryGetValue(en.level, out Stat_Data overrideChance) ? overrideChance : null;
        }

        Dictionary<int, Stat_Data> target = en.Item.IsWeapon() ? Synced_EnchantmentStats_Weapons.Value : Synced_EnchantmentStats_Armor.Value;
        return target.TryGetValue(en.level, out Stat_Data increase) ? increase : null;
    }

    public static EnchantmentReqs GetReqs(string prefab)
    {
        return prefab == null ? null : Synced_EnchantmentReqs.Value.Find(x => x.Items.Contains(prefab));
    }

    public static float GetAdditionalEnchantmentChance()
    {
        if (!Player.m_localPlayer) return 0;
        float enchantmentLevel = Player.m_localPlayer.GetSkillLevel(Enchantment_Skill.SkillType_Enchantment);
        return enchantmentLevel * AdditionalEnchantmentChancePerLevel.Value;
    }

    public enum ItemDesctructionTypeEnum{ LevelDecrease, Destroy, Combined }
    
    public static ConfigEntry<int> SafetyLevel;
    public static ConfigEntry<bool> DropEnchantmentOnUpgrade;
    public static ConfigEntry<ItemDesctructionTypeEnum> ItemFailureType;
    public static ConfigEntry<bool> AllowJewelcraftingMirrorCopyEnchant;
    public static ConfigEntry<float> AdditionalEnchantmentChancePerLevel;
    public static ConfigEntry<int> EnchantmentNotificationMinLevel;
    public static ConfigEntry<bool> EnchantmentEnableNotifications;
    public static ConfigEntry<bool> AllowVFXArmor;

    public static readonly CustomSyncedValue<Dictionary<int, Chance_Data>> Synced_EnchantmentChances =
        new(ValheimEnchantmentSystem.ConfigSync, "EnchantmentGlobalChances",
            new Dictionary<int, Chance_Data>());

    public static readonly CustomSyncedValue<Dictionary<int, VFX_Data>> Synced_EnchantmentColors =
        new(ValheimEnchantmentSystem.ConfigSync, "OverridenEnchantmentColors",
            new Dictionary<int, VFX_Data>());

    public static readonly CustomSyncedValue<Dictionary<int, Stat_Data>> Synced_EnchantmentStats_Weapons =
        new(ValheimEnchantmentSystem.ConfigSync, "EnchantmentStats_Weapons",
            new Dictionary<int, Stat_Data>());
    
    public static readonly CustomSyncedValue<Dictionary<int, Stat_Data>> Synced_EnchantmentStats_Armor =
        new(ValheimEnchantmentSystem.ConfigSync, "EnchantmentStats_Armor",
            new Dictionary<int, Stat_Data>());

    public static readonly CustomSyncedValue<List<Defaults.OverrideChances>> Overrides_EnchantmentChances =
        new(ValheimEnchantmentSystem.ConfigSync, "Overrides_EnchantmentChances",
            new());

    public static readonly CustomSyncedValue<List<Defaults.OverrideColors>> Overrides_EnchantmentColors =
            new(ValheimEnchantmentSystem.ConfigSync, "Overrides_EnchantmentColors",
                new());

    public static readonly CustomSyncedValue<List<Defaults.OverrideStats>> Overrides_EnchantmentStats =
            new(ValheimEnchantmentSystem.ConfigSync, "Overrides_EnchantmentStats",
                new());

    public static readonly CustomSyncedValue<List<EnchantmentReqs>> Synced_EnchantmentReqs =
        new(ValheimEnchantmentSystem.ConfigSync, "EnchantmentReqs",
            new List<EnchantmentReqs>());

    private static readonly Dictionary<string, Dictionary<int, Chance_Data>> OPTIMIZED_Overrides_EnchantmentChances = new();
    private static readonly Dictionary<string, Dictionary<int, VFX_Data>> OPTIMIZED_Overrides_EnchantmentColors = new();
    private static readonly Dictionary<string, Dictionary<int, Stat_Data>> OPTIMIZED_Overrides_EnchantmentStats = new();
    
    [AutoSerialize]
    public class Chance_Data : ISerializableParameter
    {
        [SerializeField] public int success;
        [SerializeField] public int destroy;
        public void Serialize  (ref ZPackage pkg) => throw new NotImplementedException();
        public void Deserialize(ref ZPackage pkg) => throw new NotImplementedException();
    }
    
    [AutoSerialize]
    public partial class Stat_Data : ISerializableParameter
    {
        [SerializeField] public int durability;
        [SerializeField] public int durability_percentage;
        [SerializeField] public int armor_percentage;
        [SerializeField] public int armor;
        [SerializeField] public int damage_percentage;
        [SerializeField] public int damage_true;
        [SerializeField] public int damage_blunt;
        [SerializeField] public int damage_slash;
        [SerializeField] public int damage_pierce;
        [SerializeField] public int damage_chop;
        [SerializeField] public int damage_pickaxe;
        [SerializeField] public int damage_fire;
        [SerializeField] public int damage_frost;
        [SerializeField] public int damage_lightning;
        [SerializeField] public int damage_poison;
        [SerializeField] public int damage_spirit;
        [SerializeField] public HitData.DamageModifier resistance_blunt = HitData.DamageModifier.Normal;
        [SerializeField] public HitData.DamageModifier resistance_slash = HitData.DamageModifier.Normal;
        [SerializeField] public HitData.DamageModifier resistance_pierce = HitData.DamageModifier.Normal;
        [SerializeField] public HitData.DamageModifier resistance_chop = HitData.DamageModifier.Normal;
        [SerializeField] public HitData.DamageModifier resistance_pickaxe = HitData.DamageModifier.Normal;
        [SerializeField] public HitData.DamageModifier resistance_fire = HitData.DamageModifier.Normal;
        [SerializeField] public HitData.DamageModifier resistance_frost = HitData.DamageModifier.Normal;
        [SerializeField] public HitData.DamageModifier resistance_lightning = HitData.DamageModifier.Normal;
        [SerializeField] public HitData.DamageModifier resistance_poison = HitData.DamageModifier.Normal;
        [SerializeField] public HitData.DamageModifier resistance_spirit = HitData.DamageModifier.Normal;
        [SerializeField] public int attack_speed;
        [SerializeField] public int slash_wave;
        [SerializeField] public int movement_speed;
        
        //api stats
        [SerializeField] public int API_backpacks_additionalrow_x;
        [SerializeField] public int API_backpacks_additionalrow_y;
        
        public void Serialize  (ref ZPackage pkg) => throw new NotImplementedException();
        public void Deserialize(ref ZPackage pkg) => throw new NotImplementedException();
        public static implicit operator bool(Stat_Data data) => data != null;
    }

    public partial class Stat_Data
    {
        private bool ShouldShow()
        {
            return damage_true != 0 || damage_blunt != 0 || damage_slash != 0 || damage_pierce != 0 ||
                   damage_chop != 0 || damage_pickaxe != 0 || damage_fire != 0 || damage_frost != 0 ||
                   damage_lightning != 0 || damage_poison != 0 || damage_spirit != 0 || armor != 0 ||
                   durability != 0 || resistance_blunt != HitData.DamageModifier.Normal || resistance_slash != HitData.DamageModifier.Normal ||
                   resistance_pierce != HitData.DamageModifier.Normal || resistance_chop != HitData.DamageModifier.Normal ||
                   resistance_pickaxe != HitData.DamageModifier.Normal || resistance_fire != HitData.DamageModifier.Normal ||
                   resistance_frost != HitData.DamageModifier.Normal || resistance_lightning != HitData.DamageModifier.Normal ||
                   resistance_poison != HitData.DamageModifier.Normal|| resistance_spirit != HitData.DamageModifier.Normal || 
                   attack_speed != 0 || slash_wave != 0 || movement_speed != 0 || API_backpacks_additionalrow_x != 0 || API_backpacks_additionalrow_y != 0;
        }
        
        private List<HitData.DamageModPair> cached_resistance_pairs;
        public List<HitData.DamageModPair> GetResistancePairs()
        {
            if (cached_resistance_pairs != null) return cached_resistance_pairs;
            cached_resistance_pairs = new()
            {
                new() { m_type = HitData.DamageType.Blunt, m_modifier = resistance_blunt },
                new() { m_type = HitData.DamageType.Slash, m_modifier = resistance_slash },
                new() { m_type = HitData.DamageType.Pierce, m_modifier = resistance_pierce },
                new() { m_type = HitData.DamageType.Chop, m_modifier = resistance_chop },
                new() { m_type = HitData.DamageType.Pickaxe, m_modifier = resistance_pickaxe },
                new() { m_type = HitData.DamageType.Fire, m_modifier = resistance_fire },
                new() { m_type = HitData.DamageType.Frost, m_modifier = resistance_frost },
                new() { m_type = HitData.DamageType.Lightning, m_modifier = resistance_lightning },
                new() { m_type = HitData.DamageType.Poison, m_modifier = resistance_poison },
                new() { m_type = HitData.DamageType.Spirit, m_modifier = resistance_spirit },
            };
            cached_resistance_pairs.RemoveAll(x => x.m_modifier == HitData.DamageModifier.Normal);
            return cached_resistance_pairs;
        }

        private string cached_tooltip;
        public string BuildAdditionalStats(string color)
        {
            if (cached_tooltip != null) return cached_tooltip;
            if (!ShouldShow())
            {
                cached_tooltip = "\n";
                return cached_tooltip;
            }
            StringBuilder builder = new StringBuilder();
            if (attack_speed > 0) builder.Append($"\n<color={color}>•</color> $enchantment_attackspeed: <color=#DF745D>{attack_speed}%</color>");
            if (movement_speed > 0) builder.Append($"\n<color={color}>•</color> $enchantment_movementspeed: <color=#DF745D>{movement_speed}%</color>");
            if (slash_wave > 0) builder.Append($"\n<color={color}>•</color> $enchantment_slashwave: <color=#DF74FD>{slash_wave}</color>");
            if (damage_true > 0) builder.Append($"\n<color={color}>•</color> $enchantment_truedamage: {damage_true}");
            if (damage_fire > 0) builder.Append($"\n<color={color}>•</color> $inventory_fire: <color=#FFA500>{damage_fire}</color>");
            if (damage_blunt > 0) builder.Append($"\n<color={color}>•</color> $inventory_blunt: <color=#FFFF00>{damage_blunt}</color>");
            if (damage_slash > 0) builder.Append($"\n<color={color}>•</color> $inventory_slash: <color=#7F00FF>{damage_slash}</color>");
            if (damage_pierce > 0) builder.Append($"\n<color={color}>•</color> $inventory_pierce: <color=#D499B9>{damage_pierce}</color>");
            if (damage_chop > 0) builder.Append($"\n<color={color}>•</color> $enchantment_chopdamage: <color=#FFAF00>{damage_chop}</color>");
            if (damage_pickaxe > 0) builder.Append($"\n<color={color}>•</color> $enchantment_pickaxedamage: <color=#FF00FF>{damage_pickaxe}</color>");
            if (damage_frost > 0) builder.Append($"\n<color={color}>•</color> $inventory_frost: <color=#00FFFF>{damage_frost}</color>");
            if (damage_lightning > 0) builder.Append($"\n<color={color}>•</color> $inventory_lightning: <color=#0000FF>{damage_lightning}</color>");
            if (damage_poison > 0) builder.Append($"\n<color={color}>•</color> $inventory_poison: <color=#00FF00>{damage_poison}</color>");
            if (damage_spirit > 0) builder.Append($"\n<color={color}>•</color> $inventory_spirit: <color=#FFFFA0>{damage_spirit}</color>");
            if (armor > 0) builder.Append($"\n<color={color}>•</color> $item_armor: <color=#808080>{armor}</color>");
            if (durability > 0) builder.Append($"\n<color={color}>•</color> $item_durability: <color=#7393B3>{durability}</color>");
            if (API_backpacks_additionalrow_x > 0) builder.Append($"\n<color={color}>•</color> $enchantment_backpacks_additionalrow_x: <color=#7393B3>{API_backpacks_additionalrow_x}</color>");
            if (API_backpacks_additionalrow_y > 0) builder.Append($"\n<color={color}>•</color> $enchantment_backpacks_additionalrow_x: <color=#7393B3>{API_backpacks_additionalrow_y}</color>");
            
            builder.Append(SE_Stats.GetDamageModifiersTooltipString(GetResistancePairs()).Replace("\n", $"\n<color={color}>•</color> "));
            
            builder.Append("\n");
            cached_tooltip = builder.ToString();
            return cached_tooltip;
        }

        public string Info_Description()
        {
            string result = "";
            if (damage_percentage > 0)
            {
                result += $"\n• $enchantment_bonusespercentdamage (<color=#AF009F>+{damage_percentage}%</color>)";
            }
            if (armor_percentage > 0)
            {
                result += $"\n• $enchantment_bonusespercentarmor (<color=#009FAF>+{armor_percentage}%</color>)";
            }
            result += BuildAdditionalStats("#FFFFFF");
            return result;
        }
    }
    
    
    [AutoSerialize]
    public class req : ISerializableParameter
    {
        [SerializeField] public string prefab;
        [SerializeField] public int amount;
        public req() { }
        public req (string prefab, int amount) { this.prefab = prefab; this.amount = amount; }
        public bool IsValid() => !string.IsNullOrEmpty(prefab) && amount > 0 && ZNetScene.instance.GetPrefab(prefab);
        public void Serialize  (ref ZPackage pkg) => throw new NotImplementedException();
        public void Deserialize(ref ZPackage pkg) => throw new NotImplementedException();
    }
    
    [AutoSerialize]
    public class EnchantmentReqs : ISerializableParameter
    {
        [SerializeField] public int required_skill = 0;
        [SerializeField] public req enchant_prefab = new();
        [SerializeField] public req blessed_enchant_prefab = new();
        [SerializeField] public List<string> Items = new();
        public void Serialize  (ref ZPackage pkg) => throw new NotImplementedException();
        public void Deserialize(ref ZPackage pkg) => throw new NotImplementedException();
    }

    [AutoSerialize]
    public class VFX_Data : ISerializableParameter
    {
        [SerializeField] public string color = "#00000000";
        [SerializeField] public int variant;
        [SerializeField] public Enchantment_AdditionalEffects.AdditionalEffectsModule additionaleffects;
        public void Serialize  (ref ZPackage pkg) => throw new NotImplementedException();
        public void Deserialize(ref ZPackage pkg) => throw new NotImplementedException();
    }
    
}