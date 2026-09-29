using UnityEngine;

namespace DepYourFriend
{
    /// <summary>
    /// Fixed 2D on-screen overlay showing the friend's bail/status while the local player is hovering
    /// or holding a DeadPlayer corpse, so the info stays readable regardless of camera/world text.
    /// </summary>
    public class FriendBailHUD : MonoBehaviour
    {
        /// <summary>Corpse currently under the cursor (set by the Item.Hover hook).</summary>
        public static DeadPlayer? HoveredCorpse;

        private static Texture2D? _panelTex;
        private static GUIStyle? _panelStyle;

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
    }
}