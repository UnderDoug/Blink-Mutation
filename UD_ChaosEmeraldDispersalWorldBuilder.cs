using Genkit;
using System.Collections.Generic;

using XRL;
using XRL.Rules;
using XRL.UI;
using XRL.Wish;
using XRL.World;
using XRL.World.Parts;
using XRL.World.ZoneBuilders;

using UD_Blink_Mutation;

using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Options;
using static UD_Blink_Mutation.Utils;
using XRL.Language;

namespace XRL.World.WorldBuilders
{
    [HasWishCommand]
    [JoppaWorldBuilderExtension]
    public class UD_ChaosEmeraldDispersalWorldBuilder : IJoppaWorldBuilderExtension
    {
        private static bool doDebug => true;
        private static bool getDoDebug(object what = null)
        {
            List<object> doList = new()
            {
                'V',    // Vomit
            };
            List<object> dontList = new()
            {
                'X',    // Trace
            };

            if (what != null && doList.Contains(what))
                return true;

            if (what != null && dontList.Contains(what))
                return false;

            return doDebug;
        }

        public JoppaWorldBuilder Builder;

        public static Dictionary<string, string> ChaosEmeraldLocations = new()
        {
            { "Green", null },
            { "Red", null },
            { "Blue", null },
            { "Yellow", null },
            { "Cyan", null },
            { "Pink", null },
            { "Orange", null },
        };

        public override void OnAfterBuild(JoppaWorldBuilder Builder)
        {
            Debug.Header(4, $"{nameof(UD_ChaosEmeraldDispersalWorldBuilder)}", $"{nameof(OnAfterBuild)}", Toggle: doDebug);
            MetricsManager.rngCheckpoint("chaosemeralds");
            this.Builder = Builder;
            Builder.BuildStep("Hiding Chaos Emeralds", HideChaosEmeralds);
            Debug.Footer(4, $"{nameof(UD_ChaosEmeraldDispersalWorldBuilder)}", $"{nameof(OnAfterBuild)}", Toggle: doDebug);
        }

        public void HideChaosEmeralds(string WorldID)
        {
            int indent = Debug.LastIndent;
            Debug.Entry(4,
                $"* {nameof(UD_ChaosEmeraldDispersalWorldBuilder)}."
                + $"{nameof(HideChaosEmeralds)}("
                + $"{nameof(WorldID)}: {WorldID})",
                Indent: indent + 1, Toggle: doDebug);

            if (WorldID != "JoppaWorld")
            {
                Debug.LastIndent = indent;
                return;
            }
            WorldCreationProgress.StepProgress("Hiding Chaos Emeralds...");

            Dictionary<string, GameObject> chaosEmeralds = new()
            {
                { "Green", GameObject.Create("Green Chaos Emerald") },
                { "Red", GameObject.Create("Red Chaos Emerald") },
                { "Blue", GameObject.Create("Blue Chaos Emerald") },
                { "Yellow", GameObject.Create("Yellow Chaos Emerald") },
                { "Cyan", GameObject.Create("Cyan Chaos Emerald") },
                { "Pink", GameObject.Create("Pink Chaos Emerald") },
                { "Orange", GameObject.Create("Orange Chaos Emerald") },
            };

            Location2D parasang;
            string zoneID;
            string emeraldName;
            string emeraldSecret;
            foreach ((string color, GameObject emeraldObject) in  chaosEmeralds)
            {
                Debug.LoopItem(4, $"{nameof(color)}", $"{color} {emeraldObject.DebugName}",
                    Indent: indent + 2, Toggle: doDebug);

                emeraldName = $"the {color} {emeraldObject?.ShortDisplayNameStripped}";
                emeraldSecret = $"${emeraldObject.Blueprint}".Replace(" ", "");
                parasang = Builder.getLocationOfTier(2, 8);
                parasang = Location2D.Get(parasang.X * 3 + 1, parasang.Y * 3 + 1);
                zoneID = Zone.XYToID("JoppaWorld", parasang.X, parasang.Y, Stat.Random(60, 99));

                Debug.LoopItem(4, $"{nameof(zoneID)}", $"{zoneID}",
                    Indent: indent + 3, Toggle: doDebug);

                The.ZoneManager.AdjustZoneGenerationTierTo(zoneID);
                The.ZoneManager.AddZonePostBuilder(zoneID, nameof(PlaceRelicBuilder), "Relic", The.ZoneManager.CacheObject(emeraldObject));
                string secretID = Builder.AddSecret(zoneID, emeraldName, new string[1] { "artifact" }, "Artifacts", emeraldSecret);

                Debug.LoopItem(4, $"{nameof(secretID)}", $"{secretID}",
                    Indent: indent + 3, Toggle: doDebug);

                if (ChaosEmeraldLocations.ContainsKey(color))
                {
                    ChaosEmeraldLocations[color] = zoneID;
                }
            }
            
            Debug.Entry(4,
                $"x {nameof(UD_ChaosEmeraldDispersalWorldBuilder)}."
                + $"{nameof(HideChaosEmeralds)}("
                + $"{nameof(WorldID)}: {WorldID})"
                + $" *//",
                Indent: indent + 1, Toggle: doDebug);
            Debug.LastIndent = indent;
        }

        [WishCommand(Command = "go chaos emerald")]
        public static void GoChaosEmerald(string Color)
        {
            if (!Color.IsNullOrEmpty() && ChaosEmeraldLocations.ContainsKey(Color))
            {
                Zone Z = The.ZoneManager.GetZone(ChaosEmeraldLocations[Grammar.MakeTitleCase(Color)]);
                The.Player.Physics.CurrentCell.RemoveObject(The.Player.Physics.ParentObject);
                Z.GetEmptyCells().GetRandomElement().AddObject(The.Player);
                The.ZoneManager.SetActiveZone(Z);
                The.ZoneManager.ProcessGoToPartyLeader();
            }
        }
    }
}
