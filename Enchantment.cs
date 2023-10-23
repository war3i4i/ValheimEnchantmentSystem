using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using fastJSON;
using HarmonyLib;
using ItemDataManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using UnityEngine;
using Random = UnityEngine.Random;

namespace kg.ValheimEnchantmentSystem;

public static class Enchantment
{
    public static IEnumerator FrameSkipEquip(ItemDrop.ItemData weapon)
    {
        if(!Player.m_localPlayer.IsItemEquiped(weapon)) yield break;
        Player.m_localPlayer.UnequipItem(weapon);
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        if (weapon != null && Player.m_localPlayer && Player.m_localPlayer.m_inventory.ContainsItem(weapon))
            Player.m_localPlayer?.EquipItem(weapon);
    }

    public class EnchantedItem : ItemData
    {
        private class enchantment_internal : ISerializableParameter
        {
            public int level;
            public Dictionary<int, int> failed_enchantments = new();

            public void Serialize(ref ZPackage pkg)
            {
                pkg.Write(level);
                pkg.Write(failed_enchantments.Count);
                foreach (var n in failed_enchantments)
                {
                    pkg.Write(n.Key);
                    pkg.Write(n.Value);
                }
            }

            public void Deserialize(ref ZPackage pkg)
            {
                level = pkg.ReadInt();
                failed_enchantments.Clear();
                int count = pkg.ReadInt();
                for (int i = 0; i < count; i++)
                {
                    int key = pkg.ReadInt();
                    int value = pkg.ReadInt();
                    failed_enchantments.Add(key, value);
                }
            }
        }

        [SerializeField] private readonly enchantment_internal _internal = new();

        public int level
        {
            get => _internal.level;
            set => _internal.level = value;
        }

        public Dictionary<int, int> failed_enchantments => _internal.failed_enchantments;

        public override void Save()
        {
            base.Save();
            Player.m_localPlayer?.m_inventory?.Changed();
        }

        public override void Upgraded()
        {
            if (SyncedData.DropEnchantmentOnUpgrade.Value)
            {
                ValheimEnchantmentSystem._thistype.DelayedInvoke(() =>
                {
                    Item?.Data().Remove<EnchantedItem>();
                    Player.m_localPlayer?.m_inventory.Changed();
                }, 1);
            }
        }

        public int GetEnchantmentChance()
        {
            return SyncedData.GetEnchantmentChance(this);
        }

        private bool HaveReqs(bool bless)
        {
            SyncedData.EnchantmentReqs.req req = bless ? SyncedData.GetReqs(Item.m_dropPrefab.name).blessed_enchant_prefab : SyncedData.GetReqs(Item.m_dropPrefab.name).enchant_prefab;
            if (req == null || !req.IsValid()) return false;
            var prefab = ZNetScene.instance.GetPrefab(req.prefab);
            if (prefab == null) return false;
            int count = Utils.CustomCountItemsNoLevel(prefab.name);
            if (count >= req.amount)
            {
                Utils.CustomRemoveItemsNoLevel(prefab.name, req.amount);
                return true;
            }
            return false;
        }

        private bool CanEnchant()
        {
            if (GetEnchantmentChance() <= 0) return false;
            SyncedData.EnchantmentReqs reqs = SyncedData.GetReqs(Item.m_dropPrefab.name);
            return reqs != null;
        }

        private bool CheckRandom()
        {
            int random = Random.Range(1, 101);
            int chance = GetEnchantmentChance();
            return random <= chance;
        }

