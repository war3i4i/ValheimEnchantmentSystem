﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using fastJSON;
using HarmonyLib;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Items_Structures;
using kg.ValheimEnchantmentSystem.Misc;
using LocalizationManager;
using ServerSync;
using UnityEngine;
using UnityEngine.Rendering;

namespace kg.ValheimEnchantmentSystem
{
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("org.bepinex.plugins.jewelcrafting", BepInDependency.DependencyFlags.SoftDependency)]
    public class ValheimEnchantmentSystem : BaseUnityPlugin
    {
        private const string GUID = "kg.ValheimEnchantmentSystem";
        private const string PLUGIN_NAME = "Valheim Enchantment System";
        private const string PLUGIN_VERSION = "1.3.4";
        
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
        private enum WorkingAs
        {
            Client,
            Server
        }
        private static WorkingAs WorkingAsType;
         
        private void Awake()
        {
            _thistype = this;
            WorkingAsType = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ? WorkingAs.Server : WorkingAs.Client;
            JSON.Parameters = new JSONParameters
            {
                UseExtensions = false,
                SerializeNullValues = false,
                DateTimeMilliseconds = false,
                UseUTCDateTime = true,
                UseOptimizedDatasetSchema = true,
                UseValuesOfEnums = true, 
            };
            Localizer.Load();
            ConfigFolder = Path.Combine(Paths.ConfigPath, "ValheimEnchantmentSystem");
            if (!Directory.Exists(ConfigFolder))
                Directory.CreateDirectory(ConfigFolder);
            Config = new ConfigFile(Path.Combine(ConfigFolder, $"{GUID}.cfg"), false);
            ItemConfig = new ConfigFile(Path.Combine(ConfigFolder, "ScrollRecipes.cfg"), false);
            _asset = GetAssetBundle("kg_enchantment");
            
            IEnumerable<KeyValuePair<VES_Autoload, Type>> toAutoload = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetCustomAttribute<VES_Autoload>() != null)
                .Select(x => new KeyValuePair<VES_Autoload, Type>(x.GetCustomAttribute<VES_Autoload>(), x))
                .OrderBy(x => x.Key.priority);
            foreach (KeyValuePair<VES_Autoload, Type> autoload in toAutoload)
            {
                MethodInfo method = autoload.Value.GetMethod(autoload.Key.InitMethod ?? "OnInit", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
                if (method == null)
                {
                    Utils.print($"Error loading {autoload.Value.Name} class, method {autoload.Key.InitMethod} not found", ConsoleColor.Red);
                    continue;
                }
                try
                {
                    method.Invoke(null, null);
                }
                catch (Exception ex)
                {
                    Utils.print($"Autoload exception on method {method}. Class {autoload.Value}\n:{ex}", ConsoleColor.Red);
                }
            }
            
            AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly())
                .Where(t => WorkingAsType switch
                {
                    WorkingAs.Client => t.GetCustomAttribute<ServerOnlyPatch>() == null,
                    WorkingAs.Server => t.GetCustomAttribute<ClientOnlyPatch>() == null,
                    _ => true
                }).Do(type => Harmony.CreateClassProcessor(type).Patch());
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