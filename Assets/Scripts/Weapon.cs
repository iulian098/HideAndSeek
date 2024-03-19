using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.VFX;

public class Weapon : MonoBehaviourPunCallbacks
{
    public enum type
    {
        Melee,
        Ranged
    }

    public type _type;

    public int damage;
    public int ammo = 150;
    public float fireRate;
    public float recoil;
    public PlayerController pc;
    public VisualEffect muzzleflash;
    public Light muzzleFlashLight;
    public ParticleSystem bullets;
    public ParticleSystem weaponSmoke;
    public AudioClip weaponSound;
    public AudioSource audioSource;

    int bulletsShotCount;
    float lastShotTime;
    float currentRecoil;

    Transform camTransform;
    PhotonView target;
    RaycastHit hit;
    GameSettings gs;
    Coroutine muzzleFlashCoroutine;
    private void Start()
    {
        gs = Resources.Load("GameSettings") as GameSettings;
        camTransform = pc.camTransform;
    }
    private void Update()
    {
        if (photonView.IsMine)
        {
            if (_type == type.Melee)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (target)
                    {
                        target.RPC("TakeDamage", RpcTarget.AllBuffered, damage);
                    }
                }
            }

            if (_type == type.Ranged && ammo > 0)
            {
                if (Input.GetButton("Fire1"))
                {
                    if (Time.time - lastShotTime > fireRate)
                    {
                        lastShotTime = Time.time;
                        photonView.RPC("FireWeapon", RpcTarget.AllViaServer);
                    }
                }
                else
                {
                    if(bulletsShotCount > 5)
                        weaponSmoke.Play();

                    if(bulletsShotCount != 0)
                        bulletsShotCount = 0;

                    if (currentRecoil != 0)
                        currentRecoil = 0;
                }
            }
        }
    }

    [PunRPC]
    public void FireWeapon()
    {
        bulletsShotCount++;
        muzzleflash.Play();
        audioSource.PlayOneShot(weaponSound);

        if (muzzleFlashCoroutine == null)
            muzzleFlashCoroutine = StartCoroutine(muzzleLight());

        if(currentRecoil < 0.1f)
            currentRecoil += recoil;

        Ray r = new Ray(camTransform.position, camTransform.forward + new Vector3(Random.Range(-currentRecoil, currentRecoil), currentRecoil, 0));
        if (Physics.Raycast(r, out hit, 100f, gs.weaponHitLayer))
        {
            if (photonView.IsMine)
            {

                bullets.transform.LookAt(hit.point);
                bullets.Emit(1);
                if (hit.collider.tag == "Hider")
                {
                    PhotonNetwork.Instantiate("VFX/" + gs.blood.name, hit.point, Quaternion.LookRotation(hit.normal));
                    try
                    {
                        hit.collider.transform.parent.TryGetComponent(out target);
                    }
                    catch
                    {
                        hit.collider.TryGetComponent(out target);
                    }
                    if (target != null)
                        target.RPC("TakeDamage", RpcTarget.All, damage, pc.name);
                    else
                        Debug.LogError("Parent PhotonView not found");
                }
                else
                {
                    GameObject go = PhotonNetwork.Instantiate("VFX/" + gs.bulletHoles[0].name, hit.point, Quaternion.LookRotation(hit.normal));
                    Vector3 randomSize = new Vector3(Random.Range(0.8f, 1.2f), Random.Range(0.8f, 1.2f), 1);
                    go.transform.localScale = randomSize;
                    go.transform.SetParent(hit.collider.transform);
                    pc.TakeDamage(1, pc.name);

                    if (hit.collider.tag == "Prop")
                    {
                        try
                        {
                            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
                            rb.isKinematic = false;
                            rb.AddForceAtPosition(-hit.normal * 150f, hit.point);
                        }
                        catch
                        {
                            Debug.Log("No rigidbody found on " + hit.collider.name);
                        }
                    }

                }
            }
            else
            {
                bullets.Emit(1);
            }
        }
        else
        {
            bullets.transform.localRotation = Quaternion.identity;
            bullets.Emit(1);
        }
#if !UNITY_EDITOR
        //ammo--;
#endif
    }

    void SpawnBulletHole(Vector3 pos)
    {
        Instantiate(gs.bulletHoles[0], pos, Quaternion.identity);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Hider" && !target)
        {
            target = other.GetComponent<PhotonView>();
            if (target == null)
                target = other.GetComponentInParent<PhotonView>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.tag == "Hider" && target)
        {
            target = null;
        }
    }

    IEnumerator muzzleLight()
    {
        muzzleFlashLight.enabled = true;
        muzzleFlashLight.intensity = Random.Range(2f, 4f);
        yield return new WaitForSeconds(0.02f);
        muzzleFlashCoroutine = null;
        muzzleFlashLight.enabled = false;
    }
}
