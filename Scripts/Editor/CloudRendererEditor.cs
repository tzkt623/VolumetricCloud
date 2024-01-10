using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace tezcat.Framework.Exp
{
    [CustomEditor(typeof(CloudRenderer))]
    public class CloudRendererEditor : Editor
    {
        CloudRenderer mRenderer;
        static GUIStyle mTitleStyle;

        class ShaderArea
        {
            public CloudRenderer mRenderer;
            Shader mAddShader = null;
            Vector2 mScroll = Vector2.one;
            bool mShow = false;

            public void draw(Editor editor)
            {
                var list = mRenderer.mShaderList;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Shaders", mTitleStyle);

                EditorGUILayout.LabelField("Drag Shader To Add");
                mAddShader = (Shader)EditorGUILayout.ObjectField(mAddShader, typeof(Shader), true);
                if (mAddShader)
                {
                    list.Insert(0, mAddShader);
                    mAddShader = null;
                }

                if (mShow = EditorGUILayout.Foldout(mShow, "List", true))
                {
                    mScroll = EditorGUILayout.BeginScrollView(mScroll, new GUILayoutOption[] { GUILayout.Height(128) });
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        if (mRenderer.shaderUsing(i))
                        {
                            GUI.backgroundColor = Color.green;
                        }
                        else
                        {
                            GUI.backgroundColor = Color.white;
                        }

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.ObjectField(list[i], typeof(Shader), true);
                        if (GUILayout.Button("Use"))
                        {
                            mRenderer.setShader(i);
                        }

                        if (GUILayout.Button("Del"))
                        {
                            if(mRenderer.shaderUsing(i))
                            {
                                mRenderer.setShader(-1);
                            }

                            list.RemoveAt(i);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();

                    GUI.backgroundColor = Color.white;
                }
            }
        }

        ShaderArea shaderArea = new ShaderArea();

        private void OnEnable()
        {
            mRenderer = this.target as CloudRenderer;
            shaderArea.mRenderer = mRenderer;

            mTitleStyle = new GUIStyle()
            {
                fontStyle = FontStyle.Bold
            };
        }

        public override void OnInspectorGUI()
        {
            this.DrawDefaultInspector();

            shaderArea.draw(this);
        }
    }
}