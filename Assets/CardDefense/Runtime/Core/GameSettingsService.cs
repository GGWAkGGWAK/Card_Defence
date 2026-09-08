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
        public float EffectiveBgmVolume => bgmSource != null ? bgmSource.volume : 0f;
        public float BgmClipDuration => bgmSource != null && bgmSource.clip != null ? bgmSource.clip.length : 0f;
        public float BgmSignalRms { get; private set; }

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
            bgmSource.dopplerLevel = 0f;
            bgmSource.volume = 0.82f * BgmVolume;
            bgmSource.clip = CreateDefenseTheme(out float signalRms);
            BgmSignalRms = signalRms;
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
            if (bgmSource != null) bgmSource.volume = 0.82f * BgmVolume;
            if (BgmEnabled && BgmVolume > 0f) ApplyBgm();
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

        private void Update()
        {
            if (BgmEnabled && BgmVolume > 0f && bgmSource != null && bgmSource.clip != null &&
                !bgmSource.isPlaying) bgmSource.Play();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused) ApplyBgm();
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

        private static AudioClip CreateDefenseTheme(out float signalRms)
        {
            const int sampleRate = 22050;
            const float duration = 16f;
            const int channels = 2;
            int frameCount = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[frameCount * channels];
            float[] chordRoots = { 110f, 98f, 82.41f, 87.31f };
            float sumSquares = 0f;
            uint noiseState = 0x9E3779B9u;
            for (int i = 0; i < frameCount; i++)
            {
                float time = i / (float)sampleRate;
                float beatTime = time * 2f;
                int beatIndex = Mathf.FloorToInt(beatTime);
                float beatPhase = beatTime - beatIndex;
                int bar = (beatIndex / 4) % 4;
                float root = chordRoots[bar];

                float padMotion = 0.72f + 0.28f * Mathf.Sin(2f * Mathf.PI * time / 8f);
                float pad = Mathf.Sin(2f * Mathf.PI * root * time) * 0.105f;
                pad += Mathf.Sin(2f * Mathf.PI * root * 1.2f * time) * 0.075f;
                pad += Mathf.Sin(2f * Mathf.PI * root * 1.5f * time) * 0.065f;
                pad *= padMotion;

                float bassEnvelope = Mathf.Pow(1f - beatPhase, 1.7f);
                float bass = Mathf.Sin(2f * Mathf.PI * root * 0.5f * time) * bassEnvelope * 0.24f;

                int eighth = Mathf.FloorToInt(time * 4f);
                float eighthPhase = time * 4f - eighth;
                float[] melodyRatios = { 2f, 2.4f, 3f, 2.4f, 3.2f, 3f, 2.4f, 2f };
                float melodyFrequency = root * melodyRatios[eighth & 7];
                float melodyEnvelope = Mathf.Pow(1f - eighthPhase, 3.1f);
                float melody = Mathf.Sin(2f * Mathf.PI * melodyFrequency * time) *
                               melodyEnvelope * 0.19f;
                melody += Mathf.Sin(2f * Mathf.PI * melodyFrequency * 2f * time) *
                          melodyEnvelope * 0.045f;

                float kickFrequency = Mathf.Lerp(105f, 43f, Mathf.Clamp01(beatPhase * 5f));
                float kick = Mathf.Sin(2f * Mathf.PI * kickFrequency * time) *
                             Mathf.Exp(-beatPhase * 13f) * (beatIndex % 4 == 0 ? 0.44f : 0.3f);

                noiseState = noiseState * 1664525u + 1013904223u;
                float noise = ((noiseState >> 8) / 16777215f) * 2f - 1f;
                float snare = (beatIndex & 1) == 1
                    ? noise * Mathf.Exp(-beatPhase * 17f) * 0.22f
                    : 0f;
                float hatPhase = time * 8f - Mathf.Floor(time * 8f);
                float hat = noise * Mathf.Exp(-hatPhase * 28f) * 0.065f;

                float loopFade = Mathf.Clamp01(time / 0.055f) *
                                 Mathf.Clamp01((duration - time) / 0.055f);
                float rhythm = kick + snare + hat;
                float left = (pad + bass + rhythm + melody * (eighth % 2 == 0 ? 0.72f : 1f)) * loopFade;
                float right = (pad * 0.96f + bass + rhythm + melody * (eighth % 2 == 0 ? 1f : 0.72f)) *
                              loopFade;
                left = Mathf.Clamp(left, -0.92f, 0.92f);
                right = Mathf.Clamp(right, -0.92f, 0.92f);
                data[i * channels] = left;
                data[i * channels + 1] = right;
                sumSquares += left * left + right * right;
            }
            signalRms = Mathf.Sqrt(sumSquares / Mathf.Max(1, data.Length));
            AudioClip clip = AudioClip.Create("CardDefenseBattleTheme", frameCount, channels, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
