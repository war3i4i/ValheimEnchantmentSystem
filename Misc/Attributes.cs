namespace kg.ValheimEnchantmentSystem.Misc;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class VES_Autoload : Attribute
{
    public enum Priority
    {
        Init,
        First,
        Normal,
        Last
    }

    public readonly Priority priority;
    public readonly string InitMethod;

    public VES_Autoload(Priority priority = Priority.Last, string InitMethod = "OnInit")
    {
        this.priority = priority;
        this.InitMethod = InitMethod;
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ClientOnlyPatch : Attribute
{
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ServerOnlyPatch : Attribute
{
}