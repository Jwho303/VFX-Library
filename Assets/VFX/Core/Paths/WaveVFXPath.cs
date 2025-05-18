using UnityEngine;
using System.Collections.Generic;

namespace Jwho303.VFX
{
    public class WaveVFXPath : BaseVFXPath
    {
        [SerializeField] private float baseAmplitude = 0.5f;
        [SerializeField] private float frequency = 2.0f;
        [SerializeField] private float phaseOffset = 0f;
        [SerializeField] private float animationSpeed = 0f;
        [SerializeField] private bool horizontalWave = false;

        [SerializeField] private AnimationCurve amplitudeModulation = AnimationCurve.Linear(0, 1, 1, 1);

        [Header("Multi-Point Path Options")]
        [SerializeField] private bool adaptToPathSegments = true;
        [SerializeField] private bool scaleFrequencyWithPathLength = true;
        [SerializeField] private bool continuousWavePhase = true;

        private float timeOffset = 0f;

        private void Update()
        {
            if (animationSpeed != 0f)
            {
                timeOffset += Time.deltaTime * animationSpeed;

                // Keep the timeOffset within a reasonable range to prevent floating point issues
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
                Debug.LogWarning("Wave path requires at least 2 points");
                return Vector3.zero;
            }

            // Get base position along the path
            Vector3 basePosition = GetBasePathPosition(pathPoints, normalizedDistance);

            // Get local path direction at this point
            Vector3 pathDir = GetLocalPathDirection(pathPoints, normalizedDistance);

            // Create perpendicular vector based on wave orientation
            Vector3 waveDir;
            if (horizontalWave)
            {
                // Horizontal wave perpendicular to path direction in XY plane
                waveDir = new Vector3(-pathDir.y, pathDir.x, 0f).normalized;
            }
            else
            {
                // Vertical wave (default up direction)
                waveDir = Vector3.up;
            }

            // Apply amplitude modulation based on the curve
            float modulation = amplitudeModulation.Evaluate(normalizedDistance);

            // Scale amplitude and frequency with path length if requested
            float adjustedAmplitude = baseAmplitude;
            float adjustedFrequency = frequency;

            if (adaptToPathSegments)
            {
                float pathLength = CalculateApproxPathLength(pathPoints);
                adjustedAmplitude *= Mathf.Min(1.0f, pathLength * 0.2f);

                if (scaleFrequencyWithPathLength)
                {
                    // Scale frequency to maintain consistent wavelength relative to path length
                    float baseDist = Vector3.Distance(pathPoints[0], pathPoints[pathPoints.Count - 1]);
                    adjustedFrequency = frequency * (pathLength / Mathf.Max(0.001f, baseDist));
                }
            }

            // Calculate wave phase based on path configuration
            float phase;
            if (continuousWavePhase)
            {
                // Continuous phase along entire path (wave flows through segments)
                float distanceTraveled = GetDistanceTraveledAtPoint(pathPoints, normalizedDistance);
                float totalLength = CalculateApproxPathLength(pathPoints);
                phase = (distanceTraveled / totalLength) * adjustedFrequency * Mathf.PI * 2 + phaseOffset + timeOffset;
            }
            else
            {
                // Simple phase based on normalized distance
                phase = normalizedDistance * adjustedFrequency * Mathf.PI * 2 + phaseOffset + timeOffset;
            }

            // Calculate wave offset using sine function with modulated amplitude
            float waveOffset = adjustedAmplitude * modulation * Mathf.Sin(phase);

            // Apply the offset in the wave direction
            return basePosition + waveDir * waveOffset;
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

        // Calculate the actual distance traveled at this point (for continuous wave)
        private float GetDistanceTraveledAtPoint(List<Vector3> pathPoints, float normalizedDistance)
        {
            int segmentCount = pathPoints.Count - 1;
            float segmentDistance = normalizedDistance * segmentCount;
            int segmentIndex = Mathf.Min(Mathf.FloorToInt(segmentDistance), segmentCount - 1);
            float segmentT = segmentDistance - segmentIndex;

            // Calculate distance up to the current segment
            float distanceTraveled = 0;
            for (int i = 0; i < segmentIndex; i++)
            {
                distanceTraveled += Vector3.Distance(pathPoints[i], pathPoints[i + 1]);
            }

            // Add portion of current segment
            distanceTraveled += Vector3.Distance(pathPoints[segmentIndex], pathPoints[segmentIndex + 1]) * segmentT;

            return distanceTraveled;
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