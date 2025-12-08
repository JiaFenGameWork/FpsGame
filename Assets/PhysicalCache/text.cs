using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class text : MonoBehaviour
{
    [SerializeField]
    public ScriptableObject asset;

    OctreeAsset octree; 
    [ContextMenu("start")]

    void LoadAndprint()
    {
         octree = (OctreeAsset)asset;
        Debug.Log(octree.nodeDic[new Vector3Int(1,-1,1)]);
    }

    private void OnDrawGizmosSelected()
    {
        if (octree != null)
        {
            var nodes = octree.octreenodes;
            foreach (var value in octree.nodeDic.Values)
            {
                Gizmos.color = new Color(0.2f, 1, 0, 0.3f);
                Gizmos.DrawCube(nodes[value].center, new Vector3(1f,1f,1f));
            }
        }
    }
}
