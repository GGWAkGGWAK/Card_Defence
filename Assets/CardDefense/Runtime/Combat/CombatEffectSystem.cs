using CardDefense.Cards;
using CardDefense.Enemies;
using UnityEngine;

namespace CardDefense.Combat
{
    public sealed class CombatEffectSystem : MonoBehaviour
    {
        private BeamEffect[] beams;
        private ProjectileEffect[] projectiles;
        private Material material;
        private int nextBeam;
        private int nextProjectile;
        private ImpactEffect[] impacts;
        private DamageTextEffect[] damageTexts;
        private DamageTextEffect[] rewardTexts;
        private int nextImpact;
        private int nextDamageText;
        private int nextRewardText;
        private float effectQuality = 0.75f;
        private int effectSequence;
        private AudioSource attackAudio;
        private AudioClip basicAttackClip;
        private AudioClip fusionAttackClip;
        private AudioClip criticalAttackClip;
        private bool sfxEnabled = true;
        private float sfxVolume = 0.8f;
        private float nextAttackAudioTime;
        private Camera shakeCamera;
        private Vector3 cameraRestPosition;
        private float shakeRemaining;
        private float shakeIntensity;
        private WaveDirector waves;

        public PokerHand LastPlayedHand { get; private set; }
        public CardSuit LastPlayedSuit { get; private set; }
        public Color LastProjectileColor { get; private set; }
        public int LastRewardGold { get; private set; }
        public int ActiveProjectileCount { get; private set; }
        public int ActiveImpactCount { get; private set; }
        public int ActiveRewardTextCount { get; private set; }

        public void Configure(int poolSize)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            material = new Material(shader) { name = "RuntimeAttackBeam" };
            attackAudio = gameObject.AddComponent<AudioSource>();
            attackAudio.playOnAwake = false;
            attackAudio.spatialBlend = 0f;
            attackAudio.volume = 0.34f * sfxVolume;
            basicAttackClip = CreateAttackClip("CardShot", 420f, 0.075f, 1.25f, 0.015f);
            fusionAttackClip = CreateAttackClip("FusionShot", 620f, 0.11f, 1.4f, 0.025f);
            criticalAttackClip = CreateAttackClip("CriticalShot", 240f, 0.18f, 2.2f, 0.09f);
            beams = new BeamEffect[Mathf.Max(8, poolSize)];
            projectiles = new ProjectileEffect[beams.Length];
            impacts = new ImpactEffect[beams.Length];
            damageTexts = new DamageTextEffect[beams.Length];
            rewardTexts = new DamageTextEffect[Mathf.Max(8, beams.Length / 2)];
            shakeCamera = Camera.main;
            if (shakeCamera != null) cameraRestPosition = shakeCamera.transform.localPosition;
            Sprite impactSprite = CreateImpactSprite();
            Sprite projectileSprite = CreateProjectileSprite();
            for (int i = 0; i < beams.Length; i++)
            {
                GameObject beamObject = new GameObject("AttackBeam_" + i.ToString("00"));
                beamObject.transform.SetParent(transform, false);
                LineRenderer line = beamObject.AddComponent<LineRenderer>();
                line.sharedMaterial = material;
                line.positionCount = 2;
                line.useWorldSpace = true;
                line.startWidth = 0.075f;
                line.endWidth = 0.025f;
                line.sortingOrder = 20;
                line.enabled = false;
                beams[i] = new BeamEffect(line);

                GameObject projectileObject = new GameObject("CardProjectile_" + i.ToString("00"));
                projectileObject.transform.SetParent(transform, false);
                SpriteRenderer projectileRenderer = projectileObject.AddComponent<SpriteRenderer>();
                projectileRenderer.sprite = projectileSprite;
                projectileRenderer.sortingOrder = 24;
                projectileRenderer.enabled = false;
                projectiles[i] = new ProjectileEffect(projectileRenderer);

                GameObject impactObject = new GameObject("HitBurst_" + i.ToString("00"));
                impactObject.transform.SetParent(transform, false);
                SpriteRenderer impactRenderer = impactObject.AddComponent<SpriteRenderer>();
                impactRenderer.sprite = impactSprite;
                impactRenderer.sortingOrder = 22;
                impactRenderer.enabled = false;
                impacts[i] = new ImpactEffect(impactRenderer);

                GameObject damageObject = new GameObject("DamageText_" + i.ToString("00"));
                damageObject.transform.SetParent(transform, false);
                TextMesh text = damageObject.AddComponent<TextMesh>();
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;
                text.fontSize = 42;
                text.characterSize = 0.075f;
                text.fontStyle = FontStyle.Bold;
                text.GetComponent<MeshRenderer>().sortingOrder = 30;
                damageObject.SetActive(false);
                damageTexts[i] = new DamageTextEffect(text);
            }
            for (int i = 0; i < rewardTexts.Length; i++)
            {
                GameObject rewardObject = new GameObject("RewardText_" + i.ToString("00"));
                rewardObject.transform.SetParent(transform, false);
                TextMesh text = rewardObject.AddComponent<TextMesh>();
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;
                text.fontSize = 42;
                text.characterSize = 0.075f;
                text.fontStyle = FontStyle.Bold;
                text.GetComponent<MeshRenderer>().sortingOrder = 31;
                rewardObject.SetActive(false);
                rewardTexts[i] = new DamageTextEffect(text);
            }
        }

