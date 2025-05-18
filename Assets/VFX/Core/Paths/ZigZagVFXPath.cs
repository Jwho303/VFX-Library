using UnityEngine;
using System.Collections.Generic;

namespace Jwho303.VFX
{
    public class ZigZagVFXPath : BaseVFXPath
    {
        [SerializeField] private float amplitude = 0.5f;
        [SerializeField] private int segments = 5;
        [SerializeField] private AnimationCurve amplitudeCurve = AnimationCurve.Linear(0, 1, 1, 1);
        [SerializeField] private bool alignWithDirection = true;
        [SerializeField] private float randomness = 0.2f;

        [Header("Multi-Point Path Options")]
        [SerializeField] private bool adaptiveSegmentCount = true;
        [SerializeField] private bool adaptAmplitudeToPathLength = true;
        [SerializeField] private float segmentsPerUnit = 1.0f;
        [SerializeField] private bool useGlobalZigZag = true;

        private Vector2[] offsets;
        private System.Random random;
        private int seed;
        private float cachedPathLength;

        private void Awake()
        {
            InitializeOffsets();
        }

        private void InitializeOffsets()
        {
            random = new System.Random(seed);

            int zigzagSegments = segments;
            if (adaptiveSegmentCount && pathPoints.Count >= 2)
            {
                // Calculate segments based on path length
                float pathLength = CalculateApproxPathLength(pathPoints);
                zigzagSegments = Mathf.Max(2, Mathf.RoundToInt(pathLength * segmentsPerUnit));
                cachedPathLength = pathLength;
            }

            offsets = new Vector2[zigzagSegments + 1];

            // Ensure start and end points have zero offset
            offsets[0] = Vector2.zero;
            offsets[zigzagSegments] = Vector2.zero;

            // Create alternating zig-zag pattern
            for (int i = 1; i < zigzagSegments; i++)
            {
                float direction = (i % 2 == 0) ? 1 : -1;
                float randomOffset = (float)random.NextDouble() * randomness * 2 - randomness;
                offsets[i] = new Vector2(0, direction * (1f + randomOffset));
            }
        }

        public override Vector3 CalculatePointOnPath(List<Vector3> pathPoints, float normalizedDistance)
        {
            if (pathPoints == null || pathPoints.Count < 2)
            {
                Debug.LogWarning("ZigZag path requires at least 2 points");
                return Vector3.zero;
            }

            // Check if path length changed significantly and we need to reinitialize
            if (adaptiveSegmentCount)
            {
                float pathLength = CalculateApproxPathLength(pathPoints);
                if (Mathf.Abs(pathLength - cachedPathLength) > 0.5f)
                {
                    InitializeOffsets();
                }
            }

            // Make sure offsets are initialized
            if (offsets == null || offsets.Length < 2)
            {
                InitializeOffsets();
            }

            if (useGlobalZigZag)
            {
                return CalculateGlobalZigZag(pathPoints, normalizedDistance);
            }
            else
            {
                return CalculateSegmentedZigZag(pathPoints, normalizedDistance);
            }
        }

        private Vector3 CalculateGlobalZigZag(List<Vector3> pathPoints, float normalizedDistance)
        {
            // Calculate base linear position
            Vector3 basePosition = GetBasePathPosition(pathPoints, normalizedDistance);

            // Calculate global path direction (from first to last point)
            Vector3 globalDir = (pathPoints[pathPoints.Count - 1] - pathPoints[0]).normalized;

            // Find which zigzag segment we're in
            float zigzagPos = normalizedDistance * (offsets.Length - 1);
            int zigzagIndex = Mathf.FloorToInt(zigzagPos);
            zigzagIndex = Mathf.Clamp(zigzagIndex, 0, offsets.Length - 2);
            float zigzagProgress = zigzagPos - zigzagIndex;

            // Calculate zigzag direction vector
            Vector3 perpDir;
            if (alignWithDirection)
            {
                perpDir = new Vector3(-globalDir.y, globalDir.x, 0).normalized;
            }
            else
            {
                perpDir = Vector3.up;
            }

            // Get the current offset by interpolating between segment points
            Vector2 currentOffset = Vector2.Lerp(offsets[zigzagIndex], offsets[zigzagIndex + 1], zigzagProgress);

            // Apply amplitude modulation
            float currentAmplitude = amplitude * amplitudeCurve.Evaluate(normalizedDistance);

            // Scale amplitude with path length if requested
            if (adaptAmplitudeToPathLength)
            {
                float pathLength = CalculateApproxPathLength(pathPoints);
                currentAmplitude *= Mathf.Min(1.0f, pathLength * 0.1f);
            }

            // Calculate final position with zigzag offset
            Vector3 offset = perpDir * currentOffset.y * currentAmplitude;

            return basePosition + offset;
        }

