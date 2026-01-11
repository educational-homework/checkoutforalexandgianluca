using UnityEditor;
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    [CustomEditor(typeof(Dialogue))]
    public class DialogueEditor : Editor
    {
        // private string dialogueLines = "";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Dialogue Lines");

            SerializedProperty linesProperty = serializedObject.FindProperty("lines");

            for (int i = 0; i < linesProperty.arraySize; i++)
            {
                SerializedProperty lineProperty = linesProperty.GetArrayElementAtIndex(i);
                SerializedProperty textProperty = lineProperty.FindPropertyRelative("Text");

                EditorGUILayout.BeginVertical();

                EditorGUILayout.PropertyField(textProperty, GUIContent.none, GUILayout.ExpandWidth(true));

                EditorGUILayout.BeginHorizontal();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button($"Remove Line {i + 1}", GUILayout.Width(150)))
                {
                    linesProperty.DeleteArrayElementAtIndex(i);
                    serializedObject.ApplyModifiedProperties();
                    return;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();

                GUILayout.Space(10);
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Add New Line"))
            {
                linesProperty.arraySize++;
                serializedObject.ApplyModifiedProperties();
            }

            // EditorGUILayout.LabelField("Dialogue Lines (one line per entry)");
            // dialogueLines = EditorGUILayout.TextArea(dialogueLines, GUILayout.Height(100));

            // if (GUILayout.Button("Generate Dialogue Lines"))
            // {
            //     string[] newLines = dialogueLines.Split('\n');
            //     foreach (string line in newLines)
            //     {
            //         string trimmedLine = line.Trim();
            //         if (!string.IsNullOrEmpty(trimmedLine))
            //         {
            //             linesProperty.arraySize++;
            //             SerializedProperty newLineProperty = linesProperty.GetArrayElementAtIndex(linesProperty.arraySize - 1); // Get the newly added element
            //             SerializedProperty textProperty = newLineProperty.FindPropertyRelative("Text");
            //             textProperty.stringValue = trimmedLine;
            //         }
            //     }

            //     dialogueLines = "";
            //     serializedObject.ApplyModifiedProperties();
            // }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
