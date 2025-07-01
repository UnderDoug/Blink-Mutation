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
using System;

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

        public static List<string> EmeraldColors = new()
        {
            "Green",
            "Red",
            "Blue",
            "Yellow",
            "Cyan",
            "Pink",
            "Orange",
        };

        public static Dictionary<string, GameObject> ChaosEmeralds = new();

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

            List<string> emeraldColors = new(EmeraldColors);

            string tombTopEmerald = emeraldColors.GetRandomElement();
            emeraldColors.Remove(tombTopEmerald);
            string tombLocation = "JoppaWorld.53.3.0.1.0";

            string moonstairEmerald = emeraldColors.GetRandomElement();
            emeraldColors.Remove(moonstairEmerald);

            foreach ((string color, GameObject emeraldObject) in  chaosEmeralds)
            {
                Debug.LoopItem(4, $"{nameof(color)}", $"{color} {emeraldObject.DebugName}",
                    Indent: indent + 2, Toggle: doDebug);

                string emeraldName = $"{emeraldObject?.T(AsIfKnown: true)}";
                string emeraldSecret = $"${emeraldObject.Blueprint}".Replace(" ", "");

                bool isTombTopEmerald = color == tombTopEmerald;
                bool isMoonstairEmerald = color == moonstairEmerald;

                int zoneTier = !isMoonstairEmerald ? Stat.RollCached("2d4") : 8;

                int zMin = Math.Abs(50 - (zoneTier * 5));
                if (zMin < 11 && !1.in10())
                {
                    zMin = 11;
                }
                int zMax = Math.Abs(zMin + (23 - (zoneTier * 2)));

                Location2D parasang = Builder.getLocationOfTier(zoneTier);
                parasang = Location2D.Get(parasang.X * 3 + 1, parasang.Y * 3 + 1);

                int locationZ = Stat.Random(zMin, zMax);
                string zoneID = Zone.XYToID("JoppaWorld", parasang.X, parasang.Y, locationZ);
                if (isTombTopEmerald)
                {
                    zoneID = tombLocation;
                }
                Debug.LoopItem(4, $"{nameof(zoneID)}", $"{zoneID}",
                    Indent: indent + 3, Toggle: doDebug);

                The.ZoneManager.AdjustZoneGenerationTierTo(zoneID);
                The.ZoneManager.AddZonePostBuilder(zoneID, nameof(PlaceRelicBuilder), "Relic", The.ZoneManager.CacheObject(emeraldObject));
                string secretID = Builder.AddSecret(zoneID, emeraldName, new string[1] { "artifact" }, "Artifacts", emeraldSecret);

                Debug.LoopItem(4, $"{nameof(secretID)}", $"{secretID}",
                    Indent: indent + 3, Toggle: doDebug);
                
                The.Game.SetObjectGameState($"UD_{nameof(ChaosEmeralds)}:{nameof(GameObject)}:{color}", emeraldObject);
                The.Game.SetStringGameState($"UD_{nameof(ChaosEmeralds)}:{nameof(Zone)}:{color}", zoneID);

                if (isTombTopEmerald)
                {
                    SecretRevealer secretRevealer = emeraldObject.RequirePart<SecretRevealer>();
                    secretRevealer.id = secretID;
                    secretRevealer.text = $"the location of {emeraldObject.DisplayName}";
                    secretRevealer.message = $"You have discovered {secretRevealer.text}!";
                    secretRevealer.category = "Artifacts";
                    secretRevealer.adjectives = "artifact";
                }
                if (ChaosEmeraldLocations.ContainsKey(color))
                {
                    ChaosEmeraldLocations[color] = zoneID;
                }
            }

            ChaosEmeralds = chaosEmeralds;

            Debug.Entry(4,
                $"x {nameof(UD_ChaosEmeraldDispersalWorldBuilder)}."
                + $"{nameof(HideChaosEmeralds)}("
                + $"{nameof(WorldID)}: {WorldID})"
                + $" *//",
                Indent: indent + 1, Toggle: doDebug);
            Debug.LastIndent = indent;
        }

        [WishCommand(Command = "go chaos emerald")]
        public static void GoChaosEmerald(string color)
        {
            if (!color.IsNullOrEmpty())
            {
                string zoneID = The.Game.GetStringGameState($"UD_{nameof(ChaosEmeralds)}:{nameof(Zone)}:{color}");
                if (!zoneID.IsNullOrEmpty())
                {
                    Zone Z = The.ZoneManager.GetZone(zoneID);
                    The.Player.Physics.CurrentCell.RemoveObject(The.Player.Physics.ParentObject);
                    Cell landingCell = Z.GetFirstObjectWithPart(nameof(ChaosEmeraldSetPiece))?.CurrentCell;
                    if (landingCell != null)
                    {
                        landingCell = landingCell.GetAdjacentCells(5).GetRandomElementCosmetic();
                    }
                    landingCell ??= Z.GetEmptyCells().GetRandomElement();
                    if (landingCell != null)
                    {
                        landingCell.AddObject(The.Player);
                        The.ZoneManager.SetActiveZone(Z);
                        The.ZoneManager.ProcessGoToPartyLeader();
                    }
                    else
                    {
                        Popup.Show("Something went very wrong trying that! Check Player.log!", "Big Oops!");
                    }
                }
            }
        }

        [WishCommand(Command = "take chaos emeralds")]
        public static void TakeChaosEmeralds()
        {
            if (!EmeraldColors.IsNullOrEmpty())
            {
                foreach (string color in EmeraldColors)
                {
                    if (The.Game.GetObjectGameState($"UD_{nameof(ChaosEmeralds)}:{nameof(GameObject)}:{color}") is GameObject chaosEmerald)
                    {
                        chaosEmerald = The.ZoneManager.PullCachedObject(chaosEmerald.ID, false) ?? chaosEmerald;
                        if (chaosEmerald != null)
                        {
                            The.Player.ReceiveObject(chaosEmerald);
                            chaosEmerald.MakeUnderstood();
                            if (chaosEmerald.TryGetPart(out SecretRevealer secretRevealer))
                            {
                                chaosEmerald.RemovePart(secretRevealer);
                            }
                        }
                    }
                }
            }
        }
    }
}
