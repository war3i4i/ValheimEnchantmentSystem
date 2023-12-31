﻿using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem.Items_Structures;

[VES_Autoload]
public static class BuildPieces
{
    private static GameObject Station;
    private static ConfigEntry<string> StationReqs;
    
    [UsedImplicitly]
    private static void OnInit()
    {
        Station = ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_EnchantmentScrollStation");
        Station.GetComponent<Piece>().m_category = Piece.PieceCategory.Crafting;
        Station.GetComponent<Piece>().m_name = "$kg_enchantment_scrollstation";
        Station.GetComponent<Piece>().m_description = "$kg_enchantment_scrollstation_description";
        StationReqs = ValheimEnchantmentSystem.config("Enchantment Scroll Station", "Station Build Requirements", "SurtlingCore:3:true:Stone:30:false:Flint:20:false", "Station requirements.");
        
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
                foreach (Piece p in piece)
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
            List<GameObject> hammer = __instance.GetPrefab("Hammer").GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces;
            if(!hammer.Contains(Station)) hammer.Add(Station);
            StationRequirementsChanged();
        }
    }
    
    
    
    
} 