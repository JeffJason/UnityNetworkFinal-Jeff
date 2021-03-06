using System;
using System.Collections;
using UnityEngine;

public class Waiting : CustomYieldInstruction
{
    private float time = 0.0f;
    private Func<bool> pred;

    public override bool keepWaiting
    {
        get
        {
            return Time.time < time && !pred.Invoke();
        }
    }

    public Waiting(float seconds, Func<bool> predicate)
    {
        time = Time.time + seconds;
        pred = predicate;
    }

}
