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
        public const int BaseBail = 100;
        public const int RedBlackDelta = 100;
        public const int GreenDelta = 3300;

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
            if (deadPlayer == null) return BaseBail;

            int id = GetObjectId(deadPlayer);
            if (id == 0) return BaseBail;

            if (_deadPlayerBails.TryGetValue(id, out int bail))
            {
                return bail;
            }

            _deadPlayerBails[id] = BaseBail;
            return BaseBail;
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

        public static int AdjustBail(DeadPlayer deadPlayer, int delta)
        {
            int current = GetBail(deadPlayer);
            int updated = current + delta;
            SetBail(deadPlayer, updated, broadcast: true);
            return updated;
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
            if (bail > 0)
            {
                return $"Залог: ${bail}";
            }
            else if (bail == 0)
            {
                return "Бесплатный выкуп! ($0)";
            }
            else
            {
                return $"Выигрыш при оживлении: +${Math.Abs(bail)}!";
            }
        }
    }
}
