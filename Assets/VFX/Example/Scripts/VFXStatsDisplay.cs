using UnityEngine;
using TMPro;
using System.Text;
using System.Collections.Generic;
using Jwho303.VFX;

namespace Jwho303.VFX.Example
{
    /// <summary>
    /// Component that gathers performance stats and displays them in a TextMeshPro component
    /// </summary>
    public class VFXStatsDisplay : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The TextMeshPro component to display stats in")]
        [SerializeField] private TextMeshProUGUI statsText;

        [Tooltip("The VFXController to get path stats from (optional)")]
        [SerializeField] private VFXController vfxController;

        [Header("Settings")]
        [Tooltip("How often to refresh the stats display (in seconds)")]
        [SerializeField] private float refreshRate = 0.2f;

        [Tooltip("Whether to show frame rate")]
        [SerializeField] private bool showFPS = true;

        [Tooltip("Whether to show particle counts")]
        [SerializeField] private bool showParticleCount = true;

        [Tooltip("Whether to show path point counts")]
        [SerializeField] private bool showPathPoints = true;

        [Tooltip("Whether to show line renderer stats")]
        [SerializeField] private bool showLineRendererStats = true;

        [Tooltip("Whether to show particle system stats")]
        [SerializeField] private bool showParticleSystemStats = true;

        [Tooltip("Whether to show system memory usage")]
        [SerializeField] private bool showMemoryUsage = true;

        // Private tracking variables
        private float deltaTime = 0f;
        private float timer = 0f;
        private readonly StringBuilder statsBuilder = new StringBuilder();

        // Cached references to LineRenderers
        private List<LineRenderer> lineRenderers = new List<LineRenderer>();

        // Cached references to ParticleSystems
        private List<ParticleSystem> particleSystems = new List<ParticleSystem>();

        private void Start()
        {
            // If no text component is assigned, try to find one on this GameObject
            if (statsText == null)
            {
                statsText = GetComponent<TextMeshProUGUI>();
            }

            // If no VFXController is assigned and showPathPoints is true, try to find one in the scene
            if (vfxController == null && showPathPoints)
            {
                vfxController = FindObjectOfType<VFXController>();
            }

            // Find and cache all LineRenderers in the scene
            if (showLineRendererStats)
            {
                lineRenderers.AddRange(FindObjectsOfType<LineRenderer>());
            }

            // Find and cache all ParticleSystems in the scene
            if (showParticleSystemStats)
            {
                particleSystems.AddRange(FindObjectsOfType<ParticleSystem>());
            }

            // Do an initial refresh
            RefreshStats();
        }

        private void Update()
        {
            // Track frame rate
            if (showFPS)
            {
                deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            }

            // Update timer and refresh stats at specified interval
            timer += Time.deltaTime;
            if (timer >= refreshRate)
            {
                RefreshStats();
                timer = 0f;
            }
        }

        /// <summary>
        /// Recalculate all stats and update the text display
        /// </summary>
        private void RefreshStats()
        {
            if (statsText == null)
                return;

            statsBuilder.Clear();

            // Frame rate
            if (showFPS)
            {
                float fps = 1.0f / deltaTime;
                statsBuilder.AppendLine($"FPS: {fps:F1}");
            }

            // VFX path points
            if (showPathPoints && vfxController != null)
            {
                int pathPointCount = vfxController.GetPathPoints().Count;
                statsBuilder.AppendLine($"Path Points: {pathPointCount}");
            }

            // Line renderer stats
            if (showLineRendererStats && lineRenderers.Count > 0)
            {
                int totalLineRenderers = lineRenderers.Count;
                int totalPoints = 0;

                foreach (var lineRenderer in lineRenderers)
                {
                    if (lineRenderer != null && lineRenderer.enabled)
                    {
                        totalPoints += lineRenderer.positionCount;
                    }
                }

                statsBuilder.AppendLine($"Line Renderers: {totalLineRenderers}");
                statsBuilder.AppendLine($"Line Points: {totalPoints}");
            }

            // Particle system stats
            if (showParticleSystemStats && particleSystems.Count > 0)
            {
                int totalParticleSystems = particleSystems.Count;
                int totalActiveParticles = 0;

                foreach (var particleSystem in particleSystems)
                {
                    if (particleSystem != null)
                    {
                        totalActiveParticles += particleSystem.particleCount;
                    }
                }

                statsBuilder.AppendLine($"Particle Systems: {totalParticleSystems}");
                statsBuilder.AppendLine($"Active Particles: {totalActiveParticles}");
            }

            // Memory usage
            if (showMemoryUsage)
            {
                float memoryMB = System.GC.GetTotalMemory(false) / (1024f * 1024f);
                statsBuilder.AppendLine($"Memory: {memoryMB:F1} MB");
            }

            // Update the text
            statsText.text = statsBuilder.ToString();
        }

        /// <summary>
        /// Find and update cached references to components in the scene
        /// </summary>
        public void RefreshReferences()
        {
            // Update VFXController reference
            if (vfxController == null && showPathPoints)
            {
                vfxController = FindObjectOfType<VFXController>();
            }

            // Update LineRenderer references
            if (showLineRendererStats)
            {
                lineRenderers.Clear();
                lineRenderers.AddRange(FindObjectsOfType<LineRenderer>());
            }

            // Update ParticleSystem references
            if (showParticleSystemStats)
            {
                particleSystems.Clear();
                particleSystems.AddRange(FindObjectsOfType<ParticleSystem>());
            }

            // Refresh stats immediately
            RefreshStats();
        }
    }
}