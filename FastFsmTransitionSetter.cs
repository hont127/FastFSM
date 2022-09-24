using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FastFsmTransitionSetter
{
    private FastFsm mFsm;
    private int mTransitionIndex;


    public void Initialize(FastFsm fsm, int stateIndex, int dstStateIndex)
    {
        mFsm = fsm;
        mTransitionIndex = mFsm.GetTransitionIndex(stateIndex, dstStateIndex);
    }

    /// <summary>
    /// 设置一个状态过渡
    /// </summary>
    /// <param name="condition">过渡条件</param>
    /// <param name="autoDetect">是否为自动判断过渡，若为true则每次更新都自动判断过渡条件</param>
    /// <returns></returns>
    public FastFsmTransitionSetter SetTransition(FastFsm.StateTransition condition, bool autoDetect = false)
    {
        mFsm.UpdateTransitionCondition(mTransitionIndex, condition, autoDetect);

        return this;
    }
}
