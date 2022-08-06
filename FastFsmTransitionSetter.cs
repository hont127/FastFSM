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
        mTransitionIndex = mFsm.TransitionToIndex(stateIndex, dstStateIndex);
    }

    public FastFsmTransitionSetter SetTransition(FastFsm.StateTransition condition)
    {
        mFsm.UpdateTransitionCondition(mTransitionIndex, condition);

        return this;
    }
}
