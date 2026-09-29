using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DepYourFriend.Patches
{
    /// <summary>
    /// Vanilla DeadPlayer.Update() destroys a corpse on the server whenever its bound player is ALIVE
    /// (Health &gt; 0). Real corpses bind to a dead player, so that's fine. The F7 debug corpse is bound
    /// to an ALIVE local player, so the vanilla stale-corpse guard tries to DestroyItem() it every
    /// server frame. Block DestroyItem() of the active debug corpse only: the rest of the corpse
    /// lifecycle (Update, UpdateResurrection, held-follow) keeps running normally, so slaps/revive
    /// still work. Real corpses pass through untouched.
    /// </summary>
    [HarmonyPatch(typeof(Item))]
    public static class DebugCorpsePatch
    {
        [HarmonyPatch("DestroyItem")]
        [HarmonyPrefix]
        public static bool DestroyItemPrefix(Item __instance)
        {
            return SoloDebugManager.ActiveDebugCorpse != __instance;
        }
    }

    /// <summary>
    /// Hover() is declared on the Item base class (not overridden by DeadPlayer), so Harmony's
    /// DeclaredMethod lookup on DeadPlayer fails to attach a postfix there. Patch Item.Hover/UnHover
    /// directly. For corpses we DON'T spawn the world-anchored 3D look-at text (it flies into the
    /// camera) — instead we flag the hovered corpse and SoloDebugManager draws a fixed 2D overlay.
    /// </summary>
    [HarmonyPatch(typeof(Item))]
    public static class DeadPlayerHoverPatch
    {
        [HarmonyPatch("Hover")]
        [HarmonyPostfix]
        public static void HoverPostfix(Item __instance)
        {
            if (__instance is not DeadPlayer dp) return;

            SoloDebugManager.HoveredCorpse = dp;
            PlayerUI.HideLookAtText();
        }

        [HarmonyPatch("UnHover")]
        [HarmonyPostfix]
        public static void UnHoverPostfix(Item __instance)
        {
            if (__instance is DeadPlayer && SoloDebugManager.HoveredCorpse == __instance)
            {
                SoloDebugManager.HoveredCorpse = null;
            }
        }
    }

    [HarmonyPatch(typeof(DeadPlayer))]
    public static class DeadPlayerPatch
    {
        [HarmonyPatch("PrimaryInput")]
        [HarmonyPrefix]
        public static bool PrimaryInputPrefix(DeadPlayer __instance, Player ____holder)
        {
            if (__instance == null || ____holder == null) return true;

            int bail = FriendBailManager.GetBail(__instance);
            if (bail > 0 && !MoneyManager.CanAfford(bail))
            {
                // Cannot afford bail
                string error = $"<color=#FF4444>НЕ ХВАТАЕТ ДЕНЕГ НА ВЫКУП!</color>\nТребуется: ${bail} (В банке: ${MoneyManager.Money})";
                PlayerUI.SetLookAtText(error, __instance.transform);
                return false; // Prevent resurrection slap
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerVitals))]
    public static class ServerResurrectPatch
    {
        /// <summary>
        /// OnResurrect() is the ONLY server-side execution point for a corpse-based resurrection
        /// (invoked from Server.RpcLogic___ResurrectPlayer___... only when the corpse is valid and the
        /// bound player is dead). It takes no parameters, so we can hook it reliably even though the
        /// FishNet-generated RPC methods have stripped parameter names (which broke a name-based prefix).
        /// At this point the corpse still exists (destroyed right after), so we settle the bail.
        /// </summary>
        [HarmonyPatch("OnResurrect")]
        [HarmonyPostfix]
        public static void OnResurrectPostfix(PlayerVitals __instance, Player ____player)
        {
            if (__instance == null || ____player == null || ____player.Dying == null) return;
            if (!__instance.IsServerInitialized) return;

            DeadPlayer corpse = ____player.Dying.DeadPlayer;
            if (corpse == null) return;

            int bail = FriendBailManager.GetBail(corpse);
            Player resurrectingPlayer = corpse.LastHolder != null ? corpse.LastHolder : ____player;
            if (resurrectingPlayer == null) return;

            if (bail > 0)
            {
                // Deduct bail
                MoneyManager.RemoveMoney(bail, resurrectingPlayer);
            }
            else if (bail < 0)
            {
                // Casino payout reward!
                int reward = Mathf.Abs(bail);
                MoneyManager.AddMoney(reward, resurrectingPlayer);
            }

            // Clear record
            FriendBailManager.RemoveBail(corpse);
        }
    }

    /// <summary>
    /// Resurrection of the F7 DEBUG friend. Vanilla Server.ResurrectPlayer(player, deadPlayer) refuses
    /// to revive a player whose Health &gt; 0 — and the debug corpse is bound to the still-ALIVE local
    /// player, so the vanilla RPC would no-op. For exactly the active debug corpse, settle the bail and
    /// despawn the corpse (the "friend comes back") ourselves, skipping the vanilla ServerRpc.
    /// Real corpses fall through to vanilla.
    /// </summary>
    [HarmonyPatch(typeof(Server))]
    public static class DebugResurrectPatch
    {
        [HarmonyPatch("ResurrectPlayer")]
        [HarmonyPrefix]
        public static bool ResurrectPlayerPrefix(Player player, DeadPlayer deadPlayer)
        {
            if (player == null || deadPlayer == null) return true;
            if (SoloDebugManager.ActiveDebugCorpse != deadPlayer) return true;

            Player resurrecting = deadPlayer.LastHolder != null ? deadPlayer.LastHolder : player;

            if (Server.Instance != null && Server.Instance.IsServerInitialized)
            {
                int bail = FriendBailManager.GetBail(deadPlayer);
                if (bail > 0)
                {
                    MoneyManager.RemoveMoney(bail, resurrecting);
                }
                else if (bail < 0)
                {
                    MoneyManager.AddMoney(Mathf.Abs(bail), resurrecting);
                }
                FriendBailManager.RemoveBail(deadPlayer);
            }

            // Clear the debug marker so the despawn below isn't blocked by DebugCorpsePatch.
            SoloDebugManager.ActiveDebugCorpse = null;
            if (Server.Instance != null && Server.Instance.IsServerInitialized && deadPlayer.gameObject != null)
            {
                deadPlayer.DestroyItem(0);
            }

            PlayerUI.SetLookAtText("<color=#55FF55>[DEBUG]</color> ДРУГ ОЖИЛ! Похлопал по лицу :)");
            return false; // skip the vanilla ServerRpc (it would refuse to revive an alive-bound corpse)
        }
    }
}
