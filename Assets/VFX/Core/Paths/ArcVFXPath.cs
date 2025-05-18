using UnityEngine;
using System.Collections.Generic;

namespace Jwho303.VFX
{
    public class ArcVFXPath : BaseVFXPath
    {
        [SerializeField] private float arcHeight = 1.0f;
        [SerializeField] private bool flipArc = false;
        [SerializeField] private bool useSingleArc = true;
        [SerializeField][Range(0f, 1f)] private float arcBias = 0.5f; // Controls where arc height reaches maximum

        public override Vector3 CalculatePointOnPath(List<Vector3> pathPoints, float normalizedDistance)
        {
            if (pathPoints == null || pathPoints.Count < 2)
            {
                Debug.LogWarning("Arc path requires at least 2 points");
                return Vector3.zero;
            }

            if (useSingleArc)
            {
                // Treat the entire path as a single arc from first to last point
                return CalculateSingleArc(pathPoints, normalizedDistance);
            }
            else
            {
                // Treat each segment as a separate arc
                return CalculateSegmentedArc(pathPoints, normalizedDistance);
            }
        }

        private Vector3 CalculateSingleArc(List<Vector3> pathPoints, float normalizedDistance)
        {
            // Get start and end of the entire path
            Vector3 start = pathPoints[0];
            Vector3 end = pathPoints[pathPoints.Count - 1];

            // Calculate position along straight line from start to end
            Vector3 linearPos = Vector3.Lerp(start, end, normalizedDistance);

            // Apply parabolic arc using normalized distance
            // A parabola with formula 4*t*(1-t) reaches 1 at t=0.5
            float t = normalizedDistance;
            float arcOffset;

            // Apply bias to adjust where the maximum arc height occurs
            if (arcBias != 0.5f)
            {
                // Remap t to create an asymmetric parabola
                float adjustedT;
                if (t < arcBias)
                {
                    // First half of the arc - remap t from [0, arcBias] to [0, 0.5]
                    adjustedT = t / (2 * arcBias);
                }
                else
                {
                    // Second half of the arc - remap t from [arcBias, 1] to [0.5, 1]
                    adjustedT = 0.5f + (t - arcBias) / (2 * (1 - arcBias));
                }

                // Apply standard parabola
                arcOffset = arcHeight * 4.0f * adjustedT * (1.0f - adjustedT);
            }
            else
            {
                // Standard symmetric parabola
                arcOffset = arcHeight * 4.0f * t * (1.0f - t);
            }

            // Flip the arc if needed
            if (flipArc)
            {
                arcOffset = -arcOffset;
            }

            // Calculate the up direction (perpendicular to path)
            Vector3 pathDirection = (end - start).normalized;
            Vector3 upDirection;

            // If path is mostly horizontal, arc up/down
            if (Mathf.Abs(pathDirection.y) < 0.707f) // ~45 degrees
            {
                upDirection = Vector3.up;
            }
            // If path is mostly vertical, arc left/right
            else
            {
                upDirection = Vector3.Cross(Vector3.forward, pathDirection).normalized;
            }

            // Apply arc offset in the up direction
            return linearPos + upDirection * arcOffset;
        }

        private Vector3 CalculateSegmentedArc(List<Vector3> pathPoints, float normalizedDistance)
        {
            int segmentCount = pathPoints.Count - 1;
            if (segmentCount < 1) return pathPoints[0];

            // Find which segment we're in
            float segmentDistance = normalizedDistance * segmentCount;
            int segmentIndex = Mathf.Min(Mathf.FloorToInt(segmentDistance), segmentCount - 1);
            float segmentT = segmentDistance - segmentIndex;

            // Get segment start and end
            Vector3 segmentStart = pathPoints[segmentIndex];
            Vector3 segmentEnd = pathPoints[segmentIndex + 1];

            // Calculate linear position along this segment
            Vector3 linearPos = Vector3.Lerp(segmentStart, segmentEnd, segmentT);

            // Calculate arc offset for this segment
            float arcOffset = arcHeight * 4.0f * segmentT * (1.0f - segmentT);
            if (flipArc) arcOffset = -arcOffset;

            // Calculate segment direction
            Vector3 segmentDirection = (segmentEnd - segmentStart).normalized;
            Vector3 upDirection;

            // If segment is mostly horizontal, arc up/down
            if (Mathf.Abs(segmentDirection.y) < 0.707f)
            {
                upDirection = Vector3.up;
            }
            // If segment is mostly vertical, arc left/right
            else
            {
                upDirection = Vector3.Cross(Vector3.forward, segmentDirection).normalized;
            }

            // Apply arc offset to linear position
            return linearPos + upDirection * arcOffset;
        }
    }
}