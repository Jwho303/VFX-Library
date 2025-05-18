using UnityEngine;
using UnityEngine.EventSystems;

namespace Jwho303.VFX.Example
{
    /// <summary>
    /// Simple draggable script using Unity's EventSystem to drag objects while keeping a fixed Z depth
    /// </summary>
    public class Draggable : MonoBehaviour, IDragHandler
    {
        private float zDepth = 0f;

        public void OnDrag(PointerEventData eventData)
        {
            // Convert screen position to world position at specific z-depth
            Vector3 worldPosition = GetWorldPositionFromPointer(eventData);

            // Apply the new position, maintaining zDepth
            this.transform.position = new Vector3(worldPosition.x, worldPosition.y, zDepth);
        }

        /// <summary>
        /// Converts a pointer position to world coordinates at the specified Z depth
        /// </summary>
        private Vector3 GetWorldPositionFromPointer(PointerEventData eventData)
        {
            // Create a plane at the specified Z depth
            Plane plane = new Plane(Vector3.forward, new Vector3(0, 0, zDepth));

            // Cast a ray from the mouse position
            Ray ray = Camera.main.ScreenPointToRay(eventData.position);

            // Find where the ray intersects the plane
            if (plane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }

            // Default fallback position (should rarely happen)
            return new Vector3(0, 0, zDepth);
        }

        /// <summary>
        /// Set a different Z depth at runtime
        /// </summary>
        public void SetZDepth(float newZDepth)
        {
            zDepth = newZDepth;

            // Update current position to the new Z depth
            Vector3 position = this.transform.position;
            position.z = zDepth;
            this.transform.position = position;
        }
    }
}