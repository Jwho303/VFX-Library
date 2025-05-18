using UnityEngine;
using System.Collections.Generic;

namespace Jwho303.VFX
{
    public class BounceVFXPath : BaseVFXPath
    {
        [SerializeField] private float bounceHeight = 1.0f;
        [SerializeField] private int bounceCount = 3;
        [SerializeField] private float bounceDamping = 0.5f;
        [SerializeField] private bool flipBounce = false;
        [SerializeField] private AnimationCurve bounceCurve;
        [SerializeField] private bool useSegmentedBounce = false;

        private void OnValidate()
        {
            if (bounceCurve == null || bounceCurve.length == 0)
            {
                bounceCurve = new AnimationCurve();
                Keyframe[] keys = new Keyframe[3];
                keys[0] = new Keyframe(0, 0, 0, 2);
                keys[1] = new Keyframe(0.5f, 1, 0, 0);
                keys[2] = new Keyframe(1, 0, -2, 0);
                bounceCurve.keys = keys;
            }
        }

        public override Vector3 CalculatePointOnPath(List<Vector3> pathPoints, float normalizedDistance)
        {
            if (pathPoints == null || pathPoints.Count < 2)
            {
                Debug.LogWarning("Bounce path requires at least 2 points");
                return Vector3.zero;
            }

            if (useSegmentedBounce)
            {
                return CalculateSegmentedBounce(pathPoints, normalizedDistance);
            }
            else
            {
                return CalculateGlobalBounce(pathPoints, normalizedDistance);
            }
        }

        private Vector3 CalculateGlobalBounce(List<Vector3> pathPoints, float normalizedDistance)
        {
            // Follow the path with normalized distance
            Vector3 basePosition = GetBasePathPosition(pathPoints, normalizedDistance);

            // Determine which bounce we're on
            float bounceSectionLength = 1f / bounceCount;
            int currentBounce = Mathf.FloorToInt(normalizedDistance / bounceSectionLength);
            float bounceProgress = (normalizedDistance - currentBounce * bounceSectionLength) / bounceSectionLength;

            // Apply damping based on which bounce we're on
            float dampedHeight = bounceHeight * Mathf.Pow(bounceDamping, currentBounce);

            // Get height from the bounce curve
            float heightOffset = bounceCurve.Evaluate(bounceProgress) * dampedHeight;

            // Flip the direction if needed
            if (flipBounce)
            {
                heightOffset = -heightOffset;
            }

            // Calculate the global path direction
            Vector3 startPoint = pathPoints[0];
            Vector3 endPoint = pathPoints[pathPoints.Count - 1];
            Vector3 globalDirection = (endPoint - startPoint).normalized;

            // Determine the up direction for bounce
            Vector3 upDirection;
            if (Mathf.Abs(globalDirection.y) < 0.707f) // If path is mostly horizontal
            {
                upDirection = Vector3.up;
            }
            else // If path is mostly vertical
            {
                upDirection = Vector3.Cross(Vector3.forward, globalDirection).normalized;
            }

            return basePosition + upDirection * heightOffset;
        }

        private Vector3 CalculateSegmentedBounce(List<Vector3> pathPoints, float normalizedDistance)
        {
            // Find which segment of the path we're on
            int segmentCount = pathPoints.Count - 1;
            float segmentDistance = normalizedDistance * segmentCount;
            int segmentIndex = Mathf.Min(Mathf.FloorToInt(segmentDistance), segmentCount - 1);
            float segmentT = segmentDistance - segmentIndex;

            // Get the linear position along this segment
            Vector3 start = pathPoints[segmentIndex];
            Vector3 end = pathPoints[segmentIndex + 1];
            Vector3 linearPos = Vector3.Lerp(start, end, segmentT);

            // Calculate bounces within this segment
            float bounceLength = 1f / bounceCount;
            int currentBounce = Mathf.FloorToInt(segmentT / bounceLength);
            float bounceProgress = (segmentT - currentBounce * bounceLength) / bounceLength;

            // Apply damping
            float dampedHeight = bounceHeight * Mathf.Pow(bounceDamping, currentBounce);

            // Get height from curve
            float heightOffset = bounceCurve.Evaluate(bounceProgress) * dampedHeight;
            if (flipBounce) heightOffset = -heightOffset;

            // Calculate segment direction for proper bounce orientation
            Vector3 segmentDirection = (end - start).normalized;
            Vector3 upDirection;

            if (Mathf.Abs(segmentDirection.y) < 0.707f) // If segment is mostly horizontal
            {
                upDirection = Vector3.up;
            }
            else // If segment is mostly vertical
            {
                upDirection = Vector3.Cross(Vector3.forward, segmentDirection).normalized;
            }

            return linearPos + upDirection * heightOffset;
        }

        // Helper to get a position along the base path (without bounce)
        private Vector3 GetBasePathPosition(List<Vector3> pathPoints, float normalizedDistance)
        {
            int segmentCount = pathPoints.Count - 1;
            float segmentDistance = normalizedDistance * segmentCount;
            int segmentIndex = Mathf.Min(Mathf.FloorToInt(segmentDistance), segmentCount - 1);
            float segmentT = segmentDistance - segmentIndex;

            return Vector3.Lerp(pathPoints[segmentIndex], pathPoints[segmentIndex + 1], segmentT);
        }
    }
}