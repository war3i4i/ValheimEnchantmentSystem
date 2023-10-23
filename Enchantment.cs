﻿using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using ItemDataManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using UnityEngine;
using Random = UnityEngine.Random;

namespace kg.ValheimEnchantmentSystem;

public static class Enchantment
{
    public static int GetEnchantment(this ItemDrop.ItemData item) =>
        item?.Data().Get<EnchantedItem>() is { } en ? en.level : 0;

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
            public int level = 1;
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

        private Dictionary<int, int> failed_enchantments => _internal.failed_enchantments;

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

        public bool Enchant(bool safeEnchant)
        {
            if (!CanEnchant())
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft,
                    "<color=red>Can't enchant this item</color>");
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                    "<color=red>Can't enchant this item</color>");
                return false;
            }

            if (!HaveReqs(safeEnchant))
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft,
                    "<color=red>Doesn't meet requirements</color>");
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                    "<color=red>Doesn't meet requirements</color>");
                return false;
            }

            if (CheckRandom())
            {
                level++;
                Save();
                MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft,
                    "<color=green>Enchantment successful</color>");
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                    "<color=green>Enchantment successful</color>");
                ValheimEnchantmentSystem._thistype.StartCoroutine(FrameSkipEquip(Item));
                return true; 
            }
            else
            {
                if (SyncedData.SafetyLevel.Value <= level && !safeEnchant)
                {
                    Player.m_localPlayer.UnequipItem(Item);
                    Player.m_localPlayer.m_inventory.RemoveItem(Item);
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft,
                        "<color=#AF0000>Enchantment failed and item was destroyed</color>");
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                        "<color=#AF0000>Enchantment failed and item was destroyed</color>");
                }
                else
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft,
                        "<color=#AF0000>Enchantment failed</color>");
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                        "<color=#AF0000>Enchantment failed</color>");
                    if(failed_enchantments.ContainsKey(level)) failed_enchantments[level]++;
                    else failed_enchantments.Add(level, 1);
                    Save();
                }

                return false;
            }
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
            if (item != null && item.Data().Get<EnchantedItem>() is { } idm)
            {
                __state = item.m_shared.m_name;
                string color = SyncedData.GetColor(idm, out _, true).IncreaseColorLight();
                item.m_shared.m_name += $" (<color={color}>+{idm.level}</color>)";
            }
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
            if (__instance.m_itemData?.Data().Get<EnchantedItem>() is { } idm)
            {
                __state = __instance.m_itemData.m_shared.m_name;
                string color = SyncedData.GetColor(idm, out _, true)
                    .IncreaseColorLight();
                __instance.m_itemData.m_shared.m_name += $" (<color={color}>+{idm.level}</color>)";
            }
        }

        [UsedImplicitly]
        private static void Postfix(ItemDrop __instance, string __state)
        {
            if (__state != null) __instance.m_itemData.m_shared.m_name = __state;
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData),
        typeof(int), typeof(bool), typeof(float))]
    public class UpdateDurabilityDisplay
    {
        public static void Postfix(ItemDrop.ItemData item, bool crafting, ref string __result, int qualityLevel)
        {
            if (item == null) return;
            bool blockShowEnchant = false;
            if (!crafting && item.Data().Get<EnchantedItem>() is { } data)
            {
                string color = SyncedData.GetColor(data, out _, true)
                    .IncreaseColorLight();
                int currentPotency = SyncedData.GetStatIncrease(data);
                __result = new Regex("(\\$item_durability.*)").Replace(__result,
                    $"$1 (<color={color}>Increased with Enchantment</color>)");

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
                    $"\n\n<color={color}>•</color> Enchantment bonuses (<color={color}>+{currentPotency}%</color>)";

                int chance = data.GetEnchantmentChance();
                if (SyncedData.ShowEnchantmentChance.Value && chance > 0)
                {
                    __result += $"\n<color={color}>•</color> Enchantment chance (<color={color}>{chance}%</color>)";
                }

                if (chance <= 0)
                {
                    __result += $"\n<color={color}>•</color> Enchantment is maxed out";
                }

                __result += "\n";

                blockShowEnchant = data.GetEnchantmentChance() <= 0;
            }


            if (blockShowEnchant) return;
            string dropName = crafting ? Utils.GetPrefabNameByItemName(item.m_shared.m_name) : item.m_dropPrefab.name;
            if (SyncedData.GetReqs(dropName) is { } reqs)
            {
                string mainName = ZNetScene.instance.GetPrefab(reqs.enchant_prefab.prefab).GetComponent<ItemDrop>()
                    .m_itemData.m_shared.m_name;
                int val1 = reqs.enchant_prefab.amount;
                string blessName = ZNetScene.instance.GetPrefab(reqs.blessed_enchant_prefab.prefab)
                    .GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
                int val2 = reqs.blessed_enchant_prefab.amount;
                string canBe =
                    $"\n• Can be enchanted with:\n<color=yellow>• {mainName} x{val1}\n• {blessName} x{val2}</color>\n";
                __result += canBe;
            }
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetBlockPower), typeof(float))]
    private static class ModifyBlockPower
    {
        [UsedImplicitly]
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (__instance.Data().Get<EnchantedItem>() is { } data)
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
            if (__instance.Data().Get<EnchantedItem>() is { } data)
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
            if (__instance.Data().Get<EnchantedItem>() is { } data)
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
            if (__instance.Data().Get<EnchantedItem>() is { } data)
            {
                __result *= 1 + SyncedData.GetStatIncrease(data) / 100f;
            }
        }
    }
}