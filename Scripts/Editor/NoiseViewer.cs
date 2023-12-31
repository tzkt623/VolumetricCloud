using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace tezcat.Framework.Exp
{
    [CustomEditor(typeof(CloudNoiseGPU))]
    public class NoiseViewer : Editor
    {
        CloudNoiseGPU mNoise;
        Material mTexMat = null;

        private void OnEnable()
        {
            mNoise = this.target as CloudNoiseGPU;
        }

        public override void OnInspectorGUI()
        {
            this.DrawDefaultInspector();

            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Generate"))
            {

            }

            if (GUILayout.Button("Update"))
            {

            }

            EditorGUILayout.EndHorizontal();
        }
    }
}