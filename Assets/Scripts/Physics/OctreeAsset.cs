using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class OctreeAsset : ScriptableObject,ISerializationCallbackReceiver
{
    [HideInInspector]
    [SerializeField]
    public SparseOctree.Octreenode[] octreenodes;
    [HideInInspector]
    [SerializeField]
    public List<int> value = new List<int>();
    [HideInInspector]
    [SerializeField]
    public List<Vector3Int> key = new List<Vector3Int>();
    [SerializeField]
    public float MinSize;
    [NonSerialized]
    public Dictionary<Vector3Int, int> nodeDic =new Dictionary<Vector3Int, int>();
    public void OnAfterDeserialize()
    {
        nodeDic.Clear();
        for(int i = 0; i < value.Count; i++)
        {
            nodeDic.Add(key[i], value[i]);
        }
    }

    public void OnBeforeSerialize()
    {
        key.Clear();
        value.Clear();
        foreach(var kv in nodeDic)
        {
            key.Add(kv.Key);
            value.Add(kv.Value);
        }
    }

    //public 

}
