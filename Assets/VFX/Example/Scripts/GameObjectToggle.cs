using UnityEngine;
using UnityEngine.UI;

namespace Jwho303.VFX.Example
{
    /// <summary>
    /// Simple component that links a UI Toggle to a GameObject's active state
    /// </summary>
    public class GameObjectToggle : MonoBehaviour
    {
        [Tooltip("The UI Toggle component")]
        [SerializeField] private Toggle toggle;

        [Tooltip("The GameObject to toggle on/off")]
        [SerializeField] private GameObject targetObject;

        private void Start()
        {
            // Try to find the toggle on this GameObject if not assigned
            if (toggle == null)
            {
                toggle = GetComponent<Toggle>();
            }

            // Set up the toggle listener
            if (toggle != null)
            {
                // Initialize toggle state based on object's active state
                if (targetObject != null)
                {
                    bool isActive = targetObject.activeSelf;
                    toggle.isOn = isActive;
                }

                // Add listener for value changes
                toggle.onValueChanged.AddListener(OnToggleValueChanged);
            }
            else
            {
                Debug.LogWarning("No Toggle component found for GameObjectToggle");
            }
        }

        private void OnToggleValueChanged(bool value)
        {
            if (targetObject != null)
            {
                // Apply toggle value to object active state (with optional inversion)
                targetObject.SetActive( value);
            }
        }

        /// <summary>
        /// Set the target GameObject at runtime
        /// </summary>
        public void SetTargetObject(GameObject newTarget)
        {
            targetObject = newTarget;

            // Update toggle state to match the new target's active state
            if (toggle != null && targetObject != null)
            {
                bool isActive = targetObject.activeSelf;
                toggle.isOn = isActive;
            }
        }

        /// <summary>
        /// Set both the toggle component and target GameObject at runtime
        /// </summary>
        public void Setup(Toggle newToggle, GameObject newTarget)
        {
            // If we already had a toggle with a listener, remove it
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            }

            // Set new references
            toggle = newToggle;
            targetObject = newTarget;

            // Set up the new toggle
            if (toggle != null)
            {
                // Initialize toggle state
                if (targetObject != null)
                {
                    bool isActive = targetObject.activeSelf;
                    toggle.isOn = isActive;
                }

                // Add listener
                toggle.onValueChanged.AddListener(OnToggleValueChanged);
            }
        }

        private void OnDestroy()
        {
            // Clean up listener when destroyed
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            }
        }
    }
}