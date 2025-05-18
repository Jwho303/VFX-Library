using UnityEngine;
using System.Collections.Generic;

namespace Jwho303.VFX
{
    [RequireComponent(typeof(ParticleSystem))]
    public class ParticleElement : BaseVFXElement
    {
        [Header("Particle System Settings")]
        [SerializeField] protected ParticleSystem particleSystem; // Use 'new' to explicitly hide inherited field
        [SerializeField] private BaseVFXPath pathCalculator;

        [Header("Movement Settings")]
        [SerializeField] private bool moveEmitterToStart = true;
        [SerializeField] private float particleSpeed = 5f;
        [SerializeField] private float speedVariation = 0.2f; // 0-1 range for variation percentage
        [SerializeField] private bool synchronizeLifetimeWithPath = true;

        [Header("Space Settings")]
        [SerializeField] private bool useWorldSpace = true;
        [SerializeField] private Transform targetTransform;

        [Header("Scatter Settings")]
        [SerializeField] private bool useScatter = true;
        [SerializeField] private bool applyScatterInNoise = false;

        [Header("X Scatter (Horizontal)")]
        [SerializeField] private float scatterXAmount = 0.2f;
        [SerializeField] private AnimationCurve scatterXOverLifetime = AnimationCurve.Linear(0, 1, 1, 1);

        [Header("Y Scatter (Vertical)")]
        [SerializeField] private float scatterYAmount = 0.2f;
        [SerializeField] private AnimationCurve scatterYOverLifetime = AnimationCurve.Linear(0, 1, 1, 1);

        [Header("Z Scatter (Depth)")]
        [SerializeField] private float scatterZAmount = 0.0f;  // Default to 0 since depth is less common in 2D
        [SerializeField] private AnimationCurve scatterZOverLifetime = AnimationCurve.Linear(0, 1, 1, 1);

        [Header("Noise Settings")]
        [SerializeField] private float noiseFrequency = 1.0f;
        [SerializeField] private int noiseSeed = 0;  // Added seed for consistent noise patterns

        private float pathLength;
        private ParticleSystem.Particle[] particleArray;
        private ParticleSystem.MainModule mainModule;
        private ParticleSystem.EmissionModule emissionModule;
        private ParticleSystem.NoiseModule noiseModule;
        private System.Random random;  // Added for seeded randomness

        public override void Initialize()
        {
            if (isInitialized) return;

            // Initialize random generator with seed
            random = new System.Random(noiseSeed);

            // Get or create particle system
            if (particleSystem == null)
            {
                particleSystem = GetComponent<ParticleSystem>();
                if (particleSystem == null)
                {
                    particleSystem = gameObject.AddComponent<ParticleSystem>();
                }
            }

            // Make sure path calculator is valid
            if (pathCalculator == null)
            {
                Debug.LogError("No path calculator assigned to ParticleElement.");
            }

            // Set target transform if not assigned
            if (targetTransform == null)
            {
                targetTransform = transform;
            }

            // Cache modules for faster access
            mainModule = particleSystem.main;
            emissionModule = particleSystem.emission;
            noiseModule = particleSystem.noise;

            // Configure world space setting
            mainModule.simulationSpace = useWorldSpace ?
                ParticleSystemSimulationSpace.World :
                ParticleSystemSimulationSpace.Local;

            // Validate curves
            ValidateCurves();

            base.Initialize();
        }

        private void ValidateCurves()
        {
            // Ensure curves aren't null
            if (scatterXOverLifetime == null || scatterXOverLifetime.length == 0)
            {
                scatterXOverLifetime = AnimationCurve.Linear(0, 1, 1, 1);
            }

            if (scatterYOverLifetime == null || scatterYOverLifetime.length == 0)
            {
                scatterYOverLifetime = AnimationCurve.Linear(0, 1, 1, 1);
            }

            if (scatterZOverLifetime == null || scatterZOverLifetime.length == 0)
            {
                scatterZOverLifetime = AnimationCurve.Linear(0, 1, 1, 1);
            }
        }

