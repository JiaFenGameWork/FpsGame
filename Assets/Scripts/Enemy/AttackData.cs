using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct AttackData 
{
    public Transform transform;
    public float damage;
    public bool KnockUp;
    public bool IsWeakPoint;
    public AttackData(Transform tr, float damage,bool isWeakPoint, bool knockUp = false)
    {
        this.transform = tr;
        this.damage = damage;
        this.KnockUp = knockUp;
        this.IsWeakPoint = isWeakPoint;
    }
}

