using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ModularBehaviour
{
    public class SavePassedValue : TriggerAction
    {
        public string saveName = "";
        public override ActionResult Fire(IntelligenceController controller, object value = null)
        {
            controller.SetMemoryValue(saveName, value);
            return ActionResult.Success;
        }

        public override void DrawGUI(Intelligence intelligence, CallbackCollection callbacks)
        {
#if UNITY_EDITOR
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:", GUILayout.Width(40));
            saveName = EditorGUILayout.TextField(saveName);
            GUILayout.EndHorizontal();
#endif
        }
    }
}