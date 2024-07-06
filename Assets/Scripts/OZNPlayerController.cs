using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]

public class OZNPlayerController : MonoBehaviour
{
    public Rigidbody rb;
    public LayerMask walls;
    public float speed;
    public float upSpeed;
    public float maxRandomForce;
    public float fadingTime = 10f;
    public bool fading;

    public Light mainLight;

    public GameObject warning;
    Color defaultLightColor;

    [Header("Debugging")]
    public float objSpeed;

    private void Start()
    {
        defaultLightColor = mainLight.color;
        StartCoroutine(fade(fadingTime));
        //rb.velocity = new Vector3(0, upSpeed, 0);
    }

    void Update()
    {
        objSpeed = rb.velocity.normalized.y;
        float h = Input.GetAxis("Horizontal");
        MovePlayer(h);

        //rb.velocity = upSpeed * (rb.velocity.normalized);
        StartCoroutine(forceTime(5));

        //Screen fading
        if (fading)
        {
            mainLight.color = Color.Lerp(mainLight.color, new Color(0, 0, 0), 0.1f);
        }
        else
        {
            mainLight.color = Color.Lerp(mainLight.color, defaultLightColor, 0.1f);
        }

        //Detecting walls
        Collider[] colls = Physics.OverlapSphere(transform.position, 1.5f, walls);

        if(colls.Length > 0)
        {
            warning.SetActive(true);
        }
        else
        {
            warning.SetActive(false);
        }
    }

    private void FixedUpdate()
    {
        Vector3 auxPos = transform.position;
        auxPos.y += upSpeed * Time.deltaTime;
        transform.position = Vector3.Lerp(transform.position, auxPos, 0.1f);
    }

    void MovePlayer(float h)
    {
        rb.AddForce(new Vector3(h * speed, 0, 0));
    }

    void AddRandomForce()
    {
        rb.AddForce(new Vector3(Random.Range(-maxRandomForce, maxRandomForce), 0, 0));
        StartCoroutine(forceTime(Random.Range(1, 5)));
    }



    IEnumerator forceTime(float time)
    {
        yield return new WaitForSeconds(time);
        AddRandomForce();
    }

    IEnumerator fade(float time)
    {
        yield return new WaitForSeconds(time + Random.Range(0, 5));
        fading = true;
        yield return new WaitForSeconds(time / 2);
        fading = false;
        StartCoroutine(fade(fadingTime));
    }
}
