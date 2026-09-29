using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace DepYourFriend
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class DepPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.antigravity.depyourfriend";
        public const string ModName = "DEP YOUR FRIEND";
        public const string ModVersion = "1.0.0";

        public static ManualLogSource ModLogger { get; private set; } = null!;
        public static DepPlugin Instance { get; private set; } = null!;

        private Harmony _harmony = null!;

        private void Awake()
        {
            Instance = this;
            ModLogger = Logger;

            ModLogger.LogInfo($"{ModName} v{ModVersion} is loading...");

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll();

            FriendBailManager.Initialize();

            gameObject.AddComponent<FriendBailHUD>();

            ModLogger.LogInfo($"{ModName} loaded successfully! Ready to gamble your friends on the roulette table.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
