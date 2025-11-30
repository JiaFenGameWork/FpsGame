#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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

        if (GUILayout.Button("烘焙八叉树", GUILayout.Height(30)))
        {
            baker.Bake();
            EditorUtility.SetDirty(baker);
        }

        if (GUILayout.Button("清除", GUILayout.Height(30)))
        {
            baker.Clear();
            EditorUtility.SetDirty(baker);
        }

        EditorGUILayout.EndHorizontal();

        if (baker.octree != null)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox($"节点数量: {baker.octree.NodeCount}", MessageType.Info);
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("自动计算边界"))
        {
            CalculateBoundsFromColliders(baker);
            EditorUtility.SetDirty(baker);
        }
    }

    private void CalculateBoundsFromColliders(Baker baker)
    {
        Collider[] allColliders = FindObjectsOfType<Collider>();

        if (allColliders.Length == 0)
        {
            Debug.LogWarning("场景中没有找到 Collider");
            return;
        }

        Bounds totalBounds = allColliders[0].bounds;

        foreach (var col in allColliders)
        {
            totalBounds.Encapsulate(col.bounds);
        }

        // 稍微扩大一点
        totalBounds.Expand(2f);

        baker.boundsCenter = totalBounds.center;
        baker.boundSize = totalBounds.size;

        Debug.Log($"边界已更新: 中心 {totalBounds.center}, 尺寸 {totalBounds.size}");
    }
}
#endif