using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 基于C# Dictionary源码思路的快速状态机实现
/// </summary>
public class FastFsm
{
    public const int kInvalid = -1;
    private const float kEps = 0.00001f;

    public delegate void StateEnter(int lastState, object arg);
    public delegate void StateUpdate();
    public delegate void StateExit(int newState);
    public delegate void StateTimerEnd();
    public delegate bool StateTransition(object arg);

    private struct StateInfo
    {
        public int identifier;
        public int transitionIndex;

        public float duration;
        public float timer;
        public StateTimerEnd onTimeEnd;

        public StateEnter onEnter;
        public StateUpdate onUpdate;
        public StateExit onExit;
    }

    private struct TransitionInfo
    {
        public int next;
        public int dstStateIndex;
        public StateTransition condition;
        public bool isAutoDetect;

        public bool Valid => condition != null;

        public override string ToString()
        {
            return $"next:{next}, dstStateIndex:{dstStateIndex}, condition:{condition}, isAutoDetect:{isAutoDetect}";
        }
    }

    private bool mNestedLock;
    private bool mIsPrepared;

    private Queue<FastFsmTransitionOperate> mNestedTransitionQueue;
    private FastFsmTransitionOperate? mTransitionQuest;

    private List<StateInfo> mConfStateList;
    private List<TransitionInfo> mConfTransitionList;

    private StateInfo[] mStates;
    private TransitionInfo[] mTransitions;

    /// <summary>
    /// 是否为直接传递,若为true将忽略传递条件
    /// </summary>
    public bool AllowDirectTransition { get; set; }

    /// <summary>
    /// 当前状态机是否开启
    /// </summary>
    public bool IsStart => CurrentStateIndex != kInvalid;

    /// <summary>
    /// 当前状态Identifier
    /// </summary>
    public int CurrentStateIdentifier => StateIndexToIdentifier(CurrentStateIndex);

    /// <summary>
    /// 当前状态索引值
    /// </summary>
    public int CurrentStateIndex { get; private set; }

    public FastFsmStateSetter this[int stateIdentifier]
    {
        get
        {
            if (mIsPrepared)
                throw new Exception("Does not support in prepared edit fsm!");

            if (!HasState(stateIdentifier))
                AddState(stateIdentifier);

            FastFsmStateSetter result = new FastFsmStateSetter();
            result.Initialize(this, StateIdentifierToIndex(stateIdentifier));
            return result;
        }
    }

    public FastFsmTransitionSetter this[int stateIdentifier, int dstStateIdentifier]
    {
        get
        {
            if (mIsPrepared)
                throw new Exception("Does not support in prepared edit fsm!");

            if (!HasState(stateIdentifier))
                AddState(stateIdentifier);

            if (!HasState(dstStateIdentifier))
                AddState(dstStateIdentifier);

            int stateIndex = StateIdentifierToIndex(stateIdentifier);
            int dstStateIndex = StateIdentifierToIndex(dstStateIdentifier);

            if (!HasTransition(stateIndex, dstStateIndex))
                AddTransition(stateIndex, dstStateIndex, null);

            FastFsmTransitionSetter result = new FastFsmTransitionSetter();
            result.Initialize(this, stateIndex, dstStateIndex);
            return result;
        }
    }


    public FastFsm()
    {
        AllowDirectTransition = false;

        CurrentStateIndex = kInvalid;

        mConfStateList = new List<StateInfo>(32);
        mConfTransitionList = new List<TransitionInfo>(32);

        mNestedTransitionQueue = new Queue<FastFsmTransitionOperate>(4);
    }

    /// <summary>
    /// 准备操作
    /// </summary>
    public void Prepare()
    {
        mStates = mConfStateList.ToArray();
        mTransitions = mConfTransitionList.ToArray();

        mIsPrepared = true;
    }

