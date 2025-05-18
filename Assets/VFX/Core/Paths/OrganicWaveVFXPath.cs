using UnityEngine;
using System.Collections.Generic;

namespace Jwho303.VFX
{
    public class OrganicWaveVFXPath : BaseVFXPath
    {
        [Header("Wave Settings")]
        [SerializeField] private float amplitude = 0.5f;
        [SerializeField] private float jaggedness = 0.3f;
        [SerializeField] private bool animate = true;

        [Header("Organic Motion")]
        [SerializeField] private float chaseSpeed = 3.0f;
        [SerializeField] private float targetMoveSpeed = 1.5f;
        [SerializeField] private float flowAmount = 0.7f;
        [SerializeField] private float microMotion = 0.2f;

        [Header("Wave Shape")]
        [SerializeField] private AnimationCurve amplitudeCurve;
        [SerializeField] private int detailLevel = 6;
        [SerializeField] private int seed = 0;
        [SerializeField] private bool adaptToPathLength = true;

        // Internal state
        private float timeOffset = 0f;
        private Vector2[] currentOffsets;
        private Vector2[] targetOffsets;
        private Vector2[] moveVelocity; // For smooth damping
        private System.Random random;

        private void Awake()
        {
            // Initialize amplitude curve if needed
            if (amplitudeCurve == null || amplitudeCurve.length == 0)
            {
                amplitudeCurve = new AnimationCurve();
                amplitudeCurve.AddKey(0f, 0.5f);
                amplitudeCurve.AddKey(0.5f, 1f);
                amplitudeCurve.AddKey(1f, 0.5f);
            }

            InitializeOffsets();
        }

        private void InitializeOffsets()
        {
            // Create the random generator with seed
            random = new System.Random(seed == 0 ? System.Environment.TickCount : seed);

            // Initialize offset arrays
            int segments = Mathf.Max(1, detailLevel);
            currentOffsets = new Vector2[segments + 1];
            targetOffsets = new Vector2[segments + 1];
            moveVelocity = new Vector2[segments + 1];

            // Generate initial offsets
            GenerateOffsets(currentOffsets);
            GenerateOffsets(targetOffsets);

            // Initialize velocity to zero
            for (int i = 0; i < moveVelocity.Length; i++)
            {
                moveVelocity[i] = Vector2.zero;
            }
        }

        private void GenerateOffsets(Vector2[] offsets)
        {
            int segments = offsets.Length - 1;

            // Ensure start and end have zero offset
            offsets[0] = Vector2.zero;
            offsets[segments] = Vector2.zero;

            // Generate smoother internal offsets for organic wave
            for (int i = 1; i < segments; i++)
            {
                // Position along the path (0-1)
                float position = i / (float)segments;

                // Get amplitude at this position
                float maxOffset = amplitude * amplitudeCurve.Evaluate(position);

                // More fluid direction pattern - less zigzag than lightning
                float angle;

                // Use noise-based pattern for organic feel
                float noiseParam = position * 5f + seed * 0.1f;
                angle = Mathf.PerlinNoise(noiseParam, noiseParam * 0.7f) * Mathf.PI * 2;

                // Apply jaggedness as variation to the angle
                angle += ((float)random.NextDouble() * 2f - 1f) * jaggedness * Mathf.PI * 0.5f;

                // Calculate offset with more y-variation for waves
                float xOffset = Mathf.Cos(angle) * maxOffset;
                float yOffset = Mathf.Sin(angle) * maxOffset * 0.6f; // More y-variation for waves

                offsets[i] = new Vector2(xOffset, yOffset);
            }
        }

