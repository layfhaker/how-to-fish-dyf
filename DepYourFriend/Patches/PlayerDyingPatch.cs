using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DepYourFriend.Patches
{
    [HarmonyPatch(typeof(PlayerDying))]
    public static class PlayerDyingPatch
    {
        public static bool SoloTestMode = false;

        public static bool ShouldBlockSelfRespawn()
        {
            if (SoloTestMode)
            {
                return BossManager.Boss == null;
            }

            // Mod rule: active only for 2+ players, outside boss fight, and while at least one teammate is alive
            return PlayerManager.Players.Count >= 2 &&
                   BossManager.Boss == null &&
                   PlayerManager.AlivePlayers.Count > 0;
        }

        [HarmonyPatch("MouseClick")]
        [HarmonyPrefix]
        public static bool MouseClickPrefix(PlayerDying __instance)
        {
            if (ShouldBlockSelfRespawn() && __instance.IsDead)
            {
                // Find current bail for local player
                int bail = FriendBailManager.BaseValue;
                if (__instance.DeadPlayer != null)
                {
                    bail = FriendBailManager.GetBail(__instance.DeadPlayer);
                }

                // Show message to dead player that they must be ransomed
                string msg = $"<color=#FF5555>САМОВОСКРЕШЕНИЕ ЗАБЛОКИРОВАНО!</color>\n{FriendBailManager.GetBailStatusString(bail)}";
                PlayerUI.SetLookAtText(msg);
                return false; // Prevent giving up
            }
            return true;
        }

        [HarmonyPatch("LocalRespawn")]
        [HarmonyPrefix]
        public static bool LocalRespawnPrefix()
        {
            if (ShouldBlockSelfRespawn())
            {
                return false; // Block respawn
            }
            return true;
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static void UpdatePrefix(PlayerDying __instance, ref bool ____isGivingUp, ref float ____curGiveUpTime)
        {
            if (ShouldBlockSelfRespawn() && __instance.IsDead)
            {
                if (____isGivingUp)
                {
                    ____isGivingUp = false;
                    ____curGiveUpTime = 0f;
                }
            }
        }
    }
}
