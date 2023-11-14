using System.Reflection.Emit;
using BepInEx.Logging;
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
        if (colorString[0] == '#')
        {
            colorString = colorString.Substring(1);
        }

        if (!uint.TryParse(colorString, System.Globalization.NumberStyles.HexNumber, null, out uint colorValue))
            return Color.white;
        switch (colorString.Length)
        {
            case 8:
            {
                float r = ((colorValue >> 24) & 0xFF) / 255.0f;
                float g = ((colorValue >> 16) & 0xFF) / 255.0f;
                float b = ((colorValue >> 8) & 0xFF) / 255.0f;
                float a = (colorValue & 0xFF) / 255.0f;
                var color = new Color(r, g, b, a);
                return color;
            }
            case 6:
            {
                float r = ((colorValue >> 16) & 0xFF) / 255.0f;
                float g = ((colorValue >> 8) & 0xFF) / 255.0f;
                float b = (colorValue & 0xFF) / 255.0f;
                var color = new Color(r, g, b, 1f);
                return color;
            }
            default:
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

    public static Color IncreaseColorLight(this Color c)
    {
        Color.RGBToHSV(c, out var h, out var s, out var v);
        v = 1f;
        c = Color.HSVToRGB(h, s, v);
        return c;
    }

    public static string GetPrefabNameByItemName(string itemname)
    {
        var find = ObjectDB.instance.m_items.FirstOrDefault(x =>
            x.GetComponent<ItemDrop>().m_itemData.m_shared.m_name == itemname);
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
            print($"Error while deserializing {path}:\n{ex}");
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

    public static double RoundOne(this float f)
    {
        return Math.Round(f, 1);
    }


    public static void IncreaseSkillEXP(Skills.SkillType skillType, float expToAdd)
    {
        Skills.Skill skill = Player.m_localPlayer.m_skills.GetSkill(skillType);

        if (skill != null)
        {
            while (expToAdd > 0)
            {
                float nextLevelRequirement = skill.GetNextLevelRequirement();
                if (skill.m_accumulator + expToAdd >= nextLevelRequirement)
                {
                    expToAdd -= nextLevelRequirement - skill.m_accumulator;
                    skill.m_accumulator = 0;
                    skill.m_level++;
                    skill.m_level = Mathf.Clamp(skill.m_level, 0f, 100f);
                }
                else
                {
                    skill.m_accumulator += expToAdd;
                    expToAdd = 0;
                }
            }
        }
    }

    public static Transform FindChild(Transform aParent, string aName)
    {
        Stack<Transform> stack = new Stack<Transform>();
        Transform transform = aParent;
        do
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                stack.Push(transform.GetChild(i));
            }

            if (stack.Count <= 0)
            {
                return null;
            }

            transform = stack.Pop();
        } while (transform.name != aName);
        return transform;
    }

    public static bool IsEnemy(this Character c)
    {
        if (c == Player.m_localPlayer) return false;
        if (c.IsPlayer())
        {
            return Player.m_localPlayer.IsPVPEnabled() && c.IsPVPEnabled();
        }

        return !c.m_baseAI || c.m_baseAI.IsEnemy(Player.m_localPlayer);
    }

    public static void InstantiateItem(GameObject prefab, int count, int level, Inventory overrideInventory = null)
    {
        Player p = Player.m_localPlayer;
        if (!p || !prefab || count == 0) return;

        var inventory = overrideInventory ?? p.m_inventory;

        if (prefab.GetComponent<ItemDrop>() is not { } item) return;
        
        if (item.m_itemData.m_shared.m_maxStackSize > 1)
        {
            GameObject go = UnityEngine.Object.Instantiate(prefab,
                p.transform.position + p.transform.forward * 1.5f + Vector3.up * 1.5f, Quaternion.identity);
            ItemDrop itemDrop = go.GetComponent<ItemDrop>();
            itemDrop.m_itemData.m_quality = level;
            itemDrop.m_itemData.m_stack = count;
            itemDrop.m_itemData.m_durability = itemDrop.m_itemData.GetMaxDurability();
            itemDrop.Save();
            if (inventory.CanAddItem(go))
            {
                inventory.AddItem(itemDrop.m_itemData);
                ZNetScene.instance.Destroy(go);
            }
        }
        else
        {
            for (int i = 0; i < count; ++i)
            {
                GameObject go = UnityEngine.Object.Instantiate(prefab,
                    p.transform.position + p.transform.forward * 1.5f + Vector3.up * 1.5f, Quaternion.identity);
                ItemDrop itemDrop = go.GetComponent<ItemDrop>();
                itemDrop.m_itemData.m_quality = level;
                itemDrop.m_itemData.m_durability = itemDrop.m_itemData.GetMaxDurability();
                itemDrop.Save();
                if (inventory.CanAddItem(go))
                {
                    inventory.AddItem(itemDrop.m_itemData);
                    ZNetScene.instance.Destroy(go);
                }
            }
        }
    }

    /*public static float RoundOne(this float f)
    {
        return f < 100 ? Mathf.Round(f * 10.0f) * 0.1f : Mathf.Round(f);
    }*/
}