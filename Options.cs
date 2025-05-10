using System;
using System.Collections.Generic;

using XRL;

using static UD_Blink_Mutation.Const;

namespace UD_Blink_Mutation
{
    [HasModSensitiveStaticCache]
    public static class Options
    {
        // Per the wiki, code is taken 1:1
        private static string GetOption(string ID, string Default = "")
        {
            return XRL.UI.Options.GetOption(ID, Default: Default);
        }


        // Checkbox settings
        

        // NPC equipment options
        

        // Debug Settings
        public static int DebugVerbosity
        {
            get
            {
                return Convert.ToInt32(GetOption("Option_UD_Blink_DebugVerbosity"));
            }
            private set
            {
                DebugVerbosity = value;
            }
        }

        public static bool DebugIncludeInMessage
        {
            get
            {
                return GetOption("Option_UD_Blink_DebugIncludeInMessage").EqualsNoCase("Yes");
            }
            private set
            {
                DebugIncludeInMessage = value;
            }
        }

        public static bool DebugBlinkDescriptions
        {
            get
            {
                return GetOption("Option_UD_Blink_DebugIncludeBlinkDebugDescriptions").EqualsNoCase("Yes");
            }
            private set
            {
                DebugBlinkDescriptions = value;
            }
        }

    } //!-- public static class Options
}
