using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FastFsmDemo : MonoBehaviour
{
    const int kStateIdle = 1;
    const int kStateRun = 2;
    const int kStateHit = 3;
    const int kStateFreeze = 4;


    private void Start()
    {
        FastFsm fsm = new();

        fsm[kStateIdle]
            .SetOnEnter((lastState, arg) =>
            {
                Debug.Log("On Idle State Enter!");
            })
            .SetOnUpdate(() =>
            {
                Debug.Log("Idle State Update!");
            })
            .SetOnExit((newState) =>
            {
                Debug.Log("On Idle State Exit!");
            })
            .SetTimer(() =>
            {
                fsm.Transition(kStateRun, "TimeEnd");
            }, 1f);

        fsm[kStateRun]
            .SetOnEnter((lastState, arg) =>
            {
                Debug.Log("On Run State Enter!");
            })
            .SetOnUpdate(() =>
            {
                Debug.Log("Run State Update!");
            })
            .SetOnExit((newState) =>
            {
                Debug.Log("On Run State Exit!");
            });

        fsm[kStateHit]
            .SetOnEnter((lastState, arg) =>
            {
                Debug.Log("On Hit State Enter!");
            })
            .SetOnUpdate(() =>
            {
                Debug.Log("Hit State Update!");
            })
            .SetOnExit((newState) =>
            {
                Debug.Log("On Hit State Exit!");
            });

        fsm[kStateFreeze]
            .SetOnEnter((lastState, arg) =>
            {
                Debug.Log("On Freeze State Enter!");
            })
            .SetOnUpdate(() =>
            {
                Debug.Log("Freeze State Update!");
            })
            .SetOnExit((newState) =>
            {
                Debug.Log("On Freeze State Exit!");
            });

        fsm[kStateIdle, kStateRun].SetTransition(arg =>
        {
            if (arg != null && arg.ToString() == "TimeEnd")
                return true;
            else
                return false;
        });

        fsm.Prepare();
        fsm.Start(kStateIdle);

        Debug.Log("Current State Old: " + fsm.CurrentStateIndex);
        for (int i = 0; i < 3360; i++)
        {
            fsm.Update(Time.deltaTime);
        }
        Debug.Log("Current State New: " + fsm.CurrentStateIndex);
    }
}