        public void Bind(WaveDirector waveDirector)
        {
            if (waves != null) waves.MonsterRewarded -= HandleMonsterRewarded;
            waves = waveDirector;
            if (waves != null) waves.MonsterRewarded += HandleMonsterRewarded;
        }

        private void Update()
        {
            if (beams == null) return;
            float deltaTime = Time.deltaTime;
            ActiveProjectileCount = 0;
            for (int i = 0; i < projectiles.Length; i++)
            {
                ProjectileEffect projectile = projectiles[i];
                if (projectile.Remaining <= 0f) continue;
                projectile.Remaining -= deltaTime;
                float normalized = 1f - Mathf.Clamp01(projectile.Remaining / projectile.Duration);
                Vector3 position = Vector3.Lerp(projectile.Start, projectile.End, normalized);
                position.y += Mathf.Sin(normalized * Mathf.PI) * projectile.ArcHeight;
                position.z = -0.28f;
                projectile.Renderer.transform.position = position;
                projectile.Renderer.transform.Rotate(0f, 0f, deltaTime * projectile.SpinSpeed);
                float pulse = 1f + Mathf.Sin(normalized * Mathf.PI) * 0.38f;
                projectile.Renderer.transform.localScale = Vector3.one * projectile.Size * pulse;
                Color color = projectile.Color;
                color.a = 1f - Mathf.Clamp01((normalized - 0.82f) / 0.18f);
                projectile.Renderer.color = color;
                if (projectile.Remaining <= 0f) projectile.Renderer.enabled = false;
                else ActiveProjectileCount++;
            }
            for (int i = 0; i < beams.Length; i++)
            {
                if (beams[i].Remaining <= 0f) continue;
                beams[i].Remaining -= deltaTime;
                if (beams[i].Remaining <= 0f) beams[i].Line.enabled = false;
            }
            ActiveImpactCount = 0;
            for (int i = 0; i < impacts.Length; i++)
            {
                ImpactEffect impact = impacts[i];
                if (impact.Remaining <= 0f) continue;
                impact.Remaining -= deltaTime;
                float normalized = 1f - Mathf.Clamp01(impact.Remaining / impact.Duration);
                float size = Mathf.Lerp(impact.Critical ? 0.18f : 0.12f,
                    impact.Critical ? 0.78f : 0.5f, normalized);
                impact.Renderer.transform.localScale = new Vector3(size, size, 1f);
                impact.Renderer.transform.Rotate(0f, 0f, deltaTime * 260f);
                Color color = impact.Color;
                color.a = 1f - normalized;
                impact.Renderer.color = color;
                if (impact.Remaining <= 0f) impact.Renderer.enabled = false;
                else ActiveImpactCount++;
            }
            for (int i = 0; i < damageTexts.Length; i++)
            {
                UpdateFloatingText(damageTexts[i], deltaTime);
            }
            ActiveRewardTextCount = 0;
            for (int i = 0; i < rewardTexts.Length; i++)
            {
                UpdateFloatingText(rewardTexts[i], deltaTime);
                if (rewardTexts[i].Remaining > 0f) ActiveRewardTextCount++;
            }
            UpdateCameraShake(deltaTime);
        }

