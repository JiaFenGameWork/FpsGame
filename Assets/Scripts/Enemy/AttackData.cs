using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum AttackRangeType { Melee, Ranged }

[CreateAssetMenu(menuName = "Combat/AttackData")]
public class AttackData : ScriptableObject
{
    public string attackName;

    public AttackRangeType rangeType = AttackRangeType.Melee;
    public float minRange = 0f;
    public float maxRange = 2f;

    public float windup = 0.2f;      // 前摇
    public float active = 0.1f;      // 出手（判定窗口）
    public float recovery = 0.3f;    // 后摇
    public float cooldown = 1.0f;

    public int damage = 10;

    // 动画参数（示例）
    public string animatorTrigger = "Attack";
}

