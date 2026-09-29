using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Broadcast;
using FishNet.Transporting;
using UnityEngine;

namespace DepYourFriend
{
    public struct BailSyncBroadcast : IBroadcast
    {
        public int DeadPlayerNetworkId;
        public int CurrentBail;
    }

    public static class FriendBailManager
    {
        public const int BaseValue = -100;    // friend's value when it dies: a $100 debt
        public const int FreeThreshold = 99; // |value| at or below which a favorable result flips the sign

        // Maps DeadPlayer NetworkObject ObjectId to current bail amount
        private static readonly Dictionary<int, int> _deadPlayerBails = new Dictionary<int, int>();

        private static bool _broadcastRegistered;

        public static void Initialize()
        {
            _broadcastRegistered = false;
            EnsureBroadcastRegistered();
        }

        /// <summary>
        /// Register the bail-sync broadcast as soon as the local ClientManager is available.
        /// Called lazily from bail writes/reads because at plugin-Awake time the networking layer
        /// usually isn't initialized yet.
        /// </summary>
        private static void EnsureBroadcastRegistered()
        {
            if (_broadcastRegistered) return;
            if (InstanceFinder.ClientManager != null)
            {
                InstanceFinder.ClientManager.RegisterBroadcast<BailSyncBroadcast>(OnBailSyncReceived);
                _broadcastRegistered = true;
            }
        }

        public static void Reset()
        {
            _deadPlayerBails.Clear();
        }

        public static int GetBail(DeadPlayer deadPlayer)
        {
            EnsureBroadcastRegistered();
            if (deadPlayer == null) return BaseValue;

            int id = GetObjectId(deadPlayer);
            if (id == 0) return BaseValue;

            if (_deadPlayerBails.TryGetValue(id, out int bail))
            {
                return bail;
            }

            _deadPlayerBails[id] = BaseValue;
            return BaseValue;
        }

        public static void SetBail(DeadPlayer deadPlayer, int amount, bool broadcast = true)
        {
            EnsureBroadcastRegistered();
            if (deadPlayer == null) return;
            int id = GetObjectId(deadPlayer);
            if (id == 0) return;

            _deadPlayerBails[id] = amount;

            if (broadcast && InstanceFinder.ServerManager != null && InstanceFinder.ServerManager.Started)
            {
                BailSyncBroadcast msg = new BailSyncBroadcast
                {
                    DeadPlayerNetworkId = id,
                    CurrentBail = amount
                };
                InstanceFinder.ServerManager.Broadcast(msg);
            }
        }

        /// <summary>
        /// Variant-B roulette: multiplicative double/halve with a sign flip at the FreeThreshold.
        /// Negative V = debt (you'll pay |V| to revive), positive V = profit (revive pays you V).
        /// </summary>
        public static int ResolveRoulette(DeadPlayer deadPlayer, int multiplier, bool won)
        {
            int v = GetBail(deadPlayer);
            if (won)
            {
                if (v > 0) v = v * multiplier;                 // profit grows
                else v = Mathf.CeilToInt(v / (float)multiplier); // debt shrinks toward 0
                if (Mathf.Abs(v) <= FreeThreshold) v = FreeThreshold; // paid off -> flips to profit
            }
            else
            {
                if (v < 0) v = v * multiplier;                  // debt grows
                else v = Mathf.FloorToInt(v / (float)multiplier); // profit shrinks
                if (Mathf.Abs(v) <= FreeThreshold) v = -FreeThreshold; // drops back to debt
            }

            SetBail(deadPlayer, v, broadcast: true);
            return v;
        }

        public static void RemoveBail(DeadPlayer deadPlayer)
        {
            if (deadPlayer == null) return;
            int id = GetObjectId(deadPlayer);
            if (id != 0)
            {
                _deadPlayerBails.Remove(id);
            }
        }

        private static int GetObjectId(DeadPlayer deadPlayer)
        {
            if (deadPlayer.NetworkObject != null)
            {
                return deadPlayer.NetworkObject.ObjectId;
            }
            return deadPlayer.gameObject.GetInstanceID();
        }

        private static void OnBailSyncReceived(BailSyncBroadcast msg, Channel channel)
        {
            _deadPlayerBails[msg.DeadPlayerNetworkId] = msg.CurrentBail;
        }

        public static string GetBailStatusString(int bail)
        {
            if (bail < 0)
            {
                return $"Залог: ${Math.Abs(bail)}";
            }
            else if (bail == 0)
            {
                return "Бесплатный выкуп! ($0)";
            }
            else
            {
                return $"Прибыль при оживлении: +${bail}!";
            }
        }
    }
}
