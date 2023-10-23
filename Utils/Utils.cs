using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using YamlDotNet.Serialization;

namespace kg.ValheimEnchantmentSystem;

public static class Utils
{
    public static bool IsDebug_VES => Player.m_debugMode || ZNet.IsSinglePlayer;
    public static bool IsDebug_Strict => Player.m_debugMode;

    public static void print(object obj, ConsoleColor color = ConsoleColor.DarkGreen)
    {
        if (Application.platform == RuntimePlatform.WindowsPlayer)
        {
            ConsoleManager.SetConsoleColor(color);
            ConsoleManager.StandardOutStream.WriteLine($"[{DateTime.Now}] [kg.ValheimEnchantmentSystem] {obj}");
            ConsoleManager.SetConsoleColor(ConsoleColor.White);
            foreach (ILogListener logListener in BepInEx.Logging.Logger.Listeners)
                if (logListener is DiskLogListener { LogWriter: not null } bepinexlog)
                    bepinexlog.LogWriter.WriteLine($"[{DateTime.Now}] [kg.ValheimEnchantmentSystem] {obj}");
        }
        else
        {
            MonoBehaviour.print($"[{DateTime.Now}] [kg.ValheimEnchantmentSystem] " + obj);
        }
    }

    public static void arr_print(IEnumerable arr, ConsoleColor color = ConsoleColor.DarkGreen)
    {
        if (Application.platform == RuntimePlatform.WindowsPlayer)
        {
            ConsoleManager.SetConsoleColor(color);
            ConsoleManager.StandardOutStream.WriteLine($"[ValheimEnchantmentSystem] printing array: {arr}");
            int c = 0;
            foreach (object item in arr)
            {
                ConsoleManager.StandardOutStream.WriteLine($"[{c++}] {item}");
                foreach (ILogListener logListener in BepInEx.Logging.Logger.Listeners)
                    if (logListener is DiskLogListener { LogWriter: not null } bepinexlog)
                        bepinexlog.LogWriter.WriteLine($"[{c++}] {item}");
            }

            ConsoleManager.SetConsoleColor(ConsoleColor.White);
        }
        else
        {
            MonoBehaviour.print("[ValheimEnchantmentSystem] " + arr);
            int c = 0;
            foreach (object item in arr)
            {
                MonoBehaviour.print($"[{c++}] {item}");
            }
        }
    }

    public static void WriteFile(this string path, string data)
    {
        File.WriteAllText(path, data);
    }

    public static string ReadFile(this string path)
    {
        return File.ReadAllText(path);
    }

    public static string Localize(this string text)
    {
        return string.IsNullOrEmpty(text) ? string.Empty : Localization.instance.Localize(text);
    }
    
    public static string Localize(this string text, params string[] args)
    {
        return string.IsNullOrEmpty(text) ? string.Empty : Localization.instance.Localize(text, args);
    }

    public static bool StlocIndex(this object obj, int index) =>
        obj is LocalBuilder builder && builder.LocalIndex == index;

    public static Color ToColorAlpha(this string colorString)
    {
        if (colorString.StartsWith("#"))
        {
            colorString = colorString.Substring(1);
        }

        if (colorString.Length == 8 && uint.TryParse(colorString, System.Globalization.NumberStyles.HexNumber, null,
                out uint colorValue))
        {
            float r = ((colorValue >> 24) & 0xFF) / 255.0f;
            float g = ((colorValue >> 16) & 0xFF) / 255.0f;
            float b = ((colorValue >> 8) & 0xFF) / 255.0f;
            float a = (colorValue & 0xFF) / 255.0f;
            var color = new Color(r, g, b, a);
            return color;
        }
        else
        {
            if (ColorUtility.TryParseHtmlString(colorString, out var color))
            {
                return color;
            }

            return Color.white;
        }
    }

    public static int CustomCountItemsNoLevel(string prefab)
    {
        int num = 0;
        foreach (ItemDrop.ItemData itemData in Player.m_localPlayer.m_inventory.m_inventory)
        {
            if (itemData.m_dropPrefab.name == prefab)
            {
                num += itemData.m_stack;
            }
        }

        return num;
    }
    
    public static void CustomRemoveItemsNoLevel(string prefab, int amount)
    {
        foreach (ItemDrop.ItemData itemData in Player.m_localPlayer.m_inventory.m_inventory)
        {
            if (itemData.m_dropPrefab.name == prefab)
            {
                int num = Mathf.Min(itemData.m_stack, amount);
                itemData.m_stack -= num;
                amount -= num;
                if (amount <= 0)
                    break;
            }
        }

        Player.m_localPlayer.m_inventory.m_inventory.RemoveAll(x => x.m_stack <= 0);
        Player.m_localPlayer.m_inventory.Changed();
    }
    
    public static string IncreaseColorLight(this string color)
    {
        if (!ColorUtility.TryParseHtmlString(color, out var c)) return color;
        Color.RGBToHSV(c, out var h, out var s, out var v);
        v = 1f;
        c = Color.HSVToRGB(h, s, v);
        return "#" + ColorUtility.ToHtmlStringRGB(c);
    }

    public static string GetPrefabNameByItemName(string itemname)
    {
        var find = ObjectDB.instance.m_items.FirstOrDefault(x => x.GetComponent<ItemDrop>().m_itemData.m_shared.m_name == itemname);
        if (find == null) return null;
        return find.name;
    }

    public static T FromYAML<T>(this string path) where T : new()
    {
        try
        {
            T obj = new DeserializerBuilder().Build().Deserialize<T>(File.ReadAllText(path));
            return obj;
        }
        catch (Exception ex)
        {
            print($"Erorr while deserializing {path}: {ex.Message}");
            return new T();
        }
    }

    private static IEnumerator DelayedAction(Action invoke, int skipFrames)
    {
        for (int i = 0; i < skipFrames; i++)
            yield return null;
        
        invoke();
    }
    
    public static void DelayedInvoke(this MonoBehaviour mb, Action invoke, int skipFrames)
    {
        mb.StartCoroutine(DelayedAction(invoke, skipFrames));
    }
    
}