    /// <summary>
    /// 开始状态机
    /// </summary>
    /// <param name="startStateIndex">开始状态索引</param>
    public void Start(int startStateIndex)
    {
        StartByIndex(StateIdentifierToIndex(startStateIndex));
    }

    /// <summary>
    /// 是否存在某个状态
    /// </summary>
    /// <param name="stateIdentifier">状态id</param>
    public bool HasState(int stateIdentifier)
    {
        bool result = false;

        if (mIsPrepared)
        {
            for (int i = 0; i < mStates.Length; i++)
            {
                if (mStates[i].identifier == stateIdentifier)
                {
                    result = true;
                    break;
                }
            }
        }
        else
        {
            for (int i = 0, iMax = mConfStateList.Count; i < iMax; i++)
            {
                if (mConfStateList[i].identifier == stateIdentifier)
                {
                    result = true;
                    break;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 是否存在某个过渡
    /// </summary>
    /// <param name="stateIndex">过渡初始状态索引</param>
    /// <param name="dstStateIndex">过渡目标状态索引</param>
    public bool HasTransition(int stateIndex, int dstStateIndex)
    {
        return GetTransitionIndex(stateIndex, dstStateIndex) != -1;
    }

    /// <summary>
    /// 状态id转换为索引
    /// </summary>
    public int StateIdentifierToIndex(int stateIdentifier)
    {
        int result = -1;

        if (mIsPrepared)
        {
            for (int i = 0; i < mStates.Length; i++)
            {
                if (mStates[i].identifier == stateIdentifier)
                {
                    result = i;
                    break;
                }
            }
        }
        else
        {
            for (int i = 0, iMax = mConfStateList.Count; i < iMax; i++)
            {
                if (mConfStateList[i].identifier == stateIdentifier)
                {
                    result = i;
                    break;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 状态索引转换为id
    /// </summary>
    public int StateIndexToIdentifier(int stateIndex)
    {
        return mIsPrepared ? mStates[stateIndex].identifier : mConfStateList[stateIndex].identifier;
    }

    /// <summary>
    /// 获得过渡索引
    /// </summary>
    public int GetTransitionIndex(int stateIndex, int dstStateIndex)
    {
        StateInfo state = mIsPrepared ? mStates[stateIndex] : mConfStateList[stateIndex];

        int transitionIndex = state.transitionIndex;
        if (transitionIndex == -1) return -1;

        TransitionInfo transition = mIsPrepared
            ? mTransitions[transitionIndex] : mConfTransitionList[transitionIndex];

        while (true)
        {
            if (transition.dstStateIndex == dstStateIndex)
                return transitionIndex;

            if (transition.next == 0)
                break;

            transitionIndex = transition.next;
            transition = mIsPrepared ? mTransitions[transitionIndex] : mConfTransitionList[transitionIndex];
        }

        return -1;
    }

    /// <summary>
    /// 通过状态机索引开始状态机
    /// </summary>
    public void StartByIndex(int startStateIndex)
    {
        if (!mIsPrepared)
            Prepare();

        CurrentStateIndex = startStateIndex;
        mStates[CurrentStateIndex].timer = mStates[CurrentStateIndex].duration;
        mStates[CurrentStateIndex].onEnter?.Invoke(-1, null);
    }

    /// <summary>
    /// 更新状态回调
    /// </summary>
    public void UpdateStateCallback(int stateIndex
        , StateEnter onEnter
        , StateUpdate onUpdate
        , StateExit onExit
        , bool isChangeTime
        , StateTimerEnd onTimeEnd
        , float duration
        , bool isAdd)
    {
        if (mIsPrepared)
            throw new System.NotSupportedException();

        StateInfo stateInfo = mConfStateList[stateIndex];

        if (isAdd)
        {
            if (onEnter != null)
                stateInfo.onEnter += onEnter;

            if (onUpdate != null)
                stateInfo.onUpdate += onUpdate;

            if (onExit != null)
                stateInfo.onExit += onExit;

            if (isChangeTime && onTimeEnd != null)
                stateInfo.onTimeEnd += onTimeEnd;
        }
        else
        {
            if (onEnter != null)
                stateInfo.onEnter = onEnter;
            if (onUpdate != null)
                stateInfo.onUpdate = onUpdate;
            if (onExit != null)
                stateInfo.onExit = onExit;
            if (isChangeTime && onTimeEnd != null)
                stateInfo.onTimeEnd += onTimeEnd;
        }

        stateInfo.duration = duration;
        mConfStateList[stateIndex] = stateInfo;
    }

    /// <summary>
    /// 更新过渡条件
    /// </summary>
    public void UpdateTransitionCondition(int transitionIndex, StateTransition condition, bool autoDetect)
    {
        if (mIsPrepared)
            throw new System.NotSupportedException();

        TransitionInfo transitionInfo = mConfTransitionList[transitionIndex];

        transitionInfo.condition = condition;
        transitionInfo.isAutoDetect = autoDetect;
        mConfTransitionList[transitionIndex] = transitionInfo;
    }

    /// <summary>
    /// 添加状态
    /// </summary>
    public void AddState(int identifier, StateEnter onEnter = null, StateUpdate onUpdate = null, StateExit onExit = null)
    {
        if (mIsPrepared)
            throw new System.NotSupportedException("Does not support in prepared edit fsm!");

        mConfStateList.Add(new StateInfo()
        {
            identifier = identifier,
            transitionIndex = -1,
            onEnter = onEnter,
            onUpdate = onUpdate,
            onExit = onExit
        });
    }

    /// <summary>
    /// 添加过渡
    /// </summary>
    public void AddTransition(int stateIndex, int dstStateIndex, StateTransition condition)
    {
        if (mIsPrepared)
            throw new System.NotSupportedException("Does not support in prepared edit fsm!");

        int transitionIndex = GetTransitionIndex(stateIndex, dstStateIndex);
        if (transitionIndex > -1)
            throw new Exception("Has same state transition!");

        StateInfo state = mConfStateList[stateIndex];

        //若该状态没有过渡，则初始值为-1，若状态没有找到目标过渡，也返回-1。否则到过渡链表结束处，初始值为0
        if (state.transitionIndex > -1)
        {
            transitionIndex = state.transitionIndex;
            TransitionInfo transition = mConfTransitionList[state.transitionIndex];

            while (true)
            {
                if (transition.next == 0)
                {
                    mConfTransitionList.Add(new TransitionInfo()
                    {
                        condition = condition,
                        dstStateIndex = dstStateIndex,
                        next = 0
                    });

                    transition.next = mConfTransitionList.Count - 1;
                    mConfTransitionList[transitionIndex] = transition;

                    break;
                }

                transitionIndex = transition.next;
                transition = mConfTransitionList[transitionIndex];
            }
        }
        else
        {
            TransitionInfo transition = new TransitionInfo()
            {
                condition = condition,
                dstStateIndex = dstStateIndex,
                next = 0
            };
            mConfTransitionList.Add(transition);

            state.transitionIndex = mConfTransitionList.Count - 1;
        }

        mConfStateList[stateIndex] = state;
    }

    /// <summary>
    /// 执行过渡
    /// </summary>
    public void Transition(int dstStateIdentifier, object arg = null, bool immediateUpdate = true)
    {
        TransitionByStateIndex(StateIdentifierToIndex(dstStateIdentifier), arg, immediateUpdate);
    }

    /// <summary>
    /// 通过状态索引执行过渡
    /// </summary>
    public void TransitionByStateIndex(int dstStateIndex, object arg = null, bool immediateUpdate = true)
    {
        if (!mIsPrepared)
            throw new Exception("You need first invoke Prepare() or Start()");

        TransitionInternal(new FastFsmTransitionOperate()
        {
            transitionDstStateIndex = dstStateIndex,
            transitionCacheIndex = -1,
            transitionArg = arg
        }, immediateUpdate);
    }

    /// <summary>
    /// 通过缓存的内部索引执行过渡
    /// </summary>
    public void TransitionByCacheIndex(int transitionIndex, object arg = null, bool immediateUpdate = true)
    {
        if (!mIsPrepared)
            throw new Exception("You need first invoke Prepare() or Start()");

        TransitionInternal(new FastFsmTransitionOperate()
        {
            transitionCacheIndex = transitionIndex,
            transitionArg = arg
        }, immediateUpdate);
    }

    /// <summary>
    /// 执行更新
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!IsStart)
            throw new System.Exception("Please First Invoke 'Start' or 'StartByIndex' Method!");

        if (mNestedLock)
            throw new System.NotSupportedException("Does not support update nested update fsm!");

        mNestedLock = true;

        if (mTransitionQuest.HasValue)
        {
            var (transitionCacheIndex, transitionDstStateIndex, transitionArg) = mTransitionQuest.Value;

            int transitionIndex = -1;
            if (transitionCacheIndex > -1)
                transitionIndex = transitionCacheIndex;
            else
                transitionIndex = GetTransitionIndex(CurrentStateIndex, transitionDstStateIndex);

            if (CanTransition(transitionIndex, transitionArg))
            {
                mStates[CurrentStateIndex].onExit?.Invoke(transitionDstStateIndex);

                ref StateInfo requestDstState = ref mStates[transitionDstStateIndex];
                requestDstState.onEnter?.Invoke(CurrentStateIndex, transitionArg);
                if (requestDstState.onTimeEnd != null)
                    requestDstState.timer = requestDstState.duration;

                CurrentStateIndex = transitionDstStateIndex;
            }

            mTransitionQuest = null;
        }
        else
        {
            ref StateInfo state = ref mStates[CurrentStateIndex];

            int transitionIndex = state.transitionIndex;
            if (transitionIndex > -1)
            {
                ref TransitionInfo transition = ref mTransitions[transitionIndex];

                while (true)
                {
                    if (transition.isAutoDetect)
                    {
                        TransitionInternal(new FastFsmTransitionOperate()
                        {
                            transitionDstStateIndex = transition.dstStateIndex,
                            transitionCacheIndex = -1,
                            transitionArg = null
                        }, false);
                    }

                    if (transition.next > 0)
                        transition = ref mTransitions[transition.next];
                    else
                        break;
                }
            }
        }

        ref StateInfo currentState = ref mStates[CurrentStateIndex];
        if (deltaTime > kEps)
            currentState.onUpdate?.Invoke();
        //If step frame will be calling onUpdate, prevent iterate calling that code.

        if (currentState.onTimeEnd != null)
        {
            if (!float.IsNaN(currentState.timer))
            {
                if (currentState.timer < 0f)
                {
                    currentState.onTimeEnd();
                    currentState.timer = float.NaN;
                }
                else
                {
                    currentState.timer -= deltaTime;
                }
            }
        }

        mNestedLock = false;

        while (mNestedTransitionQueue.TryDequeue(out var operate))
        {
            mTransitionQuest = operate;
            Update(0f);
        }
    }

    public void ResetFsm()
    {
        mNestedLock = false;
        mNestedTransitionQueue.Clear();
        mTransitionQuest = null;
        CurrentStateIndex = kInvalid;
    }

    private void TransitionInternal(FastFsmTransitionOperate operate, bool immediateUpdate)
    {
        if (mNestedLock)
        {
            mNestedTransitionQueue.Enqueue(operate);
        }
        else
        {
            mTransitionQuest = operate;
        }

        if (immediateUpdate)
            Update(0f);
    }

    private bool CanTransition(int transitionIndex, object arg)
    {
        if (transitionIndex == -1) return AllowDirectTransition;

        ref TransitionInfo transition = ref mTransitions[transitionIndex];
        return transition.Valid && transition.condition(arg);
    }

}
