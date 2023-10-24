using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using kg.ValheimEnchantmentSystem.Configs;

namespace kg.ValheimEnchantmentSystem;

public static class Fixing_JC_Item
{
    public static void Fix()
    {
        try
        {
            Type jc_gemstones = Type.GetType("Jewelcrafting.GemStones, Jewelcrafting");
            if (jc_gemstones == null) return;
            MethodInfo method = AccessTools.Method(jc_gemstones, "HandleSocketingFrameAndMirrors");
            if (method == null) return;
            HarmonyMethod transpiler = new(AccessTools.Method(typeof(Fixing_JC_Item), nameof(Transpiler)));
            new Harmony("enchantmentJC").Patch(method, transpiler: transpiler);
        }
        catch { }
    }

    private static ItemDrop.ItemData ReplaceWithCopy(ItemDrop.ItemData original)
    {
        const string key = "kg.ValheimEnchantmentSystem#kg.ValheimEnchantmentSystem.Enchantment_Core+Enchanted";
        ItemDrop.ItemData copy = original.Clone();
        if(!SyncedData.AllowJewelcraftingMirrorCopyEnchant.Value)
            copy.m_customData.Remove(key);
        return copy;
    }
    
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
    {
        CodeMatcher matcher = new(code);
        matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_0), new CodeMatch(OpCodes.Ldarg_1), new CodeMatch(OpCodes.Stfld));
        if (matcher.IsInvalid) return matcher.InstructionEnumeration();
        matcher.Advance(2).Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Fixing_JC_Item), nameof(ReplaceWithCopy))));
        return matcher.InstructionEnumeration();
    }
    
}