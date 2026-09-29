using DepYourFriend.Patches;
using UnityEngine;

namespace DepYourFriend
{
    public class SoloDebugManager : MonoBehaviour
    {
        /// <summary>
        /// The current persistent debug corpse (F7). BepIn-exclusive: bound to an ALIVE local player,
        /// so the vanilla DeadPlayer.Update() server-guard would instantly destroy it. The
        /// DebugCorpsePatch suppresses that destroy for exactly this corpse.
        /// </summary>
        public static DeadPlayer? ActiveDebugCorpse;
        public static DeadPlayer? HoveredCorpse;

        private void Update()
        {
            if (Player.LocalPlayer == null) return;

            // F6: Add $500 money for quick testing
            if (Input.GetKeyDown(KeyCode.F6))
            {
                MoneyManager.AddMoney(500, Player.LocalPlayer);
                PlayerUI.SetLookAtText($"<color=#55FF55>[DEBUG F6]</color> Добавлено +$500! Баланс: ${MoneyManager.Money}");
            }

            // F7: Toggle Dummy Dead Friend Corpse (spawn / despawn)
            if (Input.GetKeyDown(KeyCode.F7))
            {
                ToggleDebugCorpse();
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

        private void ToggleDebugCorpse()
        {
            if (ActiveDebugCorpse != null && ActiveDebugCorpse.gameObject != null)
            {
                DespawnDebugCorpse();
                return;
            }
            SpawnDummyDeadFriend();
        }

        private static Texture2D? _panelTex;
        private static GUIStyle? _panelStyle;

        /// <summary>
        /// Fixed 2D on-screen overlay while the local player is holding a friend's corpse, so the bail
        /// and next-action hint stay readable regardless of camera/world text position.
        /// </summary>
        private void OnGUI()
        {
            if (Player.LocalPlayer == null || Player.LocalPlayer.Holding == null) return;

            DeadPlayer? dp = Player.LocalPlayer.Holding.HeldItem as DeadPlayer
                             ?? (HoveredCorpse != null ? HoveredCorpse : null);
            if (dp == null) return;

            int bail = FriendBailManager.GetBail(dp);
            string status = FriendBailManager.GetBailStatusString(bail);

            if (_panelStyle == null)
            {
                _panelTex = new Texture2D(1, 1);
                _panelTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.72f));
                _panelTex.Apply();
                _panelStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 17,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    richText = true,
                    wordWrap = true,
                    normal = { textColor = Color.white, background = _panelTex }
                };
            }

            bool holding = Player.LocalPlayer.Holding.HeldItem as DeadPlayer != null;
            string hint = holding
                ? "Держи ЛКМ, чтобы оживить · или неси на стол рулетки, чтобы сыграть"
                : "Возьми в руки, чтобы оживить или понести на стол рулетки";
            string text = $"<color=#FFD700>[ ТРУП ДРУГА ]</color>  {status}\n<size=14><color=#BBBBBB>{hint}</color></size>";
            GUI.Box(new Rect(16, Screen.height - 104, Mathf.Min(840, Screen.width - 32), 76), text, _panelStyle);
        }

        private void DespawnDebugCorpse()
        {
            DeadPlayer? corpse = ActiveDebugCorpse;
            ActiveDebugCorpse = null;

            if (corpse == null || corpse.gameObject == null)
            {
                return;
            }

            if (Server.Instance != null && Server.Instance.IsServerInitialized)
            {
                corpse.DestroyItem(0);
                DepPlugin.ModLogger?.LogInfo("[F7] Debug corpse despawned (server).");
                PlayerUI.SetLookAtText("<color=#FFD700>[DEBUG F7]</color> Труп убран.");
            }
            else
            {
                Destroy(corpse.gameObject);
                DepPlugin.ModLogger?.LogInfo("[F7] Debug corpse destroyed (local).");
                PlayerUI.SetLookAtText("<color=#FFD700>[DEBUG F7]</color> Труп убран (local).");
            }
        }

        private void SpawnDummyDeadFriend()
        {
            if (Player.LocalPlayer == null) return;

            if (GameInfo.DeadPlayerPrefab == null)
            {
                PlayerUI.SetLookAtText("<color=#FF5555>[DEBUG F7] Ошибка: DeadPlayerPrefab не найден</color>");
                DepPlugin.ModLogger?.LogWarning("[F7] DeadPlayerPrefab is NULL.");
                return;
            }

            Vector3 spawnPos = Player.LocalPlayer.Transform.position + Player.LocalPlayer.CamObject.forward * 1.5f + Vector3.up * 0.5f;
            Server server = Server.Instance;
            Transform? holder = server != null ? server.DynamicObjectsHolder : null;

            DeadPlayer dummy = Instantiate(GameInfo.DeadPlayerPrefab, spawnPos, Quaternion.identity, holder);
            if (dummy == null)
            {
                PlayerUI.SetLookAtText("<color=#FF5555>[DEBUG F7] Instantiate вернул null</color>");
                DepPlugin.ModLogger?.LogWarning("[F7] Instantiate DeadPlayerPrefab returned null.");
                return;
            }

            // Must be set BEFORE the first server Update tick so the corpse isn't auto-destroyed.
            ActiveDebugCorpse = dummy;

            try
            {
                dummy.SetPlayer(Player.LocalPlayer, Vector3.up * 1.5f);
            }
            catch (System.Exception e)
            {
                DepPlugin.ModLogger?.LogError($"[F7] SetPlayer threw: {e}");
            }

            // The debug corpse is bound to the still-ALIVE player: undo the voice-target move done by
            // SetPlayer() so we don't hijack the living player's voice-chat audio onto the corpse.
            try { dummy.SetVoiceChatParent(false); } catch { }

            bool serverReady = server != null && server.IsServerInitialized;
            if (serverReady)
            {
                server!.Spawn(dummy.gameObject);
                DepPlugin.ModLogger?.LogInfo($"[F7] Debug corpse spawned (server). IsServer={serverReady}");
            }
            else
            {
                DepPlugin.ModLogger?.LogWarning("[F7] No server initialized — corpse is local-only (network interactions won't work).");
            }

            // Register standard base bail of $100
            FriendBailManager.SetBail(dummy, FriendBailManager.BaseBail);

            PlayerUI.SetLookAtText($"<color=#00FFAA>[DEBUG F7] ТРУП ДРУГА ЗАСПАВНЕН!</color>\nЗалог: $100. Бери в руки и неси на стол рулетки!{(!serverReady ? "\n<color=#FF5555>ВНИМАНИЕ: сервер не поднят — сетевое взаимодействие отключено</color>" : "")}");
        }
    }
}