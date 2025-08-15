using XRL;

using UD_Modding_Toolbox;

using static UD_Modding_Toolbox.Const;
using static UD_Modding_Toolbox.Utils;

namespace UD_Blink_Mutation
{
    public static class Utils
    {
        private static bool doDebug => true;
        private static bool getDoDebug (string MethodName)
        {
            if (MethodName == nameof(getDoDebug))
                return false;

            return doDebug;
        }

        public static ModInfo ThisMod => ModManager.GetMod(Const.MOD_ID);
    }
}