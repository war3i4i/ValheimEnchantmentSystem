using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Items_Structures;
using kg.ValheimEnchantmentSystem.Misc;
using LocalizationManager;
using ServerSync;
using UnityEngine;

namespace kg.ValheimEnchantmentSystem
{
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("org.bepinex.plugins.jewelcrafting", BepInDependency.DependencyFlags.SoftDependency)]
    public class ValheimEnchantmentSystem : BaseUnityPlugin
    {
        private const string GUID = "kg.ValheimEnchantmentSystem";
        private const string PLUGIN_NAME = "Valheim Enchantment System";
        private const string PLUGIN_VERSION = "1.3.3";
        
        public static ValheimEnchantmentSystem _thistype;
        public static AssetBundle _asset;
        public new static ConfigFile Config;
        public static ConfigFile ItemConfig;
        public static string ConfigFolder;
        private static readonly Harmony Harmony = new(GUID);
        public static readonly ConfigSync ConfigSync = new(GUID)
        { 
            DisplayName = GUID, ModRequired = true, 
            MinimumRequiredVersion = PLUGIN_VERSION, CurrentVersion = PLUGIN_VERSION
        }; 
         
        private void Awake()
        {
            Localizer.Load();
            ConfigFolder = Path.Combine(Paths.ConfigPath, "ValheimEnchantmentSystem");
            if (!Directory.Exists(ConfigFolder))
                Directory.CreateDirectory(ConfigFolder);
            
            Config = new ConfigFile(Path.Combine(ConfigFolder, $"{GUID}.cfg"), false);
            ItemConfig = new ConfigFile(Path.Combine(ConfigFolder, "ScrollRecipes.cfg"), false);
            _thistype = this;
            _asset = GetAssetBundle("kg_enchantment");
            External_AsmLoad.Init();
            SyncedData.Init();
            VES_UI.Init();
            Enchantment_VFX.Init(); 
            BuildPieces.Init();
            ScrollItems.Init();
            Harmony.PatchAll();
            Fixing_JC_Item.Fix();
        }

        private void Update() => VES_UI.Update();
 
        private static AssetBundle GetAssetBundle(string filename)
        {
            Assembly execAssembly = Assembly.GetExecutingAssembly();
            string resourceName = execAssembly.GetManifestResourceNames().Single(str => str.EndsWith(filename));
            using Stream stream = execAssembly.GetManifestResourceStream(resourceName)!;
            return AssetBundle.LoadFromStream(stream);
        }

        private static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);
            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;
            return configEntry;
        }

        public static ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true) =>
            config(group, name, value, new ConfigDescription(description), synchronizedSetting);
    }
}