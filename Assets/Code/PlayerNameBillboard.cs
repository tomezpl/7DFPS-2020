using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerNameBillboard : MonoBehaviour
{
    LobbyManager _lobby;

    // Start is called before the first frame update
    void Start()
    {
        _lobby = GameObject.Find("GameManager").GetComponent<LobbyManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if(_lobby?.localPlayerObj)
        {
            foreach(GameObject obj in GameObject.FindGameObjectsWithTag("Player"))
            {
                if(PhotonView.Get(obj) && PhotonView.Get(obj).IsMine)
                {
                    continue;
                }

                foreach(TextMeshPro tmp in obj.GetComponentsInChildren<TextMeshPro>())
                {
                    if(tmp.name == "PlayerName")
                    {
                        tmp.transform.LookAt(_lobby.localPlayerObj.transform);
                        tmp.transform.Rotate(0f, 180f, 0f, Space.Self);
                        break;
                    }
                }
            }
        }
    }
}
