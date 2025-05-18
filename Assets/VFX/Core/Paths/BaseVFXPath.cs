using UnityEngine;
using System.Collections.Generic;

namespace Jwho303.VFX
{
    /// <summary>
    /// Base class for path calculations
    /// </summary>
    public abstract class BaseVFXPath : MonoBehaviour
    {
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color gizmoColor = Color.yellow;
        [SerializeField] private int gizmoPointCount = 20;

        protected List<Vector3> pathPoints = new List<Vector3>();

        /// <summary>
        /// Calculate a point along the path based on normalized distance (0-1)
        /// </summary>
        /// <param name="pathPoints">Array of points defining the path</param>
        /// <param name="normalizedDistance">Normalized distance along the path (0-1)</param>
        /// <returns>Position at the specified distance along the path</returns>
        public abstract Vector3 CalculatePointOnPath(List<Vector3> pathPoints, float normalizedDistance);

        /// <summary>
        /// Legacy method for backward compatibility
        /// </summary>
        public virtual Vector3 CalculatePoint(Vector3 start, Vector3 end, float normalizedDistance)
        {
            List<Vector3> points = new List<Vector3> { start, end };
            return CalculatePointOnPath(points, normalizedDistance);
        }

        /// <summary>
        /// Update the stored path points (for gizmo drawing)
        /// </summary>
        public virtual void UpdatePositions(Vector3 start, Vector3 end)
        {
            // Create a simple two-point path
            List<Vector3> points = new List<Vector3> { start, end };
            UpdatePath(points);
        }

        /// <summary>
        /// Update the stored path points (for gizmo drawing)
        /// </summary>
        public virtual void UpdatePath(List<Vector3> points)
        {
            pathPoints = new List<Vector3>(points);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showGizmos || !Application.isEditor || pathPoints.Count < 2)
                return;
                
            Gizmos.color = gizmoColor;
            
            // Draw the calculated path
            Vector3 prevPoint = CalculatePointOnPath(pathPoints, 0f);
            for (int i = 1; i <= gizmoPointCount; i++)
            {
                float t = i / (float)gizmoPointCount;
                Vector3 currentPoint = CalculatePointOnPath(pathPoints, t);
                Gizmos.DrawLine(prevPoint, currentPoint);
                prevPoint = currentPoint;
            }
        }
#endif
    }
}