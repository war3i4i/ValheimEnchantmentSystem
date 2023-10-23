using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using ItemManager;
using JetBrains.Annotations;
using UnityEngine;

namespace kg.ValheimEnchantmentSystem.Items_Structures;

public static class ScrollItems
{
    private static ConfigEntry<float> DropChance;
    private static ConfigEntry<float> DropChance_Bosses;
    private static ConfigEntry<float> DropChance_Blessed;
    private static ConfigEntry<float> DropChance_Blessed_Bosses;
    
    private static ConfigEntry<bool> MonsterDroppingScrolls;
    
    private static readonly Dictionary<Heightmap.Biome, ConfigEntry<string>> BiomeMapper = new();

    private static readonly Dictionary<char, string[]> DefaultRecipes = new()
    {
        { 'D', new[]{"DeerHide,10", "Flint,5", "Wood,5", "TrophyDeer,2"}},
        { 'C', new[]{"GreydwarfEye,10", "BoneFragments,5", "FineWood,5", "TrophySkeleton,2"}},
        { 'B', new[]{"Entrails,10", "Bloodbag,5", "ElderBark,5", "TrophyLeech,2"}},
        { 'A', new[]{"WolfPelt,10", "FreezeGland,5", "FineWood,5", "TrophyHatchling,2"}},
        { 'S', new[]{"Eitr,10", "Softtissue,5", "YggdrasilWood,5", "TrophyDvergr,2"}},
    };
    private static readonly Dictionary<char, string[]> DefaultRecipes_Blessed = new()
    {
        { 'D', new[]{"HardAntler,1","SurtlingCore,1", "GreydwarfEye,20"}},
        { 'C', new[]{"TrophyTheElder,1", "AncientSeed,2", "SilverNecklace,1"}},
        { 'B', new[]{"TrophyBonemass,1", "Chitin,5", "BoneFragments,10"}},
        { 'A', new[]{"TrophyGoblinShaman,1", "GoblinTotem,2", "Needle,20", "LoxPelt,3"}},
        { 'S', new[]{"TrophySeekerBrute,1", "Eitr,5", "RoyalJelly,25", "Carapace,40"}},
    };
    
    
    private static void FillRecipe(Item item, char tier, bool bless)
    {
        var targetDic = bless ? DefaultRecipes_Blessed : DefaultRecipes;
        var recipe = targetDic[tier];
        foreach (var s in recipe)
        {
            string[] split = s.Split(',');
            var name = split[0];
            var amount = int.Parse(split[1]);
            item.RequiredItems.Add(name, amount);
        }
        item.Crafting.Add(BuildPieces.Station.name, 1);
    }
    
    public static void Init()
    {
        MonsterDroppingScrolls = ValheimEnchantmentSystem.config("Scrolls", "Drop From Monsters", true, "Allow monsters to drop scrolls.");
        DropChance = ValheimEnchantmentSystem.config("Scrolls", "Drop Chance", 3f, "Chance to drop from enemies.");
        DropChance_Bosses = ValheimEnchantmentSystem.config("Scrolls", "Drop Chance (Bosses)", 100f, "Chance to drop from bosses.");
        DropChance_Blessed = ValheimEnchantmentSystem.config("Scrolls", "Blessed Drop Chance", 0.25f, "Chance to drop from enemies.");
        DropChance_Blessed_Bosses = ValheimEnchantmentSystem.config("Scrolls", "Blessed Drop Chance (Bosses)", 40f, "Chance to drop from bosses.");
        
        BiomeMapper.Add(Heightmap.Biome.Meadows,ValheimEnchantmentSystem.config("Scrolls", "Meadows Tier", "D", "Tier of scrolls Meadows (D C B A S)"));
        BiomeMapper.Add(Heightmap.Biome.BlackForest,ValheimEnchantmentSystem.config("Scrolls", "BlackForest Tier", "C", "Tier of scrolls BlackForest (D C B A S)"));
        BiomeMapper.Add(Heightmap.Biome.Swamp,ValheimEnchantmentSystem.config("Scrolls", "Swamp Tier", "B", "Tier of scrolls Swamp (D C B A S)"));
        BiomeMapper.Add(Heightmap.Biome.Ocean,ValheimEnchantmentSystem.config("Scrolls", "Ocean Tier", "B", "Tier of scrolls Ocean (D C B A S)"));
        BiomeMapper.Add(Heightmap.Biome.Mountain,ValheimEnchantmentSystem.config("Scrolls", "Mountain Tier", "A", "Tier of scrolls Mountain (D C B A S)"));
        BiomeMapper.Add(Heightmap.Biome.Plains,ValheimEnchantmentSystem.config("Scrolls", "Plains Tier", "A", "Tier of scrolls Plains (D C B A S)"));
        BiomeMapper.Add(Heightmap.Biome.Mistlands,ValheimEnchantmentSystem.config("Scrolls", "Mistlands Tier", "S", "Tier of scrolls Mistlands (D C B A S)"));
        BiomeMapper.Add(Heightmap.Biome.AshLands,ValheimEnchantmentSystem.config("Scrolls", "Ashlands Tier", "S", "Tier of scrolls Ashlands (D C B A S)"));
        BiomeMapper.Add(Heightmap.Biome.DeepNorth,ValheimEnchantmentSystem.config("Scrolls", "Deepnorth Tier", "S", "Tier of scrolls Deepnorth (D C B A S)"));
        
        
        char[] DCBAS = {'D', 'C', 'B', 'A', 'S'};
        foreach (var c in DCBAS)
        {
            Item weaponScroll = new Item(ValheimEnchantmentSystem._asset, $"kg_EnchantScroll_Weapon_{c}")
            {
                Configurable = Configurability.Recipe
            };
            weaponScroll.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name = $"$kg_enchantscroll_weapon_{c}";
            weaponScroll.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_description = $"$kg_enchantscroll_weapon_description";
            FillRecipe(weaponScroll, c, false);
            Item weaponScroll_Bless = new Item(ValheimEnchantmentSystem._asset, $"kg_EnchantScroll_Weapon_Blessed_{c}")
            { 
                Configurable = Configurability.Recipe
            };
            weaponScroll_Bless.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name = $"$kg_enchantscroll_weapon_blessed_{c}";
            weaponScroll_Bless.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_description = $"$kg_enchantscroll_weapon_blessed_description";
            FillRecipe(weaponScroll_Bless, c, true);
            Item armorScroll = new Item(ValheimEnchantmentSystem._asset, $"kg_EnchantScroll_Armor_{c}")
            {
                Configurable = Configurability.Recipe
            };
            armorScroll.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name = $"$kg_enchantscroll_armor_{c}";
            armorScroll.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_description = $"$kg_enchantscroll_armor_description";
            FillRecipe(armorScroll, c, false);
            Item armorScroll_Bless = new Item(ValheimEnchantmentSystem._asset, $"kg_EnchantScroll_Armor_Blessed_{c}")
            {
                Configurable = Configurability.Recipe
            };
            armorScroll_Bless.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name = $"$kg_enchantscroll_armor_blessed_{c}";
            armorScroll_Bless.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_description = $"$kg_enchantscroll_armor_blessed_description";
            FillRecipe(armorScroll_Bless, c, true);
        }
    }
    
