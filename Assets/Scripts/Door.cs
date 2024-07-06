using UnityEngine;
using Photon.Pun;

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
        isOpen = !isOpen;
        anim.SetBool("IsOpen", isOpen);
        Debug.Log("DoorState changed to " + isOpen);

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
