using UnityEngine;
using System.Collections.Generic;
using Jwho303.VFX;

namespace Jwho303.VFX.Example
{
    /// <summary>
    /// Simple script that links GameObject positions to VFXController path points
    /// </summary>
    public class GameObjectToVFXConnector : MonoBehaviour
    {
        [Tooltip("The VFXController to update with object positions")]
        [SerializeField] private VFXController vfxController;

        [Tooltip("GameObjects whose positions will be used for the VFX path")]
        [SerializeField] private List<Transform> pathObjects = new List<Transform>();

        [Tooltip("Update the path every frame")]
        [SerializeField] private bool updateEveryFrame = true;

        private void Start()
        {
            UpdatePath();
        }

        private void Update()
        {
            if (updateEveryFrame)
            {
                UpdatePath();
            }
        }

        /// <summary>
        /// Update the VFX path with the current positions of the path objects
        /// </summary>
        public void UpdatePath()
        {
            if (vfxController == null || pathObjects.Count < 2)
            {
                return;
            }

            // Clear previous points
            List<Vector3> pathPoints = new List<Vector3>();

            // Add position of each path object
            foreach (var obj in pathObjects)
            {
                if (obj != null)
                {
                    pathPoints.Add(obj.position);
                }
            }

            // Update the VFX controller
            vfxController.UpdatePath(pathPoints);
        }
    }
}