        private void Update()
        {
            if (!animate) return;

            // Organic motion update
            timeOffset += Time.deltaTime;

            // Periodically move targets
            if (timeOffset >= targetMoveSpeed)
            {
                GenerateOffsets(targetOffsets);
                timeOffset = 0f;
            }

            // Update current positions to chase targets with smooth damping
            for (int i = 1; i < currentOffsets.Length - 1; i++)
            {
                // Calculate partially moved target that's between old and new position
                float lerpAmount = flowAmount; // How close we'll let the current get to target
                Vector2 currentTarget = Vector2.Lerp(currentOffsets[i], targetOffsets[i], lerpAmount);

                // Add oscillating micro-motion for organic feel
                float wobbleAmount = microMotion;
                float wobbleX = Mathf.Sin(Time.time * 3f + i * 1.3f) * wobbleAmount;
                float wobbleY = Mathf.Cos(Time.time * 2.5f + i * 0.9f) * wobbleAmount;
                currentTarget += new Vector2(wobbleX, wobbleY);

                // Smooth damp current position toward this partial target
                float smoothTime = 1f / chaseSpeed; // Lower = faster
                currentOffsets[i] = Vector2.SmoothDamp(
                    currentOffsets[i],
                    currentTarget,
                    ref moveVelocity[i],
                    smoothTime,
                    Mathf.Infinity,
                    Time.deltaTime
                );
            }

            // Keep endpoints fixed
            currentOffsets[0] = Vector2.zero;
            currentOffsets[currentOffsets.Length - 1] = Vector2.zero;
        }

        private void EnsureInitialized()
        {
            if (currentOffsets == null || targetOffsets == null ||
                currentOffsets.Length == 0 || targetOffsets.Length == 0)
            {
                InitializeOffsets();
            }

            // If detail level changed
            if (currentOffsets.Length != detailLevel + 1)
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
                Debug.LogWarning("Organic wave path requires at least 2 points");
                return Vector3.zero;
            }

            EnsureInitialized();

            // Always return exact start/end positions at 0 and 1
            if (normalizedDistance <= 0f) return pathPoints[0];
            if (normalizedDistance >= 1f) return pathPoints[pathPoints.Count - 1];

            // Get the base position along the path
            Vector3 basePosition = GetBasePathPosition(pathPoints, normalizedDistance);

            // Handle single segment case
            if (detailLevel <= 1)
            {
                return basePosition;
            }

            // Calculate the local path direction at the current point
            Vector3 pathDir = GetLocalPathDirection(pathPoints, normalizedDistance);
            Vector3 perpDir = new Vector3(-pathDir.y, pathDir.x, 0f).normalized;

            // Find which organic wave segment we're on
            float segmentPos = normalizedDistance * detailLevel;
            int segmentIndex = Mathf.FloorToInt(segmentPos);
            segmentIndex = Mathf.Clamp(segmentIndex, 0, detailLevel - 1);

            // Get segment progress
            float segmentProgress = segmentPos - segmentIndex;

            // Get interpolated offset from current points
            Vector2 finalOffset = GetInterpolatedOffset(segmentIndex, segmentProgress);

            // Calculate path scaling for amplitude
            float scale = 1.0f;
            if (adaptToPathLength)
            {
                float pathLength = CalculateApproxPathLength(pathPoints);
                scale = pathLength * 0.1f; // Scale waves based on path length
            }

            // Apply the offset - more balanced for waves
            Vector3 offset = perpDir * finalOffset.x * scale +
                            pathDir * finalOffset.y * scale * 0.3f;

            return basePosition + offset;
        }

        private Vector2 GetInterpolatedOffset(int segmentIndex, float progress)
        {
            Vector2 offset1 = currentOffsets[segmentIndex];
            Vector2 offset2 = currentOffsets[segmentIndex + 1];

            // Smoother interpolation for waves
            float t = SmoothStep(progress);
            return Vector2.Lerp(offset1, offset2, t);
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

        // Calculate local path direction at a specific point
        private Vector3 GetLocalPathDirection(List<Vector3> pathPoints, float normalizedDistance)
        {
            int segmentCount = pathPoints.Count - 1;
            float segmentDistance = normalizedDistance * segmentCount;
            int segmentIndex = Mathf.Min(Mathf.FloorToInt(segmentDistance), segmentCount - 1);

            return (pathPoints[segmentIndex + 1] - pathPoints[segmentIndex]).normalized;
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
                    amplitudeCurve.AddKey(0f, 0.5f);
                    amplitudeCurve.AddKey(0.5f, 1f);
                    amplitudeCurve.AddKey(1f, 0.5f);
                }

                EnsureInitialized();
            }
        }
    }
}