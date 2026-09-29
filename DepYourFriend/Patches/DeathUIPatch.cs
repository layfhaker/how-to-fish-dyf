using HarmonyLib;
using TMPro;
using UnityEngine;

namespace DepYourFriend.Patches
{
    [HarmonyPatch(typeof(DeathUI))]
    public static class DeathUIPatch
    {
        [HarmonyPatch("FixedUpdate")]
        [HarmonyPostfix]
        public static void FixedUpdatePostfix(DeathUI __instance, bool ____isOn, GameObject ____noBossUI, GameObject ____friendsHelpText)
        {
            if (!____isOn) return;

            if (PlayerDyingPatch.ShouldBlockSelfRespawn())
            {
                // Disable the vanilla give-up prompt so player knows they cannot self-respawn
                if (____noBossUI != null && ____noBossUI.activeSelf)
                {
                    ____noBossUI.SetActive(false);
                }

                if (____friendsHelpText != null)
                {
                    ____friendsHelpText.SetActive(true);
                    TextMeshProUGUI tmp = ____friendsHelpText.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        int bail = FriendBailManager.BaseValue;
                        if (Player.LocalPlayer != null && Player.LocalPlayer.Dying != null && Player.LocalPlayer.Dying.DeadPlayer != null)
                        {
                            bail = FriendBailManager.GetBail(Player.LocalPlayer.Dying.DeadPlayer);
                        }

                        tmp.text = $"<color=#FFD700>{FriendBailManager.GetBailStatusString(bail)}</color>\n<size=75%><color=#AAAAAA>Ждите оживления от союзника или депа на рулетке!</color></size>";
                    }
                }
            }
            else
            {
                // If everyone is dead (team wipe) or in boss fight, allow default UI
                if (PlayerManager.AlivePlayers.Count == 0 && ____noBossUI != null && !____noBossUI.activeSelf && BossManager.Boss == null)
                {
                    ____noBossUI.SetActive(true);
                    if (____friendsHelpText != null)
                    {
                        TextMeshProUGUI tmp = ____friendsHelpText.GetComponentInChildren<TextMeshProUGUI>();
                        if (tmp != null)
                        {
                            tmp.text = "<color=#FF5555>ВСЕ ПОГИБЛИ! Удерживайте ЛКМ для респавна лодки</color>";
                        }
                    }
                }
            }
        }
    }
}
