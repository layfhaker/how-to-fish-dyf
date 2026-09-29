using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DepYourFriend.Patches
{
    /// <summary>
    /// Vanilla DeadPlayer.Update() destroys a corpse on the server whenever its bound player
    /// is ALIVE (Health &gt; 0). Real corpses bind to a dead player, so that's fine. The F7 debug
    /// corpse is bound to an ALIVE local player, so vanilla logic would instantly destroy it.
    /// This patch preserves the base Item physics + resurrection lifecycle for ONLY the active
    /// debug corpse, while skipping the stale-corpse destroy. Normal corpses are untouched.
    /// </summary>
    [HarmonyPatch(typeof(DeadPlayer))]
    public static class DebugCorpsePatch
    {
        private static readonly MethodInfo _itemUpdate =
            AccessTools.Method(typeof(Item), "Update");
        private static readonly MethodInfo _updateResurrection =
            AccessTools.Method(typeof(DeadPlayer), "UpdateResurrection");

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static bool UpdatePrefix(DeadPlayer __instance)
        {
            if (SoloDebugManager.ActiveDebugCorpse != __instance)
            {
                // Normal dead player — run vanilla logic unchanged.
                return true;
            }

            // Debug corpse bound to an alive player: re-run base.Item.Update + UpdateResurrection
            // (the parts the original would otherwise do) but skip the auto-destroy branch.
            if (_itemUpdate == null || _updateResurrection == null)
            {
                // Reflection setup failed — fall back to vanilla (corpse will self-destruct).
                return true;
            }

            try
            {
                _itemUpdate.Invoke(__instance, null);
                _updateResurrection.Invoke(__instance, null);
            }
            catch
            {
                // Debug-only; swallow reflection edge cases.
            }

            return false; // skip the vanilla Destroy-on-alive-player branch
        }
    }

    /// <summary>
    /// Hover() is declared on the Item base class (not overridden by DeadPlayer), so Harmony's
    /// DeclaredMethod lookup on DeadPlayer fails to attach a postfix there. Patch Item.Hover
    /// directly and only rewrite the look-at text when the hovered item is a DeadPlayer corpse.
    /// </summary>
    [HarmonyPatch(typeof(Item))]
    public static class DeadPlayerHoverPatch
    {
        [HarmonyPatch("Hover")]
        [HarmonyPostfix]
        public static void HoverPostfix(Item __instance)
        {
            if (__instance is not DeadPlayer dp) return;

            int bail = FriendBailManager.GetBail(dp);
            string status = FriendBailManager.GetBailStatusString(bail);

            string customText = $"{dp.GetName()}\n<color=#FFD700>{status}</color>\n<size=80%><color=#00FFAA>[Можно положить на рулетку!]</color></size>\n{SpriteManager.GetPickUpInput()}";
            PlayerUI.UpdateLookAtText(customText);
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
}
