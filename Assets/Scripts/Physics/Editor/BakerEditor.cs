#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

[CustomEditor(typeof(Baker))]
public class BakerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Baker baker = (Baker)target;

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("ºæ±º°Ë²æÊ÷", GUILayout.Height(30)))
        {
            baker.Bake();
            EditorUtility.SetDirty(baker);
        }

        if (GUILayout.Button("Çå³ý", GUILayout.Height(30)))
        {
            baker.Clear();
            EditorUtility.SetDirty(baker);
        }

        EditorGUILayout.EndHorizontal();


        EditorGUILayout.Space(10);

        if (GUILayout.Button("±£´æ"))
        {
            NavMeshAsset asset = ScriptableObject.CreateInstance<NavMeshAsset>();
            asset.cells = baker.nav.Cells;
            asset.cellSize = baker.nav.CellSize;
            asset.bounds = baker.nav.WorldBounds;
            string path = new string($"Assets/PhysicalCache/{baker.name}.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
        }
    }

  
}
#endif