        public bool Enchant(bool safeEnchant, out string msg)
        {
            msg = "";
            if (!CanEnchant())
            {
                msg = "$enchantment_cannotbe".Localize();
                return false;
            }

            if (!HaveReqs(safeEnchant))
            {
                msg = "$enchantment_nomaterials".Localize();
                return false;
            }

            if (CheckRandom())
            {
                int prevLevel = level;
                level++;
                Save();
                ValheimEnchantmentSystem._thistype.StartCoroutine(FrameSkipEquip(Item));
                msg = "$enchantment_success".Localize(Item.m_shared.m_name.Localize(), prevLevel.ToString(), level.ToString()); 
                return true; 
            }
            
            if (SyncedData.SafetyLevel.Value <= level && !safeEnchant)
            {
                if (SyncedData.ItemDestroyedOnFailure.Value)
                {
                    
                    Player.m_localPlayer.UnequipItem(Item);
                    Player.m_localPlayer.m_inventory.RemoveItem(Item);
                    msg = "$enchantment_fail_destroyed".Localize(Item.m_shared.m_name.Localize());
                }
                else
                {
                    int prevLevel = level;
                    if (failed_enchantments.ContainsKey(level)) failed_enchantments[level]++;
                    else failed_enchantments.Add(level, 1);
                    level = Mathf.Max(0, level - 1);
                    Save();
                    ValheimEnchantmentSystem._thistype.StartCoroutine(FrameSkipEquip(Item));
                    msg = "$enchantment_fail_leveldown".Localize(Item.m_shared.m_name.Localize(), prevLevel.ToString(), level.ToString());
                }
            }
            else
            {
                msg = "$enchantment_fail_nochange".Localize(Item.m_shared.m_name.Localize(), level.ToString());
                if (failed_enchantments.ContainsKey(level)) failed_enchantments[level]++;
                else failed_enchantments.Add(level, 1);
                Save();
            }
            
            return false;
        }
        
