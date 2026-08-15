using HarmonyLib;

using System;
using System.Collections.Generic;

using XRL;
using XRL.CharacterBuilds.Qud;
using XRL.CharacterBuilds.Qud.UI;
using XRL.Rules;
using XRL.UI.Framework;
using XRL.World;
using XRL.World.Loaders;

using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Utils;

namespace UD_Blink_Mutation.Harmony
{
    [HarmonyPatch]
    public static class QudGenotypeModuleWindow_Patches
    {
        public static bool doDebug = true;

        [HarmonyPatch(
            declaringType: typeof(QudGenotypeModuleWindow),
            methodName: nameof(QudGenotypeModuleWindow.GetSelections))]
        [HarmonyPostfix]
        public static void GetSelections_NoPricklePigs_Postfix(ref QudGenotypeModuleWindow __instance, ref IEnumerable<ChoiceWithColorIcon> __result)
        {
            try
            {
                List<ChoiceWithColorIcon> choiceWithColorIconCopy = new();
                foreach (ChoiceWithColorIcon entry in __result)
                {
                    // CharacterBuilderModules
                    GenotypeEntry genotypeEntry = GenotypeFactory.GetGenotypeEntry(entry.Id);
                    if (genotypeEntry != null && genotypeEntry.CharacterBuilderModules != "NonePlease")
                    {
                        choiceWithColorIconCopy.Add(entry);
                    }
                }
                __result = choiceWithColorIconCopy;
            }
            catch (Exception x)
            {
                MetricsManager.LogException(
                    $"[{MOD_ID}] {nameof(QudGenotypeModuleWindow_Patches)}" +
                    $"{nameof(GetSelections_NoPricklePigs_Postfix)}()",
                    x);
            }
        }
    }
}
