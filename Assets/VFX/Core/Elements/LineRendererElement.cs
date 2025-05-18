using UnityEngine;
using System.Collections.Generic;

namespace Jwho303.VFX
{
    [RequireComponent(typeof(LineRenderer))]
    public class LineRendererElement : BaseVFXElement
    {
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private BaseVFXPath pathCalculator;
        [SerializeField] private int linePointCount = 10;

        public override void Initialize()
        {
            if (isInitialized) return;

            // Create line renderer if not assigned
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            // Apply basic settings
            lineRenderer.positionCount = linePointCount;

            // Make sure path calculator is valid
            if (pathCalculator == null)
            {
                Debug.LogError("No path calculator assigned to LineRendererElement.");
            }

            base.Initialize();
        }

        public override void UpdatePath(List<Vector3> pathPoints)
        {
            if (!isInitialized)
            {
                Initialize();
            }

            // Update our stored path
            currentPathPoints = new List<Vector3>(pathPoints);

            // Update path for gizmo drawing
            pathCalculator.UpdatePath(pathPoints);

            // Update line renderer positions
            UpdateLinePositions();
        }

        // Implement base method to ensure proper compatibility
        public new void UpdatePositions(Vector3 start, Vector3 end)
        {
            // Create a simple path with two points and update
            List<Vector3> simplePath = new List<Vector3> { start, end };
            UpdatePath(simplePath);
        }

        private void UpdateLinePositions()
        {
            // Ensure we have at least 2 points for the line renderer
            int validPointCount = Mathf.Max(2, linePointCount);

            // Apply to line renderer
            lineRenderer.positionCount = validPointCount;

            for (int i = 0; i < validPointCount; i++)
            {
                float t = i / (float)(validPointCount - 1);
                Vector3 point = GetPositionOnPath(pathCalculator, currentPathPoints, t);
                lineRenderer.SetPosition(i, point);
            }
        }

        public override void Play()
        {
            if (isPlaying) return;

            if (!isInitialized)
            {
                Initialize();
            }

            // Enable line renderer
            lineRenderer.enabled = true;

            isPlaying = true;
        }

        public override void Stop()
        {
            if (!isPlaying) return;

            // Disable line renderer
            lineRenderer.enabled = false;

            isPlaying = false;
        }
    }
}