using UnityEngine;

namespace CardDefense.Combat
{
    public sealed class CombatEffectSystem : MonoBehaviour
    {
        private BeamEffect[] beams;
        private Material material;
        private int nextBeam;

        public void Configure(int poolSize)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            material = new Material(shader) { name = "RuntimeAttackBeam" };
            beams = new BeamEffect[Mathf.Max(8, poolSize)];
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
            }
        }

        private void Update()
        {
            if (beams == null) return;
            float deltaTime = Time.deltaTime;
            for (int i = 0; i < beams.Length; i++)
            {
                if (beams[i].Remaining <= 0f) continue;
                beams[i].Remaining -= deltaTime;
                if (beams[i].Remaining <= 0f) beams[i].Line.enabled = false;
            }
        }

        public void PlayBeam(Vector3 from, Vector3 to, bool critical)
        {
            if (beams == null || beams.Length == 0) return;
            BeamEffect beam = beams[nextBeam];
            nextBeam = (nextBeam + 1) % beams.Length;
            beam.Line.SetPosition(0, from);
            beam.Line.SetPosition(1, to);
            Color color = critical ? new Color(1f, 0.3f, 0.12f, 1f) : new Color(1f, 0.86f, 0.25f, 1f);
            beam.Line.startColor = color;
            beam.Line.endColor = new Color(color.r, color.g, color.b, 0.15f);
            beam.Line.startWidth = critical ? 0.14f : 0.075f;
            beam.Remaining = critical ? 0.14f : 0.09f;
            beam.Line.enabled = true;
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
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
    }
}
