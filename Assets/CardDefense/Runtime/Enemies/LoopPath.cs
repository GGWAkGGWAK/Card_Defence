using UnityEngine;

namespace CardDefense.Enemies
{
    public sealed class LoopPath : MonoBehaviour
    {
        [SerializeField] private Transform[] waypoints;

        private Vector3[] points;
        private float[] cumulativeLengths;

        public float Length { get; private set; }

        public void Configure(Transform[] pathPoints)
        {
            waypoints = pathPoints;
            RebuildCache();
        }

        private void Awake()
        {
            RebuildCache();
        }

        public Vector3 GetPosition(float normalizedProgress)
        {
            if (points == null || points.Length < 2 || Length <= 0f) return transform.position;

            normalizedProgress -= Mathf.Floor(normalizedProgress);
            float targetDistance = normalizedProgress * Length;

            for (int i = 1; i < cumulativeLengths.Length; i++)
            {
                if (targetDistance > cumulativeLengths[i]) continue;
                float segmentStart = cumulativeLengths[i - 1];
                float segmentLength = cumulativeLengths[i] - segmentStart;
                float t = segmentLength > 0f ? (targetDistance - segmentStart) / segmentLength : 0f;
                return Vector3.LerpUnclamped(points[i - 1], points[i], t);
            }

            return points[0];
        }

        public void RebuildCache()
        {
            if (waypoints == null || waypoints.Length < 2)
            {
                points = null;
                cumulativeLengths = null;
                Length = 0f;
                return;
            }

            int count = waypoints.Length + 1;
            points = new Vector3[count];
            cumulativeLengths = new float[count];
            Length = 0f;

            for (int i = 0; i < waypoints.Length; i++) points[i] = waypoints[i].position;
            points[count - 1] = points[0];

            for (int i = 1; i < count; i++)
            {
                Length += Vector3.Distance(points[i - 1], points[i]);
                cumulativeLengths[i] = Length;
            }
        }
    }
}