        private static void UpdateFloatingText(DamageTextEffect damage, float deltaTime)
        {
            if (damage.Remaining <= 0f) return;
            damage.Remaining -= deltaTime;
            float normalized = 1f - Mathf.Clamp01(damage.Remaining / damage.Duration);
            damage.Text.transform.position = damage.Start + Vector3.up * Mathf.Lerp(0f, damage.Rise, normalized);
            float scale = damage.Critical
                ? Mathf.Lerp(1.65f, 1f, Mathf.Clamp01(normalized * 3.5f))
                : Mathf.Lerp(1.15f, 0.92f, normalized);
            damage.Text.transform.localScale = Vector3.one * scale;
            Color color = damage.Color;
            color.a = 1f - Mathf.Clamp01((normalized - 0.5f) * 2f);
            damage.Text.color = color;
            if (damage.Remaining <= 0f) damage.Text.gameObject.SetActive(false);
        }

        public void PlayBeam(Vector3 from, Vector3 to, bool critical, PokerHand hand)
        {
            PlayProjectile(from, to, critical, hand, CardSuit.Spade);
        }

        public void PlayProjectile(Vector3 from, Vector3 to, bool critical, PokerHand hand, CardSuit suit)
        {
            if (beams == null || beams.Length == 0) return;
            LastPlayedHand = hand;
            LastPlayedSuit = suit;
            Color suitColor = SuitColor(suit);
            float handBlend = Mathf.Clamp01((int)hand / (float)PokerHand.RoyalStraightFlush) * 0.42f;
            Color color = critical
                ? new Color(1f, 0.24f, 0.08f, 1f)
                : Color.Lerp(suitColor, HandColor(hand), handBlend);
            LastProjectileColor = color;

            ProjectileEffect projectile = projectiles[nextProjectile];
            nextProjectile = (nextProjectile + 1) % projectiles.Length;
            projectile.Start = from;
            projectile.End = to;
            projectile.Color = color;
            projectile.Duration = critical ? 0.13f : hand >= PokerHand.Straight ? 0.16f : 0.19f;
            projectile.Remaining = projectile.Duration;
            projectile.ArcHeight = Vector3.Distance(from, to) * (critical ? 0.11f : 0.07f);
            projectile.Size = critical ? 0.34f : hand >= PokerHand.FullHouse ? 0.28f : 0.22f;
            projectile.SpinSpeed = suit == CardSuit.Heart ? 360f : 620f;
            projectile.Renderer.transform.position = from;
            projectile.Renderer.transform.localRotation = Quaternion.identity;
            projectile.Renderer.transform.localScale = Vector3.one * projectile.Size;
            projectile.Renderer.color = color;
            projectile.Renderer.enabled = true;
            ActiveProjectileCount++;

            BeamEffect beam = beams[nextBeam];
            nextBeam = (nextBeam + 1) % beams.Length;
            beam.Line.SetPosition(0, from);
            beam.Line.SetPosition(1, to);
            beam.Line.startColor = color;
            beam.Line.endColor = new Color(color.r, color.g, color.b, 0.15f);
            beam.Line.startWidth = critical ? 0.14f : 0.075f;
            beam.Remaining = critical ? 0.14f : 0.09f;
            beam.Line.enabled = true;
            PlayAttackAudio(critical, hand);

            effectSequence++;
            if (!critical && effectQuality < 0.6f && (effectSequence & 1) == 0) return;
            ImpactEffect impact = impacts[nextImpact];
            nextImpact = (nextImpact + 1) % impacts.Length;
            impact.Renderer.transform.position = new Vector3(to.x, to.y, -0.2f);
            impact.Renderer.transform.localRotation = Quaternion.identity;
            impact.Color = color;
            impact.Critical = critical;
            impact.Duration = critical ? 0.24f : 0.16f;
            impact.Remaining = impact.Duration;
            impact.Renderer.color = color;
            impact.Renderer.enabled = true;
            ActiveImpactCount++;
            if (!critical && hand >= PokerHand.FullHouse)
            {
                shakeRemaining = Mathf.Max(shakeRemaining, 0.075f);
                shakeIntensity = Mathf.Max(shakeIntensity, 0.035f + (int)hand * 0.003f);
            }
        }

