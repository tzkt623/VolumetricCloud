using UnityEditor;
using UnityEngine;

namespace tezcat.Framework.Exp
{
    public abstract class Widget
    {
        public delegate void OnDraw();
        public OnDraw onDraw;
        public static GUIStyle mTitleStyle = new GUIStyle()
        {
            fontStyle = FontStyle.Bold,    
        };

        public abstract void draw();


    }

    public class FoldOut : Widget
    {
        bool show;
        public string name;

        public override void draw()
        {
            if (show = EditorGUILayout.Foldout(show, name, true))
            {
                EditorGUI.indentLevel++;
                this.onDraw();
                EditorGUI.indentLevel--;
            }
        }
    }
}