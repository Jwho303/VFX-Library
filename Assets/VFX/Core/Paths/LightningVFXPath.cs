using UnityEngine;
using System.Collections.Generic;

namespace Jwho303.VFX
{
    public class LightningVFXPath : BaseVFXPath
    {
        [Header("Lightning Settings")]
        [SerializeField] private float amplitude = 0.5f;
        [SerializeField] private float jaggedness = 0.5f;
        [SerializeField] private float strobeFrequency = 0.1f;
        [SerializeField] private bool animate = true;

        [Header("Lightning Shape")]
        [SerializeField] private AnimationCurve amplitudeCurve;
        [SerializeField] private int detailLevel = 6;
        [SerializeField] private int seed = 0;

        // Internal state
        private float timeOffset = 0f;
        private Vector2[] offsets;
        private System.Random random;

        private void Awake()
        {
            // Initialize amplitude curve if needed
            if (amplitudeCurve == null || amplitudeCurve.length == 0)
            {
                amplitudeCurve = new AnimationCurve();
                amplitudeCurve.AddKey(0f, 0.2f);
                amplitudeCurve.AddKey(0.5f, 1f);
                amplitudeCurve.AddKey(1f, 0.2f);
            }

            InitializeOffsets();
        }

        private void InitializeOffsets()
        {
            // Create the random generator with seed
            random = new System.Random(seed == 0 ? System.Environment.TickCount : seed);

            // Initialize offset array
            int segments = Mathf.Max(1, detailLevel);
            offsets = new Vector2[segments + 1];

            // Generate initial pattern
            GeneratePattern();
        }

        private void GeneratePattern()
        {
            int segments = offsets.Length - 1;

            // Ensure start and end have zero offset
            offsets[0] = Vector2.zero;
            offsets[segments] = Vector2.zero;

            // Generate internal segment offsets
            for (int i = 1; i < segments; i++)
            {
                // Position along the path (0-1)
                float position = i / (float)segments;

                // Get amplitude at this position
                float maxOffset = amplitude * amplitudeCurve.Evaluate(position);

                // Alternate direction for zigzag effect
                float angle;
                if (i % 2 == 0)
                {
                    // Even segments go one way with variation
                    angle = Mathf.PI * 0.5f + ((float)random.NextDouble() - 0.5f) * jaggedness * Mathf.PI * 0.8f;
                }
                else
                {
                    // Odd segments go the other way with variation
                    angle = Mathf.PI * 1.5f + ((float)random.NextDouble() - 0.5f) * jaggedness * Mathf.PI * 0.8f;
                }

                // Calculate offset
                float xOffset = Mathf.Cos(angle) * maxOffset;
                float yOffset = Mathf.Sin(angle) * maxOffset * 0.3f; // Limit y-offset to prevent knots

                offsets[i] = new Vector2(xOffset, yOffset);
            }
        }

        private void Update()
        {
            if (!animate) return;

            // Update strobe timing
            timeOffset += Time.deltaTime;

            if (timeOffset >= strobeFrequency)
            {
                // Time to switch to a new pattern
                GeneratePattern();
                timeOffset = 0f;
            }
        }

        private void EnsureInitialized()
        {
            if (offsets == null || offsets.Length == 0)
            {
                InitializeOffsets();
            }

            // If detail level changed
            if (offsets.Length != detailLevel + 1)
            {
                InitializeOffsets();
            }
        }

        public void SetSeed(int newSeed)
        {
            seed = newSeed;
            InitializeOffsets();
        }

