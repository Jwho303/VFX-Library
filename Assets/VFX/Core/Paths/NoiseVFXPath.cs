using UnityEngine;
using System.Collections.Generic;

namespace Jwho303.VFX
{
    public class NoiseVFXPath : BaseVFXPath
    {
        [Header("Noise Strength")]
        [SerializeField] private float noiseAmountX = 0.5f;
        [SerializeField] private float noiseAmountY = 0.5f;
        [SerializeField] private float noiseAmountZ = 0.0f;  // Z-axis noise (depth in 2D)

        [Header("Noise Settings")]
        [SerializeField] private float noiseScale = 1.0f;
        [SerializeField] private float noiseSpeed = 0.5f;
        [SerializeField] private AnimationCurve noiseAmplitudeCurve = AnimationCurve.Linear(0, 1, 1, 1);
        [SerializeField] private bool adaptToPathSegments = true;

        [Header("Advanced Noise")]
        [SerializeField] private int octaves = 2;
        [SerializeField] private float persistence = 0.5f;
        [SerializeField] private int seed = 0;

        private float timeOffset = 0f;
        private System.Random random;

        private void Awake()
        {
            // Initialize random with seed
            random = new System.Random(seed == 0 ? System.Environment.TickCount : seed);

            // Validate curve
            if (noiseAmplitudeCurve == null || noiseAmplitudeCurve.length == 0)
            {
                noiseAmplitudeCurve = AnimationCurve.Linear(0, 1, 1, 1);
            }
        }

        private void Update()
        {
            timeOffset += Time.deltaTime * noiseSpeed;

            // Keep the timeOffset within a reasonable range
            if (Mathf.Abs(timeOffset) > 1000f)
            {
                timeOffset = timeOffset % 1000f;
            }
        }

        public override Vector3 CalculatePointOnPath(List<Vector3> pathPoints, float normalizedDistance)
        {
            if (pathPoints == null || pathPoints.Count < 2)
            {
                Debug.LogWarning("Noise path requires at least 2 points");
                return Vector3.zero;
            }

            // Get base position along the path
            Vector3 basePosition = GetBasePathPosition(pathPoints, normalizedDistance);

            // Calculate local path direction at this point
            Vector3 pathDir = GetLocalPathDirection(pathPoints, normalizedDistance);

            // Create perpendicular vector for proper noise orientation
            // This is the "right" vector perpendicular to path direction in 2D (X axis in local space)
            Vector3 rightDir = new Vector3(-pathDir.y, pathDir.x, 0f).normalized;

            // The "up" vector (Y axis in local space) - in 2D this is perpendicular to the path and right vector
            Vector3 upDir = Vector3.Cross(pathDir, rightDir).normalized;

            // Apply amplitude modulation based on position along path
            float amplitudeMultiplier = noiseAmplitudeCurve.Evaluate(normalizedDistance);

            // Calculate unique noise sampling coordinates
            // Use different seeds/offsets for each axis to ensure independent noise
            float noiseX = SampleOctavePerlin(
                normalizedDistance * noiseScale + timeOffset,
                0.5f,
                seed * 0.01f) * 2f - 1f;

            float noiseY = SampleOctavePerlin(
                normalizedDistance * noiseScale,
                0.5f + timeOffset,
                seed * 0.01f + 10f) * 2f - 1f;

            float noiseZ = SampleOctavePerlin(
                normalizedDistance * noiseScale + 0.7f,
                timeOffset * 0.7f,
                seed * 0.01f + 20f) * 2f - 1f;

            // Scale for path length
            float pathLength = CalculateApproxPathLength(pathPoints);
            float scale = adaptToPathSegments ? pathLength * 0.1f : 1.0f;

            // Create offset vector with CORRECT axis assignment:
            // - noiseX is applied to the rightDir (local X axis)
            // - noiseY is applied to the upDir (local Y axis)
            // - noiseZ is applied to the pathDir (local Z/forward axis)
            Vector3 noiseOffset =
                rightDir * noiseX * noiseAmountX * amplitudeMultiplier * scale +
                upDir * noiseY * noiseAmountY * amplitudeMultiplier * scale +
                pathDir * noiseZ * noiseAmountZ * amplitudeMultiplier * scale;

            // Add noise offset to base position
            return basePosition + noiseOffset;
        }

        // Sample Perlin noise with multiple octaves for more natural patterns
        private float SampleOctavePerlin(float x, float y, float z)
        {
            float total = 0;
            float frequency = 1;
            float amplitude = 1;
            float maxValue = 0;

            for (int i = 0; i < octaves; i++)
            {
                // Use different combinations of coordinates for varied patterns
                float noiseValue = Mathf.PerlinNoise(
                    x * frequency + z,
                    y * frequency + i * 0.1f
                );

                total += noiseValue * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= 2;
            }

            return total / maxValue;
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

        // Calculate path direction at the current point for proper perpendicular alignment
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

        public void SetSeed(int newSeed)
        {
            seed = newSeed;
            random = new System.Random(seed);
        }

        // Editor support
        private void OnValidate()
        {
            // Ensure curve isn't null
            if (noiseAmplitudeCurve == null || noiseAmplitudeCurve.length == 0)
            {
                noiseAmplitudeCurve = AnimationCurve.Linear(0, 1, 1, 1);
            }
        }
    }
}