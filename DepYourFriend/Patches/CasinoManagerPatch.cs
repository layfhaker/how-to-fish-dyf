using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace DepYourFriend.Patches
{
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
                    // DeadPlayer counts as $100 stake so roulette buttons activate
                    total += 100;
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

                        int delta;
                        if (curBetColor == BetColor.Green)
                        {
                            delta = won ? -FriendBailManager.GreenDelta : FriendBailManager.GreenDelta;
                        }
                        else
                        {
                            delta = won ? -FriendBailManager.RedBlackDelta : FriendBailManager.RedBlackDelta;
                        }

                        int newBail = FriendBailManager.AdjustBail(item.DeadPlayer, delta);

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

            // Recalculate worth and trigger bet result effects
            Traverse.Create(typeof(CasinoManager)).Method("CalculateWorth", won).GetValue();
            Traverse.Create(__instance).Method("BetResultEffects", (byte)curBetColor, won).GetValue();

            // Set IsBetting = false
            Traverse.Create(typeof(CasinoManager)).Property("IsBetting").SetValue(false);

            return false; // Skip vanilla method
        }
    }
}
