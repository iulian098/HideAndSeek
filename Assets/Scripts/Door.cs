using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Door : MonoBehaviourPunCallbacks
{
    public Animator anim;
    public PhotonView pv;
    public bool isOpen;
    public AudioSource audioSource;
    GameManager gm;

    private void Start()
    {
        gm = GameManager.instance;
    }

    [PunRPC]
    public void DoorState()
    {
        
        Debug.Log("DoorState changed to " + isOpen);
        if (isOpen)
        {
            isOpen = false;
            anim.SetBool("IsOpen", false);
        }
        else
        {
            isOpen = true;
            anim.SetBool("IsOpen", true);
        }

    }

    public void PlayDoorOpen()
    {
        audioSource.PlayOneShot(gm.gs.door_Open[Random.Range(0, gm.gs.door_Open.Length)]);
    }

    public void PlayDoorClose()
    {
        audioSource.PlayOneShot(gm.gs.door_Close[Random.Range(0, gm.gs.door_Close.Length)]);
    }
}
