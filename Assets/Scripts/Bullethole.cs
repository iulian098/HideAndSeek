using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullethole : MonoBehaviour
{
    public AudioSource audioSource;
    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, 2);
        audioSource.PlayOneShot(GameManager.instance.gs.bulletImpact[Random.Range(0, GameManager.instance.gs.bulletImpact.Length)]);
    }
}
