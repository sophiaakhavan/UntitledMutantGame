using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class DefaultEnemy : EnemyAI
{
    // No additional functionality for now; inherits all behavior from EnemyAI

#if UNITY_EDITOR
    [CustomEditor(typeof(DefaultEnemy))]
    public class EnemyAIEditor : Editor
    {
        public void OnSceneGUI()
        {
            var ai = target as DefaultEnemy;

            // draw the detectopm range
            Handles.color = ai.ProximityDetectionColor;
            Handles.DrawSolidDisc(ai.transform.position, Vector3.up, ai.ProximityDetectionRange);

            // draw the hearing range
            Handles.color = ai.HearingRangeColor;
            Handles.DrawSolidDisc(ai.transform.position, Vector3.up, ai.HearingRange);

            // work out the start point of the vision cone
            Vector3 startPoint = Mathf.Cos(-ai.VisionConeAngle * Mathf.Deg2Rad) * ai.transform.forward +
                                 Mathf.Sin(-ai.VisionConeAngle * Mathf.Deg2Rad) * ai.transform.right;

            // draw the vision cone
            Handles.color = ai.VisionConeColor;
            Handles.DrawSolidArc(ai.transform.position, Vector3.up, startPoint, ai.VisionConeAngle * 2f, ai.VisionConeRange);
        }
    }
#endif // UNITY_EDITOR
}
