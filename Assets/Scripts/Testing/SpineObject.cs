using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpineObject : MonoBehaviour
{
    public SpineObject nextObject;
    public Vector3 offset;
    public List<Vector3> verts;
    public float endingVertsOffset = 0.1f;
    public float endingVertsXPos;
    public List<int> endingVertsIndexes;
    public float startingVertsOffset;
    public List<int> startingVertsIndexes;
    public MeshFilter mf;
    // Start is called before the first frame update
    void Start()
    {
        mf = GetComponent<MeshFilter>();
        verts = new List<Vector3>(mf.mesh.vertices);
        for(int i = 0; i < verts.Count; i++)
        {
            if(verts[i].x > endingVertsOffset)
            {
                endingVertsIndexes.Add(i);
            }
            else if(verts[i].x < startingVertsOffset)
            {
                startingVertsIndexes.Add(i);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        /*for(int i = 0; i < endingVertsIndexes.Count; i++)
        {
            int index = endingVertsIndexes[i];
            Vector3 pos = verts[index];
            pos.x = endingVertsXPos;
            verts[index] = pos;
        }*/

        if (Input.GetKeyDown(KeyCode.Space) && nextObject != null)
        {
            ConnectVerts();
        }
    }

    public void ConnectVerts()
    {
        for(int i = 0; i < endingVertsIndexes.Count; i++)
        {
            Debug.Log(i);
            int mesh1Index = endingVertsIndexes[i];
            int mesh2Index = nextObject.startingVertsIndexes[i];

            Vector3 v1World = transform.TransformPoint(verts[mesh1Index]);
            Vector3 v2World = nextObject.transform.TransformPoint(verts[mesh2Index]);
            Debug.Log($"V1Local: {verts[mesh1Index].x}, V1World: {v1World.x}, V2Local: {nextObject.verts[mesh2Index].x}, V2World: {v2World.x}");
            Debug.Log($"V1World: {v1World.x}, V1Local: {transform.InverseTransformPoint(v1World).x}, V2World: {v2World.x}, V2Local: {transform.InverseTransformPoint(v2World).x}");
            //Vector3 newPos = (v1World + v2World) / 2;
            Vector3 newPos = verts[mesh1Index];
            newPos.x = transform.InverseTransformPoint(v2World).x;
            Debug.Log("newPos.x : " + newPos.x);
            verts[mesh1Index] = newPos;//transform.InverseTransformPoint(newPos);
            //nextObject.verts[mesh2Index] = nextObject.transform.InverseTransformPoint(newPos);
        }
        mf.mesh.vertices = verts.ToArray();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawCube(transform.position + new Vector3(startingVertsOffset, mf.mesh.bounds.size.y / 2, 0), new Vector3(0, mf.mesh.bounds.size.y, mf.mesh.bounds.size.z));
        Gizmos.DrawCube(transform.position + new Vector3(endingVertsOffset, mf.mesh.bounds.size.y / 2, 0), new Vector3(0, mf.mesh.bounds.size.y, mf.mesh.bounds.size.z));
    }
}
