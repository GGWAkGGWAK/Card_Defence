using UnityEngine;

namespace CardDefense.Enemies
{
    public sealed class LoopPath : MonoBehaviour
    {
        [SerializeField] private Transform[] waypoints;

        private Vector3[] points;
        private float[] cumulativeLengths;
        private Vector3[] baseLocalWaypoints;
        private Camera viewportCamera;
        private float horizontalPadding;
        private float verticalPadding;
        private float lastAspect = -1f;

        public float Length { get; private set; }
        public float HorizontalViewportScale { get; private set; } = 1f;
        public float VerticalViewportScale { get; private set; } = 1f;

        public void Configure(Transform[] pathPoints)
        {
            waypoints = pathPoints;
            RebuildCache();
        }

        private void Awake()
        {
            CaptureBaseWaypoints();
            RebuildCache();
        }

        private void Update()
        {
            if (viewportCamera == null || Mathf.Approximately(lastAspect, viewportCamera.aspect)) return;
            ApplyViewportSafety();
        }

        public void EnableViewportSafety(Camera targetCamera, float requiredHorizontalPadding,
            float requiredVerticalPadding)
        {
            viewportCamera = targetCamera;
            horizontalPadding = Mathf.Max(0f, requiredHorizontalPadding);
            verticalPadding = Mathf.Max(0f, requiredVerticalPadding);
            CaptureBaseWaypoints();
            ApplyViewportSafety();
        }

        public static float CalculateAxisScale(float cameraHalfExtent, float requiredPadding,
            float originalPathHalfExtent)
        {
            if (originalPathHalfExtent <= 0f) return 1f;
            float available = Mathf.Max(0.5f, cameraHalfExtent - Mathf.Max(0f, requiredPadding));
            return Mathf.Clamp01(available / originalPathHalfExtent);
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

        private void CaptureBaseWaypoints()
        {
            if (waypoints == null || waypoints.Length < 2) return;
            if (baseLocalWaypoints != null && baseLocalWaypoints.Length == waypoints.Length) return;
            baseLocalWaypoints = new Vector3[waypoints.Length];
            for (int i = 0; i < waypoints.Length; i++)
                baseLocalWaypoints[i] = waypoints[i].localPosition;
        }

        private void ApplyViewportSafety()
        {
            if (viewportCamera == null || !viewportCamera.orthographic || baseLocalWaypoints == null) return;
            float sourceHalfWidth = 0f;
            float sourceHalfHeight = 0f;
            for (int i = 0; i < baseLocalWaypoints.Length; i++)
            {
                sourceHalfWidth = Mathf.Max(sourceHalfWidth, Mathf.Abs(baseLocalWaypoints[i].x));
                sourceHalfHeight = Mathf.Max(sourceHalfHeight, Mathf.Abs(baseLocalWaypoints[i].y));
            }
            float cameraHalfHeight = viewportCamera.orthographicSize;
            float cameraHalfWidth = cameraHalfHeight * viewportCamera.aspect;
            HorizontalViewportScale = CalculateAxisScale(cameraHalfWidth, horizontalPadding, sourceHalfWidth);
            VerticalViewportScale = CalculateAxisScale(cameraHalfHeight, verticalPadding, sourceHalfHeight);
            for (int i = 0; i < waypoints.Length; i++)
            {
                Vector3 source = baseLocalWaypoints[i];
                waypoints[i].localPosition = new Vector3(source.x * HorizontalViewportScale,
                    source.y * VerticalViewportScale, source.z);
            }
            lastAspect = viewportCamera.aspect;
            RebuildCache();
            RefreshLineRenderer();
        }

        private void RefreshLineRenderer()
        {
            LineRenderer route = GetComponent<LineRenderer>();
            if (route == null || waypoints == null || waypoints.Length < 2) return;
            route.positionCount = waypoints.Length + 1;
            for (int i = 0; i < waypoints.Length; i++) route.SetPosition(i, waypoints[i].position);
            route.SetPosition(waypoints.Length, waypoints[0].position);
        }
    }
}