        public override Vector3 CalculatePointOnPath(List<Vector3> pathPoints, float normalizedDistance)
        {
            if (pathPoints == null || pathPoints.Count < 2)
            {
                Debug.LogWarning("Lightning path requires at least 2 points");
                return Vector3.zero;
            }

            EnsureInitialized();

            // Always return exact start/end positions at 0 and 1
            if (normalizedDistance <= 0f) return pathPoints[0];
            if (normalizedDistance >= 1f) return pathPoints[pathPoints.Count - 1];

            // Follow the path with normalized distance
            Vector3 basePosition = GetBasePathPosition(pathPoints, normalizedDistance);

            // Handle single segment case
            if (detailLevel <= 1)
            {
                return basePosition;
            }

            // Calculate the path direction at the current point
            Vector3 pathDir = GetLocalPathDirection(pathPoints, normalizedDistance);
            Vector3 perpDir = new Vector3(-pathDir.y, pathDir.x, 0f).normalized;

            // Find which lightning segment we're on
            float segmentPos = normalizedDistance * detailLevel;
            int segmentIndex = Mathf.FloorToInt(segmentPos);
            segmentIndex = Mathf.Clamp(segmentIndex, 0, detailLevel - 1);

            // Get segment progress
            float segmentProgress = segmentPos - segmentIndex;

            // Get interpolated offset
            Vector2 finalOffset = GetInterpolatedOffset(segmentIndex, segmentProgress);

            // Calculate path length for proper scaling
            float pathLength = CalculateApproxPathLength(pathPoints);

            // Apply the offset
            Vector3 offset = perpDir * finalOffset.x * pathLength * 0.1f +  // Scale offset based on path length
                            pathDir * finalOffset.y * pathLength * 0.02f;   // Less offset along path direction

            return basePosition + offset;
        }

        private Vector2 GetInterpolatedOffset(int segmentIndex, float progress)
        {
            Vector2 offset1 = offsets[segmentIndex];
            Vector2 offset2 = offsets[segmentIndex + 1];

            // Use smoother interpolation for less jaggedness
            float smoothness = 1f - jaggedness * 0.8f;

            if (smoothness < 0.5f && (progress < 0.2f || progress > 0.8f))
            {
                // Sharper corners for high jaggedness
                return progress < 0.5f ? offset1 : offset2;
            }
            else
            {
                // Use smoothstep for smoother transitions
                float t = SmoothStep(progress);
                return Vector2.Lerp(offset1, offset2, t);
            }
        }

        // Calculate the position along the base path with multi-point support
        private Vector3 GetBasePathPosition(List<Vector3> pathPoints, float normalizedDistance)
        {
            int segmentCount = pathPoints.Count - 1;
            float segmentDistance = normalizedDistance * segmentCount;
            int segmentIndex = Mathf.Min(Mathf.FloorToInt(segmentDistance), segmentCount - 1);
            float segmentT = segmentDistance - segmentIndex;

            return Vector3.Lerp(pathPoints[segmentIndex], pathPoints[segmentIndex + 1], segmentT);
        }

        // Calculate the local path direction at a specific point
        private Vector3 GetLocalPathDirection(List<Vector3> pathPoints, float normalizedDistance)
        {
            int segmentCount = pathPoints.Count - 1;
            float segmentDistance = normalizedDistance * segmentCount;
            int segmentIndex = Mathf.Min(Mathf.FloorToInt(segmentDistance), segmentCount - 1);

            return (pathPoints[segmentIndex + 1] - pathPoints[segmentIndex]).normalized;
        }

        // Calculate approximate path length for scaling
        private float CalculateApproxPathLength(List<Vector3> pathPoints)
        {
            float length = 0;
            for (int i = 0; i < pathPoints.Count - 1; i++)
            {
                length += Vector3.Distance(pathPoints[i], pathPoints[i + 1]);
            }
            return length;
        }

        // Smooth step function for better interpolation
        private float SmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }

        // Editor support
        private void OnValidate()
        {
            if (Application.isPlaying && enabled)
            {
                // Initialize amplitude curve if needed
                if (amplitudeCurve == null || amplitudeCurve.length == 0)
                {
                    amplitudeCurve = new AnimationCurve();
                    amplitudeCurve.AddKey(0f, 0.2f);
                    amplitudeCurve.AddKey(0.5f, 1f);
                    amplitudeCurve.AddKey(1f, 0.2f);
                }

                EnsureInitialized();
            }
        }
    }
}