using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace DepYourFriend.Patches
{
    [HarmonyPatch(typeof(CasinoBox))]
    public static class CasinoBoxPatch
    {
        [HarmonyPatch("FixedUpdate")]
        [HarmonyPrefix]
        public static bool FixedUpdatePrefix(CasinoBox __instance, BoxCollider ____col)
        {
            if (!Server.Instance || !Server.Instance.IsServerInitialized || ((bool)CasinoManager.Instance && CasinoManager.IsBetting))
            {
                return false;
            }

            Vector3 center = ____col.transform.TransformPoint(____col.center);
            Vector3 halfExtents = Vector3.Scale(____col.size * 0.5f, __instance.transform.localScale);
            
            // Check both ItemLayer and Default layers in case ragdoll limbs have different layers
            int layerMask = GameInfo.ItemLayer | (1 << LayerMask.NameToLayer("Default"));
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

            CasinoManager.SetBetItems(list);
            return false; // Skip vanilla method
        }
    }
}
