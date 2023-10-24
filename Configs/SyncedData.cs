﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using ServerSync;
using UnityEngine;

namespace kg.ValheimEnchantmentSystem.Configs;

public static class SyncedData
{
    private static FileSystemWatcher FSW;
    private static string YAML_Chances;
    private static string YAML_Stats_Weapons;
    private static string YAML_Stats_Armor;
    private static string YAML_Colors;
    private static string YAML_Reqs;
    private static string YAML_Ovverides_Chances;
    private static string YAML_Ovverides_Stats;
    private static string YAML_Ovverides_Colors;

    public static void Init()
    {
        SafetyLevel = ValheimEnchantmentSystem.config("Enchantment", "SafetyLevel", 3,
            "The level until which enchantments won't destroy the item. Set to 0 to disable.");
        ShowEnchantmentChance = ValheimEnchantmentSystem.config("Enchantment", "ShowEnchantmentChance", true,
            "Show the chance of enchantment in the item tooltip.");
        DropEnchantmentOnUpgrade = ValheimEnchantmentSystem.config("Enchantment", "DropEnchantmentOnUpgrade", false,
            "Drop enchantment on item upgrade.");
        ItemDestroyedOnFailure = ValheimEnchantmentSystem.config("Enchantment", "ItemDestroyedOnFailure", false,
            "Destroy item on enchantment failure. Otherwise decrease enchantment level by 1.");
        AllowJewelcraftingMirrorCopyEnchant = ValheimEnchantmentSystem.config("Enchantment",
            "AllowJewelcraftingMirrorCopyEnchant", false,
            "Allow jewelcrafting to copy enchantment from one item to another using mirror.");

        YAML_Stats_Weapons = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "EnchantmentStats_Weapons.yml");
        YAML_Stats_Armor = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "EnchantmentStats_Armor.yml");
        YAML_Colors = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "EnchantmentColors.yml");
        YAML_Reqs = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "EnchantmentReqs.yml");
        YAML_Chances = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "EnchantmentChances.yml");
        YAML_Ovverides_Chances =
            Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "Overrides_EnchantmentChances.yml");
        YAML_Ovverides_Stats = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "Overrides_EnchantmentStats.yml");
        YAML_Ovverides_Colors = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "Overrides_EnchantmentColors.yml");

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
        if (!File.Exists(YAML_Ovverides_Chances))
            YAML_Ovverides_Chances.WriteFile(Defaults.YAML_Overrides_Chances);
        if (!File.Exists(YAML_Ovverides_Stats))
            YAML_Ovverides_Stats.WriteFile(Defaults.YAML_Overrides_Stats);
        if (!File.Exists(YAML_Ovverides_Colors))
            YAML_Ovverides_Colors.WriteFile(Defaults.YAML_Overrides_Colors);

        Synced_EnchantmentChances.Value = YAML_Chances.FromYAML<Dictionary<int, int>>();
        Synced_EnchantmentStats_Weapons.Value = YAML_Stats_Weapons.FromYAML<Dictionary<int, Stat_Data>>();
        Synced_EnchantmentStats_Armor.Value = YAML_Stats_Armor.FromYAML<Dictionary<int, Stat_Data>>();
        Synced_EnchantmentColors.Value = YAML_Colors.FromYAML<Dictionary<int, VFX_Data>>();
        Synced_EnchantmentReqs.Value = YAML_Reqs.FromYAML<List<EnchantmentReqs>>();
        Overrides_EnchantmentChances.Value =
            YAML_Ovverides_Chances.FromYAML<Dictionary<string, Dictionary<int, int>>>();
        Overrides_EnchantmentStats.Value =
            YAML_Ovverides_Stats.FromYAML<Dictionary<string, Dictionary<int, Stat_Data>>>();
        Overrides_EnchantmentColors.Value =
            YAML_Ovverides_Colors.FromYAML<Dictionary<string, Dictionary<int, VFX_Data>>>();
        Synced_EnchantmentChances.ValueChanged += ResetInventory;
        Synced_EnchantmentStats_Weapons.ValueChanged += ResetInventory;
        Synced_EnchantmentStats_Armor.ValueChanged += ResetInventory;
        Synced_EnchantmentColors.ValueChanged += ResetInventory;
        Synced_EnchantmentReqs.ValueChanged += ResetInventory;
        Overrides_EnchantmentChances.ValueChanged += ResetInventory;
        Overrides_EnchantmentStats.ValueChanged += ResetInventory;
        Overrides_EnchantmentColors.ValueChanged += ResetInventory;
        FSW = new FileSystemWatcher(ValheimEnchantmentSystem.ConfigFolder)
        {
            EnableRaisingEvents = true,
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.LastWrite,
            SynchronizingObject = ThreadingHelper.SynchronizingObject
        };
        FSW.Changed += ConfigChanged;
    }

    private static void ResetInventory() => Player.m_localPlayer?.m_inventory?.Changed();
    
    private static void ConfigChanged(object sender, FileSystemEventArgs e)
    {
        if (!Game.instance || !ZNet.instance || !ZNet.instance.IsServer()) return;
        if (e.ChangeType != WatcherChangeTypes.Changed) return;
        if (e.FullPath == YAML_Chances)
        {
            Synced_EnchantmentChances.Value = YAML_Chances.FromYAML<Dictionary<int, int>>();
            Utils.print($"{e.FullPath} changed. Reloading");
        }
        else if (e.FullPath == YAML_Stats_Weapons)
        {
            Synced_EnchantmentStats_Weapons.Value = YAML_Stats_Weapons.FromYAML<Dictionary<int, Stat_Data>>();
            Utils.print($"{e.FullPath} changed. Reloading");
        }
        else if (e.FullPath == YAML_Stats_Armor)
        {
            Synced_EnchantmentStats_Armor.Value = YAML_Stats_Armor.FromYAML<Dictionary<int, Stat_Data>>();
            Utils.print($"{e.FullPath} changed. Reloading");
        }
        else if (e.FullPath == YAML_Colors)
        {
            Synced_EnchantmentColors.Value = YAML_Colors.FromYAML<Dictionary<int, VFX_Data>>();
            Utils.print($"{e.FullPath} changed. Reloading");
        }
        else if (e.FullPath == YAML_Reqs)
        {
            Synced_EnchantmentReqs.Value = YAML_Reqs.FromYAML<List<EnchantmentReqs>>();
            Utils.print($"{e.FullPath} changed. Reloading");
        }
        else if (e.FullPath == YAML_Ovverides_Chances)
        {
            Overrides_EnchantmentChances.Value =
                YAML_Ovverides_Chances.FromYAML<Dictionary<string, Dictionary<int, int>>>();
            Utils.print($"{e.FullPath} changed. Reloading");
        }
        else if (e.FullPath == YAML_Ovverides_Stats)
        {
            Overrides_EnchantmentStats.Value =
                YAML_Ovverides_Stats.FromYAML<Dictionary<string, Dictionary<int, Stat_Data>>>();
            Utils.print($"{e.FullPath} changed. Reloading");
        }
        else if (e.FullPath == YAML_Ovverides_Colors)
        {
            Overrides_EnchantmentColors.Value =
                YAML_Ovverides_Colors.FromYAML<Dictionary<string, Dictionary<int, VFX_Data>>>();
            Utils.print($"{e.FullPath} changed. Reloading");
        }
        else if (e.FullPath == ValheimEnchantmentSystem.Config.ConfigFilePath)
        {
            ValheimEnchantmentSystem.Config.Reload();
            Utils.print($"{e.FullPath} changed. Reloading");
        }
        else if (e.FullPath == ValheimEnchantmentSystem.ItemConfig.ConfigFilePath)
        {
            ValheimEnchantmentSystem.ItemConfig.Reload();
            Utils.print($"{e.FullPath} changed. Reloading");
        }
    }

    public static string GetColor(Enchantment_Core.Enchanted en, out int variant, bool trimApha) =>
        GetColor(en.Item.m_dropPrefab?.name, en.level, out variant, trimApha);

    public static string GetColor(string dropPrefab, int level, out int variant, bool trimApha)
    {
        variant = 0;
        if (level == 0) return trimApha ? "#000000" : "#00000000";
        if (dropPrefab != null && Overrides_EnchantmentColors.Value.TryGetValue(dropPrefab, out var overriden))
        {
            if (overriden.TryGetValue(level, out var overrideVfxData))
            {
                var result = overrideVfxData.color;
                if (trimApha) result = result.Substring(0, result.Length - 2);
                variant = Mathf.Clamp(overrideVfxData.variant, 0, Enchantment_VFX.VFXs.Count - 1);
                return result;
            }
        }
        else if (Synced_EnchantmentColors.Value.TryGetValue(level, out var vfxData))
        {
            var result = vfxData.color;
            if (trimApha) result = result.Substring(0, result.Length - 2);
            variant = Mathf.Clamp(vfxData.variant, 0, Enchantment_VFX.VFXs.Count - 1);
            return result;
        }

        return trimApha ? "#000000" : "#00000000";
    }

    public static int GetEnchantmentChance(Enchantment_Core.Enchanted en)
    {
        if (en.level == 0) return 100;
        string dropPrefab = en.Item.m_dropPrefab?.name;
        if (dropPrefab != null && Overrides_EnchantmentChances.Value.TryGetValue(dropPrefab, out var overriden))
        {
            if (overriden.TryGetValue(en.level, out var overrideChance))
                return overrideChance;
        }
        else if (Synced_EnchantmentChances.Value.TryGetValue(en.level, out var chance))
            return chance;

        return 0;
    }

    public static Stat_Data GetStatIncrease(Enchantment_Core.Enchanted en)
    {
        if (en.level == 0) return null;
        string dropPrefab = en.Item.m_dropPrefab?.name;
        if (dropPrefab != null && Overrides_EnchantmentStats.Value.TryGetValue(dropPrefab, out var overriden))
        {
            return overriden.TryGetValue(en.level, out var overrideChance) ? overrideChance : null;
        }

        var target = en.Item.IsWeapon() ? Synced_EnchantmentStats_Weapons.Value : Synced_EnchantmentStats_Armor.Value;
        return target.TryGetValue(en.level, out var increase) ? increase : null;
    }

    public static EnchantmentReqs GetReqs(string prefab)
    {
        return prefab == null ? null : Synced_EnchantmentReqs.Value.Find(x => x.Items.Contains(prefab));
    }

    public static ConfigEntry<int> SafetyLevel;
    public static ConfigEntry<bool> ShowEnchantmentChance;
    public static ConfigEntry<bool> DropEnchantmentOnUpgrade;
    public static ConfigEntry<bool> ItemDestroyedOnFailure;
    public static ConfigEntry<bool> AllowJewelcraftingMirrorCopyEnchant;

    private static readonly CustomSyncedValue<Dictionary<int, int>> Synced_EnchantmentChances =
        new(ValheimEnchantmentSystem.configSync, "EnchantmentGlobalChances",
            new Dictionary<int, int>());

    private static readonly CustomSyncedValue<Dictionary<int, VFX_Data>> Synced_EnchantmentColors =
        new(ValheimEnchantmentSystem.configSync, "OverridenEnchantmentColors",
            new Dictionary<int, VFX_Data>());

    private static readonly CustomSyncedValue<Dictionary<int, Stat_Data>> Synced_EnchantmentStats_Weapons =
        new(ValheimEnchantmentSystem.configSync, "OverridenEnchantmentStats_Weapons",
            new Dictionary<int, Stat_Data>());

    private static readonly CustomSyncedValue<Dictionary<string, Dictionary<int, int>>> Overrides_EnchantmentChances =
        new(ValheimEnchantmentSystem.configSync, "Overrides_EnchantmentChances",
            new Dictionary<string, Dictionary<int, int>>());

    private static readonly CustomSyncedValue<Dictionary<string, Dictionary<int, VFX_Data>>>
        Overrides_EnchantmentColors =
            new(ValheimEnchantmentSystem.configSync, "Overrides_EnchantmentColors",
                new Dictionary<string, Dictionary<int, VFX_Data>>());

    private static readonly CustomSyncedValue<Dictionary<string, Dictionary<int, Stat_Data>>>
        Overrides_EnchantmentStats =
            new(ValheimEnchantmentSystem.configSync, "Overrides_EnchantmentStats",
                new Dictionary<string, Dictionary<int, Stat_Data>>());

    private static readonly CustomSyncedValue<Dictionary<int, Stat_Data>> Synced_EnchantmentStats_Armor =
        new(ValheimEnchantmentSystem.configSync, "OverridenEnchantmentStats_Armor",
            new Dictionary<int, Stat_Data>());

    private static readonly CustomSyncedValue<List<EnchantmentReqs>> Synced_EnchantmentReqs =
        new(ValheimEnchantmentSystem.configSync, "EnchantmentReqs",
            new List<EnchantmentReqs>());

    public class Stat_Data : ISerializableParameter
    {
        public int durability;
        public int durability_percentage;
        public int armor_percentage;
        public int armor;
        public int damage_percentage;
        public int damage_true;
        public int damage_blunt;
        public int damage_slash;
        public int damage_pierce;
        public int damage_chop;
        public int damage_pickaxe;
        public int damage_fire;
        public int damage_frost;
        public int damage_lightning;
        public int damage_poison;
        public int damage_spirit;

        public string BuildTooltip(string color)
        {
            if (damage_true == 0 && damage_blunt == 0 && damage_slash == 0 && damage_pierce == 0 &&
                damage_chop == 0 && damage_pickaxe == 0 && damage_fire == 0 && damage_frost == 0 &&
                damage_lightning == 0 && damage_poison == 0 && damage_spirit == 0 && armor == 0 &&
                durability == 0) return "";
            
            StringBuilder builder = new StringBuilder();
            builder.Append($"\n<color={color}>•</color> $enchantment_additionalstats:");
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
            builder.Append("\n");
            return builder.ToString();
        }

        public static implicit operator bool(Stat_Data data) => data != null;

        public void Serialize(ref ZPackage pkg)
        {
            pkg.Write(durability);
            pkg.Write(durability_percentage);
            pkg.Write(armor_percentage);
            pkg.Write(armor);
            pkg.Write(damage_percentage);
            pkg.Write(damage_true);
            pkg.Write(damage_blunt);
            pkg.Write(damage_slash);
            pkg.Write(damage_pierce);
            pkg.Write(damage_chop);
            pkg.Write(damage_pickaxe);
            pkg.Write(damage_fire);
            pkg.Write(damage_frost);
            pkg.Write(damage_lightning);
            pkg.Write(damage_poison);
            pkg.Write(damage_spirit);
        }

        public void Deserialize(ref ZPackage pkg)
        {
            durability = pkg.ReadInt();
            durability_percentage = pkg.ReadInt();
            armor_percentage = pkg.ReadInt();
            armor = pkg.ReadInt();
            damage_percentage = pkg.ReadInt();
            damage_true = pkg.ReadInt();
            damage_blunt = pkg.ReadInt();
            damage_slash = pkg.ReadInt();
            damage_pierce = pkg.ReadInt();
            damage_chop = pkg.ReadInt();
            damage_pickaxe = pkg.ReadInt();
            damage_fire = pkg.ReadInt();
            damage_frost = pkg.ReadInt();
            damage_lightning = pkg.ReadInt();
            damage_poison = pkg.ReadInt();
            damage_spirit = pkg.ReadInt();
        }
    }

    public class EnchantmentReqs : ISerializableParameter
    {
        public class req
        {
            public string prefab;
            public int amount;

            public req()
            {
            }

            public req(string prefab, int amount)
            {
                this.prefab = prefab;
                this.amount = amount;
            }

            public bool IsValid() =>
                !string.IsNullOrEmpty(prefab) && amount > 0 && ZNetScene.instance.GetPrefab(prefab);
        }

        public req enchant_prefab = new();
        public req blessed_enchant_prefab = new();
        public List<string> Items = new();

        public void Serialize(ref ZPackage pkg)
        {
            pkg.Write(enchant_prefab.prefab ?? "");
            pkg.Write(enchant_prefab.amount);

            pkg.Write(blessed_enchant_prefab.prefab ?? "");
            pkg.Write(blessed_enchant_prefab.amount);

            pkg.Write(Items.Count);
            foreach (var item in Items)
            {
                pkg.Write(item);
            }
        }

        public void Deserialize(ref ZPackage pkg)
        {
            enchant_prefab = new(pkg.ReadString(), pkg.ReadInt());
            blessed_enchant_prefab = new(pkg.ReadString(), pkg.ReadInt());
            int count = pkg.ReadInt();
            for (int i = 0; i < count; i++)
            {
                Items.Add(pkg.ReadString());
            }
        }
    }

    public class VFX_Data : ISerializableParameter
    {
        public string color;
        public int variant;

        public void Serialize(ref ZPackage pkg)
        {
            pkg.Write(color ?? "#00000000");
            pkg.Write(variant);
        }

        public void Deserialize(ref ZPackage pkg)
        {
            color = pkg.ReadString();
            variant = pkg.ReadInt();
        }
    }

    /*[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    private static class ZNetScene_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ZNetScene __instance)
        {
            var recipes = ObjectDB.instance.m_recipes;
            List<string> S_tiercomponents = new List<string>() { "Eitr", "YggdrasilWood" };
            List<string> A_tiercomponents = new List<string>() { "Silver", "BlackMetal", "DragonTear" };
            List<string> B_tiercomponents = new List<string>() { "Iron", "Obsidian", "Crystal", "IronNails", "ElderBark" };
            List<string> C_tiercomponents = new List<string>() { "Bronze", "TrollHide", "BronzeNails" };
            List<string> D_tiercomponents = new List<string>() { "DeerHide", "Wood", "Stone" };

            string _tier_prefab_weapon = "kg_EnchantScroll_Weapon_";
            string _tier_prefab_weapon_blessed = "kg_EnchantScroll_Weapon_Blessed_";
            string _tier_prefab_armor = "kg_EnchantScroll_Armor_";
            string _tier_prefab_armor_blessed = "kg_EnchantScroll_Armor_Blessed_";

            List<string> S_tierList = new List<string>();
            List<string> A_tierList = new List<string>();
            List<string> B_tierList = new List<string>();
            List<string> C_tierList = new List<string>();
            List<string> D_tierList = new List<string>();
            List<string> NOTFOUND_tierList = new List<string>();

            StringBuilder builder = new StringBuilder();

            void TryFindStuff(List<string> source, List<string> to, Piece.Requirement[] reqs, string itemName, string usePrefab, string usePrefab_blessed, ref bool done)
            {
                foreach (var req in reqs)
                {
                    if (source.Contains(req.m_resItem.name))
                    {
                        to.Add($"{{\"{itemName}\", new EnchantmentReqs() {{ enchant_prefab = new(\"{usePrefab}\", 1), blessed_enchant_prefab = new(\"{usePrefab_blessed}\", 1) }} }},\n");
                        done = true;
                        break;
                    }
                }
            }

            foreach (var recipe in recipes)
            {
                string usePrefab = _tier_prefab_armor;
                string usePrefab_blessed = _tier_prefab_armor_blessed;
                if(recipe.m_item == null) continue;
                if (recipe.m_item.m_itemData.IsWeapon())
                {
                    usePrefab = _tier_prefab_weapon;
                    usePrefab_blessed = _tier_prefab_weapon_blessed;
                }
                else if (recipe.m_item.m_itemData.IsEquipable() &&
                         recipe.m_item.m_itemData.m_shared.m_itemType is not ItemDrop.ItemData.ItemType.Ammo
                             or ItemDrop.ItemData.ItemType.AmmoNonEquipable or ItemDrop.ItemData.ItemType.Utility)
                {
                    usePrefab = _tier_prefab_armor;
                    usePrefab_blessed = _tier_prefab_armor_blessed;
                }
                else
                {
                    continue;
                }

                if(recipe.m_craftingStation == null) continue;

                var reqs = recipe.m_resources;

                bool done = false;
                TryFindStuff(S_tiercomponents, S_tierList, reqs, recipe.m_item.name, usePrefab + "S", usePrefab_blessed + "S", ref done);
                if (done) continue;
                TryFindStuff(A_tiercomponents, A_tierList, reqs, recipe.m_item.name, usePrefab + "A", usePrefab_blessed + "A", ref done);
                if (done) continue;
                TryFindStuff(B_tiercomponents, B_tierList, reqs, recipe.m_item.name, usePrefab + "B", usePrefab_blessed + "B", ref done);
                if (done) continue;
                TryFindStuff(C_tiercomponents, C_tierList, reqs, recipe.m_item.name, usePrefab + "C", usePrefab_blessed + "C", ref done);
                if (done) continue;
                TryFindStuff(D_tiercomponents, D_tierList, reqs, recipe.m_item.name, usePrefab + "D", usePrefab_blessed + "D", ref done);
                if (!done)
                {
                    NOTFOUND_tierList.Add($"{{\"{recipe.m_item.name}(NOTFOUND)\", new EnchantmentReqs() {{ enchant_prefab = new(\"{usePrefab}\", 1), blessed_enchant_prefab = new(\"{usePrefab_blessed}\", 1) }} }},\n");
                }
            }

            S_tierList.ForEach(s => builder.Append(s));
            A_tierList.ForEach(s => builder.Append(s));
            B_tierList.ForEach(s => builder.Append(s));
            C_tierList.ForEach(s => builder.Append(s));
            D_tierList.ForEach(s => builder.Append(s));
            NOTFOUND_tierList.ForEach(s => builder.Append(s));

            string result = builder.ToString();
            File.WriteAllText("EnchantmentReqs.txt", result);

        }
    }*/
}