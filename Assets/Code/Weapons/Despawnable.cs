using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Despawnable : MonoBehaviour
{
    public float timeToLive = 1f;

    float _timePassed = 0f;

    // Update is called once per frame
    public virtual void Update()
    {
        _timePassed += Time.deltaTime;

        if(_timePassed >= timeToLive)
        {
            Destroy(gameObject);
        }
    }
}