        public override void UpdatePath(List<Vector3> pathPoints)
        {
            if (!isInitialized)
            {
                Initialize();
            }

            // Update our stored path
            currentPathPoints = new List<Vector3>(pathPoints);

            // Calculate path length (approximate)
            pathLength = CalculateApproximatePathLength(pathPoints);

            // Update path for gizmo drawing
            pathCalculator.UpdatePath(pathPoints);

            // Move emitter to start point if configured
            if (moveEmitterToStart && pathPoints.Count > 0)
            {
                targetTransform.position = pathPoints[0];
            }

            // Apply scatter via noise module if enabled
            UpdateNoiseModule();

            // Update particle lifetime based on path length and speed if synchronizing
            UpdateParticleLifetime();

            // Update particle positions - moved from LateUpdate
            UpdateParticlePositions();
        }

        // Implement old method to ensure proper compatibility
        public new void UpdatePositions(Vector3 start, Vector3 end)
        {
            // Create a simple path with two points and update
            List<Vector3> simplePath = new List<Vector3> { start, end };
            UpdatePath(simplePath);
        }

        private void UpdateNoiseModule()
        {
            if (useScatter && applyScatterInNoise)
            {
                noiseModule.enabled = true;

                // Set overall noise strength based on the maximum scatter amount
                noiseModule.strength = Mathf.Max(scatterXAmount, Mathf.Max(scatterYAmount, scatterZAmount));
                noiseModule.frequency = noiseFrequency;

                // Set noise options
                noiseModule.scrollSpeed = 0.5f;
                noiseModule.damping = true;

                // Set noise position parameters - can only apply to X, Y and Z unequally using separateAxes
                noiseModule.separateAxes = true;

                // Set XYZ strength separately
                var strengthX = noiseModule.strengthX;
                strengthX.constant = scatterXAmount;
                noiseModule.strengthX = strengthX;

                var strengthY = noiseModule.strengthY;
                strengthY.constant = scatterYAmount;
                noiseModule.strengthY = strengthY;

                var strengthZ = noiseModule.strengthZ;
                strengthZ.constant = scatterZAmount;
                noiseModule.strengthZ = strengthZ;
            }
        }

        private void UpdateParticleLifetime()
        {
            // Skip if not properly initialized or we're in edit mode without a valid particle system
            if (!isInitialized || particleSystem == null || !synchronizeLifetimeWithPath)
                return;

            float lifetime = pathLength / particleSpeed;

            // Make sure lifetime is positive and reasonable
            lifetime = Mathf.Max(0.1f, lifetime);

            // Update particle lifetime in the main module
            ParticleSystem.MinMaxCurve lifetimeCurve = new ParticleSystem.MinMaxCurve(
                lifetime * (1 - speedVariation),
                lifetime * (1 + speedVariation)
            );

            // Re-get the main module to ensure it's valid
            var main = particleSystem.main;
            main.startLifetime = lifetimeCurve;
        }

        private float CalculateApproximatePathLength(List<Vector3> pathPoints)
        {
            if (pathPoints == null || pathPoints.Count < 2)
                return 0f;

            // For simple paths, we can just use the direct distance
            if (pathPoints.Count == 2 && pathCalculator is LineVFXPath)
            {
                return Vector3.Distance(pathPoints[0], pathPoints[1]);
            }

            // For more complex paths, calculate segment length sum
            float length = 0;

            // Sample along the path to get better approximation for curved paths
            int sampleCount = 20;
            Vector3 prevPoint = pathCalculator.CalculatePointOnPath(pathPoints, 0);

            for (int i = 1; i <= sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                Vector3 point = pathCalculator.CalculatePointOnPath(pathPoints, t);
                length += Vector3.Distance(prevPoint, point);
                prevPoint = point;
            }

            return length;
        }

