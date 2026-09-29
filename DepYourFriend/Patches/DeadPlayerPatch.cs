using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DepYourFriend.Patches
{
    /// <summary>
    /// Hover() is declared on the Item base class (not overridden by DeadPlayer), so Harmony's
    /// DeclaredMethod lookup on DeadPlayer fails to attach a postfix there. Patch Item.Hover/UnHover
    /// directly. For corpses we DON'T spawn the world-anchored 3D look-at text (it flies into the
    /// camera) — instead we flag the hovered corpse and FriendBailHUD draws a fixed 2D overlay.
    /// </summary>
    [HarmonyPatch(typeof(Item))]
    public static class DeadPlayerHoverPatch
    {
        [HarmonyPatch("Hover")]
        [HarmonyPostfix]
        public static void HoverPostfix(Item __instance)
        {
            if (__instance is not DeadPlayer dp) return;

            FriendBailHUD.HoveredCorpse = dp;
            PlayerUI.HideLookAtText();
        }

        [HarmonyPatch("UnHover")]
        [HarmonyPostfix]
        public static void UnHoverPostfix(Item __instance)
        {
            if (__instance is DeadPlayer && FriendBailHUD.HoveredCorpse == __instance)
            {
                FriendBailHUD.HoveredCorpse = null;
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

            // Negative value = debt (you pay |bail| to revive); positive value = profit (revive pays you).
            int bail = FriendBailManager.GetBail(__instance);
            if (bail < 0 && !MoneyManager.CanAfford(Mathf.Abs(bail)))
            {
                // Cannot afford to pay the debt
                string error = $"<color=#FF4444>НЕ ХВАТАЕТ ДЕНЕГ НА ВЫКУП!</color>\nТребуется: ${Mathf.Abs(bail)} (В банке: ${MoneyManager.Money})";
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

            if (bail < 0)
            {
                // Debt: the bank pays |bail| to revive the friend.
                MoneyManager.RemoveMoney(Mathf.Abs(bail), resurrectingPlayer);
            }
            else if (bail > 0)
            {
                // Profit: the revive pays out.
                MoneyManager.AddMoney(bail, resurrectingPlayer);
            }

            // Clear record
            FriendBailManager.RemoveBail(corpse);
        }
    }
}