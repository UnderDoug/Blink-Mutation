using HarmonyLib;

using System;
using System.Collections.Generic;

using XRL;
using XRL.CharacterBuilds.Qud;
using XRL.Rules;
using XRL.World;
using XRL.World.Loaders;

using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Utils;

namespace UD_Blink_Mutation.Harmony
{
    [HarmonyPatch]
    public static class QudGenotypeModule_Patches
    {
        public static bool doDebug = true;

        [HarmonyPatch(
            declaringType: typeof(QudGenotypeModule),
            methodName: nameof(QudGenotypeModule.handleUIEvent),
            argumentTypes: new Type[] { typeof(string), typeof(object) },
            argumentVariations: new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal })]
        [HarmonyPrefix]
        public static bool handleUIEvent_NoPricklePigs_Prefix(ref QudGenotypeModule __instance, ref object __result)
        {
            try
            {
                GenotypeEntry genotypeEntry = __instance.data?.Entry;
                if (genotypeEntry != null && genotypeEntry.CharacterBuilderModules == "NonePlease")
                {
                    __instance.data.Genotype = "Mutated Human";
                }
            }
            catch (Exception x)
            {
                MetricsManager.LogException(
                    $"[{MOD_ID}] {nameof(QudGenotypeModule_Patches)}." +
                    $"{nameof(handleUIEvent_NoPricklePigs_Prefix)}()",
                    x);
            }
            return true;
        }

        [HarmonyPatch(
            declaringType: typeof(QudGenotypeModule),
            methodName: nameof(QudGenotypeModule.getSelected))]
        [HarmonyPostfix]
        public static void getSelected_NoPricklePigs_Prefix(ref QudGenotypeModule __instance, ref string __result)
        {
            try
            {
                GenotypeEntry genotypeEntry = GenotypeFactory.GetGenotypeEntry(__result);
                if (genotypeEntry != null && genotypeEntry.CharacterBuilderModules == "NonePlease")
                {
                    __result = "Mutated Human";
                }
            }
            catch (Exception x)
            {
                MetricsManager.LogException(
                    $"[{MOD_ID}] {nameof(QudGenotypeModule_Patches)}." +
                    $"{nameof(getSelected_NoPricklePigs_Prefix)}()",
                    x);
            }
        }
    }
}
