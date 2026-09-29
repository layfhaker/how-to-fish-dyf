using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DepYourFriend.Patches
{
    [HarmonyPatch(typeof(DeadPlayer))]
    public static class DeadPlayerPatch
    {
        [HarmonyPatch("Hover")]
        [HarmonyPostfix]
        public static void HoverPostfix(DeadPlayer __instance)
        {
            if (__instance == null) return;

            int bail = FriendBailManager.GetBail(__instance);
            string status = FriendBailManager.GetBailStatusString(bail);

            string customText = $"{__instance.GetName()}\n<color=#FFD700>{status}</color>\n<size=80%><color=#00FFAA>[Можно положить на рулетку!]</color></size>\n{SpriteManager.GetPickUpInput()}";
            PlayerUI.UpdateLookAtText(customText);
        }

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

    [HarmonyPatch(typeof(Server))]
    public static class ServerResurrectPatch
    {
        [HarmonyPatch("RpcLogic___ResurrectPlayer___2247010027")]
        [HarmonyPrefix]
        public static void RpcLogicResurrectPrefix(Player P_0, DeadPlayer P_1)
        {
            if (P_0 != null && P_1 != null && P_0.Vitals.Health <= 0)
            {
                int bail = FriendBailManager.GetBail(P_1);
                Player resurrectingPlayer = P_1.LastHolder != null ? P_1.LastHolder : P_0;

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
                FriendBailManager.RemoveBail(P_1);
            }
        }
    }
}
