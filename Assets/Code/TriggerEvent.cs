using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Trigger controller that invokes <see cref="Event"/> if a Trigger contact is reported, and can be triggered from other scripts via <see cref="TryTrigger(RoombaRumbleEvent)"/>.
/// </summary>
public class TriggerEvent : MonoBehaviour
{
    public UnityEvent Event;
    public string[] AllowedTags = new string[0];

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TryTrigger(RoombaRumbleEvent eventType)
    {
        switch(eventType)
        {
            case RoombaRumbleEvent.HammerHitAnimable:
                Event.Invoke();
                break;
        }
    }

    public void TryTrigger(Component caller)
    {
        foreach (string tag in AllowedTags)
        {
            if (caller.CompareTag(tag))
            {
                Event.Invoke();
                break;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTrigger(other);
    }
}
