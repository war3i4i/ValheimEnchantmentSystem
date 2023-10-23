using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace kg.ValheimEnchantmentSystem.Configs;

public static class Defaults
{
    private static readonly Dictionary<int, int> DefaultStats_Weapons = new Dictionary<int, int>()
    {
        { 1, 2 }, { 2, 4 }, { 3, 8 }, { 4, 12 }, { 5, 16 }, { 6, 20 }, { 7, 24 }, { 8, 28 }, { 9, 32 }, { 10, 40 },
        { 11, 44 }, { 12, 48 }, { 13, 52 }, { 14, 56 }, { 15, 60 }, { 16, 65 }, { 17, 70 }, { 18, 80 }, { 19, 90 }, { 20, 100 }
    };
    private static readonly Dictionary<int, int> DefaultStats_Armor = new Dictionary<int, int>()
    {
        { 1, 2 }, { 2, 4 }, { 3, 6 }, { 4, 8 }, { 5, 10 }, { 6, 12 }, { 7, 14 }, { 8, 16 }, { 9, 18 }, { 10, 20 },
        { 11, 22 }, { 12, 24 }, { 13, 26 }, { 14, 28 }, { 15, 30 }, { 16, 33 }, { 17, 36 }, { 18, 39 }, { 19, 42 }, { 20, 45 }
    };
    private static readonly Dictionary<int, int> DefaultChances = new()
    {
        { 1, 80 }, { 2, 75 }, { 3, 70 }, { 4, 60 }, { 5, 55 }, { 6, 50 }, { 7, 40 }, { 8, 35 }, { 9, 30 }, { 10, 26 },
        { 11, 22 }, { 12, 18 }, { 13, 14 }, { 14, 10 }, { 15, 8 }, { 16, 6 }, { 17, 5 }, { 18, 4 }, { 19, 3 }, { 20, 0 }
    };
    private static readonly Dictionary<int, SyncedData.VFX_Data> DefaultColors = new Dictionary<int, SyncedData.VFX_Data>
    {
        { 1,  new() { color = "#1E151C01", variant = 0 } },
        { 2,  new() { color = "#1E181F02", variant = 0 } },
        { 3,  new() { color = "#1E1A2A03", variant = 0 } },
        { 4,  new() { color = "#1E1E3AA6", variant = 0 } },
        { 5,  new() { color = "#1E1E4AB0", variant = 0 } },
        { 6,  new() { color = "#23415A9B", variant = 0 } },
        { 7,  new() { color = "#28577EA2", variant = 0 } },
        { 8,  new() { color = "#1E508EA9", variant = 0 } },
        { 9,  new() { color = "#14469EB0", variant = 0 } },
        { 10, new() { color = "#0A3CAFB7", variant = 0 } },
        { 11, new() { color = "#0038BFC0", variant = 0 } },
        { 12, new() { color = "#0038BFC0", variant = 0 } },
        { 13, new() { color = "#001CDBC4", variant = 0 } },
        { 14, new() { color = "#001CDBDB", variant = 0 } },
        { 15, new() { color = "#001CDFE2", variant = 0 } },
        { 16, new() { color = "#A0140EE9", variant = 0 } },
        { 17, new() { color = "#B40A0EF0", variant = 0 } },
        { 18, new() { color = "#C8000EF7", variant = 0 } },
        { 19, new() { color = "#D2000EFE", variant = 0 } },
        { 20, new() { color = "#FF000EFF", variant = 0 } }
    };
    private static readonly List<SyncedData.EnchantmentReqs> DefaultReqs = new()
    {
        new SyncedData.EnchantmentReqs() { enchant_prefab = new("kg_EnchantScroll_Weapon_S", 1), blessed_enchant_prefab = new("kg_EnchantScroll_Weapon_Blessed_S", 1), Items = new()
        {
            "AtgeirHimminAfl", "AxeJotunBane", "BowSpineSnap", "PickaxeBlackMetal", "ShieldCarapace", "ShieldCarapaceBuckler", "SledgeDemolisher", "SpearCarapace",
            "StaffFireball", "StaffIceShards", "StaffShield", "StaffSkeleton", "SwordMistwalker", "THSwordKrom", "CrossbowArbalest", "SwordCheat"
        }},
        new SyncedData.EnchantmentReqs() { enchant_prefab = new("kg_EnchantScroll_Armor_S", 1), blessed_enchant_prefab = new("kg_EnchantScroll_Armor_Blessed_S", 1), Items = new()
        {
            "ArmorCarapaceChest", "ArmorCarapaceLegs", "ArmorMageChest", "ArmorMageLegs", "CapeFeather", "HelmetCarapace", "HelmetMage"
        }},
        new SyncedData.EnchantmentReqs() { enchant_prefab = new("kg_EnchantScroll_Weapon_A", 1), blessed_enchant_prefab = new("kg_EnchantScroll_Weapon_Blessed_A", 1), Items = new()
        {
           "AtgeirBlackmetal", "AxeBlackMetal", "BattleaxeCrystal", "BowDraugrFang","Demister", "FistFenrirClaw", "KnifeBlackMetal", "KnifeSilver", 
           "KnifeSkollAndHati", "MaceSilver", "ShieldBlackmetal", "ShieldBlackmetalTower", "ShieldSilver", "SpearWolfFang", "SwordBlackmetal", "SwordSilver",
          
        }},
        new SyncedData.EnchantmentReqs() { enchant_prefab = new("kg_EnchantScroll_Armor_A", 1), blessed_enchant_prefab = new("kg_EnchantScroll_Armor_Blessed_A", 1), Items = new()
        {
            "ArmorWolfChest", "ArmorWolfLegs", "CapeLinen", "CapeLox", "CapeWolf", "HelmetDrake", "ArmorFenringChest", "ArmorFenringLegs", "HelmetFenring", 
        }},
        new SyncedData.EnchantmentReqs() { enchant_prefab = new("kg_EnchantScroll_Weapon_B", 1), blessed_enchant_prefab = new("kg_EnchantScroll_Weapon_Blessed_B", 1), Items = new()
        {
            "AtgeirIron", "AxeIron", "Battleaxe", "BowHuntsman","Lantern", "MaceIron", "MaceNeedle", "PickaxeIron", 
            "ShieldBanded", "ShieldIronBuckler", "ShieldIronSquare", "ShieldIronTower", "ShieldSerpentscale",
            "SledgeIron", "SpearElderbark", "SwordIron", "TankardAnniversary", "TorchMist", "KnifeChitin",  "SpearChitin"
        }},
        new SyncedData.EnchantmentReqs() { enchant_prefab = new("kg_EnchantScroll_Armor_B", 1), blessed_enchant_prefab = new("kg_EnchantScroll_Armor_Blessed_B", 1), Items = new()
        {
            "ArmorIronChest", "ArmorIronLegs", "ArmorPaddedCuirass", "ArmorPaddedGreaves", "ArmorRootChest", "ArmorRootLegs", "HelmetIron", "HelmetPadded", "HelmetRoot"
        }},
        new SyncedData.EnchantmentReqs() { enchant_prefab = new("kg_EnchantScroll_Weapon_C", 1), blessed_enchant_prefab = new("kg_EnchantScroll_Weapon_Blessed_C", 1), Items = new()
        {
            "AtgeirBronze", "AxeBronze", "Cultivator", "MaceBronze", "PickaxeBronze", "ShieldBronzeBuckler", "SpearBronze", "SwordBronze"
        }},
        new SyncedData.EnchantmentReqs() { enchant_prefab = new("kg_EnchantScroll_Armor_C", 1), blessed_enchant_prefab = new("kg_EnchantScroll_Armor_Blessed_C", 1), Items = new()
        {
            "ArmorBronzeChest", "ArmorBronzeLegs", "ArmorTrollLeatherChest", "ArmorTrollLeatherLegs", "CapeTrollHide", "HelmetBronze", "HelmetTrollLeather"
        }},
        new SyncedData.EnchantmentReqs() { enchant_prefab = new("kg_EnchantScroll_Weapon_D", 1), blessed_enchant_prefab = new("kg_EnchantScroll_Weapon_Blessed_D", 1), Items = new()
        {
            "AxeFlint", "Bow", "BowFineWood", "Hoe", "KnifeButcher", "KnifeCopper", "KnifeFlint", "PickaxeAntler", "PickaxeStone",
            "ShieldBoneTower", "ShieldWood", "ShieldWoodTower", "SpearFlint", "SledgeStagbreaker"
        }},
        new SyncedData.EnchantmentReqs() { enchant_prefab = new("kg_EnchantScroll_Armor_D", 1), blessed_enchant_prefab = new("kg_EnchantScroll_Armor_Blessed_D", 1), Items = new()
        {
            "ArmorLeatherChest", "ArmorLeatherLegs", "CapeDeerHide", "HelmetLeather", "ArmorRagsChest", "ArmorRagsLegs"
        }}
    };
    
