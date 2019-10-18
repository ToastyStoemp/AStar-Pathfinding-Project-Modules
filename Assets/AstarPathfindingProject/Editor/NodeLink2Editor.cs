using UnityEditor;

namespace Pathfinding {
    
    [CustomEditor(typeof(NodeLink2))]
    [CanEditMultipleObjects]
    public class NodeLink2Editor : EditorBase
    {
        protected override void Inspector()
        {
            DrawDefaultInspector();
            
            var tagValue = FindProperty("setTag");
            
            EditorGUI.BeginChangeCheck();
            var newTag = EditorGUILayoutx.TagField("Tag Value", tagValue.intValue, () => AstarPathEditor.EditTags());
            if (EditorGUI.EndChangeCheck()) {
                tagValue.intValue = newTag;
            }
        }
    }
}