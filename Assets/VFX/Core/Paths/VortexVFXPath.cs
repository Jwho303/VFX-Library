using UnityEngine;
using System.Collections.Generic;

namespace Jwho303.VFX
{
    public class VortexVFXPath : BaseVFXPath
    {
        [SerializeField] private float startRadius = 1.0f;
        [SerializeField] private float endRadius = 0.2f;
        [SerializeField] private float rotations = 3.0f;
        [SerializeField] private bool clockwise = true;
        [SerializeField] private AnimationCurve radiusCurve;
        [SerializeField] private float animationSpeed = 1.0f;

        [Header("Multi-Point Path Options")]
        [SerializeField] private bool applyVortexAlongEntirePath = true;
        [SerializeField] private bool adaptRotationsToPathLength = true;

        private float timeOffset = 0f;

        private void OnValidate()
        {
            if (radiusCurve == null || radiusCurve.length == 0)
            {
                radiusCurve = AnimationCurve.Linear(0, 1, 1, 0);
            }
        }

        private void Update()
        {
            if (animationSpeed != 0f)
            {
                timeOffset += Time.deltaTime * animationSpeed;
                if (Mathf.Abs(timeOffset) > 1000f)
                {
                    timeOffset = timeOffset % 1000f;
                }
            }
        }

        public override Vector3 CalculatePointOnPath(List<Vector3> pathPoints, float normalizedDistance)
        {
            if (pathPoints == null || pathPoints.Count < 2)
            {
                Debug.LogWarning("Vortex path requires at least 2 points");
                return Vector3.zero;
            }

            if (applyVortexAlongEntirePath)
            {
                return CalculateGlobalVortex(pathPoints, normalizedDistance);
            }
            else
            {
                return CalculateSegmentedVortex(pathPoints, normalizedDistance);
            }
        }

        private Vector3 CalculateGlobalVortex(List<Vector3> pathPoints, float normalizedDistance)
        {
            // Calculate base position along the path
            Vector3 basePosition = GetBasePathPosition(pathPoints, normalizedDistance);

            // Calculate global path direction from first to last point
            Vector3 globalDirection = (pathPoints[pathPoints.Count - 1] - pathPoints[0]).normalized;

            // Calculate radius with curve modulation
            float radiusMultiplier = radiusCurve.Evaluate(normalizedDistance);
            float currentRadius = Mathf.Lerp(startRadius, endRadius, normalizedDistance) * radiusMultiplier;

            // Calculate total path length for rotation scaling if needed
            float totalRotations = rotations;
            if (adaptRotationsToPathLength)
            {
                float pathLength = CalculateApproxPathLength(pathPoints);
                float basePathLength = Vector3.Distance(pathPoints[0], pathPoints[pathPoints.Count - 1]);
                totalRotations = rotations * (pathLength / basePathLength);
            }

            // Calculate rotation angle with animation
            float angle = normalizedDistance * totalRotations * Mathf.PI * 2f + timeOffset;
            if (!clockwise) angle = -angle;

            // Calculate perpendicular vectors for circular motion
            Vector3 right = Vector3.Cross(Vector3.up, globalDirection).normalized;
            if (right.magnitude < 0.001f)
            {
                right = Vector3.Cross(Vector3.forward, globalDirection).normalized;
            }
            Vector3 up = Vector3.Cross(globalDirection, right).normalized;

            // Apply circular offset
            Vector3 offset = right * Mathf.Cos(angle) * currentRadius +
                             up * Mathf.Sin(angle) * currentRadius;

            return basePosition + offset;
        }

        private Vector3 CalculateSegmentedVortex(List<Vector3> pathPoints, float normalizedDistance)
        {
            // Find which segment we're on
            int segmentCount = pathPoints.Count - 1;
            float segmentDistance = normalizedDistance * segmentCount;
            int segmentIndex = Mathf.Min(Mathf.FloorToInt(segmentDistance), segmentCount - 1);
            float segmentT = segmentDistance - segmentIndex;

            // Get current segment start and end
            Vector3 segmentStart = pathPoints[segmentIndex];
            Vector3 segmentEnd = pathPoints[segmentIndex + 1];

            // Calculate segment direction
            Vector3 segmentDirection = (segmentEnd - segmentStart).normalized;

            // Calculate base position along this segment
            Vector3 basePosition = Vector3.Lerp(segmentStart, segmentEnd, segmentT);

            // Calculate segment-specific radius
            float segmentNormalized = (float)segmentIndex / segmentCount + segmentT / segmentCount;
            float radiusMultiplier = radiusCurve.Evaluate(segmentNormalized);
            float currentRadius = Mathf.Lerp(startRadius, endRadius, segmentNormalized) * radiusMultiplier;

            // Calculate segment-specific rotation
            float segmentLength = Vector3.Distance(segmentStart, segmentEnd);
            float segmentRotations = rotations * segmentLength / CalculateApproxPathLength(pathPoints) * segmentCount;

            // Apply rotation offset
            float angle = segmentT * segmentRotations * Mathf.PI * 2f + timeOffset;
            if (!clockwise) angle = -angle;

            // Calculate perpendicular vectors for this segment
            Vector3 right = Vector3.Cross(Vector3.up, segmentDirection).normalized;
            if (right.magnitude < 0.001f)
            {
                right = Vector3.Cross(Vector3.forward, segmentDirection).normalized;
            }
            Vector3 up = Vector3.Cross(segmentDirection, right).normalized;

            // Apply circular offset for this segment
            Vector3 offset = right * Mathf.Cos(angle) * currentRadius +
                             up * Mathf.Sin(angle) * currentRadius;

            return basePosition + offset;
        }

        // Calculate base position along a multi-point path
        private Vector3 GetBasePathPosition(List<Vector3> pathPoints, float normalizedDistance)
        {
            int segmentCount = pathPoints.Count - 1;
            float segmentDistance = normalizedDistance * segmentCount;
            int segmentIndex = Mathf.Min(Mathf.FloorToInt(segmentDistance), segmentCount - 1);
            float segmentT = segmentDistance - segmentIndex;

            return Vector3.Lerp(pathPoints[segmentIndex], pathPoints[segmentIndex + 1], segmentT);
        }

        // Calculate approximate path length for proper scaling
        private float CalculateApproxPathLength(List<Vector3> pathPoints)
        {
            float length = 0;
            for (int i = 0; i < pathPoints.Count - 1; i++)
            {
                length += Vector3.Distance(pathPoints[i], pathPoints[i + 1]);
            }
            return length;
        }
    }
}