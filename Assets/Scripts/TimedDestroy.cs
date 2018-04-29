using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class TimedDestroy : NetworkBehaviour
{
    public float Time;

    Coroutine c = null;

    void Update()
    {
        if (c == null)
        {
            c = StartCoroutine(TimedDestroyHelper());
        }
    }

    private IEnumerator TimedDestroyHelper()
    {
        yield return new WaitForSeconds(Time);
        this.enabled = false;
        Destroy(this.gameObject);
    }
}
