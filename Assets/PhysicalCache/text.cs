using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class text : MonoBehaviour
{
    [SerializeField]
    public ScriptableObject asset;

    //OctreeAsset octree; 
    [ContextMenu("start")]

    void LoadAndprint()
    {
         //octree = (OctreeAsset)asset;
      //  Debug.Log(octree.nodeDic[new Vector3Int(1,-1,1)]);
    }

    private void OnDrawGizmosSelected()
    {
       
    }
}
