﻿using ItemManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace kg.ValheimEnchantmentSystem.Items_Structures;

[VES_Autoload]
public static class ScrollItems
{
    private static ConfigEntry<float> DropChance;
    private static ConfigEntry<float> DropChance_Bosses;
    private static ConfigEntry<float> DropChance_Blessed;
    private static ConfigEntry<float> DropChance_Blessed_Bosses;
    private static ConfigEntry<float> DropChance_Skill;
    private static ConfigEntry<float> DropChance_Skill_Bosses;
    
    private static ConfigEntry<bool> MonsterDroppingScrolls;
    private static ConfigEntry<bool> MonsterDroppingSkilllScrolls;

    private static ConfigEntry<string> ExcludePrefabsFromDrop;
    
    private static readonly Dictionary<Heightmap.Biome, ConfigEntry<string>> BiomeMapper = new();
    private static readonly Dictionary<char, ConfigEntry<int>> BookXPMapper = new();
    private static readonly List<GameObject> SkillScrolls = new(5);

    private static readonly Dictionary<char, int> SkillExpScroll_DefaultValues = new()
    {
        {'F', 15}, { 'D', 25 }, { 'C', 50 }, { 'B', 75 }, { 'A', 100 }, { 'S', 140 }
    };

    private static readonly HashSet<string> ExludedDroPrefabs = new();

    private static readonly Dictionary<char, string[]> DefaultRecipes = new()
    {
        { 'F', new[]{"DeerHide,10", "Flint,5", "Wood,5", "TrophyDeer,2"}},
        { 'D', new[]{"GreydwarfEye,10", "BoneFragments,5", "FineWood,5", "TrophySkeleton,2"}},
        { 'C', new[]{"Entrails,10", "Bloodbag,5", "ElderBark,5", "TrophyLeech,2"}},
        { 'B', new[]{"WolfPelt,10", "FreezeGland,5", "FineWood,5", "TrophyHatchling,2"}},
        { 'A', new[]{"LoxPelt,10", "Needle,5", "FineWood,5", "TrophyGoblin,2"}},
        { 'S', new[]{"Eitr,10", "Softtissue,5", "YggdrasilWood,5", "TrophyDvergr,2"}},
    };
    private static readonly Dictionary<char, string[]> DefaultRecipes_Blessed = new()
    {
        { 'F', new[]{"HardAntler,1","SurtlingCore,1", "GreydwarfEye,20"}},
        { 'D', new[]{"TrophyTheElder,1", "AncientSeed,2", "SilverNecklace,1"}},
        { 'C', new[]{"TrophyBonemass,1", "Chitin,5", "BoneFragments,10"}},
        { 'B', new[]{"TrophySGolem,1", "WolfFang,2", "Crystal,20", "WolfHairBundle,3"}},
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
        item.Crafting.Add("kg_EnchantmentScrollStation", 1);
    }
    
