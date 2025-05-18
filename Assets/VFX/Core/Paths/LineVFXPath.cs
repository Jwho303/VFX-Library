using UnityEngine;
using System.Collections.Generic;

namespace Jwho303.VFX
{
    public class LineVFXPath : BaseVFXPath
    {
        [SerializeField] private bool smoothInterpolation = false;
        [SerializeField][Range(0f, 1f)] private float tension = 0.5f; // For catmull-rom interpolation

        public override Vector3 CalculatePointOnPath(List<Vector3> pathPoints, float normalizedDistance)
        {
            if (pathPoints == null || pathPoints.Count < 2)
            {
                Debug.LogWarning("Line path requires at least 2 points");
                return Vector3.zero;
            }

            // Always return exact start/end positions at 0 and 1
            if (normalizedDistance <= 0f) return pathPoints[0];
            if (normalizedDistance >= 1f) return pathPoints[pathPoints.Count - 1];

            // If we're using straight-line segments (no interpolation)
            if (!smoothInterpolation)
            {
                return CalculateSegmentedPosition(pathPoints, normalizedDistance);
            }
            else
            {
                // Use Catmull-Rom spline interpolation for smoother path
                return CalculateSplinePosition(pathPoints, normalizedDistance);
            }
        }

        private Vector3 CalculateSegmentedPosition(List<Vector3> pathPoints, float normalizedDistance)
        {
            int segmentCount = pathPoints.Count - 1;
            float segmentDistance = normalizedDistance * segmentCount;
            int segmentIndex = Mathf.Min(Mathf.FloorToInt(segmentDistance), segmentCount - 1);
            float segmentT = segmentDistance - segmentIndex;

            return Vector3.Lerp(pathPoints[segmentIndex], pathPoints[segmentIndex + 1], segmentT);
        }

        private Vector3 CalculateSplinePosition(List<Vector3> pathPoints, float normalizedDistance)
        {
            int segmentCount = pathPoints.Count - 1;
            float segmentDistance = normalizedDistance * segmentCount;
            int segmentIndex = Mathf.Min(Mathf.FloorToInt(segmentDistance), segmentCount - 1);
            float segmentT = segmentDistance - segmentIndex;

            // For Catmull-Rom spline, we need 4 points: p0, p1, p2, p3
            // Where the interpolation happens between p1 and p2
            Vector3 p0, p1, p2, p3;

            // Get the four points for interpolation
            p1 = pathPoints[segmentIndex];
            p2 = pathPoints[segmentIndex + 1];

            // For the endpoints, we need to handle special cases
            if (segmentIndex == 0)
            {
                // For first segment, duplicate first point or extrapolate
                p0 = p1 * 2 - p2; // Extrapolate backwards
            }
            else
            {
                p0 = pathPoints[segmentIndex - 1];
            }

            if (segmentIndex >= segmentCount - 1)
            {
                // For last segment, duplicate last point or extrapolate
                p3 = p2 * 2 - p1; // Extrapolate forwards
            }
            else
            {
                p3 = pathPoints[segmentIndex + 2];
            }

            // Calculate Catmull-Rom spline interpolation
            return CatmullRomPoint(p0, p1, p2, p3, segmentT);
        }

        // Catmull-Rom spline interpolation
        private Vector3 CatmullRomPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            // Convert tension parameter to alpha
            float alpha = 1f - tension;

            // Compute coefficients
            Vector3 a = -alpha * p0 + (2f - alpha) * p1 + (alpha - 2f) * p2 + alpha * p3;
            Vector3 b = 2f * alpha * p0 + (alpha - 3f) * p1 + (3f - 2f * alpha) * p2 - alpha * p3;
            Vector3 c = -alpha * p0 + alpha * p2;
            Vector3 d = p1;

            // Compute the position using the cubic polynomial
            return a * t * t * t + b * t * t + c * t + d;
        }
    }
}