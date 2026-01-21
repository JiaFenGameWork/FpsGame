using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IState
{
    void OnEnter();  // ����״̬ʱ���������Ŷ��������ü�ʱ����
    void Tick();     // ÿ֡�߼����൱��Update��
    void OnExit();   // �˳�״̬ʱ�������������ݡ�ֹͣ������
}

/// <summary>
/// 可选：让 State 接收动画事件（AnimationClip Event），用于精确的攻击判定窗口/位移窗口等。
/// 这样做 Boss 动作时，不需要在 Controller 里为每个动作写一堆 AE_XXX 函数。
/// </summary>
public interface IAnimationEventReceiver
{
    /// <summary>
    /// 接收 string 参数的动画事件（推荐：用 key 描述事件，例如 "HitStart", "HitEnd", "Shoot:3"）。
    /// </summary>
    void OnAnimationEvent(string key);

    /// <summary>
    /// 接收 int 参数的动画事件（可用于索引/枚举映射）。
    /// </summary>
    void OnAnimationEventInt(int value);
}
public class StateMachine
{
    private IState _currentState;
    
    /// <summary>
    /// 获取当前状态
    /// </summary>
    public IState CurrentState => _currentState;

    public void ChangeState(IState newState)
    {
        if(_currentState!=null) _currentState.OnExit();
        _currentState = newState;
        _currentState.OnEnter();
        Debug.Log($"State changed to {newState.GetType().Name}");
    }
    public void Update()
    {
        if(_currentState!=null)_currentState.Tick();
    }
}