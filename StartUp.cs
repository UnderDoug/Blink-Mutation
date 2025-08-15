using System;
using System.Collections.Generic;

using XRL;
using XRL.UI;
using XRL.UI.ObjectFinderClassifiers;
using XRL.World;
using XRL.World.Parts.Mutation;

using UD_Blink_Mutation;

using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Options;
using static UD_Blink_Mutation.Utils;
using Debug = UD_Blink_Mutation.Debug;
using SerializeField = UnityEngine.SerializeField;

namespace UD_Blink_Mutation
{
    [PlayerMutator]
    public class PrepareColdSteelPreset : IPlayerMutator
    {
        public void mutate(GameObject player)
        {
            bool playerExists = player != null;

            bool playerIsColdSteel =
                playerExists
             && player.GetStartingPregen() != null
             && player.GetStartingPregen() == "Cold Steel";

            bool playerStartedWithBlink = 
                playerExists 
             && (player.GetStartingMutations().Contains("Blink") || player.GetStartingMutationClasses().Contains(nameof(UD_Blink)));

            bool playerStartedWithQuills =
                playerExists
             && (player.GetStartingMutations().Contains("Quills") 
                || player.GetStartingMutationClasses().Contains("UD_QuillsPlus")
                || player.GetStartingMutationClasses().Contains("Quills"));

            WorldCreationProgress.StepProgress("Quenching carbide...");

            Debug.Header(3, $"{nameof(PrepareColdSteelPreset)}", $"{nameof(mutate)}(GameObject player: {player.DebugName})");

            if (playerIsColdSteel && playerStartedWithBlink && playerStartedWithQuills)
            {
                Debug.Entry(3, $"COLD STEEL DETECTED, PERFORMING PREPARATIONS...", Indent: 1);

                bool didSetSpecies = SetColdSteelGenotypeSubtypeSpeciesPricklePigBadass(player);
                if (!didSetSpecies)
                {
                    Debug.Warn(4, 
                        nameof(PrepareColdSteelPreset), 
                        nameof(SetColdSteelGenotypeSubtypeSpeciesPricklePigBadass), 
                        $"Failed to set Coldsteel Genotype and/or Subtype", 
                        Indent: 0);
                }

                Debug.Entry(3, $"WARNING!! APPROACHING CRITICAL LEVELS OF BADASS...", Indent: 1);

                bool didPierceEars = GiveColdSteelGoldEarrings(player);
                if (!didSetSpecies)
                {
                    Debug.Warn(4,
                        nameof(PrepareColdSteelPreset),
                        nameof(GiveColdSteelGoldEarrings),
                        $"Failed to pierce Coldsteel's ears, must be built different",
                        Indent: 0);
                }

                Debug.Entry(3, $"ALERT!! CRITICAL BADASS ACHIEVED! DIVERTING CONTROL TO PLAYER!", Indent: 1);
            }
            else
            {
                Debug.Entry(3, $"NO COLD STEEL DETECTED, LAME. ABORTING...", Indent: 1);
            }

            Debug.Footer(3, $"{nameof(PrepareColdSteelPreset)}", $"{nameof(mutate)}(GameObject player: {player.DebugName})");
        }
        public static bool SetColdSteelGenotypeSubtypeSpeciesPricklePigBadass(GameObject player)
        {
            Debug.Entry(3, $"* {nameof(SetColdSteelGenotypeSubtypeSpeciesPricklePigBadass)}(GameObject player)", Indent: 1);
            Debug.Entry(3, $"SHAPING ULTIMATE LIFE FORM...", Indent: 2);
            Debug.Entry(3, $"TURNING TO DARKNESS...", Indent: 2);
            player.SetStringProperty("Genotype", "Prickle Pig");
            Debug.Entry(3, $"GENOTYPICAL ALTERATION PROCESSED...", Indent: 2);
            Debug.Entry(3, $"GROWING NAILS TO NINNE INCHES...", Indent: 2);
            player.SetStringProperty("Subtype", "Badass");
            Debug.Entry(3, $"SUBTYPICAL ADJUSTMENT MANIFESTED...", Indent: 2);
            Debug.Entry(3, $"INCREASING DISDAIN FOR SUNSHINE...", Indent: 2);
            player.SetStringProperty("Species", "prickle pig");
            Debug.Entry(3, $"SPECIES REALISGNMENT COMPLETED...", Indent: 2);
            Debug.Entry(3, $"CHECKING FOR JINCO JEANS...", Indent: 2);
            Debug.Entry(3, $"FOUND, CONTINUING PREPARATIONS...", Indent: 2);
            Debug.Entry(3, $"x {nameof(SetColdSteelGenotypeSubtypeSpeciesPricklePigBadass)}(GameObject player) *//", Indent: 1);
            return player.genotypeEntry?.Name == "Prickle Pig" && player.subtypeEntry?.Name == "Badass";
        }
        public static bool GiveColdSteelGoldEarrings(GameObject player)
        {
            Debug.Entry(3, $"* {nameof(GiveColdSteelGoldEarrings)}(GameObject player)", Indent: 1);
            Debug.Entry(3, $"GENERATING BADASS EARRINGS...", Indent: 2);
            GameObject badassEarrings = GameObjectFactory.Factory.CreateObject("Badass Earrings");
            Debug.Entry(3, $"BADASS EARRINGS CREATED, BESTOWING PLAYER WITH ADDITIONAL COOLNESS...", Indent: 2);
            player.ReceiveObject(badassEarrings);
            Debug.Entry(3, $"DEFINITELY NOT GURLY EARRINGS BESTOWED, ASSIGNING TO MUG...", Indent: 2);
            player.AutoEquip(badassEarrings, Silent: true);
            Debug.Entry(3, $"SICK-ASS BLING ALLOCATED, CONTINUING PREPARATIONS...", Indent: 2);
            Debug.Entry(3, $"x {nameof(GiveColdSteelGoldEarrings)}(GameObject player) *//", Indent: 1);
            return player.HasEquippedItem(badassEarrings.Blueprint);
        }
    }

    // Start-up calls in order that they happen.

    [HasModSensitiveStaticCache]
    public static class UD_Blink_Mutation_ModBasedInitialiser
    {
        [ModSensitiveCacheInit]
        public static void AdditionalSetup()
        {
            // Called at game startup and whenever mod configuration changes
        }
    } //!-- public static class UD_Blink_Mutation_ModBasedInitialiser

    [HasGameBasedStaticCache]
    public static class UD_Blink_Mutation_GameBasedInitialiser
    {
        [GameBasedCacheInit]
        public static void AdditionalSetup()
        {
            // Called once when world is first generated.

            // The.Game registered events should go here.
        }
    } //!-- public static class UD_Blink_Mutation_GameBasedInitialiser

    [PlayerMutator]
    public class UD_Blink_Mutation_OnPlayerLoad : IPlayerMutator
    {
        public void mutate(GameObject player)
        {
            // Gets called once when the player is first generated
        }
    } //!-- public class UD_Blink_Mutation_OnPlayerLoad : IPlayerMutator

    [HasCallAfterGameLoaded]
    public class UD_Blink_Mutation_OnLoadGameHandler
    {
        [CallAfterGameLoaded]
        public static void OnLoadGameCallback()
        {
            // Gets called every time the game is loaded but not during generation
        }
    } //!-- public class UD_Blink_Mutation_OnLoadGameHandler
}
