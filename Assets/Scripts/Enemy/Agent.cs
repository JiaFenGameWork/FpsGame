using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class Agent : MonoBehaviour
{
    [System.NonSerialized]
    public int Queryid;

    public Vector3 Position => transform.position;

    public int Id;

}
