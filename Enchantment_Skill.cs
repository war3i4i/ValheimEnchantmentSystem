using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;
using SkillManager;
using UnityEngine;

namespace kg.ValheimEnchantmentSystem;

[VES_Autoload]
public static class Enchantment_Skill
{
    public static Skills.SkillType SkillType_Enchantment;
    [UsedImplicitly]
    private static void OnInit()
    {
        new Skill("kg_Enchantment", "enchantment.png") { Configurable = true };
        SkillType_Enchantment = (Skills.SkillType)Mathf.Abs("kg_Enchantment".GetStableHashCode());
    }
}