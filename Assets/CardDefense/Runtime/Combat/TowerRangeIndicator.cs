using UnityEngine;

namespace CardDefense.Combat
{
    public sealed class TowerRangeIndicator : MonoBehaviour
    {
        private const int Segments = 48;
        private CardSummonController summon;
        private LineRenderer line;
        private Material material;
        private CardTower lastTower;
        private float lastRange = -1f;

        public void Configure(CardSummonController controller)
        {
            summon = controller;
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            material = new Material(shader) { name = "RuntimeTowerRange" };
            line = gameObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = Segments;
            line.startWidth = 0.045f;
            line.endWidth = 0.045f;
            line.startColor = new Color(0.25f, 0.9f, 1f, 0.6f);
            line.endColor = line.startColor;
            line.sortingOrder = 8;
            line.enabled = false;
        }

        private void LateUpdate()
        {
            if (summon == null || line == null) return;
            CardTower tower = summon.FocusedTower;
            if (tower == null || tower.IsDragging)
            {
                line.enabled = false;
                lastTower = null;
                return;
            }

            transform.position = tower.transform.position;
            float range = tower.CurrentRange;
            if (tower != lastTower || !Mathf.Approximately(range, lastRange)) Rebuild(range);
            lastTower = tower;
            lastRange = range;
            line.enabled = true;
        }

        private void Rebuild(float radius)
        {
            for (int i = 0; i < Segments; i++)
            {
                float angle = Mathf.PI * 2f * i / Segments;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
