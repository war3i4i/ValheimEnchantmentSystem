namespace kg.ValheimEnchantmentSystem.Misc;

public class ImplicitBool
{
    public static implicit operator bool(ImplicitBool b) => b != null;
}