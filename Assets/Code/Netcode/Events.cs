using ExitGames.Client.Photon;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class Events
{
    public const int DealDamageCode = 1;

    public static void DealDamage(DamageData damage)
    {
        PhotonNetwork.RaiseEvent(DealDamageCode, damage.ToArray(), new Photon.Realtime.RaiseEventOptions
        {
            // Send the event to all players to keep damage in sync; the event handler filters for ownership using ViewIDs.
            Receivers = Photon.Realtime.ReceiverGroup.All
        },
        SendOptions.SendReliable);
    }
}
