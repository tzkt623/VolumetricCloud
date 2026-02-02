using UnityEditor;
using UnityEngine;

namespace tezcat.Framework.Exp
{
    [CustomEditor(typeof(CloudNoiseGenerator))]
    public class CloudNoiseEditor : Editor
    {
        CloudNoiseGenerator mTarget;

        private void OnEnable()
        {
            mTarget = this.target as CloudNoiseGenerator;

        }

        public override void OnInspectorGUI()
        {
            this.DrawDefaultInspector();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Use Noise"))
            {
                mTarget.useNoise();
            }

            if(GUILayout.Button("Use Texture"))
            {
                mTarget.useTexture();
            }

            EditorGUILayout.EndToggleGroup();
        }
    }
}