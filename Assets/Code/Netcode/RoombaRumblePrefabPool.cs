using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class RoombaRumblePrefabPool : IPunPrefabPool
{
    protected Dictionary<string, UnityEngine.Object> PrefabCache = new Dictionary<string, UnityEngine.Object>();

    public void Destroy(GameObject gameObject)
    {
        GameObject.Destroy(gameObject);
    }

    public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
    {
        if(!PrefabCache.TryGetValue(prefabId, out UnityEngine.Object prefab))
        {
            prefab = Resources.Load(prefabId);
            PrefabCache.Add(prefabId, prefab);
        }

        GameObject ret = (GameObject)UnityEngine.Object.Instantiate(prefab, position, rotation);

        // PhotonNetwork.Instantiate expects an inactive gameobject
        ret.SetActive(false);

        return ret;
    }
}