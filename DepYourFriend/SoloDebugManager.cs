using DepYourFriend.Patches;
using UnityEngine;

namespace DepYourFriend
{
    public class SoloDebugManager : MonoBehaviour
    {
        private void Update()
        {
            if (Player.LocalPlayer == null) return;

            // F6: Add $500 money for quick testing
            if (Input.GetKeyDown(KeyCode.F6))
            {
                MoneyManager.AddMoney(500, Player.LocalPlayer);
                PlayerUI.SetLookAtText($"<color=#55FF55>[DEBUG F6]</color> Добавлено +$500! Баланс: ${MoneyManager.Money}");
            }

            // F7: Spawn Dummy Dead Friend Corpse
            if (Input.GetKeyDown(KeyCode.F7))
            {
                SpawnDummyDeadFriend();
            }

            // F8: Toggle Solo Test Mode (enables death-lock in solo)
            if (Input.GetKeyDown(KeyCode.F8))
            {
                PlayerDyingPatch.SoloTestMode = !PlayerDyingPatch.SoloTestMode;
                string state = PlayerDyingPatch.SoloTestMode ? "<color=#55FF55>ВКЛЮЧЕН</color>" : "<color=#FF5555>ВЫКЛЮЧЕН</color>";
                PlayerUI.SetLookAtText($"<color=#FFD700>[DEBUG F8]</color> Режим теста в соло: {state}");
            }

            // F9: Kill Local Player to test death UI & lock
            if (Input.GetKeyDown(KeyCode.F9))
            {
                PlayerUI.SetLookAtText("<color=#FF5555>[DEBUG F9] Суицид для теста экрана смерти</color>");
                Player.LocalPlayer.Vitals.TakeDamage(99999, default, default, ignoreInvulnerability: true);
            }

            // F10: Revive Local Player
            if (Input.GetKeyDown(KeyCode.F10))
            {
                if (Player.LocalPlayer.Dying.IsDead)
                {
                    Player.LocalPlayer.Dying.LocalResurrect();
                    Player.LocalPlayer.Vitals.ServerResetVitals();
                    PlayerUI.SetLookAtText("<color=#55FF55>[DEBUG F10] Локальное воскрешение</color>");
                }
            }
        }

        private void SpawnDummyDeadFriend()
        {
            if (GameInfo.DeadPlayerPrefab == null)
            {
                PlayerUI.SetLookAtText("<color=#FF5555>[DEBUG] Ошибка: DeadPlayerPrefab не найден</color>");
                return;
            }

            Vector3 spawnPos = Player.LocalPlayer.Transform.position + Player.LocalPlayer.CamObject.forward * 1.5f + Vector3.up * 0.5f;
            Transform holder = Server.Instance != null ? Server.Instance.DynamicObjectsHolder : null;

            DeadPlayer dummy = Instantiate(GameInfo.DeadPlayerPrefab, spawnPos, Quaternion.identity, holder);
            if (dummy != null)
            {
                dummy.SetPlayer(Player.LocalPlayer, Vector3.up * 1.5f);

                if (Server.Instance != null && Server.Instance.IsServerInitialized)
                {
                    Server.Instance.Spawn(dummy.gameObject);
                }

                // Register standard base bail of $100
                FriendBailManager.SetBail(dummy, FriendBailManager.BaseBail);

                PlayerUI.SetLookAtText("<color=#00FFAA>[DEBUG F7] ТРУП ДРУГА ЗАСПАВНЕН!</color>\nЗалог: $100. Бери в руки и неси на стол рулетки!");
            }
        }
    }
}
