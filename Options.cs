using System;
using System.Collections.Generic;

using XRL;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using XRL.World.WorldBuilders;
using static UD_Blink_Mutation.Const;

namespace UD_Blink_Mutation
{
    [HasModSensitiveStaticCache]
    [HasOptionFlagUpdate(Prefix = "Option_UD_BlinkMutation_")]
    public static class Options
    {
        public static bool doDebug = true;
        public static Dictionary<string, bool> classDoDebug = new()
        {
            // General
            { nameof(UD_Blink), true },
            { nameof(UD_CyberneticsOverclockedCentralNervousSystem), true },
            { nameof(ChaosEmeraldSetPiece), true },
            { nameof(ChaosEmeraldSetBonus), true },
            { nameof(UD_ChaosEmeraldDispersalWorldBuilder), true },

            // Events
            { nameof(GetBlinkRangeEvent), true },
            { nameof(BeforeBlinkEvent), true },
            { nameof(AfterBlinkEvent), true },

            // AI Parts
            { nameof(AI_UD_SquareUp), true },
            { nameof(AI_UD_Blinker), true },
            { nameof(AI_UD_Flickerer), true },
        };

        public static bool getClassDoDebug(string Class)
        {
            if (classDoDebug.ContainsKey(Class))
            {
                return classDoDebug[Class];
            }
            return doDebug;
        }

        // Debug Settings
        [OptionFlag] public static int DebugVerbosity;
        [OptionFlag] public static bool DebugIncludeInMessage;
        [OptionFlag] public static bool DebugBlinkDebugDescriptions;
        [OptionFlag] public static bool DebugAI_UD_BlinkerDebugDescriptions;
        [OptionFlag] public static bool DebugAI_UD_FlickererDebugDescriptions;
        [OptionFlag] public static bool DebugAI_UD_SquareUpDebugDescriptions;
        [OptionFlag] public static bool DebugChaosEmeraldSetBonusDebugDescriptions;
        [OptionFlag] public static bool DebugIgnorePlayerWhenSquaringUp;

        // Checkbox settings
        [OptionFlag] public static bool ObnoxiousYelling;
        [OptionFlag] public static bool AllowWeGoAgain;
        [OptionFlag] public static bool CyberFlickerFallsBackToRandom;
        [OptionFlag] public static bool GenerateChaosEmeralds;

    } //!-- public static class Options
}
