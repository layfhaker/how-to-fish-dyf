using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace DepYourFriend.Patches
{
    [HarmonyPatch(typeof(CasinoBox))]
    public static class CasinoBoxPatch
    {
        /// <summary>Whether the BetButtons were last shown. Kept in sync with the corpse staying on the
        /// table so plan buttons re-appear after a spin (vanilla hides them after a loss).</summary>
        public static bool ButtonsVisible;

        [HarmonyPatch("FixedUpdate")]
        [HarmonyPrefix]
        public static bool FixedUpdatePrefix(CasinoBox __instance, BoxCollider ____col)
        {
            if (!Server.Instance || !Server.Instance.IsServerInitialized || ((bool)CasinoManager.Instance && CasinoManager.IsBetting))
            {
                // While a spin is running the CasinoManager (ServerStartBet/BetResultEffects) owns the
                // buttons; don't fight it here.
                return false;
            }

            Vector3 center = ____col.transform.TransformPoint(____col.center);
            Vector3 halfExtents = Vector3.Scale(____col.size * 0.5f, __instance.transform.localScale);
            
            // Check both ItemLayer and Default layers in case ragdoll limbs have different layers.
            // Also include the drop/hold item layers: a freshly-dropped corpse uses the ItemPart
            // layer, so without it the table only occasionally catches the corpse (the "pick up and
            // put it back again" bug).
            int layerMask = GameInfo.ItemLayer | GameInfo.ItemPartLayer | GameInfo.ItemAndInHandLayer | (1 << LayerMask.NameToLayer("Default"));
            Collider[] colliders = Physics.OverlapBox(center, halfExtents, __instance.transform.rotation, layerMask);
            
            List<Item> list = new List<Item>();
            foreach (Collider collider in colliders)
            {
                Item item = ItemManager.Get(collider);
                
                // Also check if collider belongs to a DeadPlayer directly
                if (item == null)
                {
                    DeadPlayer dp = collider.GetComponentInParent<DeadPlayer>();
                    if (dp != null)
                    {
                        item = dp;
                    }
                }

                if (item != null && !list.Contains(item) && (!item.Creature || item.Creature.IsDead))
                {
                    // DEAD PLAYER IS NOW ALLOWED ON THE CASINO TABLE!
                    list.Add(item);
                }
            }

            // Re-show the bet buttons whenever something is on the table (vanilla hides them after a
            // loss, but a surviving corpse should keep the table bettable).
            bool show = list.Count > 0;
            if (show != ButtonsVisible)
            {
                BetButton.ToggleAll(show);
                ButtonsVisible = show;
            }

            CasinoManager.SetBetItems(list);
            return false; // Skip vanilla method
        }
    }
}
