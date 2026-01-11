using UnityEngine;
using UnityEditor;
using System.IO;

namespace CryingSnow.CheckoutFrenzy
{
    public static class GameConfigEditor
    {
        private const string resourcePath = "Assets/CheckoutFrenzy/Resources/GameConfig.asset";

        [MenuItem("Tools/Checkout Frenzy/Game Config")]
        public static void OpenOrCreateGameConfig()
        {
            GameConfig config = Resources.Load<GameConfig>("GameConfig");

            if (config == null)
            {
                if (!Directory.Exists("Assets/CheckoutFrenzy/Resources"))
                {
                    Directory.CreateDirectory("Assets/CheckoutFrenzy/Resources");
                }

                config = ScriptableObject.CreateInstance<GameConfig>();
                AssetDatabase.CreateAsset(config, resourcePath);
                AssetDatabase.SaveAssets();
                Debug.Log("GameConfig asset created at " + resourcePath);
            }

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }
    }
}