    [UsedImplicitly]
    private static void OnInit()
    {
        MonsterDroppingScrolls = ValheimEnchantmentSystem.config("Scrolls", "Drop From Monsters", true, "Allow monsters to drop scrolls.");
        MonsterDroppingSkilllScrolls = ValheimEnchantmentSystem.config("Skill Scrolls", "Drop From Monsters (Skill exp)", true, "Allow monsters to drop enchant skill exp scrolls.");
        DropChance = ValheimEnchantmentSystem.config("Scrolls", "Drop Chance", 3f, "Chance to drop from enemies.");
        DropChance_Bosses = ValheimEnchantmentSystem.config("Scrolls", "Drop Chance (Bosses)", 100f, "Chance to drop from bosses.");
        DropChance_Blessed = ValheimEnchantmentSystem.config("Scrolls", "Blessed Drop Chance", 0.25f, "Chance to drop from enemies.");
        DropChance_Blessed_Bosses = ValheimEnchantmentSystem.config("Scrolls", "Blessed Drop Chance (Bosses)", 40f, "Chance to drop from bosses.");
        DropChance_Skill = ValheimEnchantmentSystem.config("Skill Scrolls", "Drop Chance (Skill exp)", 0.10f, "Chance to drop from enemies.");
        DropChance_Skill_Bosses = ValheimEnchantmentSystem.config("Skill Scrolls", "Drop Chance (Skill exp)", 25f, "Chance to drop from bosses.");
        ExcludePrefabsFromDrop = ValheimEnchantmentSystem.config("Scrolls", "Exclude Prefabs From Drop", "TentaRoot", "Comma separated list of prefabs to exclude from dropping scrolls.");
        ExcludePrefabsFromDrop.SettingChanged += FillExclude;
        FillExclude();
        
        BiomeMapper.Add(Heightmap.Biome.Meadows,ValheimEnchantmentSystem.config("Scrolls", "Meadows Tier", "F", "Tier of scrolls Meadows (F D C B A S)"));
        BiomeMapper.Add(Heightmap.Biome.BlackForest,ValheimEnchantmentSystem.config("Scrolls", "BlackForest Tier", "D", "Tier of scrolls BlackForest (F D C B A S)"));
        BiomeMapper.Add(Heightmap.Biome.Swamp,ValheimEnchantmentSystem.config("Scrolls", "Swamp Tier", "C", "Tier of scrolls Swamp (F D C B A S)"));
        BiomeMapper.Add(Heightmap.Biome.Ocean,ValheimEnchantmentSystem.config("Scrolls", "Ocean Tier", "C", "Tier of scrolls Ocean (F D C B A S)"));
        BiomeMapper.Add(Heightmap.Biome.Mountain,ValheimEnchantmentSystem.config("Scrolls", "Mountain Tier", "B", "Tier of scrolls Mountain (F D C B A S)"));
        BiomeMapper.Add(Heightmap.Biome.Plains,ValheimEnchantmentSystem.config("Scrolls", "Plains Tier", "A", "Tier of scrolls Plains (F D C B A S)"));
        BiomeMapper.Add(Heightmap.Biome.Mistlands,ValheimEnchantmentSystem.config("Scrolls", "Mistlands Tier", "S", "Tier of scrolls Mistlands (F D C B A S)"));
        BiomeMapper.Add(Heightmap.Biome.AshLands,ValheimEnchantmentSystem.config("Scrolls", "Ashlands Tier", "S", "Tier of scrolls Ashlands (F D C B A S)"));
        BiomeMapper.Add(Heightmap.Biome.DeepNorth,ValheimEnchantmentSystem.config("Scrolls", "Deepnorth Tier", "S", "Tier of scrolls Deepnorth (F D C B A S)"));
        
        
        char[] DCBAS = {'F', 'D', 'C', 'B', 'A', 'S'};
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
          
            BookXPMapper.Add(c, ValheimEnchantmentSystem.config("Skill Scrolls", $"Skill EXP Scroll {c}", SkillExpScroll_DefaultValues[c], $"Skill EXP Scroll {c}"));
            SkillScrolls.Add(ValheimEnchantmentSystem._asset.LoadAsset<GameObject>($"kg_EnchantSkillScroll_{c}"));
        }
        