        private Vector3 CalculateSegmentedZigZag(List<Vector3> pathPoints, float normalizedDistance)
        {
            // Find which path segment we're on
            int segmentCount = pathPoints.Count - 1;
            float segmentDistance = normalizedDistance * segmentCount;
            int segmentIndex = Mathf.Min(Mathf.FloorToInt(segmentDistance), segmentCount - 1);
            float segmentT = segmentDistance - segmentIndex;

            // Get current segment start and end
            Vector3 segmentStart = pathPoints[segmentIndex];
            Vector3 segmentEnd = pathPoints[segmentIndex + 1];

            // Calculate linear position along this segment
            Vector3 linearPos = Vector3.Lerp(segmentStart, segmentEnd, segmentT);

            // Calculate segment direction for proper zigzag orientation
            Vector3 segmentDir = (segmentEnd - segmentStart).normalized;

            // Calculate the normalized segment progress (0-1) for the zigzag pattern
            int zigzagSegmentsPerPathSegment = Mathf.Max(2, (offsets.Length - 1) / segmentCount);
            float zigzagSegmentT = segmentT * zigzagSegmentsPerPathSegment;
            int zigzagSubSegment = Mathf.FloorToInt(zigzagSegmentT);
            float zigzagSubProgress = zigzagSegmentT - zigzagSubSegment;

            // Calculate the actual zigzag index based on path segment
            int zigzagBaseIndex = segmentIndex * zigzagSegmentsPerPathSegment;
            int zigzagIndex = Mathf.Min(zigzagBaseIndex + zigzagSubSegment, offsets.Length - 2);

            // Get zigzag direction
            Vector3 perpDir;
            if (alignWithDirection)
            {
                perpDir = new Vector3(-segmentDir.y, segmentDir.x, 0).normalized;
            }
            else
            {
                perpDir = Vector3.up;
            }

            // Get the current offset by interpolating between zigzag points
            Vector2 offset1 = offsets[zigzagIndex];
            Vector2 offset2 = offsets[zigzagIndex + 1];
            Vector2 currentOffset = Vector2.Lerp(offset1, offset2, zigzagSubProgress);

            // Apply amplitude modulation
            float segmentNormalized = (float)segmentIndex / segmentCount + segmentT / segmentCount;
            float currentAmplitude = amplitude * amplitudeCurve.Evaluate(segmentNormalized);

            // Scale amplitude with segment length if requested
            if (adaptAmplitudeToPathLength)
            {
                float segmentLength = Vector3.Distance(segmentStart, segmentEnd);
                currentAmplitude *= Mathf.Min(1.0f, segmentLength * 0.5f);
            }

            // Calculate final position with zigzag offset
            Vector3 zigzagOffset = perpDir * currentOffset.y * currentAmplitude;

            return linearPos + zigzagOffset;
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

        public void SetSeed(int newSeed)
        {
            seed = newSeed;
            InitializeOffsets();
        }

        public override void UpdatePath(List<Vector3> points)
        {
            base.UpdatePath(points);

            // Check if we need to reinitialize offsets with new path
            if (adaptiveSegmentCount && points.Count >= 2)
            {
                float pathLength = CalculateApproxPathLength(points);
                if (Mathf.Abs(pathLength - cachedPathLength) > 0.5f)
                {
                    InitializeOffsets();
                }
            }
        }
    }
}