        public void SetQuality(float quality)
        {
            effectQuality = Mathf.Clamp01(quality);
        }

        public void SetAudio(bool enabled, float volume)
        {
            sfxEnabled = enabled;
            sfxVolume = Mathf.Clamp01(volume);
            if (attackAudio != null) attackAudio.volume = 0.34f * sfxVolume;
        }

        public void PlayDamageNumber(Vector3 position, float amount, bool critical)
        {
            if (damageTexts == null || damageTexts.Length == 0) return;
            DamageTextEffect damage = damageTexts[nextDamageText];
            nextDamageText = (nextDamageText + 1) % damageTexts.Length;
            damage.Start = new Vector3(position.x, position.y + 0.35f, -0.35f);
            damage.Critical = critical;
            damage.Rise = critical ? 0.9f : 0.72f;
            damage.Duration = critical ? 0.72f : 0.52f;
            damage.Remaining = damage.Duration;
            damage.Color = critical ? new Color(1f, 0.72f, 0.08f, 1f) : Color.white;
            damage.Text.text = critical
                ? "치명! " + Mathf.Max(1, Mathf.RoundToInt(amount))
                : Mathf.Max(1, Mathf.RoundToInt(amount)).ToString();
            damage.Text.color = damage.Color;
            damage.Text.transform.position = damage.Start;
            damage.Text.gameObject.SetActive(true);
            if (critical)
            {
                shakeRemaining = Mathf.Max(shakeRemaining, 0.11f);
                shakeIntensity = Mathf.Max(shakeIntensity, 0.065f);
            }
        }

        public void PlayGoldReward(Vector3 position, int amount)
        {
            if (rewardTexts == null || rewardTexts.Length == 0 || amount <= 0) return;
            LastRewardGold = amount;
            DamageTextEffect reward = rewardTexts[nextRewardText];
            nextRewardText = (nextRewardText + 1) % rewardTexts.Length;
            reward.Start = new Vector3(position.x + 0.18f, position.y + 0.18f, -0.36f);
            reward.Critical = false;
            reward.Rise = 1.02f;
            reward.Duration = 0.82f;
            reward.Remaining = reward.Duration;
            reward.Color = new Color(1f, 0.82f, 0.12f, 1f);
            reward.Text.text = "+" + amount + "G";
            reward.Text.color = reward.Color;
            reward.Text.transform.position = reward.Start;
            reward.Text.gameObject.SetActive(true);
            ActiveRewardTextCount++;
        }

        private void HandleMonsterRewarded(Vector3 position, int reward)
        {
            PlayGoldReward(position, reward);
        }

        private void PlayAttackAudio(bool critical, PokerHand hand)
        {
            if (!sfxEnabled || attackAudio == null || Time.unscaledTime < nextAttackAudioTime) return;
            nextAttackAudioTime = Time.unscaledTime + (critical ? 0.025f : 0.055f);
            AudioClip clip = critical
                ? criticalAttackClip
                : hand >= PokerHand.Straight ? fusionAttackClip : basicAttackClip;
            float volume = critical ? 1f : hand >= PokerHand.Straight ? 0.75f : 0.5f;
            attackAudio.pitch = critical ? Random.Range(0.92f, 1.04f) : Random.Range(0.97f, 1.06f);
            attackAudio.PlayOneShot(clip, volume);
        }

        private void UpdateCameraShake(float deltaTime)
        {
            if (shakeCamera == null) return;
            if (shakeRemaining > 0f)
            {
                shakeRemaining -= deltaTime;
                float fade = Mathf.Clamp01(shakeRemaining / 0.11f);
                Vector2 offset = Random.insideUnitCircle * shakeIntensity * fade;
                shakeCamera.transform.localPosition = cameraRestPosition + new Vector3(offset.x, offset.y, 0f);
                return;
            }
            shakeCamera.transform.localPosition = cameraRestPosition;
            shakeIntensity = 0f;
        }

