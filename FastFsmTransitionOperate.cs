using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct FastFsmTransitionOperate
{
    public int transitionCacheIndex;
    public int transitionDstStateIndex;
    public object transitionArg;


    public void Deconstruct(out int desTransitionCacheIndex, out int desTransitionDstStateIndex, out object desTransitionArg)
    {
        desTransitionCacheIndex = transitionCacheIndex;
        desTransitionDstStateIndex = transitionDstStateIndex;
        desTransitionArg = transitionArg;
    }
}
