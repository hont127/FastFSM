using System;
using System.Collections;
using System.Collections.Generic;

public class FastFsm
{
    public const float kTimerInvalid = -100f;
    public const float kFloatEps = 0.000001f;

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

        public bool Valid => condition != null;
    }

    private bool mNestedLock;
    private bool mIsPrepared;

    private Queue<FastFsmTransitionOperate> mNestedTransitionQueue;
    private FastFsmTransitionOperate? mTransitionQuest;

    private List<StateInfo> mConfStateList;
    private List<TransitionInfo> mConfTransitionList;

    private StateInfo[] mStates;
    private TransitionInfo[] mTransitions;

    public bool AllowDirectTransition { get; set; }

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
        AllowDirectTransition = true;

        mConfStateList = new List<StateInfo>(32);
        mConfTransitionList = new List<TransitionInfo>(32);

        mNestedTransitionQueue = new Queue<FastFsmTransitionOperate>(4);
    }

    public void Prepare()
    {
        mStates = mConfStateList.ToArray();
        mTransitions = mConfTransitionList.ToArray();
        mIsPrepared = true;
    }

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

    public bool HasTransition(int stateIndex, int dstStateIndex)
    {
        return TransitionToIndex(stateIndex, dstStateIndex) != -1;
    }

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

    public int StateIndexToIdentifier(int stateIndex)
    {
        return mIsPrepared ? mStates[stateIndex].identifier : mConfStateList[stateIndex].identifier;
    }

    public int TransitionToIndex(int stateIndex, int dstStateIndex)
    {
        StateInfo state = mIsPrepared ? mStates[stateIndex] : mConfStateList[stateIndex];

        int transitionIndex = state.transitionIndex;
        if (transitionIndex == -1) return -1;

        TransitionInfo transition = mIsPrepared ? mTransitions[transitionIndex] : mConfTransitionList[transitionIndex];

        while (true)
        {
            if (transition.dstStateIndex == dstStateIndex)
            {
                return transitionIndex;
            }

            if (transition.next == 0)
            {
                break;
            }

            transitionIndex = transition.next;
            transition = mIsPrepared ? mTransitions[transitionIndex] : mConfTransitionList[transitionIndex];
        }

        return -1;
    }

    public void Start(int startStateIndex)
    {
        StartByIndex(StateIdentifierToIndex(startStateIndex));
    }

    public void StartByIndex(int startStateIndex)
    {
        if (!mIsPrepared)
            Prepare();

        CurrentStateIndex = startStateIndex;
        mStates[CurrentStateIndex].timer = mStates[CurrentStateIndex].duration;
        mStates[CurrentStateIndex].onEnter?.Invoke(-1, null);
    }

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

    public void UpdateTransitionCondition(int transitionIndex, StateTransition condition)
    {
        if (mIsPrepared)
            throw new System.NotSupportedException();

        TransitionInfo transitionInfo = mConfTransitionList[transitionIndex];
        transitionInfo.condition = condition;
        mConfTransitionList[transitionIndex] = transitionInfo;
    }

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

    public void AddTransition(int stateIndex, int dstStateIndex, StateTransition condition)
    {
        if (mIsPrepared)
            throw new System.NotSupportedException("Does not support in prepared edit fsm!");

        int transitionIndex = TransitionToIndex(stateIndex, dstStateIndex);
        if (transitionIndex > -1)
            throw new Exception("Has same state transition!");

        StateInfo state = mConfStateList[stateIndex];
        if (state.transitionIndex > -1)
        {
            TransitionInfo transition = mTransitions[state.transitionIndex];

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

                    break;
                }

                transition = mTransitions[transition.next];
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

    public void Transition(int dstStateIdentifier, object arg = null, bool immediateUpdate = true)
    {
        TransitionByStateIndex(StateIdentifierToIndex(dstStateIdentifier), arg, immediateUpdate);
    }

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

    public void Update(float deltaTime)
    {
        if(mNestedLock)
            throw new System.NotSupportedException("Does not support update nested update fsm!");

        mNestedLock = true;

        if (mTransitionQuest.HasValue)
        {
            var (transitionCacheIndex, transitionDstStateIndex, transitionArg) = mTransitionQuest.Value;

            int transitionIndex = -1;
            if (transitionCacheIndex > -1)
                transitionIndex = transitionCacheIndex;
            else
                transitionIndex = TransitionToIndex(CurrentStateIndex, transitionDstStateIndex);

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

        ref StateInfo currentState = ref mStates[CurrentStateIndex];
        currentState.onUpdate?.Invoke();

        if (currentState.onTimeEnd != null)
        {
            if (currentState.timer < kFloatEps && currentState.timer > kTimerInvalid + kFloatEps)
            {
                currentState.onTimeEnd();
                currentState.timer = kTimerInvalid;
            }
            else
            {
                currentState.timer -= deltaTime;
            }
        }

        mNestedLock = false;

        while (mNestedTransitionQueue.TryDequeue(out FastFsmTransitionOperate operate))
        {
            mTransitionQuest = operate;
            Update(0f);
        }
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
