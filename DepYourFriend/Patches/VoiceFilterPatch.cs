using HarmonyLib;
using UnityEngine;

namespace DepYourFriend.Patches
{
    [HarmonyPatch(typeof(PlayerDying))]
    public static class VoiceFilterPatch
    {
        private const float DeadPitch = 0.80f;
        private const float NormalPitch = 1.0f;

        [HarmonyPatch("DeathEffects")]
        [HarmonyPostfix]
        public static void DeathEffectsPostfix(PlayerDying __instance, Player ____player)
        {
            if (____player != null && ____player.Mouth != null)
            {
                ApplyDeadVoiceEffects(____player.Mouth);
            }
        }

        [HarmonyPatch("ResurrectEffect")]
        [HarmonyPostfix]
        public static void ResurrectEffectPostfix(PlayerDying __instance, Player ____player)
        {
            if (____player != null && ____player.Mouth != null)
            {
                RemoveDeadVoiceEffects(____player.Mouth);
            }
        }

        private static void ApplyDeadVoiceEffects(PlayerMouth mouth)
        {
            AudioSource source = Traverse.Create(mouth).Field("_voiceChatSource").GetValue<AudioSource>();
            if (source == null) return;

            // Spooky low pitch
            source.pitch = DeadPitch;

            // Low Pass filter for muffled/coffin voice
            AudioLowPassFilter lowPass = source.gameObject.GetComponent<AudioLowPassFilter>();
            if (lowPass == null)
            {
                lowPass = source.gameObject.AddComponent<AudioLowPassFilter>();
            }
            lowPass.enabled = true;
            lowPass.cutoffFrequency = 1400f;

            // Echo filter for ghostly reverberation
            AudioEchoFilter echo = source.gameObject.GetComponent<AudioEchoFilter>();
            if (echo == null)
            {
                echo = source.gameObject.AddComponent<AudioEchoFilter>();
            }
            echo.enabled = true;
            echo.delay = 200f;
            echo.decayRatio = 0.45f;
            echo.wetMix = 0.6f;
            echo.dryMix = 0.8f;

            // Cave reverb preset
            AudioReverbFilter reverb = source.gameObject.GetComponent<AudioReverbFilter>();
            if (reverb == null)
            {
                reverb = source.gameObject.AddComponent<AudioReverbFilter>();
            }
            reverb.enabled = true;
            reverb.reverbPreset = AudioReverbPreset.Cave;
        }

        private static void RemoveDeadVoiceEffects(PlayerMouth mouth)
        {
            AudioSource source = Traverse.Create(mouth).Field("_voiceChatSource").GetValue<AudioSource>();
            if (source == null) return;

            source.pitch = NormalPitch;

            AudioLowPassFilter lowPass = source.gameObject.GetComponent<AudioLowPassFilter>();
            if (lowPass != null)
            {
                lowPass.enabled = false;
            }

            AudioEchoFilter echo = source.gameObject.GetComponent<AudioEchoFilter>();
            if (echo != null)
            {
                echo.enabled = false;
            }

            AudioReverbFilter reverb = source.gameObject.GetComponent<AudioReverbFilter>();
            if (reverb != null)
            {
                reverb.enabled = false;
            }
        }
    }
}
