using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Obeliskial_Content;
using Obeliskial_Essentials;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Collections;
using TMPro.Examples;
using System.Text;
using System.Text.RegularExpressions;

namespace TraitMod
{
    [HarmonyPatch]
    internal class Traits
    {

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), "DamageBonus")]
        public static void DamageBonusPostfix(ref Character __instance, ref float[] __result)
        {
            if (AtOManager.Instance.TeamHaveTrait("shazixnarhealingbrew") && __instance.GetMaxHP() > 120 && __instance.IsHero)
            {
                int hpDifference = __instance.GetMaxHP() - 120;
                if (hpDifference >= 9)
                {
                    __result[0] += (float)(hpDifference / 9f * 0.2);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), "GetTraitDamagePercentModifiers")]
        public static void GetTraitDamagePercentModifiersPostfix(ref Character __instance, ref float __result)
        {
            if (AtOManager.Instance.TeamHaveTrait("shazixnarhealingbrew") && __instance.GetMaxHP() > 120 && __instance.IsHero)
            {
                int hpDifference = __instance.GetMaxHP() - 120;
                if (hpDifference >= 9)
                {
                    float num1 = hpDifference / 9f;
                    float num2 = 2f;
                    __result += num1 * num2;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), "HealReceivedBonus")]
        public static void HealReceivedPostfix(ref Character __instance, ref float[] __result)
        {
            if (AtOManager.Instance.TeamHaveTrait("shazixnarhealingbrew") && __instance.GetMaxHP() > 120 && __instance.IsHero)
            {
                int hpDifference = __instance.GetMaxHP() - 120;
                if (hpDifference >= 9)
                {
                    __result[0] += (float)(hpDifference / 9f * 0.2);
                    __result[1] += (float)(hpDifference / 9f * 2);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CardData), "SetDescriptionNew")]
        public static void SetDescriptionNewPostfix(ref CardData __instance)
        {
            if (__instance.Item != null && __instance.Item.AuracurseCustomString == "itemCustomTextExplodeChargesMonsters")
            {
                if (__instance.Item.AuracurseCustomModValue1 - 25 >= 0)
                __instance.DescriptionNormalized = Regex.Replace(__instance.DescriptionNormalized, "monsters explode at [0-9]*", "monsters explosion charges <color=#263ABC><size=+.1>+" + (__instance.Item.AuracurseCustomModValue1 - 25) + "</color></size> stacks");
                else if (__instance.Item.AuracurseCustomModValue1 - 25 < 0)
                __instance.DescriptionNormalized = Regex.Replace(__instance.DescriptionNormalized, "monsters explode at [0-9]*", "monsters explosion charges <color=#263ABC><size=+.1>-" + (25 - __instance.Item.AuracurseCustomModValue1) + "</color></size> stacks");
            }
        }

        // list of your trait IDs
        public static string[] myTraitList = { "shazixnarjinx", "shazixnarhealingbrew" };

        private static readonly Traits _instance = new Traits();

        public static void myDoTrait(
            string trait,
            Enums.EventActivation evt,
            Character character,
            Character target,
            int auxInt,
            string auxString,
            CardData castedCard)
        {
            switch(trait)
            {
                case "shazixnarjinx":
                    _instance.shazixnarjinx(evt, character, target, auxInt, auxString, castedCard, trait);
                    break;
                    
                case "shazixnarhealingbrew":
                    _instance.shazixnarhealingbrew(evt, character, target, auxInt, auxString, castedCard, trait);
                    break;
            }
        }

        // activate traits
        public void shazixnarjinx(
            Enums.EventActivation evt,
            Character character,
            Character target,
            int auxInt,
            string auxString,
            CardData castedCard,
            string trait)
        {
            if (character == null || target == null || !target.Alive) return;

            // 只要 trait 被触发就施加效果，不区分事件
            target.SetAuraTrait(character, "dark", 2);
            target.SetAuraTrait(character, "poison", 2);

            character.HeroItem?.ScrollCombatText(
                Texts.Instance.GetText("traits_Jinx", ""),
                Enums.CombatScrollEffectType.Trait
            );
        }

        public void shazixnarhealingbrew(
            Enums.EventActivation evt,
            Character character,
            Character target,
            int auxInt,
            string auxString,
            CardData castedCard,
            string trait)
        {
            if (character == null || castedCard == null) return;

            // 只在使用卡牌时触发
            if (evt != Enums.EventActivation.CastCard) return;

            // 只能在能量刚被消耗后触发
            if (MatchManager.Instance.energyJustWastedByHero <= 0) return;

            // 必须是治疗或暗影法术
            if (!castedCard.HasCardType(Enums.CardType.Healing_Spell) &&
                !castedCard.HasCardType(Enums.CardType.Shadow_Spell)) return;

            if (character.HeroData == null) return;

            TraitData data = Globals.Instance.GetTraitData(trait);
            int used = MatchManager.Instance.activatedTraits.ContainsKey(trait) ? MatchManager.Instance.activatedTraits[trait] : 0;
            if (used >= data.TimesPerTurn) return;

            // 更新次数
            MatchManager.Instance.activatedTraits[trait] = used + 1;
            MatchManager.Instance.SetTraitInfoText();

            // 返还能量
            character.ModifyEnergy(1, true);

            character.HeroItem?.ScrollCombatText(
                Texts.Instance.GetText("traits_Healing Brew", "")
                + Functions.TextChargesLeft(used + 1, data.TimesPerTurn),
                Enums.CombatScrollEffectType.Trait
            );

            EffectsManager.Instance.PlayEffectAC("energy", true, character.HeroItem?.CharImageT, false, 0f);

            // 找到当前生命值最低的英雄
            Hero[] teamHero = MatchManager.Instance.GetTeamHero();
            Hero lowHpHero = null;
            int lowestHp = int.MaxValue;
            foreach (var hero in teamHero)
            {
                if (hero != null && hero.HeroData != null && hero.Alive && hero.HpCurrent < lowestHp)
                {
                    lowestHp = hero.HpCurrent;
                    lowHpHero = hero;
                }
            }

            if (lowHpHero != null)
            {
                lowHpHero.SetAuraTrait(character, "regeneration", 2);
                lowHpHero.SetAuraTrait(character, "vitality", 1);

                if (lowHpHero.HeroItem != null)
                {
                    EffectsManager.Instance.PlayEffectAC("regeneration", true, lowHpHero.HeroItem.CharImageT, false, 0f);
                    EffectsManager.Instance.PlayEffectAC("vitality", true, lowHpHero.HeroItem.CharImageT, false, 0f);
                }
            }
        }

        [HarmonyPatch(typeof(Trait), "DoTrait")]
        public static class Trait_DoTrait_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(
                Enums.EventActivation __0,   // theEvent
                string __1,                  // trait id
                Character __2,               // character
                Character __3,               // target
                int __4,                     // auxInt
                string __5,                  // auxString
                CardData __6,                // castedCard
                Trait __instance)
            {
                string trait = __1;

                // 如果是自定义 trait，就直接调用我们的逻辑
                if (myTraitList.Contains(trait))
                {
                    myDoTrait(
                        trait,
                        __0,        // event
                        __2,        // character
                        __3,        // target
                        __4,        // auxInt
                        __5,        // auxString
                        __6         // castedCard
                    );

                    // 返回 false = 阻止原版 DoTrait 执行
                    return false;
                }

                // 否则走原版逻辑
                return true;
            }
        }

        public static string TextChargesLeft(int currentCharges, int chargesTotal)
        {
            int cCharges = currentCharges;
            int cTotal = chargesTotal;
            return "<br><color=#FFF>" + cCharges.ToString() + "/" + cTotal.ToString() + "</color>";
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AtOManager), "GlobalAuraCurseModificationByTraitsAndItems")]
        public static void GlobalAuraCurseModificationByTraitsAndItemsPostfix(ref AtOManager __instance, ref AuraCurseData __result, string _type, string _acId, Character _characterCaster, Character _characterTarget)
        {
            bool flag = false;
            bool flag2 = false;
            if (_characterCaster != null && _characterCaster.IsHero)
            {
                flag = _characterCaster.IsHero;
            }
            if (_characterTarget != null && _characterTarget.IsHero)
            {
                flag2 = true;
            }
            if (_acId == "dark")
            {
                int ExplodeAtStacksModify = 25;
                if (_type == "set")
                {
                    float DamageWhenConsumedPerChargeModify = 2;
                    if (!flag2)
                    {
                        if (__instance.TeamHaveItem("thedarkone", 0, true))
                        {
                            ExplodeAtStacksModify += Globals.Instance.GetItemData("thedarkone").AuracurseCustomModValue1 - 25;
                        }
                        if (__instance.TeamHaveItem("blackdeck", 0, true))
                        {
                            ExplodeAtStacksModify += Globals.Instance.GetItemData("blackdeck").AuracurseCustomModValue1 - 25;
                        }
                        if (__instance.TeamHaveItem("cupofdeath", 0, true))
                        {
                            ExplodeAtStacksModify += Globals.Instance.GetItemData("cupofdeath").AuracurseCustomModValue1 - 25;
                        }
                        if (__instance.TeamHaveTrait("shazixnarshadowform"))
                        {
                            ExplodeAtStacksModify += 5;
                            DamageWhenConsumedPerChargeModify += 0.5f;
                        }
                        if (__instance.TeamHavePerk("mainperkdark2c"))
                        {
                            DamageWhenConsumedPerChargeModify += 0.7f;
                        }
                        __result.ExplodeAtStacks = ExplodeAtStacksModify;
                        __result.DamageWhenConsumedPerCharge = DamageWhenConsumedPerChargeModify;
                    }
                }
                else if (_type == "consume")
                {
                    float DamageWhenConsumedPerChargeModify = 2;
                    if (!flag)
                    {
                        if (__instance.TeamHaveTrait("shazixnarshadowform"))
                        {
                            DamageWhenConsumedPerChargeModify += 0.5f;
                        }
                        if (__instance.TeamHavePerk("mainperkdark2c"))
                        {
                            DamageWhenConsumedPerChargeModify += 0.7f;
                        }
                        __result.DamageWhenConsumedPerCharge = DamageWhenConsumedPerChargeModify;
                    }
                }
            }
            else if (_acId == "sharp" && _type == "set" && flag2)
            {
                if (__instance.TeamHavePerk("mainperkSharp1d"))
                { // Sharp on heros also increases the Shadow damage by 1 per charge.
                    __result.AuraDamageType3 = Enums.DamageType.Shadow;
                    __result.AuraDamageIncreasedPerStack3 = 1;
                }
                if (__instance.TeamHaveTrait("shrilltone"))
                {
                    __result.AuraDamageType4 = Enums.DamageType.Mind;
                    __result.AuraDamageIncreasedPerStack4 = 1;
                }
            }
            else if (_acId == "regeneration" && _type == "set" && flag2)
            {
                if (__instance.TeamHavePerk("mainperkregeneration1c"))
                { // Regeneration on heroes also increases all resistance by 0.5% per charge and increases Max HP by 1 per charge.
                    __result.ResistModified = Enums.DamageType.All;
                    __result.ResistModifiedPercentagePerStack = 0.5f;
                    __result.CharacterStatModified = Enums.CharacterStat.Hp;
                    __result.CharacterStatModifiedValuePerStack = 1;
                }
                if (__instance.TeamHaveTrait("shazixnarjinx"))
                { // Regeneration on heroes increases Shadow damage by 0.5 per charge.
                    __result.AuraDamageType = Enums.DamageType.Shadow;
                    __result.AuraDamageIncreasedPerStack = 0.5f;
                }
                if (__instance.TeamHaveTrait("shazixnarmojo"))
                { // Increase Max charge 25
                    __result.MaxMadnessCharges = 75;
                }
            }
            else if (_acId == "vitality" && _type == "set" && flag2)
            {
                if (__instance.TeamHaveTrait("shazixnarmojo"))
                { // Increase Max charge 25
                    __result.MaxMadnessCharges = 75;
                }
                if (__instance.TeamHavePerk("mainperkvitality1a") && _characterTarget != null)
                { // Vitality on heroes instead increases Max HP by 8 per charge.
                    __result.CharacterStatModifiedValuePerStack = 8;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Character), "SetEvent")]
        public static void SetEventPrefix(ref Character __instance, ref Enums.EventActivation theEvent, Character target = null)
        {
        }
    }
}
