using ItemDataManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem;

[VES_Autoload]
public static class Enchantment_AdditionalEffects
{
    private static readonly Dictionary<string, Material> _wingsMaterials = new();
    private static readonly List<GameObject> _wingsModels = new();
    private static readonly List<GameObject> _auraModels = new();

    private static readonly List<string> _wingsMaterialNames = new()
    {
        "Blood", "Electric", "Flame", "Forest", "Glow", "Lava", "Magic", "Mist", "Ocean", "Organic", "Sharp", "Smoke", "Sun", "Unholy", "Wind", "Lagoshi"
    };

    private class CurrentVFX
    {
        public int wingsModel;
        public string wingsMaterial;
        public int auraModel;
        public string auraColor;
    }

    private static CurrentVFX _current = new();
    private static readonly int AlphaIntensity = Shader.PropertyToID("_AlphaIntensity");
    
    public static ConfigEntry<bool> _enableWingsEffects;
    public static ConfigEntry<bool> _enableAuraEffects;


    [UsedImplicitly]
    private static void OnInit()
    {
        _enableWingsEffects = ValheimEnchantmentSystem._thistype.Config.Bind("Visual", "EnableWingsVFX", true, "Enable Wings VFX");
        _enableAuraEffects = ValheimEnchantmentSystem._thistype.Config.Bind("Visual", "EnableAuraVFX", true, "Enable Aura VFX");
        
        for (int i = 1; i <= 10; ++i)
        {
            GameObject wing = ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_Enchantment_Wing" + i);
            _wingsModels.Add(wing);
            wing.AddComponent<WingsAlphaSmooth>();
        }

        for (int i = 1; i <= 5; ++i)
        {
            GameObject aura = ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_Enchantment_Aura" + i);
            _auraModels.Add(aura);
        }

        foreach (string materialName in _wingsMaterialNames)
            _wingsMaterials.Add(materialName.ToLower(),
                ValheimEnchantmentSystem._asset.LoadAsset<Material>("kg_Enchantment_Wings_Mat_" + materialName));
    }

    private static void UpdateVFXs()
    {
        if (!Player.m_localPlayer) return;
        List<ItemDrop.ItemData> toProcess = new();
        foreach (var item in Player.m_localPlayer.m_inventory.GetAllItems())
            if (item is { m_equipped: true })
                toProcess.Add(item);

        int wingsmodel = 0;
        string wingsmaterial = null;
        float wingsScale = 1f;
        int auramodel = 0;
        string auracolor = null;

        foreach (var item in toProcess)
        {
            if (item.Data().Get<Enchantment_Core.Enchanted>() is not { level: > 0 } enchantment) continue;

            var vfxModule = SyncedData.GetAdditionalEffects(enchantment);
            if (vfxModule is null) continue;

            wingsmodel = vfxModule.wingsmodel;
            wingsmaterial ??= vfxModule.wingsmaterial;
            wingsScale = vfxModule.wingsscale;
            auramodel = vfxModule.auramodel;
            auracolor ??= vfxModule.auracolor;
        }

        if (_current.wingsModel != wingsmodel || _current.wingsMaterial != wingsmaterial)
        {
            _current.wingsModel = wingsmodel;
            _current.wingsMaterial = wingsmaterial;
            Player.m_localPlayer.m_nview.InvokeRPC(ZNetView.Everybody, "kg_Enchantment_ApplyWings", wingsmodel, wingsmaterial ?? "none", wingsScale);
        }

        if (_current.auraModel != auramodel || _current.auraColor != auracolor)
        {
            _current.auraModel = auramodel;
            _current.auraColor = auracolor;
            Player.m_localPlayer.m_nview.InvokeRPC(ZNetView.Everybody, "kg_Enchantment_ApplyAura", auramodel, auracolor ?? "none");
        }
    }

    public class AdditionalEffectsModule : ISerializableParameter
    {
        public int wingsmodel;
        public string wingsmaterial;
        public float wingsscale = 1f;

        public int auramodel;
        public string auracolor;

        public void Serialize(ref ZPackage pkg)
        {
            pkg.Write(wingsmodel);
            pkg.Write(wingsmaterial ?? "");
            pkg.Write(wingsscale);
            pkg.Write(auramodel);
            pkg.Write(auracolor ?? "");
        }

        public void Deserialize(ref ZPackage pkg)
        {
            wingsmodel = pkg.ReadInt();
            wingsmaterial = pkg.ReadString();
            wingsscale = pkg.ReadSingle();
            auramodel = pkg.ReadInt();
            auracolor = pkg.ReadString();
        }
    }

