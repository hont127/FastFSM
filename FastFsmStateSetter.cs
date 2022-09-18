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

    public FastFsmStateSetter SetOnEnter(FastFsm.StateEnter onEnter)
    {
        mFsm.UpdateStateCallback(mStateIndex, onEnter, null, null, isChangeTime: false, null, 0f, false);

        return this;
    }

    public FastFsmStateSetter AddOnEnter(FastFsm.StateEnter onEnter)
    {
        mFsm.UpdateStateCallback(mStateIndex, onEnter, null, null, isChangeTime: false, null, 0f, true);

        return this;
    }

    public FastFsmStateSetter SetOnUpdate(FastFsm.StateUpdate onUpdate)
    {
        mFsm.UpdateStateCallback(mStateIndex, null, onUpdate, null, isChangeTime: false, null, 0f, false);

        return this;
    }

    public FastFsmStateSetter AddOnUpdate(FastFsm.StateUpdate onUpdate)
    {
        mFsm.UpdateStateCallback(mStateIndex, null, onUpdate, null, isChangeTime: false, null, 0f, true);

        return this;
    }

    public FastFsmStateSetter SetOnExit(FastFsm.StateExit onExit)
    {
        mFsm.UpdateStateCallback(mStateIndex, null, null, onExit, isChangeTime: false, null, 0f, false);

        return this;
    }

    public FastFsmStateSetter AddOnExit(FastFsm.StateExit onExit)
    {
        mFsm.UpdateStateCallback(mStateIndex, null, null, onExit, isChangeTime: false, null, 0f, true);

        return this;
    }

    public FastFsmStateSetter SetTimer(FastFsm.StateTimerEnd onTimeEnd, float duration)
    {
        mFsm.UpdateStateCallback(mStateIndex, null, null, null, isChangeTime: true, onTimeEnd, duration, false);

        return this;
    }
}