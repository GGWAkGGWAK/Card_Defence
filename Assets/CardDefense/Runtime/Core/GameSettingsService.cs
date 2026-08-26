using CardDefense.Cards;
using CardDefense.Combat;
using CardDefense.Enemies;
using CardDefense.UI;
using UnityEngine;

namespace CardDefense.Core
{
    public sealed class GameSettingsService : MonoBehaviour
    {
        private const string DefaultPrefix = "CardDefense.Settings.";

        public bool BgmEnabled { get; private set; }
        public bool SfxEnabled { get; private set; }
        public bool VibrationEnabled { get; private set; }
        public float BgmVolume { get; private set; }
        public float SfxVolume { get; private set; }
        public bool IsBgmPlaying => bgmSource != null && bgmSource.isPlaying;

#if UNITY_EDITOR
        public static string EditorSettingsPrefixOverride;
#endif

        private static string Prefix
        {
            get
            {
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(EditorSettingsPrefixOverride)) return EditorSettingsPrefixOverride;
#endif
                return DefaultPrefix;
            }
        }

        private AudioSource bgmSource;
        private AudioSource sfxSource;
        private AudioClip summonClip;
        private AudioClip mergeClip;
        private AudioClip upgradeClip;
        private AudioClip alertClip;
        private AudioClip defeatClip;
        private CardSummonController summon;
        private PokerProgressionService progression;
        private WaveDirector waves;
        private GrowthChoiceController growth;
        private CombatEffectSystem combatEffects;

