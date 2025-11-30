using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class OctreeDate
{
    public List<SparseOctree.Octreenode> nodes;
    public int rootIndex;
    public Vector3 boundsCenter;
    public Vector3 boundsSize;
}
[CreateAssetMenu(fileName = "OctreeData", menuName = "Pathfinding/Octree Asset")]
public class OctreeAsset : ScriptableObject
{
    public SparseOctree octree;
}
public static class OctreeSerializer
{
    public static void SaveToFile(SparseOctree octree,string path)
    {
        string json = JsonUtility.ToJson(octree,true);
        File.WriteAllText(path, json);
        Debug.Log($"八叉树已保存到: {path}");
    }
    public static SparseOctree LoadFromFile(string path)
    {
        if (!File.Exists(path))
        {

            return null;
        }
        string json = File.ReadAllText(path);
        SparseOctree octree = JsonUtility.FromJson<SparseOctree>(json);
        return octree;
    }
    public static void SaveAsset(SparseOctree octree,string path)
    {
#if UNITY_EDITOR
        OctreeAsset asset = ScriptableObject.CreateInstance<OctreeAsset>();
        asset.octree = octree;
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        Debug.Log($"八叉树已保存为资产: {path}");
#endif
    }
}
