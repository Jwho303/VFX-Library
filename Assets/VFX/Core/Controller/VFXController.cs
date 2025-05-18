using UnityEngine;
using System.Collections.Generic;

namespace Jwho303.VFX
{
    public class VFXController : MonoBehaviour
    {
        public bool IsPlaying => isPlaying;

        [SerializeField] private bool playOnStart = false;

        // Replace start/end positions with an array of path points
        [SerializeField] private List<Vector3> pathPoints = new List<Vector3>();

        // Minimum number of points required for a valid path
        private const int MIN_PATH_POINTS = 2;

        // Gizmo settings
        [Header("Gizmo Settings")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color pathStartColor = Color.green;
        [SerializeField] private Color pathEndColor = Color.red;
        [SerializeField] private Color pathPointColor = Color.yellow;
        [SerializeField] private float gizmoSize = 0.1f;

        // List of VFX elements managed by this controller
        [SerializeField] private List<BaseVFXElement> vfxElements = new List<BaseVFXElement>();

        private bool isPlaying = false;
        private bool isInitialized = false;

        private void Awake()
        {
            InitializeElements();
        }

        private void Start()
        {
            if (playOnStart)
            {
                Play();
            }
        }

        /// <summary>
        /// Initialize all VFX elements
        /// </summary>
        public void InitializeElements()
        {
            if (isInitialized) return;

            foreach (var element in vfxElements)
            {
                element.Initialize();
            }

            isInitialized = true;
        }

        public void FixedUpdate()
        {
            // Only update positions during runtime if needed
            if (Application.isPlaying)
            {
                UpdatePath(pathPoints);
            }
        }

        /// <summary>
        /// Set the path points for all VFX elements
        /// </summary>
        public void UpdatePath(List<Vector3> points)
        {
            // Make sure we have at least the minimum required points
            if (points == null || points.Count < MIN_PATH_POINTS)
            {
                Debug.LogWarning("Path requires at least " + MIN_PATH_POINTS + " points. Path not updated.");
                return;
            }

            // Update our stored path
            pathPoints = new List<Vector3>(points);

            if (!isInitialized)
            {
                InitializeElements();
            }

            // Update all elements with the new path
            foreach (var element in vfxElements)
            {
                element.UpdatePath(pathPoints);
            }
        }

        /// <summary>
        /// Add a point to the end of the path
        /// </summary>
        public void AddPathPoint(Vector3 point)
        {
            pathPoints.Add(point);
            UpdatePath(pathPoints);
        }

        /// <summary>
        /// Insert a point at the specified index
        /// </summary>
        public void InsertPathPoint(int index, Vector3 point)
        {
            // Clamp index to valid range
            index = Mathf.Clamp(index, 0, pathPoints.Count);
            pathPoints.Insert(index, point);
            UpdatePath(pathPoints);
        }

        /// <summary>
        /// Remove a point at the specified index
        /// </summary>
        public void RemovePathPoint(int index)
        {
            if (index >= 0 && index < pathPoints.Count && pathPoints.Count > MIN_PATH_POINTS)
            {
                pathPoints.RemoveAt(index);
                UpdatePath(pathPoints);
            }
        }

        /// <summary>
        /// Get the current path points
        /// </summary>
        public List<Vector3> GetPathPoints()
        {
            return new List<Vector3>(pathPoints);
        }

        /// <summary>
        /// Play all VFX elements
        /// </summary>
        public void Play()
        {
            if (isPlaying) return;

            if (!isInitialized)
            {
                InitializeElements();
            }

            // Ensure we have at least the minimum required points
            if (pathPoints.Count < MIN_PATH_POINTS)
            {
                Debug.LogWarning("Path requires at least " + MIN_PATH_POINTS + " points. Cannot play effect.");
                return;
            }

            foreach (var element in vfxElements)
            {
                element.UpdatePath(pathPoints);
                element.Play();
            }

            isPlaying = true;
        }

        /// <summary>
        /// Stop all VFX elements
        /// </summary>
        public void Stop()
        {
            if (!isPlaying) return;

            foreach (var element in vfxElements)
            {
                element.Stop();
            }

            isPlaying = false;
        }

        /// <summary>
        /// Draw gizmos to visualize the path points
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showGizmos || pathPoints.Count < MIN_PATH_POINTS) return;

            // Draw start point
            Gizmos.color = pathStartColor;
            Gizmos.DrawSphere(pathPoints[0], gizmoSize);

            // Draw end point
            Gizmos.color = pathEndColor;
            Gizmos.DrawSphere(pathPoints[pathPoints.Count - 1], gizmoSize);

            // Draw intermediate points
            Gizmos.color = pathPointColor;
            for (int i = 1; i < pathPoints.Count - 1; i++)
            {
                Gizmos.DrawSphere(pathPoints[i], gizmoSize * 0.8f);
            }

            // Draw connecting lines
            for (int i = 0; i < pathPoints.Count - 1; i++)
            {
                // Gradient color from start to end
                float t = i / (float)(pathPoints.Count - 1);
                Gizmos.color = Color.Lerp(pathStartColor, pathEndColor, t);
                Gizmos.DrawLine(pathPoints[i], pathPoints[i + 1]);
            }

            // Force update positions to VFX elements even in edit mode
            if (!Application.isPlaying)
            {
                foreach (var element in vfxElements)
                {
                    if (element != null)
                    {
                        element.UpdatePath(pathPoints);
                    }
                }
            }
        }

        /// <summary>
        /// When selected in the editor, ensure gizmos are continuously updated
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;

            // Make sure that paths are updated when this object is selected
            if (!Application.isPlaying)
            {
                foreach (var element in vfxElements)
                {
                    if (element != null)
                    {
                        // This will cause the paths to update their gizmos
                        element.UpdatePath(pathPoints);
                    }
                }
            }
        }
    }
}