        SkillScrolls.ForEach(x => x.AddComponent<ExpScroll>());
    }

    private static void FillExclude(object sender = null, EventArgs e = null)
    {
        ExludedDroPrefabs.Clear();
        if(string.IsNullOrWhiteSpace(ExcludePrefabsFromDrop.Value)) return;
        var split = ExcludePrefabsFromDrop.Value.Replace(" ","").Split(',');
        foreach (var s in split)
        {
            ExludedDroPrefabs.Add(s);
        }
    }

    [HarmonyPatch(typeof(ZNetScene),nameof(ZNetScene.Awake))]
    private static class ZNetScene_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ZNetScene __instance)
        {
            foreach (var go in SkillScrolls)
                __instance.m_namedPrefabs[go.name.GetStableHashCode()] = go;
        }
    }
    
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
    
    [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
    [ClientOnlyPatch]
    static class Tome_SpawnLoot_Patch
    {
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
            string prefabName = global::Utils.GetPrefabName(__instance.gameObject);
            if (ExludedDroPrefabs.Contains(prefabName)) return;
            Heightmap.Biome biome = EnvMan.instance.m_currentBiome;
            if(!BiomeMapper.TryGetValue(biome, out ConfigEntry<string> tier)) return;
            var position = __instance.transform.position;
            char tierValue = tier.Value[0];
            TryDropDefault(tierValue, __instance.IsBoss(), position);
            TryDropBlessed(tierValue, __instance.IsBoss(), position);
        }
    }
    
    [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
    [ClientOnlyPatch]
    static class Tome_SpawnLootSkill_Patch
    {
        private static void TryDropSkillScroll(char tier, bool isBoss, Vector3 pos)
        {
            float rand = Random.value;
            var dropChance = isBoss ? DropChance_Skill_Bosses.Value : DropChance_Skill.Value;
            dropChance /= 100f;
            if(rand <= dropChance)
            {
                DropItem(ZNetScene.instance.GetPrefab($"kg_EnchantSkillScroll_{tier}"), pos + Vector3.up * 0.75f, 0.5f);
            }
        }

        [UsedImplicitly]
        private static void Prefix(Character __instance)
        {
            if (!MonsterDroppingSkilllScrolls.Value || __instance.IsPlayer() || !__instance.m_nview.IsOwner() || __instance.IsTamed()) return;
            string prefabName = global::Utils.GetPrefabName(__instance.gameObject);
            if (ExludedDroPrefabs.Contains(prefabName)) return;
            Heightmap.Biome biome = EnvMan.instance.m_currentBiome;
            if(!BiomeMapper.TryGetValue(biome, out ConfigEntry<string> tier)) return;
            var position = __instance.transform.position;
            char tierValue = tier.Value[0];
            TryDropSkillScroll(tierValue, __instance.IsBoss(), position);
        }
    }


    public class ExpScroll : MonoBehaviour, Interactable, Hoverable
    {
        private ZNetView _znv;

        private int CreationTime {
            get => _znv.GetZDO().GetInt("CreationTime");
            set => _znv.GetZDO().Set("CreationTime", value);
        }

        private const int MaxDuration = 120;
        
        private void Awake()
        {
            _znv = GetComponent<ZNetView>();
            if(!_znv.IsValid() || !_znv.IsOwner()) return;
            
            if (CreationTime == 0)
            {
                CreationTime = (int)EnvMan.instance.m_totalSeconds;
                return;
            }

            if (EnvMan.instance.m_totalSeconds - CreationTime > MaxDuration)
            {
                _znv.ClaimOwnership();
                ZNetScene.instance.Destroy(this.gameObject);
            }
            
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            string prefabName = global::Utils.GetPrefabName(this.gameObject);
            char tier = prefabName[prefabName.Length - 1];
            if (!BookXPMapper.TryGetValue(tier, out ConfigEntry<int> exp)) return false;
            Utils.IncreaseSkillEXP(Enchantment_Skill.SkillType_Enchantment, exp.Value);
            Instantiate(ZNetScene.instance.GetPrefab("fx_Potion_frostresist"), Player.m_localPlayer.transform.position, Quaternion.identity);
            _znv.ClaimOwnership();
            ZNetScene.instance.Destroy(this.gameObject);
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        public string GetHoverText()
        {
            return "<b>$enchantment_skill_scroll</b>\n\n[<color=yellow><b>$KEY_Use</b></color>] $enchantment_skill_scroll_use".Localize();
        }

        public string GetHoverName()
        {
            return "";
        }
    }
}