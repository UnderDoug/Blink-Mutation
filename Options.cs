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
    public static class Options
    {
        private static string OptionsLabel => "Option_UD_Blink_";
        private static Dictionary<string, string> Directory => new()
        {
            { nameof(ObnoxiousYelling), "ObnoxiousYelling" },
            { nameof(AllowWeGoAgain), "AllowWeGoAgain" },
            { nameof(GenerateChaosEmeralds), "GenerateChaosEmeralds" },
            { nameof(DebugVerbosity), "DebugVerbosity" },
            { nameof(DebugIncludeInMessage), "DebugIncludeInMessage" },
            { nameof(DebugBlinkDescriptions), "DebugIncludeBlinkDebugDescriptions" },
            { nameof(DebugAI_SquareUpDescriptions), "DebugIncludeAI_SquareUpDebugDescriptions" },
            { nameof(DebugChaosEmeraldSetBonusDescriptions), "DebugIncludeChaosEmeraldSetBonusDebugDescriptions" },
            { nameof(DebugIgnorePlayerWhenSquaringUp), "DebugIgnorePlayerWhenSquaringUp" },
        };

        public static string GetOptionID(string Option)
        {
            return Directory.ContainsKey(Option) ? OptionsLabel + Directory[Option] : null;
        }
        public static bool TryGetOptionID(string Option, out string ID)
        {
            return (ID = GetOptionID(Option)) != null;
        }

        // Per the wiki, code is taken 1:1
        private static string GetStringOption(string Option, string Default = "")
        {
            if (TryGetOptionID(Option, out string ID))
            {
                return XRL.UI.Options.GetOption(ID, Default: Default);
            }
            return Default;
        }
        private static bool GetBoolOption(string ID, bool Default = false)
        {
            return GetStringOption(ID, Default ? "Yes" : "No").EqualsNoCase("Yes");
        }
        private static int GetIntOption(string ID, int Default = 0)
        {
            return int.Parse(GetStringOption(ID, $"{Default}"));
        }

        private static void SetBoolOption(string Option, bool Value)
        {
            if (TryGetOptionID(Option, out string ID))
            {
                XRL.UI.Options.SetOption(ID, Value);
            }
        }
        private static void SetStringOption(string Option, string Value)
        {
            if (TryGetOptionID(Option, out string ID))
            {
                XRL.UI.Options.SetOption(ID, Value);
            }
        }
        private static void SetIntOption(string ID, int Value)
        {
            SetStringOption(ID, $"{Value}");
        }

        public static bool doDebug = true;
        public static Dictionary<string, bool> classDoDebug = new()
        {
            // General
            { nameof(UD_Blink), true },
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
        public static int DebugVerbosity
        {
            get
            {
                return GetIntOption(nameof(DebugVerbosity), 0);
            }
            set
            {
                SetIntOption(nameof(DebugVerbosity), value);
            }
        }

        public static bool DebugIncludeInMessage
        {
            get
            {
                return GetBoolOption(nameof(DebugIncludeInMessage), false);
            }
            set
            {
                SetBoolOption(nameof(DebugIncludeInMessage), value);
            }
        }

        public static bool DebugBlinkDescriptions
        {
            get
            {
                return GetBoolOption($"{nameof(DebugBlinkDescriptions)}", false);
            }
            set
            {
                SetBoolOption(nameof(DebugBlinkDescriptions), value);
            }
        }

        public static bool DebugAI_SquareUpDescriptions
        {
            get
            {
                return GetBoolOption($"{nameof(DebugAI_SquareUpDescriptions)}", false);
            }
            set
            {
                SetBoolOption(nameof(DebugAI_SquareUpDescriptions), value);
            }
        }
        
        public static bool DebugChaosEmeraldSetBonusDescriptions
        {
            get
            {
                return GetBoolOption($"{nameof(DebugChaosEmeraldSetBonusDescriptions)}", false);
            }
            set
            {
                SetBoolOption(nameof(DebugChaosEmeraldSetBonusDescriptions), value);
            }
        }

        public static bool DebugIgnorePlayerWhenSquaringUp
        {
            get
            {
                return GetBoolOption($"{nameof(DebugIgnorePlayerWhenSquaringUp)}", false);
            }
            set
            {
                SetBoolOption(nameof(DebugIgnorePlayerWhenSquaringUp), value);
            }
        }

        // Checkbox settings

        public static bool ObnoxiousYelling
        {
            get
            {
                return GetBoolOption(nameof(ObnoxiousYelling), true);
            }
            set
            {
                SetBoolOption(nameof(ObnoxiousYelling), value);
            }
        }

        public static bool AllowWeGoAgain
        {
            get
            {
                return GetBoolOption(nameof(AllowWeGoAgain), true);
            }
            set
            {
                SetBoolOption(nameof(AllowWeGoAgain), value);
            }
        }

        public static bool GenerateChaosEmeralds
        {
            get
            {
                return GetBoolOption(nameof(GenerateChaosEmeralds), true);
            }
            set
            {
                SetBoolOption(nameof(GenerateChaosEmeralds), value);
            }
        }

    } //!-- public static class Options
}