        public static implicit operator bool (EnchantedItem item) => item != null;
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.CreateItemTooltip))]
    private static class InventoryGrid_CreateItemTooltip_Patch
    {
        [UsedImplicitly]
        private static void Prefix(InventoryGrid __instance, ItemDrop.ItemData item, out string __state)
        {
            __state = null;
            if (item?.Data().Get<EnchantedItem>() is not { level: > 0 } idm) return;
            __state = item.m_shared.m_name;
            string color = SyncedData.GetColor(idm, out _, true).IncreaseColorLight();
            item.m_shared.m_name += $" (<color={color}>+{idm.level}</color>)";
        }

        [UsedImplicitly]
        private static void Postfix(InventoryGrid __instance, ItemDrop.ItemData item, string __state)
        {
            if (__state != null) item.m_shared.m_name = __state;
        }
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.GetHoverText))]
    private static class ItemDrop_GetHoverText_Patch
    {
        [UsedImplicitly]
        private static void Prefix(ItemDrop __instance, out string __state)
        {
            __state = null;
            if (__instance.m_itemData?.Data().Get<EnchantedItem>() is not { level: > 0 } idm) return;
            __state = __instance.m_itemData.m_shared.m_name;
            string color = SyncedData.GetColor(idm, out _, true)
                .IncreaseColorLight();
            __instance.m_itemData.m_shared.m_name += $" (<color={color}>+{idm.level}</color>)";
        }

        [UsedImplicitly]
        private static void Postfix(ItemDrop __instance, string __state)
        {
            if (__state != null) __instance.m_itemData.m_shared.m_name = __state;
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float))]
    public class TooltipPatch
    {
        [UsedImplicitly]
        public static void Postfix(ItemDrop.ItemData item, bool crafting, int qualityLevel, ref string __result)
        {
            bool blockShowEnchant = false;
            if (item.Data().Get<EnchantedItem>() is { level: > 0 } en)
            {
                string color = SyncedData.GetColor(en, out _, true).IncreaseColorLight();
                int currentPotency = SyncedData.GetStatIncrease(en);
                __result = new Regex("(\\$item_durability.*)").Replace(__result,
                    $"$1 (<color={color}>$enchantment_increasedwithenchantment</color>)");

                Player.m_localPlayer.GetSkills().GetRandomSkillRange(out float minFactor, out float maxFactor,
                    item.m_shared.m_skillType);
                var damage = item.GetDamage(qualityLevel, item.m_worldLevel);
                __result = new Regex("(\\$inventory_damage.*)").Replace(__result,
                    $"$1 (<color={color}>+{Mathf.RoundToInt(damage.m_damage * currentPotency / 100f * minFactor)} - {Mathf.RoundToInt(damage.m_damage * currentPotency / 100f * maxFactor)}</color>)");
                __result = new Regex("(\\$inventory_blunt.*)").Replace(__result,
                    $"$1 (<color={color}>+{Mathf.RoundToInt(damage.m_blunt * currentPotency / 100f * minFactor)} - {Mathf.RoundToInt(damage.m_blunt * currentPotency / 100f * maxFactor)}</color>)");
                __result = new Regex("(\\$inventory_slash.*)").Replace(__result,
                    $"$1 (<color={color}>+{Mathf.RoundToInt(damage.m_slash * currentPotency / 100f * minFactor)} - {Mathf.RoundToInt(damage.m_slash * currentPotency / 100f * maxFactor)}</color>)");
                __result = new Regex("(\\$inventory_pierce.*)").Replace(__result,
                    $"$1 (<color={color}>+{Mathf.RoundToInt(damage.m_pierce * currentPotency / 100f * minFactor)} - {Mathf.RoundToInt(damage.m_pierce * currentPotency / 100f * maxFactor)}</color>)");
                __result = new Regex("(\\$inventory_fire.*)").Replace(__result,
                    $"$1 (<color={color}>+{Mathf.RoundToInt(damage.m_fire * currentPotency / 100f * minFactor)} - {Mathf.RoundToInt(damage.m_fire * currentPotency / 100f * maxFactor)}</color>)");
                __result = new Regex("(\\$inventory_frost.*)").Replace(__result,
                    $"$1 (<color={color}>+{Mathf.RoundToInt(damage.m_frost * currentPotency / 100f * minFactor)} - {Mathf.RoundToInt(damage.m_frost * currentPotency / 100f * maxFactor)}</color>)");
                __result = new Regex("(\\$inventory_lightning.*)").Replace(__result,
                    $"$1 (<color={color}>+{Mathf.RoundToInt(damage.m_lightning * currentPotency / 100f * minFactor)} - {Mathf.RoundToInt(damage.m_lightning * currentPotency / 100f * maxFactor)}</color>)");
                __result = new Regex("(\\$inventory_poison.*)").Replace(__result,
                    $"$1 (<color={color}>+{Mathf.RoundToInt(damage.m_poison * currentPotency / 100f * minFactor)} - {Mathf.RoundToInt(damage.m_poison * currentPotency / 100f * maxFactor)}</color>)");
                __result = new Regex("(\\$inventory_spirit.*)").Replace(__result,
                    $"$1 (<color={color}>+{Mathf.RoundToInt(damage.m_spirit * currentPotency / 100f * minFactor)} - {Mathf.RoundToInt(damage.m_spirit * currentPotency / 100f * maxFactor)}</color>)");

                __result = new Regex("(\\$item_blockarmor.*)").Replace(__result,
                    $"$1 (<color={color}>+{item.GetBaseBlockPower(qualityLevel) * currentPotency / 100f}</color>)");

                __result = new Regex("(\\$item_armor.*)").Replace(__result,
                    $"$1 (<color={color}>+{item.GetArmor(qualityLevel, item.m_worldLevel) * currentPotency / 100f}</color>)");

                __result +=
                    $"\n\n<color={color}>•</color> $enchantment_bonusespercent (<color={color}>+{currentPotency}%</color>)";

                int chance = en.GetEnchantmentChance();
                if (SyncedData.ShowEnchantmentChance.Value && chance > 0)
                {
                    __result += $"\n<color={color}>•</color> $enchantment_chance (<color={color}>{chance}%</color>)";
                }

                if (chance <= 0)
                {
                    __result += $"\n<color={color}>•</color> $enchantment_maxedout";
                }
                
                blockShowEnchant = en.GetEnchantmentChance() <= 0;
                var fails = en.failed_enchantments;
                if (fails.Count > 0)
                {
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        __result += "\n• $enchantment_failedenchantments:\n";
                        foreach (var fail in fails.OrderBy(f => f.Key))
                        {
                            __result += $"• Lvl {fail.Key} - <color=yellow>x{fail.Value}</color>\n";
                        }
                    }
                    else
                    {
                        __result += "\n• $enchantment_holdshift\n";
                    }
                }
               
            }


            if (blockShowEnchant) return;
            string dropName = item.m_dropPrefab ? item.m_dropPrefab.name : Utils.GetPrefabNameByItemName(item.m_shared.m_name);
            if (SyncedData.GetReqs(dropName) is { } reqs)
            {
                string canBe = $"\n• $enchantment_canbeenchantedwith:";
                if (reqs.enchant_prefab.IsValid())
                {
                    string mainName = ZNetScene.instance.GetPrefab(reqs.enchant_prefab.prefab).GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
                    int val1 = reqs.enchant_prefab.amount;
                    canBe += $"\n<color=yellow>• {mainName} x{val1}</color>";
                }
                if (reqs.blessed_enchant_prefab.IsValid())
                {
                    string blessName = ZNetScene.instance.GetPrefab(reqs.blessed_enchant_prefab.prefab).GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
                    int val2 = reqs.blessed_enchant_prefab.amount;
                    canBe += $"\n<color=yellow>• {blessName} x{val2}</color>";
                }
                __result += canBe;
            }
        }
    }
    
    [HarmonyPatch(typeof(InventoryGui),nameof(InventoryGui.UpdateRecipe))]
    private static class InventoryGui_UpdateRecipe_Patch
    {
        [UsedImplicitly]
        private static void Postfix(InventoryGui __instance)
        {
            EnchantedItem en = __instance.m_selectedRecipe.Value?.Data().Get<EnchantedItem>();
            if (!en) return;
            string color = SyncedData.GetColor(en, out _, true).IncreaseColorLight();
            __instance.m_recipeName.text  += $" (<color={color}>+{en!.level}</color>)";
        }
    }
    
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.AddRecipeToList))]
    private static class InventoryGui_AddRecipeToList_Patch
    {
        private static void Modify(ref string text, ItemDrop.ItemData item)
        {
            EnchantedItem en = item?.Data().Get<EnchantedItem>();
            if (!en) return;
            string color = SyncedData.GetColor(en, out _, true).IncreaseColorLight();
            text  += $" (<color={color}>+{en!.level}</color>)";
        }

        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            CodeMatcher matcher = new(code);
            matcher.MatchForward(false, new CodeMatch(OpCodes.Stloc_2));
            if (matcher.IsInvalid) return matcher.InstructionEnumeration();
            var method = AccessTools.Method(typeof(InventoryGui_AddRecipeToList_Patch), nameof(Modify));
            matcher.Advance(1).InsertAndAdvance(new CodeInstruction(OpCodes.Ldloca_S, 2)).InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_3)).InsertAndAdvance(new CodeInstruction(OpCodes.Call, method));
            return matcher.InstructionEnumeration();
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetBlockPower), typeof(float))]
    private static class ModifyBlockPower
    {
        [UsedImplicitly]
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (__instance.Data().Get<EnchantedItem>() is { level: > 0 } data)
            {
                __result *= 1 + SyncedData.GetStatIncrease(data) / 100f;
            }
        }
    }

    [HarmonyPatch]
    private static class ModifyArmor
    {
        [UsedImplicitly]
        private static MethodInfo TargetMethod()
        {
            return AccessTools.Method(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetArmor));
        }

        [UsedImplicitly]
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (__instance.Data().Get<EnchantedItem>() is { level: > 0 } data)
            {
                __result *= 1 + SyncedData.GetStatIncrease(data) / 100f;
            }
        }
    }

    [HarmonyPatch]
    private static class ModifyDamage
    {
        [UsedImplicitly]
        private static MethodInfo TargetMethod()
        {
            return AccessTools.Method(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDamage));
        }

        [UsedImplicitly]
        private static void Postfix(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
        {
            if (__instance.Data().Get<EnchantedItem>() is { level: > 0 } data)
            {
                __result.Modify(1 + SyncedData.GetStatIncrease(data) / 100f);
            }
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetMaxDurability), typeof(int))]
    public class ApplySkillToDurability
    {
        [UsedImplicitly]
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (__instance.Data().Get<EnchantedItem>() is { level: > 0 } data)
            {
                __result *= 1 + SyncedData.GetStatIncrease(data) / 100f;
            }
        }
    }
}