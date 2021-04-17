using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A base class for Weapon scripts.
/// </summary>
public class Weapon : MonoBehaviour
{
    /// <summary>
    /// Does this weapon belong to the local player?
    /// </summary>
    public bool IsMine { get { return transform.root.GetComponent<RoombaControl>().PlayerControlled; } }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
