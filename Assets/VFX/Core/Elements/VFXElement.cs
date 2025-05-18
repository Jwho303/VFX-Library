using UnityEngine;
using System.Collections.Generic;

namespace Jwho303.VFX
{
    /// <summary>
    /// Interface for all VFX elements that can be controlled by VFXController
    /// </summary>
    public interface IVFXElement
    {
        /// <summary>
        /// Initialize the VFX element
        /// </summary>
        void Initialize();

        /// <summary>
        /// Update the path points of the effect
        /// </summary>
        void UpdatePath(List<Vector3> pathPoints);

        /// <summary>
        /// Play the visual effect
        /// </summary>
        void Play();

        /// <summary>
        /// Stop the visual effect
        /// </summary>
        void Stop();

        /// <summary>
        /// Get whether the effect is currently playing
        /// </summary>
        bool IsPlaying { get; }
    }

    /// <summary>
    /// Base class for all VFX elements to ensure proper lifecycle management
    /// </summary>
    public abstract class BaseVFXElement : MonoBehaviour, IVFXElement
    {
        public bool IsPlaying => isPlaying;

        [Header("Path Range")]
        [Range(0f, 1f)]
        [SerializeField] protected float pathStartRange = 0f;
        [Range(0f, 1f)]
        [SerializeField] protected float pathEndRange = 1f;
        [SerializeField] protected bool clampToRange = true;

        // Store the current path
        protected List<Vector3> currentPathPoints = new List<Vector3>();
        [SerializeField] protected bool isInitialized = false;
        protected bool isPlaying = false;

        /// <summary>
        /// Default implementation of Initialize - can be overridden
        /// </summary>
        public virtual void Initialize()
        {
            if (isInitialized) return;
            isInitialized = true;
        }

        public abstract void UpdatePath(List<Vector3> pathPoints);
        public abstract void Play();
        public abstract void Stop();

        /// <summary>
        /// Map a 0-1 normalized value to our path range
        /// </summary>
        protected float RemapToRange(float normalizedPosition)
        {
            // Ensure start is less than end
            float validStart = Mathf.Min(pathStartRange, pathEndRange);
            float validEnd = Mathf.Max(pathStartRange, pathEndRange);

            // Remap the normalized position to our range
            float remappedPosition = validStart + normalizedPosition * (validEnd - validStart);

            // Optionally clamp to ensure we stay within the path
            if (clampToRange)
            {
                remappedPosition = Mathf.Clamp(remappedPosition, 0f, 1f);
            }

            return remappedPosition;
        }

        /// <summary>
        /// Helper function to get a position along the path at a normalized distance (0-1)
        /// </summary>
        protected Vector3 GetPositionOnPath(BaseVFXPath pathCalculator, List<Vector3> pathPoints, float normalizedDistance)
        {
            // Remap the distance based on our range settings
            float remappedDistance = RemapToRange(normalizedDistance);

            // Let the path calculator determine the actual position
            return pathCalculator.CalculatePointOnPath(pathPoints, remappedDistance);
        }

        /// <summary>
        /// Backwards compatibility method for elements still using start/end points
        /// </summary>
        public void UpdatePositions(Vector3 start, Vector3 end)
        {
            // Create a simple two-point path and update
            List<Vector3> simplePath = new List<Vector3> { start, end };
            UpdatePath(simplePath);
        }
    }
}