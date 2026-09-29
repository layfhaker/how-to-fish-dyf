using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace DepYourFriend.Patches
{
    /// <summary>
    /// When a DeadPlayer corpse is on the table, the casino should show the friend's SIGNED value
    /// (a debt shows a minus, e.g. "-$100"). Vanilla uses the single _totalWorth for BOTH the bet
    /// buttons (only shown when worth &gt; 0) and the displayed text, so we reimplement the display
    /// here: buttons are driven by "something is wagered", the text is signed. Casual items keep the
    /// vanilla path.
    /// </summary>
    [HarmonyPatch(typeof(LocalCasino))]
    public static class LocalCasinoSignedPatch
    {
        [HarmonyPatch("SetTotalWorthText")]
        [HarmonyPrefix]
        public static bool SetTotalWorthTextPrefix(LocalCasino __instance)
        {
            var items = Traverse.Create(typeof(CasinoManager)).Field("_itemsToBet").GetValue<List<Item>>();
            if (items == null) return true;

            int signed = 0;
            bool hasCorpse = false;
            bool hasWager = false;
            foreach (Item item in items)
            {
                if (item == null) continue;
                if (item.DeadPlayer != null)
                {
                    hasCorpse = true;
                    signed += FriendBailManager.GetBail(item.DeadPlayer);
                }
                else
                {
                    signed += Mathf.Max(0, item.TotalWorth);
                }
                hasWager = true;
            }

            if (!hasCorpse) return true; // casual items: keep vanilla

            // Stop a previously running worth-count coroutine so our text isn't overwritten.
            Coroutine? routine = Traverse.Create(__instance).Field("_worthRoutine").GetValue<Coroutine>();
            if (routine != null)
            {
                __instance.StopCoroutine(routine);
                Traverse.Create(__instance).Field("_worthRoutine").SetValue(null);
            }

            Traverse.Create(__instance).Field("_curBetWorth").SetValue(signed);
            Traverse.Create(__instance).Field("_targetBetWorth").SetValue(signed);

            TextMeshProUGUI? txt = Traverse.Create(__instance).Field("_curWorthText").GetValue<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = signed < 0 ? $"-${Mathf.Abs(signed)}" : $"${signed}";
            }

            // Buttons shown as long as something is actually wagered.
            BetButton.ToggleAll(hasWager && signed != 0);

            return false; // replaced vanilla display for corpse bets
        }
    }

    [HarmonyPatch(typeof(CasinoManager))]
    public static class CasinoManagerPatch
    {
        [HarmonyPatch("CalculateWorth")]
        [HarmonyPrefix]
        public static bool CalculateWorthPrefix(bool won)
        {
            var items = Traverse.Create(typeof(CasinoManager)).Field("_itemsToBet").GetValue<List<Item>>();
            if (items == null) return true;

            int total = 0;
            foreach (Item item in items)
            {
                if (item == null) continue;

                if (item.DeadPlayer != null)
                {
                    // The casino stake must stay POSITIVE (SetTotalWorthText shows BetButtons only when
                    // worth > 0). Show the debt/profit MAGNITUDE here; the sign is conveyed by the 2D
                    // HUD and the friend status string.
                    int v = FriendBailManager.GetBail(item.DeadPlayer);
                    total += Mathf.Max(1, Mathf.Abs(v));
                }
                else
                {
                    total += item.TotalWorth;
                }
            }

            if (CasinoManager.Instance != null && total != CasinoManager.Instance._totalWorth.Value)
            {
                CasinoManager.Instance._totalWorth.Value = total;
                Traverse.Create(CasinoManager.Instance).Method("UpdateTotalWorth", total, won).GetValue();
            }

            return false; // Override vanilla calculation
        }

        [HarmonyPatch("ServerRouletteResult")]
        [HarmonyPrefix]
        public static bool ServerRouletteResultPrefix(CasinoManager __instance, BetColor winColor)
        {
            var items = Traverse.Create(typeof(CasinoManager)).Field("_itemsToBet").GetValue<List<Item>>();
            var curBetColor = Traverse.Create(typeof(CasinoManager)).Field("_curBetColor").GetValue<BetColor>();

            bool won = (winColor == curBetColor);
            int multiplier = 0;
            if (won)
            {
                switch (winColor)
                {
                    case BetColor.Black:
                    case BetColor.Red:
                        multiplier = 2;
                        break;
                    case BetColor.Green:
                        multiplier = 35;
                        break;
                }
            }

            if (items != null)
            {
                foreach (Item item in items)
                {
                    if (item == null) continue;

                    if (item.DeadPlayer != null)
                    {
                        // DEAD PLAYER LOGIC: Never destroy corpse on loss!
                        item.ToggleInteractable(true);

                        int rouletteMult = curBetColor == BetColor.Green ? 35 : 2;
                        int newBail = FriendBailManager.ResolveRoulette(item.DeadPlayer, rouletteMult, won);

                        // Visual WorldText feedback
                        if (LocalCasino.Instance != null)
                        {
                            Transform effectsPos = Traverse.Create(LocalCasino.Instance).Field("_effectsPos").GetValue<Transform>();
                            Vector3 spawnPos = effectsPos != null ? effectsPos.position + Vector3.up * 0.4f : item.transform.position + Vector3.up * 1f;
                            WorldText worldText = Object.Instantiate(GameInfo.WorldTextPrefab, spawnPos, Quaternion.identity);
                            if (worldText != null)
                            {
                                string text = won
                                    ? $"<color=#55FF55>ДЕП ЗАШЕЛ!</color>\n{FriendBailManager.GetBailStatusString(newBail)}"
                                    : $"<color=#FF5555>ДЕП НЕ ЗАШЕЛ!</color>\n{FriendBailManager.GetBailStatusString(newBail)}";
                                worldText.SetText(text, 0.25f);
                                worldText.FloatAndRemoveAnimation();
                            }
                        }
                    }
                    else
                    {
                        // Vanilla behavior for regular items
                        if (won)
                        {
                            item.ToggleInteractable(true);
                            item.AddBetMultiplier(multiplier);
                        }
                        else
                        {
                            item.DestroyItem(0);
                        }
                    }
                }
            }

            bool anyRemain = items != null && items.Count > 0;

            // Recalculate worth and trigger bet result effects. The corpse survives losses (not
            // vanilla), so re-open the bet buttons whenever anything is still on the table; vanilla
            // would leave them hidden after a loss.
            Traverse.Create(typeof(CasinoManager)).Method("CalculateWorth", won).GetValue();
            Traverse.Create(__instance).Method("BetResultEffects", (byte)curBetColor, won || anyRemain).GetValue();

            // Set IsBetting = false
            Traverse.Create(typeof(CasinoManager)).Property("IsBetting").SetValue(false);

            return false; // Skip vanilla method
        }
    }
}
