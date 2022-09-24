using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FastFsmStateSetter
{
    private FastFsm mFsm;
    private int mStateIndex;


    public void Initialize(FastFsm fsm, int stateIndex)
    {
        mFsm = fsm;
        mStateIndex = stateIndex;
    }

    /// <summary>
    /// 当状态进入时
    /// </summary>
    /// <param name="onEnter">参数1-lastState, 参数2-arg</param>
    public FastFsmStateSetter SetOnEnter(FastFsm.StateEnter onEnter)
    {
        mFsm.UpdateStateCallback(mStateIndex, onEnter, null, null, isChangeTime: false, null, 0f, false);

        return this;
    }

    /// <summary>
    /// 当状态进入时(回调添加)
    /// </summary>
    /// <param name="onEnter">参数1-lastState, 参数2-arg</param>
    public FastFsmStateSetter AddOnEnter(FastFsm.StateEnter onEnter)
    {
        mFsm.UpdateStateCallback(mStateIndex, onEnter, null, null, isChangeTime: false, null, 0f, true);

        return this;
    }

    /// <summary>
    /// 当状态更新时
    /// </summary>
    public FastFsmStateSetter SetOnUpdate(FastFsm.StateUpdate onUpdate)
    {
        mFsm.UpdateStateCallback(mStateIndex, null, onUpdate, null, isChangeTime: false, null, 0f, false);

        return this;
    }

    /// <summary>
    /// 当状态更新时(回调添加)
    /// </summary>
    public FastFsmStateSetter AddOnUpdate(FastFsm.StateUpdate onUpdate)
    {
        mFsm.UpdateStateCallback(mStateIndex, null, onUpdate, null, isChangeTime: false, null, 0f, true);

        return this;
    }

    /// <summary>
    /// 当状态退出时
    /// </summary>
    /// <param name="onEnter">参数1-newState</param>
    public FastFsmStateSetter SetOnExit(FastFsm.StateExit onExit)
    {
        mFsm.UpdateStateCallback(mStateIndex, null, null, onExit, isChangeTime: false, null, 0f, false);

        return this;
    }

    /// <summary>
    /// 当状态退出时(回调添加)
    /// </summary>
    /// <param name="onEnter">参数1-newState</param>
    public FastFsmStateSetter AddOnExit(FastFsm.StateExit onExit)
    {
        mFsm.UpdateStateCallback(mStateIndex, null, null, onExit, isChangeTime: false, null, 0f, true);

        return this;
    }

    /// <summary>
    /// 设置该状态的计时器
    /// </summary>
    /// <param name="onTimeEnd">当计时器触发</param>
    /// <param name="duration">持续时间</param>
    /// <returns></returns>
    public FastFsmStateSetter SetTimer(FastFsm.StateTimerEnd onTimeEnd, float duration)
    {
        mFsm.UpdateStateCallback(mStateIndex, null, null, null, isChangeTime: true, onTimeEnd, duration, false);

        return this;
    }
}
