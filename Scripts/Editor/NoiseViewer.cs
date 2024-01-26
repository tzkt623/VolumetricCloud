using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace tezcat.Framework.Exp
{
    [CustomEditor(typeof(CloudNoiseGenerator))]
    public class NoiseViewer : Editor
    {
        CloudNoiseGenerator mNoise;
        Material mTexMat = null;
        Vector2 mSPos = new Vector2();

        private void OnEnable()
        {
            mNoise = this.target as CloudNoiseGenerator;
        }

        public override void OnInspectorGUI()
        {
            this.DrawDefaultInspector();

            /*
            GUILayout.Label("Channels");
            EditorGUI.Popup(EditorGUILayout.GetControlRect(), "Channels", 0, new[]{ "haha", "hehe" });
            mSPos = GUILayout.BeginScrollView(mSPos, new GUILayoutOption[] { GUILayout.Height(128) });
            for (int i = 0; i < 10; i++)
            {
                GUILayout.Toggle(false, i.ToString());
            }
            GUILayout.EndScrollView();
            */
        }
    }
}