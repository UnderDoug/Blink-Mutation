using System;
using System.Collections.Generic;

using Genkit;

using XRL.Language;
using XRL.Rules;
using XRL.UI;
using XRL.Wish;
using XRL.World.Parts;
using XRL.World.ZoneBuilders;

using UD_Blink_Mutation;

using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Options;
using Debug = UD_Blink_Mutation.Debug;
using XRL.Collections;
using System.Linq;

namespace XRL.World.WorldBuilders
{
    [HasWishCommand]
    [JoppaWorldBuilderExtension]
    public class UD_ChaosEmeraldDispersalWorldBuilder : IJoppaWorldBuilderExtension
    {
        private static bool doDebug => getClassDoDebug(nameof(UD_ChaosEmeraldDispersalWorldBuilder));
        private static bool getDoDebug(object what = null)
        {
            List<object> doList = new()
            {
                'V',    // Vomit
                'X',    // Trace
            };
            List<object> dontList = new()
            {
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
        public static string ChaosEmeraldSuperBoss = "ChaosEmeraldSuperBoss";

        public override void OnAfterBuild(JoppaWorldBuilder Builder)
        {
            Debug.Header(4, $"{nameof(UD_ChaosEmeraldDispersalWorldBuilder)}", $"{nameof(OnAfterBuild)}", Toggle: getDoDebug());
            MetricsManager.rngCheckpoint("chaosemeralds");
            this.Builder = Builder;
            Builder.BuildStep("Hiding Chaos Emeralds", HideChaosEmeralds);
            Debug.Footer(4, $"{nameof(UD_ChaosEmeraldDispersalWorldBuilder)}", $"{nameof(OnAfterBuild)}", Toggle: getDoDebug());
        }

        public void HideChaosEmeralds(string WorldID)
        {
            int indent = Debug.LastIndent;
            Debug.Entry(4,
                $"* {nameof(UD_ChaosEmeraldDispersalWorldBuilder)}."
                + $"{nameof(HideChaosEmeralds)}("
                + $"{nameof(WorldID)}: {WorldID})",
                Indent: indent + 1, Toggle: getDoDebug());

            if (WorldID != "JoppaWorld")
            {
                Debug.LastIndent = indent;
                return;
            }

            if (!GenerateChaosEmeralds)
            {
                Debug.CheckNah(4, $"{nameof(GenerateChaosEmeralds)} is Disabled", Indent: indent + 2, Toggle: getDoDebug());

                Debug.Entry(4,
                    $"x {nameof(UD_ChaosEmeraldDispersalWorldBuilder)}."
                    + $"{nameof(HideChaosEmeralds)}("
                    + $"{nameof(WorldID)}: {WorldID})"
                    + $" *//",
                    Indent: indent + 1, Toggle: getDoDebug());
                Debug.LastIndent = indent;
                return;
            }
            Debug.CheckYeh(4, $"{nameof(GenerateChaosEmeralds)} is Enabled", Indent: indent + 2, Toggle: getDoDebug());

            WorldCreationProgress.StepProgress("Hiding Chaos Emeralds...");

            var chaosEmeralds = new Dictionary<string, GameObject>()
            {
                { "Green", GameObject.Create("UD Green Chaos Emerald") },
                { "Red", GameObject.Create("UD Red Chaos Emerald") },
                { "Blue", GameObject.Create("UD Blue Chaos Emerald") },
                { "Yellow", GameObject.Create("UD Yellow Chaos Emerald") },
                { "Cyan", GameObject.Create("UD Cyan Chaos Emerald") },
                { "Pink", GameObject.Create("UD Pink Chaos Emerald") },
                { "Orange", GameObject.Create("UD Orange Chaos Emerald") },
            };

            foreach ((var color, var chaosEmerald) in chaosEmeralds)
                The.Game.SetObjectGameState($"UD_BlinkMutation_{color}_ChaosEmerald", chaosEmerald);

            string tombLocation = "JoppaWorld.53.3.0.1.0";
            string tombTopEmerald = null;
            string moonstairEmerald = null;
            string pricklePigEmerald = null;
            using (var emeraldColors = ScopeDisposedList<string>.GetFromPoolFilledWith(EmeraldColors))
            {
                tombTopEmerald = emeraldColors.DrawRandomToken(ExceptForToken: "Orange");
                moonstairEmerald = emeraldColors.DrawRandomToken();
                pricklePigEmerald = emeraldColors.DrawRandomToken();
            }

            bool superBoss = 2.in1000();

            Debug.LoopItem(4, $"{nameof(superBoss)}", $"{superBoss}",
                Indent: indent + 2, Toggle: getDoDebug());

            The.Game.SetBooleanGameState($"ChaosEmeraldSuperBoss", superBoss);

            var weightedPricklePigs = new Dictionary<string, int>(4)
            {
                { "UD Prickle Pig", 2 },
                { "UD Burrowing Prickle Pig", 5 },
                { "UD Dark Prickle Pig", 5 },
                { "UD UD Metal Prickle Pig", 3 },
            };

            string blueprint = superBoss
                ? "UD Dark Prickle Pig"
                : weightedPricklePigs.GetRandomElement(Stat.Rnd)
                ;

            var pricklePig = GameObjectFactory.Factory.CreateObject(
                ObjectBlueprint: blueprint,
                BeforeObjectCreated: GO => HeroMaker.MakeHero(GO, superBoss ? $"UD_ChaosEmerald_SuperBoss {blueprint}" : $"UD_ChaosEmerald {blueprint}"));

            int zoneTier = 8;
            int zMin = 11;
            int zMax = 15;
            var parasang = Builder.getLocationOfTier(zoneTier);
            parasang = Location2D.Get(parasang.X * 3 + 1, parasang.Y * 3 + 1);
            int locationZ = Stat.Random(zMin, zMax);
            string zoneID = Zone.XYToID("JoppaWorld", parasang.X, parasang.Y, locationZ);
            string secretID = null;

            string secretAttributesString = "artifact,chaos emerald";
            var secretAttributes = secretAttributesString.Split(',');

            foreach ((var color, var emeraldObject) in chaosEmeralds)
            {
                Debug.LoopItem(4, $"{nameof(color)}", $"[{color}] ({emeraldObject.ID}) {emeraldObject.DebugName}",
                    Indent: indent + 2, Toggle: getDoDebug());

                string emeraldName = $"{emeraldObject?.T(AsIfKnown: true)}";
                string emeraldSecret = $"${emeraldObject.Blueprint}".Replace(" ", "");

                bool isTombTopEmerald = color == tombTopEmerald;
                bool isMoonstairEmerald = color == moonstairEmerald;
                bool isPricklePigEmerald = color == pricklePigEmerald && pricklePig != null;

                Debug.LoopItem(4, $"Special Flags", Indent: indent + 2, Toggle: getDoDebug());

                Debug.LoopItem(4, $"{nameof(isTombTopEmerald)}", $"{isTombTopEmerald}",
                    Good: isTombTopEmerald, Indent: indent + 4, Toggle: getDoDebug());

                Debug.LoopItem(4, $"{nameof(isMoonstairEmerald)}", $"{isMoonstairEmerald}",
                    Good: isMoonstairEmerald, Indent: indent + 4, Toggle: getDoDebug());

                Debug.LoopItem(4, $"{nameof(isPricklePigEmerald)}", $"{isPricklePigEmerald}",
                    Good: isPricklePigEmerald, Indent: indent + 4, Toggle: getDoDebug());

                if (!superBoss)
                {
                    zoneTier = !isMoonstairEmerald ? Stat.RollCached("2d4") : 8;

                    zMin = Math.Abs(50 - (zoneTier * 5));
                    if (zMin < 11
                        && !1.in10())
                        zMin = 11;

                    zMax = Math.Abs(zMin + (23 - (zoneTier * 2)));

                    parasang = Builder.getLocationOfTier(zoneTier);
                    parasang = Location2D.Get(parasang.X * 3 + 1, parasang.Y * 3 + 1);

                    locationZ = Stat.Random(zMin, zMax);
                    zoneID = Zone.XYToID("JoppaWorld", parasang.X, parasang.Y, locationZ);

                    if (isTombTopEmerald)
                        zoneID = tombLocation;

                    Debug.LoopItem(4, $"{nameof(zoneID)}", $"{zoneID}",
                        Indent: indent + 3, Toggle: getDoDebug());

                    The.ZoneManager.AdjustZoneGenerationTierTo(zoneID);

                    if (isPricklePigEmerald)
                    {
                        secretID = Builder.AddSecret(zoneID, $"the prickle pig carrying {emeraldName}", secretAttributes, "Artifacts", emeraldSecret);

                        var secretRevealer = pricklePig.RequirePart<SecretRevealer>();
                        secretRevealer.id = secretID;
                        secretRevealer.text = $"the location of the prickle pig carrying {emeraldName}";
                        secretRevealer.message = $"You have discovered {secretRevealer.text}!";
                        secretRevealer.category = "Artifacts";
                        secretRevealer.adjectives = "artifact";

                        pricklePig?.ReceiveObject(emeraldObject);
                        The.ZoneManager.AddZonePostBuilder(zoneID, nameof(AddObjectWithUniqueItemBuilder), "Object", The.ZoneManager.CacheObject(pricklePig));
                    }
                    else
                    {
                        The.ZoneManager.AddZonePostBuilder(zoneID, nameof(PlaceRelicBuilder), "Relic", The.ZoneManager.CacheObject(emeraldObject));

                        secretID = Builder.AddSecret(zoneID, emeraldName, secretAttributes, "Artifacts", emeraldSecret);

                        if (isTombTopEmerald)
                        {
                            var secretRevealer = emeraldObject.RequirePart<SecretRevealer>();
                            secretRevealer.id = secretID;
                            secretRevealer.text = $"the location of {emeraldName}";
                            secretRevealer.message = $"You have discovered {secretRevealer.text}!";
                            secretRevealer.category = "Artifacts";
                            secretRevealer.adjectives = secretAttributesString;
                        }
                    }
                }
                else
                {
                    emeraldObject.SetIntProperty($"AlwaysEquipAsArmor", 1);
                    pricklePig?.ReceiveObject(emeraldObject);
                    pricklePig?.Body.GetFirstPart(p => p.Type == "Floating Nearby" && p.Equipped == null).Equip(emeraldObject);
                }

                The.Game.SetIntGameState($"UD_{nameof(ChaosEmeralds)}:{nameof(GameObject.ID)}:{color}", int.Parse(emeraldObject.ID));
                The.Game.SetStringGameState($"UD_{nameof(ChaosEmeralds)}:{nameof(Zone)}:{color}", zoneID);

                Debug.LoopItem(4, $"{nameof(secretID)}", $"{secretID}",
                    Indent: indent + 3, Toggle: getDoDebug());

                Debug.LoopItem(4, $"{nameof(emeraldObject)}.{nameof(emeraldObject.ID)}", $"{int.Parse(emeraldObject.ID)}",
                    Indent: indent + 3, Toggle: getDoDebug());

                if (ChaosEmeraldLocations.ContainsKey(color))
                    ChaosEmeraldLocations[color] = zoneID;
            }
            if (superBoss)
            {
                secretID = Builder.AddSecret(zoneID, $"the prickle pig carrying all 7 of The Chaos Emeralds", secretAttributes, "Artifacts", $"${ChaosEmeraldSuperBoss}");

                var secretRevealer = pricklePig.RequirePart<SecretRevealer>();
                secretRevealer.id = secretID;
                secretRevealer.text = $"the location of the prickle pig carrying all 7 of The Chaos Emeralds";
                secretRevealer.message = $"You have discovered {secretRevealer.text}!";
                secretRevealer.category = "Artifacts";
                secretRevealer.adjectives = secretAttributesString;

                The.Game.SetIntGameState($"UD_{nameof(ChaosEmeralds)}:{nameof(GameObject.ID)}:{ChaosEmeraldSuperBoss}", int.Parse(pricklePig.ID));
                The.Game.SetStringGameState($"UD_{nameof(ChaosEmeralds)}:{nameof(Zone)}:{ChaosEmeraldSuperBoss}", zoneID);

                The.ZoneManager.AddZonePostBuilder(zoneID, nameof(AddObjectWithUniqueItemBuilder), "Object", The.ZoneManager.CacheObject(pricklePig));
            }

            ChaosEmeralds = chaosEmeralds;

            Debug.Entry(4,
                $"x {nameof(UD_ChaosEmeraldDispersalWorldBuilder)}."
                + $"{nameof(HideChaosEmeralds)}("
                + $"{nameof(WorldID)}: {WorldID})"
                + $" *//",
                Indent: indent + 1, Toggle: getDoDebug());
            Debug.LastIndent = indent;
        }

        [WishCommand(Command = "go chaos emerald")]
        public static void GoChaosEmerald(string color)
        {
            if (!color.IsNullOrEmpty())
            {
                color = Grammar.MakeTitleCase(color);
                string zoneID = The.Game.GetStringGameState($"UD_{nameof(ChaosEmeralds)}:{nameof(Zone)}:{color}");
                if (!zoneID.IsNullOrEmpty())
                {
                    var Z = The.ZoneManager.GetZone(zoneID);
                    var landingCell = Z.GetFirstObjectWithPart(nameof(ChaosEmeraldSetPiece))?.CurrentCell;
                    if (landingCell != null)
                    {
                        var nearbyCells = Event.NewCellList(landingCell.GetAdjacentCells(5));
                        nearbyCells.RemoveAll(c => c.IsSolidFor(The.Player));
                        if (!nearbyCells.IsNullOrEmpty())
                            landingCell = nearbyCells.GetRandomElementCosmetic();
                    }
                    landingCell ??= Z.GetEmptyCells().GetRandomElement();
                    if (landingCell != null)
                    {
                        The.Player.CurrentCell.RemoveObject(The.Player);
                        landingCell.AddObject(The.Player);
                        The.ZoneManager.SetActiveZone(Z);
                        The.ZoneManager.ProcessGoToPartyLeader();
                    }
                    else
                        Popup.Show("Something went very wrong trying that! Check Player.log!", "Big Oops!");
                }
                else
                {
                    string validColors = "";
                    foreach (string validColor in EmeraldColors)
                    {
                        if (!validColors.IsNullOrEmpty())
                            validColors += ", ";

                        validColors += validColor;
                    }
                    Popup.Show($"{color} doesn't seem to match any Emerald locations! Valid colors are (case-insensitive): {validColors}", "Uh-Oh!");
                }
            }
        }

        [WishCommand(Command = "take chaos emeralds")]
        public static void TakeChaosEmeralds()
        {
            if (!EmeraldColors.IsNullOrEmpty())
            {
                using var takenEmeralds = ScopeDisposedList<string>.GetFromPool();
                foreach (string color in EmeraldColors)
                {
                    if (!TakeChaosEmerald(color))
                        break;
                    else
                        takenEmeralds.Add(color);
                }

                if (takenEmeralds.IsNullOrEmpty())
                    takenEmeralds.Add("None!?");

                Popup.Show(takenEmeralds
                    .Select(s
                        => (The.Game?.GetObjectGameState($"UD_BlinkMutation_{s}_ChaosEmerald") as GameObject)?.ShortDisplayName
                        ?? GameObjectFactory.Factory?.GetBlueprintIfExists($"UD {s} Chaos Emerald")?.DisplayName()
                        ?? s
                    )
                    .Aggregate(
                        seed: $"=subject.T= had the following Chaos Emeralds materialize in =subject.possessive= inventory:".StartReplace().AddObject(The.Player).ToString(),
                        func: (a, n) => a + (!a.IsNullOrEmpty() ? "\n" : null) + $"\xff\xff:\xff{n}"),
                        Title: $"Emeralds Acquired!");
            }
            else
                Popup.Show($"Something went very wrong trying that! {nameof(EmeraldColors)} is {NULL} for some reason...", "Big Oops!");
        }

        [WishCommand(Command = "take chaos emerald")]
        public static bool TakeChaosEmerald(string color)
        {
            if (!color.IsNullOrEmpty())
            {
                if (!The.Game.GetBooleanGameState($"ChaosEmeraldSuperBoss", false))
                {
                    color = Grammar.MakeTitleCase(color);
                    int chaosEmeraldID = The.Game.GetIntGameState($"UD_{nameof(ChaosEmeralds)}:{nameof(GameObject.ID)}:{color}");
                    string zoneID = The.Game.GetStringGameState($"UD_{nameof(ChaosEmeralds)}:{nameof(Zone)}:{color}");
                    if (chaosEmeraldID != 0
                        && !zoneID.IsNullOrEmpty())
                    {
                        var emeraldZone = The.ZoneManager.GetZone(zoneID);

                        var chaosEmeraldObject = emeraldZone.GetFirstObject(GO => GO.ID == chaosEmeraldID.ToString())
                            ?? emeraldZone.FindObjectByID(chaosEmeraldID);

                        if (chaosEmeraldObject != null)
                        {
                            if (chaosEmeraldObject.Holder == null
                                || chaosEmeraldObject.Holder != The.Player)
                            {
                                The.Player.ReceiveObject(chaosEmeraldObject);
                                chaosEmeraldObject.MakeUnderstood();
                                if (chaosEmeraldObject.TryGetPart(out SecretRevealer secretRevealer))
                                    chaosEmeraldObject.RemovePart(secretRevealer);
                            }
                            else
                                Popup.Show($"You are already holding {chaosEmeraldObject.T(AsIfKnown: true)}!");
                        }
                        else
                        {
                            if (The.Player.Inventory == null
                                || !The.Player.Inventory.HasObject(GO => GO.ID == chaosEmeraldID.ToString()))
                                Popup.Show($"Something went very wrong with {color}, {nameof(chaosEmeraldObject)} is {NULL}!");
                        }
                    }
                    else
                        Popup.Show($"Something went very wrong with {color}, {nameof(chaosEmeraldID)} is {chaosEmeraldID}!");
                }
                else
                {
                    int pricklePigID = The.Game.GetIntGameState($"UD_{nameof(ChaosEmeralds)}:{nameof(GameObject.ID)}:{ChaosEmeraldSuperBoss}");
                    string zoneID = The.Game.GetStringGameState($"UD_{nameof(ChaosEmeralds)}:{nameof(Zone)}:{ChaosEmeraldSuperBoss}");

                    if (pricklePigID != 0 && !zoneID.IsNullOrEmpty())
                    {
                        var pricklePigZone = The.ZoneManager.GetZone(zoneID);

                        var pricklePigObject = pricklePigZone.GetFirstObject(GO => GO.ID == pricklePigID.ToString())
                            ?? pricklePigZone.FindObjectByID(pricklePigID);

                        if (pricklePigObject != null)
                        {
                            var Z = The.Player.CurrentZone;
                            var landingCell = The.Player.CurrentCell;
                            if (landingCell != null)
                            {
                                var nearbyCells = Event.NewCellList(landingCell.GetAdjacentCells(5));
                                nearbyCells.RemoveAll(c => c.IsSolidFor(pricklePigObject));
                                if (!nearbyCells.IsNullOrEmpty())
                                    landingCell = nearbyCells.GetRandomElementCosmetic();
                            }
                            landingCell ??= Z.GetEmptyCells().GetRandomElement();
                            if (landingCell != null)
                            {
                                if (pricklePigObject.CurrentZone != The.Player.CurrentZone)
                                {
                                    Popup.Show($"/!\\ Warning!");
                                    Popup.Show($"A new challenger is approaching!");
                                }
                                // pricklePigObject.Physics.CurrentCell.RemoveObject(pricklePigObject.Physics.ParentObject);
                                // landingCell.AddObject(pricklePigObject);
                                var landingGlobalLocation = new GlobalLocation(Z.ZoneWorld, Z.wX, Z.wY, Z.X, Z.Y, Z.Z, landingCell.X, landingCell.Y);
                                pricklePigObject.DirectMoveTo(landingGlobalLocation, Forced: true, IgnoreCombat: true);
                                return false;
                            }
                            else
                                Popup.Show("Something went very wrong trying that! Check Player.log!", "Big Oops!");
                        }
                        else
                        {
                            Popup.Show($"Something went very wrong with {nameof(pricklePigObject)}, it's {NULL}!");
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
}
