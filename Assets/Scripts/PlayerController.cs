using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.VFX;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class PlayerController : Player, IPunObservable
{
    [System.Serializable]
    public class Inputs
    {
        public float Movement;
        public float Strafe;
        public float MouseX;
        public float MouseY;

        public bool crouch;
        public bool jump;
        public bool attack;
        public bool run;
        public bool use;
        public bool transform;
        public bool freeCam;
        public bool changeCam;
    }

    [System.Serializable]
    public class Speeds
    {
        public float jogSpeed = 7;
        public float runSpeed = 10;
        public float crouchSpeed = 5;
        public float jumpForce = 10;
        public float XRotationSpeed = 10;
        public float YRotationSpeed = 3;
        public float transformedSpeed = 5;
    }

    public bool canControl = true;
    public bool isSeeker;

    public GameObject[] mainPlayerObj;
    public GameObject transformObject;
    public Weapon _weapon;
    
    [Header("Camera control")]
    //Camera target
    [HideInInspector]
    public Transform camTransform;
    public Transform cameraTarget;

    public FPSCamera fpsCamera;

    public Transform tpsCamParent;

    public SkinnedMeshRenderer headObject;

    Vector3 cameraTargetDefaultPos;
    public Transform HeadIKTarget;

    [HideInInspector]
    public AudioSource audioSource;
    [HideInInspector]
    public Animator anim;
    [HideInInspector]
    public Rigidbody rb;

    //Layer mask
    public LayerMask jumpLayer;
    public LayerMask propsLayer;
    public LayerMask lookAtLayer;

    public PhysicMaterial zeroFrictionMaterial;
    public Inputs _input;
    public Speeds _speed;

    public VisualEffect transformVFX;
    public ParticleSystem bloodVFX;

    bool grounded;
    bool died = false;
    bool canRun;

    GameManager gm;

    //Door
    bool doorDetected;
    PhotonView doorPV;

    CapsuleCollider mainColl;
    RaycastHit hit;
    int targetTrasformObject;
    int playerID;
    int seekerID;
    public int transformID;

    float xAxisLastValue;
    float colliderDiv;
    float defaultCollH;

    GameSettings gs;
    bool fpsCam;

    AxisState xAxis;
    AxisState yAxis;

    Coroutine taunt;

    private void Start()
    {
        GetComponents();
        gs = Resources.Load("GameSettings") as GameSettings;
        camTransform = Camera.main.transform;
        Cursor.visible = false;
        gm = GameManager.instance;
        cameraTargetDefaultPos = cameraTarget.localPosition;

        if (photonView.IsMine)
        {
            //Setup
            xAxis = gs.xAxis;
            yAxis = gs.yAxis;
            gm.cvc.Follow = cameraTarget;
            gm.cvc.LookAt = cameraTarget;
            gm._player = this;
            gm.localPhotonView = photonView;
        }
        else
            rb.isKinematic = true;

        photonView.RPC("SetName", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.NickName);
    }

    private void Update() {

        if (!photonView.IsMine) return;
        if (!isSeeker) {
            if (rb.velocity.magnitude < 0.1f) {
                if (taunt == null)
                    taunt = StartCoroutine(tauntSound(15));
            }
            else {
                if (taunt != null) {
                    StopCoroutine(taunt);
                    taunt = null;
                }
            }
        }

        if (canControl) {
            GetInput();

            //Jump
            if (grounded && _input.jump)
                Jump();
            anim.SetBool("Jump", _input.jump);

            //Transform
            int objID = CheckForObjects();

            if (!isSeeker) {
                if (objID != -1) gm._panels.transformPanel.SetActive(true);
                else gm._panels.transformPanel.SetActive(false);

                if (gm.hidersCanTransform && _input.transform)
                    TransformToObject(objID);

                if (Input.GetKeyDown(KeyCode.R))
                    photonView.RPC(nameof(TransformToPlayer), RpcTarget.AllBuffered);
            }

            //Open/Close door
            if (_input.use && doorDetected)
                doorPV.RPC(nameof(Door.DoorState), RpcTarget.AllViaServer);
            anim.SetBool("Grounded", grounded);

            if (_input.changeCam && fpsCamera.parentTransform != null) {
                fpsCam = !fpsCam;
                ChangeCam();
            }

            if (fpsCam)
                fpsCamera.parentTransform.position = fpsCamera.target.position;//new Vector3(fpsCamera.parentTransform.position.x, localPos.y, localPos.z);
        }
    }

    void FixedUpdate()
    {
        if (photonView.IsMine)
        {
            xAxis.Update(Time.fixedDeltaTime);
            yAxis.Update(Time.fixedDeltaTime);
            if (canControl)
            {
                //Animation
                anim.SetFloat("V", _input.Movement);
                anim.SetFloat("H", _input.Strafe);

                
                anim.SetBool("Crouch", _input.crouch);

                MovePlayer();

                CheckGround();

                if(HeadIKTarget && !_input.freeCam)
                    CameraIKPoint();
            }

            if(fpsCam && camTransform.localPosition != Vector3.zero)
            {
                camTransform.localPosition = Vector3.zero;
                camTransform.localRotation = Quaternion.identity;
            }

            if (!isSeeker)
            {
                if (_input.run && !_input.crouch && (_input.Movement != 0 || _input.Strafe != 0) && stamina > 0f && canRun)
                {
                    stamina -= Time.deltaTime * 4;
                    anim.SetBool("Run", true);
                }
                else if(!_input.run && stamina < maxStamina)
                {
                    stamina += Time.deltaTime * 3;
                    anim.SetBool("Run", false);
                }
            }

            if(stamina < 0f && canRun)
            {
                stamina = 0;
                canRun = false;
            }else if(stamina > maxStamina)
            {
                stamina = maxStamina;
            }

            if (stamina > 10f && !canRun)
                canRun = true;

        }

        colliderDiv = anim.GetFloat("CollDiv");
        if (colliderDiv > 1)
        {
            mainColl.height = defaultCollH / colliderDiv;
            Vector3 center = mainColl.center;
            center.y = mainColl.height / 2;
            mainColl.center = center;
        }
        else if (mainColl.height != defaultCollH)
        {
            mainColl.height = defaultCollH;
            Vector3 center = mainColl.center;
            center.y = mainColl.height / 2;
            mainColl.center = center;
        }
    }

    void GetComponents()
    {
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        mainColl = GetComponent<CapsuleCollider>();
        defaultCollH = mainColl.height;
    }

    void GetInput()
    {
        if (Input.GetButtonDown("Free look"))
            xAxisLastValue = xAxis.Value;
        if (Input.GetButtonUp("Free look"))
            xAxis.Value = xAxisLastValue;

        _input.use = Input.GetButtonDown("Use");
        _input.Movement = Input.GetAxis("Vertical");
        _input.Strafe = Input.GetAxis("Horizontal");
        _input.crouch = Input.GetKey(KeyCode.LeftControl);
        _input.jump = Input.GetButtonDown("Jump");
        _input.attack = Input.GetButtonDown("Fire1");
        _input.MouseX = Input.GetAxis("Mouse X");
        _input.MouseY = Input.GetAxis("Mouse Y");
        _input.transform = Input.GetButtonDown("Transform");
        _input.run = Input.GetButton("Sprint");
        _input.freeCam = Input.GetKey(KeyCode.LeftAlt);
        _input.changeCam = Input.GetKeyDown(KeyCode.V);
    }

    void MovePlayer()
    {
        float movementSpeed = 0;

        //Modify speed
        #region Speed
        if (!isTransformed)
        {
            if (_input.run && !_input.crouch)
            {
                if (!isSeeker && canRun)
                {
                    if (movementSpeed != _speed.runSpeed)
                        movementSpeed = _speed.runSpeed;
                }else if (isSeeker)
                {
                    if (movementSpeed != _speed.runSpeed)
                        movementSpeed = _speed.runSpeed;
                }
                else
                {
                    if (movementSpeed != _speed.jogSpeed)
                        movementSpeed = _speed.jogSpeed;
                }
            }
            else if (_input.crouch)
            {
                if (movementSpeed != _speed.crouchSpeed)
                    movementSpeed = _speed.crouchSpeed;
            }
            else
            {
                if (movementSpeed != _speed.jogSpeed)
                    movementSpeed = _speed.jogSpeed;
            }
        }
        else
        {
            if (movementSpeed != _speed.transformedSpeed)
                movementSpeed = _speed.transformedSpeed;
        }
        #endregion

        //Movement
        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);
        localVelocity.z = _input.Movement * movementSpeed;
        localVelocity.x = _input.Strafe * movementSpeed;
        rb.velocity = transform.TransformDirection(localVelocity);

        //Rotate player if freeCam is disabled
        if (!_input.freeCam)
        {
            Vector3 rot = transform.rotation.eulerAngles;
            rot.y = xAxis.Value;
            transform.rotation = Quaternion.Euler(rot);
        }


        if (fpsCam)
        {
            Vector3 camRot = fpsCamera.childTransform.localRotation.eulerAngles;
            if (!_input.freeCam)
            {
                //First person camera control
                if(xAxis.m_MaxValue != gs.xAxis.m_MaxValue)
                    xAxis = gs.xAxis;
                //Rotate on the y axis if free cam is disabled
                camRot.x = yAxis.Value;
                camRot.y = 0;
                
                //Set camera rotation

            }
            else
            {
                //Get local rotation
                if (xAxis.m_MaxValue != gs.xAxisCapped.m_MaxValue)
                    xAxis = gs.xAxisCapped;
                //Change y rotation
                camRot.y = xAxis.Value;
                camRot.x = yAxis.Value;
            }

            //Assign rotation to childTransform
            fpsCamera.childTransform.localRotation = Quaternion.Euler(camRot);
        }
        else
        {
            Vector3 camRot = cameraTarget.localRotation.eulerAngles;
            if (!_input.freeCam)
            {
                //Third person camera control
                camRot.x = yAxis.Value;
                camRot.y = 0;
            }
            else
            {
                //Free look
                camRot.y = xAxis.Value;
                camRot.x = yAxis.Value;
            }
            cameraTarget.localRotation = Quaternion.Euler(camRot);
        }
    }

    void CheckGround()
    {
        if(Physics.Raycast(transform.position + new Vector3(0,0.1f,0), Vector3.down, out hit, 0.3f, jumpLayer))
            grounded = true;
        else
            grounded = false;
    }

    [PunRPC]
    void SetName(string _name)
    {
        gameObject.name = _name;
    }

    void CameraIKPoint()
    {
        RaycastHit _hit;
        if(Physics.Raycast(camTransform.position, camTransform.forward, out _hit, 100f, lookAtLayer))
            HeadIKTarget.position = Vector3.Lerp(HeadIKTarget.position, _hit.point, 0.5f);
    }

    void Jump()
    {
        rb.AddForce(Vector3.up * _speed.jumpForce, ForceMode.Impulse) ;
    }

    #region Audio

    public void PlayFootStep()
    {
        if (photonView.IsMine)
        {
            if (!_input.crouch && audioSource.volume != 0.5f)
                photonView.RPC(nameof(ChangeFootstepsVolume), RpcTarget.All, 0.5f, 10);
            else if (_input.crouch && audioSource.volume != 0.2f)
                photonView.RPC(nameof(ChangeFootstepsVolume), RpcTarget.All, 0.2f, 4);

            if (audioSource && grounded)
            {
                audioSource.PlayOneShot(gs.concreteFootsteps[Random.Range(0, gs.concreteFootsteps.Length)]);
                photonView.RPC(nameof(RPC_PlayFootstep), RpcTarget.Others);
            }
        }
    }

    [PunRPC]
    void RPC_PlayFootstep()
    {
        audioSource.PlayOneShot(gs.concreteFootsteps[Random.Range(0, gs.concreteFootsteps.Length)]);
        Debug.Log(photonView.name + " Footstep");
    }

    [PunRPC]
    public void ChangeFootstepsVolume(float volume, int distance)
    {
        audioSource.volume = volume;
        audioSource.maxDistance = distance;
    }

    #endregion

    #region Damage

    [PunRPC]
    public void TakeDamage(int dmg, string killerName)
    {
        if(health > 0)
            health -= dmg;

        if (health <= 0 && !isDead)
            Die(killerName);
    }

    void Die(string killerName)
    {
        anim.SetBool("Dead", true);
        isDead = true;
        canControl = false;

        if (photonView.IsMine)
        {
            photonView.RPC(nameof(TransformToPlayer), RpcTarget.AllBuffered);
            gm.spectateMode = true;
            gm.ChangePlayerCam();
        }
        StartCoroutine(destroyPlayer());
        Debug.Log($"<color=red>{killerName}</color> killed <color=blue>{gameObject.name}</color>");
        gm.AddKillStat(killerName, gameObject.name);
        gm.CheckHidersAlive();
        gm.RemovePlayerFromList(photonView.ViewID);
    }

    #endregion

    public void ChangeCam()
    {
        if (fpsCam)
        {
            camTransform.SetParent(fpsCamera.childTransform);
            camTransform.localPosition = Vector3.zero;
            camTransform.localRotation = Quaternion.identity;
            headObject.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            gm.cvc.enabled = false;
        }
        else
        {
            camTransform.SetParent(null);
            headObject.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            gm.cvc.enabled = true;
        }
    }

    #region Transform

    int CheckForObjects()
    {
        RaycastHit _hitObject;
        PhotonView target = null;
        Ray _ray = new Ray(camTransform.position, camTransform.forward);
        if(Physics.Raycast(_ray, out _hitObject, 5f, propsLayer))
        {
            _hitObject.collider.TryGetComponent(out target);
            if(target == null)
                target = _hitObject.collider.GetComponentInParent<PhotonView>();
        }

        return target != null ? target.ViewID : -1;
    }

    [PunRPC]
    void TransformToObject(int targetID)
    {
        GameObject targetObj = null;

        if (targetID != -1)
        {
            Debug.Log("Target id is : " + targetID);
            targetObj = PhotonNetwork.GetPhotonView(targetID).gameObject;
            Debug.Log($"Object found is : " + targetObj.name);
        }

        if (targetObj != null)
        {
            photonView.RPC(nameof(DisableColliders), RpcTarget.AllBuffered);

            //Destroy current transform object
            if (transformObject != null)
            {
                //Destroy(transformObject.gameObject);
                PhotonNetwork.Destroy(transformObject);
                transformObject = null;
            }

            //Get mesh filter
            MeshFilter mf = targetObj.GetComponent<MeshFilter>();
            

            //If mesh filter is still null search in Children
            if (mf == null)
                mf = targetObj.GetComponentInChildren<MeshFilter>();

            Mesh m = mf.mesh;

            //Get y size of object
            float yOffset = m.bounds.size.y * targetObj.transform.localScale.y;
            float avgSize = (m.bounds.size.x + m.bounds.size.y + m.bounds.size.z) / 3;
            float avgScale = (targetObj.transform.localScale.x + targetObj.transform.localScale.y + targetObj.transform.localScale.z) / 3;

            health = 100 * avgSize * avgScale;
            maxHealth = 100 * avgSize * avgScale;

            //Change cameraTarget position
            Vector3 newCameraTargetPos = cameraTarget.localPosition;
            newCameraTargetPos.y = yOffset;
            cameraTarget.localPosition = newCameraTargetPos;
            
            Debug.Log("<color=green>Transform to object</color>");
            //Spawn target object
            transformObject = PhotonNetwork.Instantiate(targetObj.GetComponent<PropData>().prefabLocation, transform.position, transform.rotation);
            Debug.Log("TransformObject: " + transformObject.name);
            transformID = transformObject.GetComponent<PhotonView>().ViewID;
            isTransformed = true;
            photonView.RPC(nameof(SetupObject), RpcTarget.AllBuffered, transformID, photonView.ViewID, targetID, yOffset);
        }
    }

    [PunRPC]
    void DisableColliders()
    {
        //Disable main player objects
        foreach (GameObject go in mainPlayerObj)
            go.SetActive(false);
        //Disable collider
        GetComponent<Collider>().enabled = false;
    }

    [PunRPC]
    void SetupObject(int targetID, int parentID, int foundObjectID, float camOffset)
    {
        //Find GameObjects by ViewID
        GameObject targetObj = PhotonNetwork.GetPhotonView(targetID).gameObject;
        GameObject parentObj = PhotonNetwork.GetPhotonView(parentID).gameObject;
        GameObject foundObj = PhotonNetwork.GetPhotonView(foundObjectID).gameObject;

        Vector3 newCameraTargetPos = cameraTarget.localPosition;

        Vector3 newScale = new Vector3(foundObj.transform.localScale.x / parentObj.transform.localScale.x,
            foundObj.transform.localScale.y / parentObj.transform.localScale.y,
            foundObj.transform.localScale.z / parentObj.transform.localScale.z);
        newCameraTargetPos.y = camOffset + 0.2f;
        cameraTarget.localPosition = newCameraTargetPos;
        targetObj.transform.SetParent(parentObj.transform);
        targetObj.transform.localPosition = Vector3.zero;
        targetObj.transform.localScale = newScale;
        targetObj.gameObject.layer = gameObject.layer;
        targetObj.tag = "Hider";
        try
        {
            Destroy(targetObj.GetComponent<PropData>());
            Destroy(targetObj.GetComponent<PhotonRigidbodyView>());
            Destroy(targetObj.GetComponent<Rigidbody>());
        }
        catch
        {
            Debug.Log("Rigidbody not found");
        }
        parentObj.transform.position = parentObj.transform.position + new Vector3(0, 0.1f, 0);

        List<Collider> colls = new List<Collider>(targetObj.GetComponents<Collider>());

        foreach (Collider c in colls)
        {
            c.sharedMaterial = zeroFrictionMaterial;
            c.tag = "Hider";
        }

        transformObject = targetObj;

        transformVFX.Play();
    }



    [PunRPC]
    void TransformToPlayer()
    {
        if(transformObject != null)
        {
            //Enable main player objects
            foreach (GameObject go in mainPlayerObj)
                go.SetActive(true);
            GetComponent<Collider>().enabled = true;
            isTransformed = false;
            //Reset camera position
            cameraTarget.localPosition = cameraTargetDefaultPos;
            maxHealth = 100;
            Destroy(transformObject);
        }
        transformVFX.Play();
    }

    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(transform.position, 0.1f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(HeadIKTarget.position, 0.2f);
    }
    
    [PunRPC]
    public void DestroyProp(GameObject prop)
    {
        Destroy(prop);
    }

    [PunRPC]
    void PlayTauntSound()
    {
        audioSource.PlayOneShot(gs.tountSound);
    }

    IEnumerator tauntSound(int time)
    {
        yield return new WaitForSeconds(time);
        photonView.RPC("PlayTauntSound", RpcTarget.Others);
        PlayTauntSound();
        taunt = null;
    }

    #region Trigger enter/exit

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Door")
        {
            doorDetected = true;
            doorPV = other.GetComponentInParent<PhotonView>();
            if (photonView.IsMine)
                gm._panels.usePanel.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Door")
        {
            doorPV = null;
            doorDetected = false;
            if (photonView.IsMine)
                gm._panels.usePanel.SetActive(false);
        }
    }

    #endregion

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
            stream.SendNext(health);
        else
            health = (float)stream.ReceiveNext();
    }

    IEnumerator destroyPlayer()
    {
        yield return new WaitForSeconds(5);
        if(photonView.IsMine)
            PhotonNetwork.Destroy(gameObject);
    }
}
