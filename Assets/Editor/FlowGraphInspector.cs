#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TacticsGame.Flow.Editor
{
    // Custom inspector for FlowGraph assets: hide raw properties,
    // push people to use the graph editor window.
    [CustomEditor(typeof(FlowGraph))]
    public class FlowGraphInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "Flow Graph assets are edited via the Flow Graph window.\n\n" +
                "Menu: Window → Tactics → Flow Graph.",
                MessageType.Info);
        }
    }
}
#endif
