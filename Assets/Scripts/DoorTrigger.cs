using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    public PhotonView pv;

    public void TriggerDoor()
    {
        pv.RPC("DoorState", RpcTarget.AllBuffered);
    }
}
