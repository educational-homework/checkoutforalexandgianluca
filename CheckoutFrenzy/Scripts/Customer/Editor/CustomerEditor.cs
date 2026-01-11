using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    [CustomEditor(typeof(Customer), true)]
    public class CustomerEditor : Editor
    {
        SerializedProperty handAttachments;
        private const string HAND_ATTACHMENTS_PREFAB_PATH = "Assets/CheckoutFrenzy/Prefabs/HandAttachments.prefab";

        private void OnEnable()
        {
            handAttachments = serializedObject.FindProperty("handAttachments");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (handAttachments == null || handAttachments.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Hand Attachments are missing. Please load the appropriate prefab and attach it to the right hand.", MessageType.Error);

                bool inPrefabMode = PrefabStageUtility.GetCurrentPrefabStage() != null;

                GUI.enabled = inPrefabMode;

                if (GUILayout.Button("Load Hand Attachments"))
                {
                    LoadHandAttachments();
                }

                GUI.enabled = true;
            }
            else
            {
                EditorGUILayout.HelpBox("Hand Attachments are properly assigned. Adjust the position as you see fit.", MessageType.Info);

                bool inPrefabMode = PrefabStageUtility.GetCurrentPrefabStage() != null;

                GUI.enabled = inPrefabMode;

                if (GUILayout.Button("Select Hand Attachments"))
                {
                    Selection.activeObject = handAttachments.objectReferenceValue;
                }

                GUI.enabled = true;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void LoadHandAttachments()
        {
            Customer customer = (Customer)target;
            Animator animator = customer.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Animator not found on the Customer object.");
                return;
            }

            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (rightHand == null)
            {
                Debug.LogError("Right hand bone not found.");
                return;
            }

            GameObject handAttachmentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HAND_ATTACHMENTS_PREFAB_PATH);

            if (handAttachmentPrefab == null)
            {
                Debug.LogError("HandAttachments prefab not found at path: " + HAND_ATTACHMENTS_PREFAB_PATH);
                return;
            }

            GameObject handAttachmentInstance = PrefabUtility.InstantiatePrefab(handAttachmentPrefab, rightHand) as GameObject;

            handAttachmentInstance.transform.localPosition = Vector3.zero;
            handAttachmentInstance.transform.localRotation = Quaternion.identity;
            handAttachmentInstance.transform.localScale = Vector3.one;

            handAttachments.objectReferenceValue = handAttachmentInstance;

            EditorUtility.SetDirty(customer);
            EditorSceneManager.MarkSceneDirty(customer.gameObject.scene);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
