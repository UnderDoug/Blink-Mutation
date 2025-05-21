using System;
using System.Collections.Generic;

using XRL;
using XRL.World;
using XRL.World.Parts.Mutation;

using static UD_Blink_Mutation.Utils;
using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Options;

namespace UD_Blink_Mutation
{
    [PlayerMutator]
    public class GiveColdSteelPresetBadassEarrings : IPlayerMutator
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
             && player.GetStartingMutationClasses().Contains(nameof(UD_Blink));

            Debug.Header(3, $"{nameof(GiveColdSteelPresetBadassEarrings)}", $"{nameof(mutate)}(GameObject player: {player.DebugName})");
            if (playerIsColdSteel && playerStartedWithBlink)
            {
                Debug.Entry(3, "COLD STEEL DETECTED, GENERATING BADASS EARRINGS...", Indent: 1);
                GameObject badassEarrings = GameObjectFactory.Factory.CreateObject("Badass Earrings");
                Debug.Entry(3, "BADASS EARRINGS CREATED, BESTOWING PLAYER WITH ADDITIONAL COOLNESS...", Indent: 1);
                player.ReceiveObject(badassEarrings);
                Debug.Entry(3, "WARNING!! APPROACHING CRITICAL LEVELS OF BADASS...", Indent: 1);
                player.AutoEquip(badassEarrings, Silent: true);
                Debug.Entry(3, "ALERT!! CRITICAL BADASS ACHIEVED! DIVERTING CONTROL TO PLAYER!", Indent: 1);
            }
            else
            {
                Debug.Entry(3, "NO COLD STEEL DETECTED, ABORTING...", Indent: 1);
            }
            Debug.Footer(3, $"{nameof(GiveColdSteelPresetBadassEarrings)}", $"{nameof(mutate)}(GameObject player: {player.DebugName})");
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
