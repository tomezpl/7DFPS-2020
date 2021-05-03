using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A base script for an object that will destroy after enough time has passed.
/// </summary>
public class Despawnable : MonoBehaviour
{
    /// <summary>
    /// Lifespan of this object (in seconds).
    /// </summary>
    public float TimeToLive = 1f;

    /// <summary>
    /// <para>Current lifetime of this object.</para>
    /// <para>Once <see cref="TimeToLive"/> is reached, the object is destroyed.</para>
    /// <para>Derived classes can override this to 0 to delay the despawn.</para>
    /// </summary>
    protected float timePassed = 0f;

    // Update is called once per frame
    public virtual void Update()
    {
        timePassed += Time.deltaTime;

        if(timePassed >= TimeToLive)
        {
            Destroy(gameObject);
        }
    }
}
