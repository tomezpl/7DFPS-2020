using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"{other.name} ({other.tag}) triggered me");
        foreach (string tag in AllowedTags)
        {
            if (other.CompareTag(tag))
            {
                Event.Invoke();
                break;
            }
        }
    }
}