        private static AudioClip CreateAttackClip(string name, float frequency, float duration,
            float endPitch, float noiseAmount)
        {
            const int sampleRate = 22050;
            int count = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[count];
            uint noiseState = 0xC2B2AE35u;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                float n = i / (float)count;
                float pitch = Mathf.Lerp(1f, endPitch, n);
                float envelope = Mathf.Clamp01(n / 0.04f) * Mathf.Pow(1f - n, 2.4f);
                float sample = Mathf.Sin(2f * Mathf.PI * frequency * pitch * t) * 0.55f;
                sample += Mathf.Sin(2f * Mathf.PI * frequency * 2.03f * pitch * t) * 0.18f;
                noiseState = noiseState * 1664525u + 1013904223u;
                float noise = ((noiseState >> 8) / 16777215f) * 2f - 1f;
                data[i] = Mathf.Clamp((sample + noise * noiseAmount) * envelope, -0.9f, 0.9f);
            }
            AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static Color HandColor(PokerHand hand)
        {
            switch (hand)
            {
                case PokerHand.OnePair: return new Color(0.3f, 0.78f, 1f, 1f);
                case PokerHand.TwoPair: return new Color(0.25f, 1f, 0.72f, 1f);
                case PokerHand.ThreeOfAKind: return new Color(0.55f, 0.48f, 1f, 1f);
                case PokerHand.Straight: return new Color(1f, 0.76f, 0.18f, 1f);
                case PokerHand.Flush: return new Color(0.2f, 0.95f, 0.95f, 1f);
                case PokerHand.FullHouse: return new Color(0.95f, 0.35f, 0.9f, 1f);
                case PokerHand.FourOfAKind: return new Color(1f, 0.35f, 0.18f, 1f);
                case PokerHand.StraightFlush: return new Color(0.45f, 1f, 0.28f, 1f);
                case PokerHand.RoyalStraightFlush: return new Color(1f, 0.9f, 0.2f, 1f);
                default: return new Color(0.92f, 0.94f, 1f, 1f);
            }
        }

        public static Color SuitColor(CardSuit suit)
        {
            switch (suit)
            {
                case CardSuit.Diamond: return new Color(0.16f, 0.92f, 1f, 1f);
                case CardSuit.Heart: return new Color(1f, 0.2f, 0.42f, 1f);
                case CardSuit.Club: return new Color(0.2f, 1f, 0.46f, 1f);
                default: return new Color(0.48f, 0.38f, 1f, 1f);
            }
        }

        private static Sprite CreateImpactSprite()
        {
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                float dx = Mathf.Abs(x - 7.5f) / 7.5f;
                float dy = Mathf.Abs(y - 7.5f) / 7.5f;
                float alpha = Mathf.Clamp01(1f - (dx + dy) * 0.72f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CreateProjectileSprite()
        {
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            texture.name = "RuntimeCardProjectile";
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                float dx = Mathf.Abs(x - 7.5f) / 7.5f;
                float dy = Mathf.Abs(y - 7.5f) / 7.5f;
                float diamond = Mathf.Clamp01(1.15f - dx - dy);
                float core = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 2.2f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Max(diamond * 0.85f, core)));
            }
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
        }

        private void OnDestroy()
        {
            if (waves != null) waves.MonsterRewarded -= HandleMonsterRewarded;
            if (material != null) Destroy(material);
        }

        private sealed class ProjectileEffect
        {
            public readonly SpriteRenderer Renderer;
            public Vector3 Start;
            public Vector3 End;
            public Color Color;
            public float ArcHeight;
            public float Size;
            public float SpinSpeed;
            public float Duration;
            public float Remaining;
            public ProjectileEffect(SpriteRenderer renderer) { Renderer = renderer; }
        }

        private sealed class BeamEffect
        {
            public readonly LineRenderer Line;
            public float Remaining;

            public BeamEffect(LineRenderer line)
            {
                Line = line;
            }
        }

        private sealed class ImpactEffect
        {
            public readonly SpriteRenderer Renderer;
            public Color Color;
            public bool Critical;
            public float Duration;
            public float Remaining;
            public ImpactEffect(SpriteRenderer renderer) { Renderer = renderer; }
        }

        private sealed class DamageTextEffect
        {
            public readonly TextMesh Text;
            public Vector3 Start;
            public Color Color;
            public bool Critical;
            public float Rise;
            public float Duration;
            public float Remaining;
            public DamageTextEffect(TextMesh text) { Text = text; }
        }
    }
}
