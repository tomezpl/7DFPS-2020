using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Animation controller for the ironing board collapse animation.
/// </summary>
public class CollapseIronBoardAnim : NetworkBehaviour
{
    private Animation Animation;

    private bool HasCollapsed = false;

    private NetworkVariable<bool> HasCollapsedGlobal = new NetworkVariable<bool>(NetworkVariableReadPermission.Everyone, false);

    // Start is called before the first frame update
    void Start()
    {
        Animation = GetComponent<Animation>();
        HasCollapsed = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(!HasCollapsed && HasCollapsedGlobal.Value)
        {
            PlayCollapseAnim();
        }
    }

    [ServerRpc]
    public void TriggerCollapseServerRpc(ServerRpcParams rpcParams = default)
    {
        PlayCollapseAnimClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds } });
        HasCollapsedGlobal.Value = true;
    }

    [ClientRpc]
    public void PlayCollapseAnimClientRpc(ClientRpcParams rpcParams = default)
    {
        if (!HasCollapsed)
        {
            PlayCollapseAnim();
        }
    }

    public void PlayCollapseAnim()
    {
        Animation.Play();
        HasCollapsed = true;
    }

    public void TriggerCollapse()
    {
        TriggerCollapseServerRpc();
    }
}
