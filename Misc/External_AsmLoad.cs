using System;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;

namespace kg.ValheimEnchantmentSystem.Misc;

[VES_Autoload(VES_Autoload.Priority.Init)]
public static class External_AsmLoad
{
    [UsedImplicitly]
    private static void OnInit()
    {
        LoadAsm("UI_VFX");
    }
    private static void LoadAsm(string name)
    {
        Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("kg.ValheimEnchantmentSystem.Resources." + name + ".dll")!;
        byte[] buffer = new byte[stream.Length];
        // ReSharper disable once MustUseReturnValue
        stream.Read(buffer, 0, buffer.Length); 
        try
        {
            Assembly.Load(buffer);
            stream.Dispose();
        }
        catch(Exception ex)
        {
            Utils.print($"Error loading {name} assembly\n:{ex}", ConsoleColor.Red);
        }
    }
}