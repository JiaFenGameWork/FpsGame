using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class OctreeDate
{
   // public List<SparseOctree.Octreenode> nodes;
    public int rootIndex;
    public Vector3 boundsCenter;
    public Vector3 boundsSize;
}
[CreateAssetMenu(fileName = "OctreeData", menuName = "Pathfinding/Octree Asset/")]

public static class OctreeSerializer
{
    public static void SaveAsset(string path)
    {
#if UNITY_EDITOR
        NavMeshAsset asset = ScriptableObject.CreateInstance<NavMeshAsset>();
     
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        Debug.Log($"八叉树已保存为资产: {path}");
#endif
    }
}
