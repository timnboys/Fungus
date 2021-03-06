using System;
using UnityEngine;
using Pathfinding;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ModularBehaviour
{
    [ActionUsage(UsageType.AsCondition)]
    public class IsInRange : AIAction
    {
        public string entityVarName = "target";
        public float range = 0.5f;
        public override ActionResult Run(IntelligenceController controller, float deltaTime)
        {
            return Fire(controller);
        }

        public override ActionResult Fire(IntelligenceController controller)
        {
            Entity t = null;
            if (controller.GetMemoryValue(entityVarName, out t) && t != null)
            {
                if (Mathf.Sqrt(AstarMath.SqrMagnitudeXZ(controller.Owner.transform.position, t.transform.position)) < range)
                {
                    return ActionResult.Success;
                }
            }
            return ActionResult.Failed;
        }

        public override void DrawGUI(IntelligenceState parentState, Intelligence intelligence, CallbackCollection callbacks)
        {
#if UNITY_EDITOR
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("target var name: ");
            entityVarName = EditorGUILayout.TextField(entityVarName);
            EditorGUILayout.EndHorizontal();
            range = EditorGUILayout.FloatField("Range:", range);
#endif
        }
    }
}