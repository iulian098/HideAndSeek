using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomSpawn : MonoBehaviour
{
    public GameObject[] props;
    public float chanceToSpawn;
    public Vector2 area;
    public float radius;
    public float spawnOffset;

    public List<Vector3> positions;
    private void Start()
    {
        int widthLen = (int)(area.x / radius);
        int lengthLen = (int)(area.y / radius);

        for(int i = 0; i < widthLen; i++)
        {
            for (int j = 0; j < lengthLen; j++)
            {
                float rand = Random.Range(0f, 1f);
                if (rand < chanceToSpawn)
                {
                    positions.Add(new Vector3(transform.position.x - area.x / 2 + radius / 2 + radius * i, transform.position.y, transform.position.z - area.y / 2 + radius / 2 + radius * j));
                }
            }
        }
        RaycastHit hit;
        for (int i = 0; i < positions.Count; i++)
        {
            if (Physics.Raycast(positions[i], -transform.up, out hit))
            {
                PropData pd = props[Random.Range(0, props.Length)].GetComponent<PropData>();
                pd.isInstantiated = true;
                PhotonNetwork.InstantiateRoomObject(pd.prefabLocation, hit.point + new Vector3(Random.Range(-radius / 2 + spawnOffset, radius / 2 - spawnOffset), 0, Random.Range(-radius / 2 + spawnOffset, radius / 2 - spawnOffset)), Quaternion.Euler(0, Random.Range(0, 360), 0));
            }
        }
    }

    private void OnDrawGizmosSelected()
    {

        Color c = Color.green;
        c.a = 0.5f;

        Gizmos.color = c;
        Gizmos.DrawCube(transform.position, new Vector3(area.x, 0.1f, area.y));

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, radius / 2);

        Gizmos.color = Color.white;

        foreach(Vector3 pos in positions)
        {
            Gizmos.DrawSphere(pos, radius / 2);
        }
    }

}