    [HarmonyPatch]
    [ClientOnlyPatch]
    private static class EventsToUpdate_Bulk1
    {
        [UsedImplicitly]
        private static IEnumerable<MethodInfo> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Humanoid), nameof(Humanoid.EquipItem));
            yield return AccessTools.Method(typeof(Humanoid), nameof(Humanoid.UnequipItem));
        }

        [UsedImplicitly]
        private static void Postfix(Humanoid __instance)
        {
            if (__instance != Player.m_localPlayer) return;
            UpdateVFXs();
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
    [ClientOnlyPatch]
    private static class Player_OnDeath_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Player __instance)
        {
            if (__instance == Player.m_localPlayer)
            {
                _current = new CurrentVFX();
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    [ClientOnlyPatch]
    private static class Player_Awake_Patch
    {
        private static void ApplyWings(Player p, int i, string mat, float scale)
        {
            Transform spine2 = p.transform.Find("Visual/Armature/Hips/Spine/Spine1/Spine2");
            if (spine2 is null) return;
            Transform wings = spine2.Find("kg_Enchantment_Wings");
            if (wings) UnityEngine.Object.Destroy(wings.gameObject);
            if (!_enableWingsEffects.Value || i == 0 || i > _wingsModels.Count) return;
            GameObject gameObject = UnityEngine.Object.Instantiate(_wingsModels[i - 1], spine2);
            gameObject.transform.localScale *= scale;
            gameObject.name = "kg_Enchantment_Wings";
            Material useMat = _wingsMaterials.TryGetValue(mat.ToLower(), out Material material)
                ? material
                : _wingsMaterials["flame"];
            gameObject.GetComponent<MeshRenderer>().material = useMat;
        }

        private static void ApplyAura(Player p, int i, string color)
        {
            Transform aura = p.transform.Find("kg_Enchantment_Aura");
            if (aura) UnityEngine.Object.Destroy(aura.gameObject);
            if (!_enableAuraEffects.Value || i == 0 || i > _auraModels.Count) return;
            ColorUtility.TryParseHtmlString(color, out var c);
            foreach (var mr in _auraModels[i - 1].GetComponentsInChildren<ParticleSystem>())
            {  
                var main = mr.main;  
                main.startColor = new Color(c.r, c.g, c.b, main.startColor.color.a);
            }
            GameObject gameObject = UnityEngine.Object.Instantiate(_auraModels[i - 1], p.transform);
            gameObject.name = "kg_Enchantment_Aura";
        }

        [UsedImplicitly]
        private static void Postfix(Player __instance)
        {
            if(!__instance.m_nview || !__instance.m_nview.IsValid()) return; 
            
            __instance.m_nview.Register<int, string, float>("kg_Enchantment_ApplyWings", (_, i, mat, scale) =>
            {
                ApplyWings(__instance, i, mat, scale);
                if (__instance.m_nview.IsOwner())
                {
                    __instance.m_nview.m_zdo.Set("kg_Enchantment_Wings", i);
                    __instance.m_nview.m_zdo.Set("kg_Enchantment_Wings_Mat", mat);
                    __instance.m_nview.m_zdo.Set("kg_Enchantment_Wings_Scale", scale);
                }
            });

            __instance.m_nview.Register<int, string>("kg_Enchantment_ApplyAura", (_, i, color) =>
            {
                ApplyAura(__instance, i, color);
                if (__instance.m_nview.IsOwner())
                {
                    __instance.m_nview.m_zdo.Set("kg_Enchantment_Aura", i);
                    __instance.m_nview.m_zdo.Set("kg_Enchantment_Aura_Color", color);
                }
            });

            int wingsmodel = __instance.m_nview.m_zdo.GetInt("kg_Enchantment_Wings");
            if (wingsmodel > 0)
            {
                string wingsmaterial = __instance.m_nview.m_zdo.GetString("kg_Enchantment_Wings_Mat");
                float wingsScale = __instance.m_nview.m_zdo.GetFloat("kg_Enchantment_Wings_Scale");
                ApplyWings(__instance, wingsmodel, wingsmaterial, wingsScale);
            }

            int auramodel = __instance.m_nview.m_zdo.GetInt("kg_Enchantment_Aura");
            if (auramodel > 0)
            {
                string auracolor = __instance.m_nview.m_zdo.GetString("kg_Enchantment_Aura_Color");
                ApplyAura(__instance, auramodel, auracolor);
            }
        }
    }
    
    public class WingsAlphaSmooth : MonoBehaviour
    {
        public Material mat;
        public float defaultAlpha;

        private void Start()
        {
            mat = GetComponent<MeshRenderer>().material;
            defaultAlpha = mat.GetFloat(AlphaIntensity);
            mat.SetFloat(AlphaIntensity, 0f);
        }

        private float counter;

        private void Update()
        {
            counter += Time.deltaTime * 0.5f;
            float lerp = Mathf.Lerp(0f, defaultAlpha, counter);
            mat.SetFloat(AlphaIntensity, lerp);
            if (counter <= 1f) return;
            mat.SetFloat(AlphaIntensity, defaultAlpha);
            Destroy(this);
        }
    }
}