        public void Configure(CardSummonController summonController,
            PokerProgressionService progressionService, WaveDirector waveDirector,
            GrowthChoiceController growthController, CombatEffectSystem effectSystem)
        {
            summon = summonController;
            progression = progressionService;
            waves = waveDirector;
            growth = growthController;
            combatEffects = effectSystem;
            BgmEnabled = PlayerPrefs.GetInt(Prefix + "Bgm", 1) != 0;
            SfxEnabled = PlayerPrefs.GetInt(Prefix + "Sfx", 1) != 0;
            VibrationEnabled = PlayerPrefs.GetInt(Prefix + "Vibration", 1) != 0;
            BgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "BgmVolume", 0.65f));
            SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "SfxVolume", 0.8f));

            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.spatialBlend = 0f;
            bgmSource.priority = 32;
            bgmSource.ignoreListenerPause = true;
            bgmSource.volume = 0.62f * BgmVolume;
            bgmSource.clip = CreateAmbientLoop();
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.volume = 0.42f * SfxVolume;
            summonClip = CreateEffect("Summon", 0.14f, new[] { 523.25f, 783.99f }, 1.32f, 0.02f);
            mergeClip = CreateEffect("Merge", 0.42f, new[] { 392f, 523.25f, 659.25f, 783.99f }, 1.08f, 0.015f);
            upgradeClip = CreateEffect("Upgrade", 0.26f, new[] { 659.25f, 987.77f, 1318.51f }, 1.18f, 0.025f);
            alertClip = CreateEffect("BossAlert", 0.72f, new[] { 82.41f, 123.47f, 164.81f }, 0.76f, 0.08f);
            defeatClip = CreateEffect("Defeat", 0.9f, new[] { 220f, 174.61f, 130.81f }, 0.55f, 0.045f);
            ApplyBgm();
            if (combatEffects != null) combatEffects.SetAudio(SfxEnabled, SfxVolume);

            summon.CardSummoned += HandleSummoned;
            summon.CardsMerged += HandleMerged;
            progression.HandUpgraded += HandleUpgraded;
            waves.RoundChanged += HandleRoundChanged;
            waves.ChallengeBossSpawned += HandleChallengeBossSpawned;
            waves.GameLost += HandleGameLost;
            growth.ChoiceSelected += HandleGrowthSelected;
        }

        private void OnDestroy()
        {
            if (summon != null)
            {
                summon.CardSummoned -= HandleSummoned;
                summon.CardsMerged -= HandleMerged;
            }
            if (progression != null) progression.HandUpgraded -= HandleUpgraded;
            if (waves != null)
            {
                waves.RoundChanged -= HandleRoundChanged;
                waves.ChallengeBossSpawned -= HandleChallengeBossSpawned;
                waves.GameLost -= HandleGameLost;
            }
            if (growth != null) growth.ChoiceSelected -= HandleGrowthSelected;
        }

        public void SetBgmEnabled(bool enabled)
        {
            BgmEnabled = enabled;
            Save("Bgm", enabled);
            ApplyBgm();
        }

        public void SetSfxEnabled(bool enabled)
        {
            SfxEnabled = enabled;
            Save("Sfx", enabled);
            if (combatEffects != null) combatEffects.SetAudio(enabled, SfxVolume);
            if (enabled) Play(upgradeClip);
        }

        public void SetBgmVolume(float volume)
        {
            BgmVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(Prefix + "BgmVolume", BgmVolume);
            PlayerPrefs.Save();
            if (bgmSource != null) bgmSource.volume = 0.62f * BgmVolume;
        }

        public void SetSfxVolume(float volume)
        {
            SfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(Prefix + "SfxVolume", SfxVolume);
            PlayerPrefs.Save();
            if (sfxSource != null) sfxSource.volume = 0.42f * SfxVolume;
            if (combatEffects != null) combatEffects.SetAudio(SfxEnabled, SfxVolume);
        }

        public void SetVibrationEnabled(bool enabled)
        {
            VibrationEnabled = enabled;
            Save("Vibration", enabled);
            if (enabled) Vibrate();
        }

        public static void DeleteSettings()
        {
            PlayerPrefs.DeleteKey(Prefix + "Bgm");
            PlayerPrefs.DeleteKey(Prefix + "Sfx");
            PlayerPrefs.DeleteKey(Prefix + "Vibration");
            PlayerPrefs.DeleteKey(Prefix + "BgmVolume");
            PlayerPrefs.DeleteKey(Prefix + "SfxVolume");
            PlayerPrefs.Save();
        }

        private void HandleSummoned() => Play(summonClip);

        private void HandleMerged(PokerHand hand)
        {
            Play(mergeClip);
            Vibrate();
        }

        private void HandleUpgraded(PokerHand hand, int level) => Play(upgradeClip);

        private void HandleRoundChanged(int round)
        {
            if (round % 10 != 0) return;
            Play(alertClip);
            Vibrate();
        }

        private void HandleGrowthSelected(RunGrowthChoice choice) => Play(upgradeClip);

        private void HandleChallengeBossSpawned()
        {
            Play(alertClip);
            Vibrate();
        }

        private void HandleGameLost()
        {
            Play(defeatClip);
            Vibrate();
        }

        private void ApplyBgm()
        {
            if (bgmSource == null) return;
            if (BgmEnabled)
            {
                if (!bgmSource.isPlaying) bgmSource.Play();
            }
            else bgmSource.Stop();
        }

        private void Play(AudioClip clip)
        {
            if (!SfxEnabled || sfxSource == null || clip == null) return;
            sfxSource.PlayOneShot(clip);
        }

        private void Vibrate()
        {
            if (!VibrationEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        private static void Save(string name, bool value)
        {
            PlayerPrefs.SetInt(Prefix + name, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        private static AudioClip CreateEffect(string name, float duration, float[] frequencies,
            float endPitchMultiplier, float noiseAmount)
        {
            const int sampleRate = 22050;
            int count = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[count];
            uint noiseState = 0xA341316Cu;
            for (int i = 0; i < count; i++)
            {
                float time = i / (float)sampleRate;
                float normalized = i / (float)count;
                float attack = Mathf.Clamp01(normalized / 0.045f);
                float envelope = attack * Mathf.Pow(1f - normalized, 1.75f);
                float pitch = Mathf.Lerp(1f, endPitchMultiplier, normalized);
                float sample = 0f;
                for (int note = 0; note < frequencies.Length; note++)
                {
                    float stagger = Mathf.Clamp01(normalized * frequencies.Length - note * 0.18f);
                    sample += Mathf.Sin(2f * Mathf.PI * frequencies[note] * pitch * time) *
                              (0.72f / frequencies.Length) * stagger;
                    sample += Mathf.Sin(2f * Mathf.PI * frequencies[note] * 2f * pitch * time) *
                              (0.16f / frequencies.Length) * stagger;
                }
                noiseState = noiseState * 1664525u + 1013904223u;
                float noise = ((noiseState >> 8) / 16777215f) * 2f - 1f;
                data[i] = Mathf.Clamp(sample * envelope + noise * noiseAmount * envelope, -0.92f, 0.92f);
            }
            AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateAmbientLoop()
        {
            const int sampleRate = 22050;
            const float duration = 12f;
            int count = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[count];
            float[] notes = { 110f, 130.81f, 164.81f, 196f, 164.81f, 130.81f,
                              98f, 123.47f, 146.83f, 185f, 146.83f, 123.47f };
            for (int i = 0; i < count; i++)
            {
                float time = i / (float)sampleRate;
                int beat = Mathf.FloorToInt(time) % notes.Length;
                float phase = time - Mathf.Floor(time);
                float pluck = Mathf.Pow(1f - phase, 2.2f);
                float pad = 0.5f - 0.5f * Mathf.Cos(Mathf.PI * 2f * time / duration);
                float root = notes[beat];
                float sample = Mathf.Sin(2f * Mathf.PI * root * time) * pluck * 0.12f;
                sample += Mathf.Sin(2f * Mathf.PI * root * 1.5f * time) * pluck * 0.05f;
                sample += Mathf.Sin(2f * Mathf.PI * 55f * time) * (0.035f + pad * 0.015f);
                sample += Mathf.Sin(2f * Mathf.PI * (root * 0.5f) * time) * 0.035f;
                data[i] = Mathf.Clamp(sample * 2.15f, -0.82f, 0.82f);
            }
            AudioClip clip = AudioClip.Create("PrototypeAmbient", count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
