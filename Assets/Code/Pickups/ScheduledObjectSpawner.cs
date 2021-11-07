using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Script that controls periodically spawning a networked prefab in the scene. Only one instance can be spawned at a time. Cooldown starts when the instance is destroyed.
/// </summary>
public class ScheduledObjectSpawner : NetworkBehaviour
{
    /// <summary>
    /// Last time the object was spawned in.
    /// </summary>
    float lastSpawnTime = 0f;

    /// <summary>
    /// Cooldown to wait before respawning after <see cref="currentInstance"/> was destroyed.
    /// </summary>
    public float SpawnCooldown = 15f;
    GameObject currentInstance = null;
    public GameObject PrefabToSpawn;

    // Start is called before the first frame update
    void Start()
    {
        if(IsServer)
        {
            SpawnObject();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (IsServer)
        {
            if (currentInstance)
            {
                lastSpawnTime = Time.time;
            }
            else if (currentInstance == null && (lastSpawnTime == 0f || Time.time - lastSpawnTime >= SpawnCooldown))
            {
                SpawnObject();
            }
        }
    }

    void SpawnObject()
    {
        lastSpawnTime = Time.time;

        GameObject spawned = Instantiate(PrefabToSpawn, transform.position, transform.rotation);
        if (spawned.TryGetComponent(out NetworkObject netObj))
        {
            netObj.Spawn();
            currentInstance = spawned;
        }
    }
}