    private static Dictionary<string, Dictionary<int,int>> DefaultOverrides_Chances = new()
    {
        {"SwordCheat", new()
        {
            {1, 50}, {2, 45}, {3, 40}, {4, 30}, {5, 25}, {6, 20}, {7, 10}, {8, 5}, {9, 3}, {10, 0}
        }}
    };
    
    private static readonly Dictionary<string, Dictionary<int, SyncedData.VFX_Data>> DefaultOverrides_Colors = new()
    {
        {"SwordCheat", new()
        {
            { 1,  new() { color = "#00190019", variant = 0 } },
            { 2,  new() { color = "#00320032", variant = 0 } },
            { 3,  new() { color = "#004B004B", variant = 0 } },
            { 4,  new() { color = "#00640064", variant = 0 } },
            { 5,  new() { color = "#007D007D", variant = 0 } },
            { 6,  new() { color = "#00960096", variant = 0 } },
            { 7,  new() { color = "#00AF00AF", variant = 0 } },
            { 8,  new() { color = "#00C800C8", variant = 0 } },
            { 9,  new() { color = "#00E100E1", variant = 0 } },
            { 10, new() { color = "#00FA00FA", variant = 0 } }
        }}
    };
    
    private static readonly Dictionary<string, Dictionary<int, int>> DefaultOverrides_Stats = new()
    {
        {"SwordCheat", new()
        {
            { 1, 5 }, { 2, 10 }, { 3, 15 }, { 4, 20 }, { 5, 25 }, { 6, 30 }, { 7, 35 }, { 8, 40 }, { 9, 45 }, { 10, 50 }
        }}
    };
    
    public static string YAML_Stats_Weapons => new SerializerBuilder().Build().Serialize(DefaultStats_Weapons);
    public static string YAML_Stats_Armor => new SerializerBuilder().Build().Serialize(DefaultStats_Armor);
    public static string YAML_Reqs => new SerializerBuilder().Build().Serialize(DefaultReqs);
    public static string YAML_Colors => new SerializerBuilder().Build().Serialize(DefaultColors);
    public static string YAML_Chances => new SerializerBuilder().Build().Serialize(DefaultChances);
    public static string YAML_Overrides_Chances => new SerializerBuilder().Build().Serialize(DefaultOverrides_Chances);
    public static string YAML_Overrides_Colors => new SerializerBuilder().Build().Serialize(DefaultOverrides_Colors);
    public static string YAML_Overrides_Stats => new SerializerBuilder().Build().Serialize(DefaultOverrides_Stats);
}