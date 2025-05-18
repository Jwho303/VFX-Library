using UnityEngine;
using System.Collections.Generic;

namespace Jwho303.VFX
{
    public class BlendPathVFXPath : BaseVFXPath
    {
        [SerializeField] private BaseVFXPath pathA;
        [SerializeField] private BaseVFXPath pathB;
        [SerializeField] private AnimationCurve blendCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField][Range(0f, 1f)] private float transitionStart = 0.3f;
        [SerializeField][Range(0f, 1f)] private float transitionEnd = 0.7f;

        public override Vector3 CalculatePointOnPath(List<Vector3> pathPoints, float normalizedDistance)
        {
            if (pathA == null || pathB == null)
            {
                Debug.LogError("BlendPathVFXPath requires two path calculators to be assigned.");
                return Vector3.zero;
            }

            if (pathPoints == null || pathPoints.Count < 2)
            {
                Debug.LogWarning("BlendPath requires at least 2 points");
                return Vector3.zero;
            }

            // Calculate points from both paths
            Vector3 pointA = pathA.CalculatePointOnPath(pathPoints, normalizedDistance);
            Vector3 pointB = pathB.CalculatePointOnPath(pathPoints, normalizedDistance);

            // Calculate blend factor
            float blend;
            if (normalizedDistance < transitionStart)
            {
                blend = 0; // Use only pathA
            }
            else if (normalizedDistance > transitionEnd)
            {
                blend = 1; // Use only pathB
            }
            else
            {
                // Normalize distance within transition range
                float normalizedTransition = (normalizedDistance - transitionStart) /
                                            (transitionEnd - transitionStart);

                // Apply blend curve
                blend = blendCurve.Evaluate(normalizedTransition);
            }

            // Interpolate between the two paths
            return Vector3.Lerp(pointA, pointB, blend);
        }

        public override void UpdatePath(List<Vector3> points)
        {
            base.UpdatePath(points);

            // Update path points on child paths
            if (pathA != null) pathA.UpdatePath(points);
            if (pathB != null) pathB.UpdatePath(points);
        }
    }
}