    [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
    static class Tome_SpawnLoot_Patch
    {
        static void DropItem(GameObject prefab, Vector3 centerPos, float dropArea)
        {
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0, 360), 0f);
            Vector3 b = Random.insideUnitSphere * dropArea;
            GameObject gameObject = Object.Instantiate(prefab, centerPos + b, rotation);
            Rigidbody component = gameObject.GetComponent<Rigidbody>();
            if (component)
            {
                Vector3 insideUnitSphere = Random.insideUnitSphere;
                if (insideUnitSphere.y < 0f)
                {
                    insideUnitSphere.y = -insideUnitSphere.y;
                }

                component.AddForce(insideUnitSphere * 5f, ForceMode.VelocityChange);
            }
        }

        private static void TryDropDefault(char tier, bool isBoss, Vector3 pos)
        {
            float rand = Random.value;
            var dropChance = isBoss ? DropChance_Bosses.Value : DropChance.Value;
            dropChance /= 100f;
            if(rand <= dropChance)
            {
                bool isWeapon = Random.value < 0.5f;
                var book = isWeapon ? $"kg_EnchantScroll_Weapon_{tier}" : $"kg_EnchantScroll_Armor_{tier}";
                DropItem(ZNetScene.instance.GetPrefab(book), pos + Vector3.up * 0.75f, 0.5f);
            }
        }
        
        private static void TryDropBlessed(char tier, bool isBoss, Vector3 pos)
        {
            float rand = Random.value;
            var dropChance = isBoss ? DropChance_Blessed_Bosses.Value : DropChance_Blessed.Value;
            dropChance /= 100f;
            if(rand <= dropChance)
            {
                bool isWeapon = Random.value < 0.5f;
                var book = isWeapon ? $"kg_EnchantScroll_Weapon_Blessed_{tier}" : $"kg_EnchantScroll_Armor_Blessed_{tier}";
                DropItem(ZNetScene.instance.GetPrefab(book), pos + Vector3.up * 0.75f, 0.5f);
            }
        }

        [UsedImplicitly]
        private static void Prefix(Character __instance)
        {
            if (!MonsterDroppingScrolls.Value || __instance.IsPlayer() || !__instance.m_nview.IsOwner() || __instance.IsTamed()) return;
            Heightmap.Biome biome = EnvMan.instance.m_currentBiome;
            if(!BiomeMapper.TryGetValue(biome, out ConfigEntry<string> tier)) return;
            var position = __instance.transform.position;
            char tierValue = tier.Value[0];
            TryDropDefault(tierValue, __instance.IsBoss(), position);
            TryDropBlessed(tierValue, __instance.IsBoss(), position);
        }
    }
}