        // Method that contains the particle position update logic
        private void UpdateParticlePositions()
        {
            if (!isPlaying) return;
            if (currentPathPoints == null || currentPathPoints.Count < 2) return;

            // Skip if there are no active particles
            if (particleSystem.particleCount == 0) return;

            // Get all current particles
            if (particleArray == null || particleArray.Length < particleSystem.particleCount)
            {
                particleArray = new ParticleSystem.Particle[particleSystem.particleCount];
            }

            int numParticles = particleSystem.GetParticles(particleArray);

            // Skip if no particles
            if (numParticles == 0) return;

            // Update each particle position based on its age
            for (int i = 0; i < numParticles; i++)
            {
                // Calculate normalized lifetime (0 = just born, 1 = about to die)
                float normalizedAge = 1f - (particleArray[i].remainingLifetime / particleArray[i].startLifetime);

                // Apply our range remapping to normalize age
                Vector3 pathPosition = GetPositionOnPath(pathCalculator, currentPathPoints, normalizedAge);

                // Apply manual scatter if using custom scatter and not noise module
                if (useScatter && !applyScatterInNoise)
                {
                    // Get scatter amount from curves for each axis
                    float xScatterMultiplier = scatterXOverLifetime.Evaluate(normalizedAge);
                    float yScatterMultiplier = scatterYOverLifetime.Evaluate(normalizedAge);
                    float zScatterMultiplier = scatterZOverLifetime.Evaluate(normalizedAge);

                    // Use particle index and normalized age for varied noise with consistent seed
                    float noiseX = SamplePerlinNoise(normalizedAge * noiseFrequency * 3.17f, i * 0.421f, 0) - 0.5f;
                    float noiseY = SamplePerlinNoise(i * 0.273f, normalizedAge * noiseFrequency * 2.83f, 1) - 0.5f;
                    float noiseZ = SamplePerlinNoise(normalizedAge * noiseFrequency * 1.53f, i * 0.637f, 2) - 0.5f;

                    // Apply scatter with different X, Y, and Z amounts
                    float xOffset = noiseX * scatterXAmount * xScatterMultiplier;
                    float yOffset = noiseY * scatterYAmount * yScatterMultiplier;
                    float zOffset = noiseZ * scatterZAmount * zScatterMultiplier;

                    pathPosition += new Vector3(xOffset, yOffset, zOffset);
                }

                particleArray[i].position = pathPosition;
            }

            particleSystem.SetParticles(particleArray, numParticles);
        }

        // Helper method for more consistent noise sampling
        private float SamplePerlinNoise(float x, float y, int offset)
        {
            // Add seed and offset to coordinates for variety between different noise channels
            return Mathf.PerlinNoise(
                x + noiseSeed * 0.1f + offset * 10.3f,
                y + noiseSeed * 0.1f + offset * 5.7f
            );
        }

        public override void Play()
        {
            if (isPlaying) return;

            if (!isInitialized)
            {
                Initialize();
            }

            // Move emitter to start
            if (moveEmitterToStart && currentPathPoints.Count > 0)
            {
                targetTransform.position = currentPathPoints[0];
            }

            // Start the particle system
            particleSystem.Play();

            isPlaying = true;
        }

        public override void Stop()
        {
            if (!isPlaying) return;

            // Stop the particle system
            particleSystem.Stop();

            isPlaying = false;
        }

        // Set the noise seed
        public void SetNoiseSeed(int seed)
        {
            noiseSeed = seed;
            if (isInitialized)
            {
                random = new System.Random(noiseSeed);
            }
        }

        private void LateUpdate()
        {
            // Call UpdateParticlePositions every frame to ensure particles follow the path
            if (isPlaying)
            {
                UpdateParticlePositions();
            }
        }

        // Editor support
        private void OnValidate()
        {
            ValidateCurves();
        }
    }
}