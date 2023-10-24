using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine; 

namespace kg.ValheimEnchantmentSystem.Items_Structures;

public static class BuildPieces
{
    public static GameObject Station;
    private static ConfigEntry<string> StationReqs;
    
    public static void Init()
    {
        Station = ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_EnchantmentScrollStation");
        Station.GetComponent<Piece>().m_category = Piece.PieceCategory.Crafting;
        Station.GetComponent<Piece>().m_name = "$kg_enchantment_scrollstation";
        Station.GetComponent<Piece>().m_description = "$kg_enchantment_scrollstation_description";
        StationReqs = ValheimEnchantmentSystem.config("Enchantment Scroll Station", "Station Build Requirements", "SurtlingCore:10:true:Stone:30:false:Flint:20:false", "Station requirements.");
        
        StationReqs.SettingChanged += StationRequirementsChanged;
    } 

    private static void StationRequirementsChanged(object sender = null, EventArgs e = null)
    { 
        if (!ZNetScene.instance) return; 
        try
        {
            string[] split = StationReqs.Value.Split(':');
            List<Piece.Requirement> reqs = new();
            for (int i = 0; i < split.Length; i += 3)
            {
                string prefab = split[i];
                int amount = int.Parse(split[i + 1]);
                bool recover = bool.Parse(split[i + 2]);
                reqs.Add(new Piece.Requirement
                {
                    m_resItem = ObjectDB.instance.GetItemPrefab(prefab).GetComponent<ItemDrop>(),
                    m_amount = amount, 
                    m_recover = recover
                });
            } 

            Station.GetComponent<Piece>().m_resources = reqs.ToArray();
            if (Piece.s_allPieces?.Count > 0)
            {
                IEnumerable<Piece> piece = Piece.s_allPieces.Where(x => global::Utils.GetPrefabName(x.gameObject) == Station.name);
                foreach (var p in piece)
                    p.m_resources = reqs.ToArray();
            }
        }
        catch (Exception ex)
        {
            Utils.print(ex);
            Station.GetComponent<Piece>().m_resources = Array.Empty<Piece.Requirement>();
        }
    }


    [HarmonyPatch(typeof(ZNetScene),nameof(ZNetScene.Awake))]
    private static class ZNetScene_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[Station.name.GetStableHashCode()] = Station;
            var hammer = __instance.GetPrefab("Hammer").GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces;
            if(!hammer.Contains(Station)) hammer.Add(Station);
            StationRequirementsChanged();
        }
    }
    
    
    
    
} 