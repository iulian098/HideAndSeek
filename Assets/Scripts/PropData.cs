using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PropData : MonoBehaviour
{
    public string prefabLocation;
    public Rigidbody rb;
    bool isMoving;
    public bool propDetected;
    public bool dontDestroy;
    public bool isInstantiated;
    Coroutine toggleKinematic;
    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (!isInstantiated || !dontDestroy)
            {
                float rand = Random.Range(0f, 1f);
                if (rand < 0.3f)
                    PhotonNetwork.Destroy(this.gameObject);
            }
            TryGetComponent(out rb);
        }
    }
    private void FixedUpdate()
    {
        if(rb != null)
        {
            if (!propDetected)
            {
                if (rb.velocity.magnitude < 0.01f && rb.angularVelocity.magnitude < 0.01f && isMoving)
                {
                    isMoving = false;
                    if (toggleKinematic == null)
                        toggleKinematic = StartCoroutine(kinematic());
                }
                else if (rb.velocity.magnitude >= 0.01f && rb.angularVelocity.magnitude >= 0.01f && !isMoving)
                {
                    isMoving = true;
                }

                if (!isMoving)
                {

                }
            }
            else
            {
                if (rb.isKinematic)
                    rb.isKinematic = false;
            }
        }
    }

    IEnumerator kinematic()
    {
        Debug.Log("Stop moving " + rb.name);
        yield return new WaitForSeconds(1);
        rb.isKinematic = true;
        toggleKinematic = null;
    }

    private void OnCollisionStay(Collision collision)
    {
        if(collision.collider.tag == "Prop")
        {
            propDetected = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if(collision.collider.tag == "Prop")
        {
            propDetected = false;
        }
    }
}
