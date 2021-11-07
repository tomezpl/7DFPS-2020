using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ScheduledObjectSpawner : NetworkBehaviour
{
    float lastSpawnTime = 0f;
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
