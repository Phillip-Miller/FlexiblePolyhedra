using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriangleShape : MonoBehaviour
{
    Mesh myMesh;
    MeshFilter myFilter;
    Rigidbody myRb;
    MeshCollider myCollider;

    // Start is called before the first frame update
    void Start()
    {
        myFilter = gameObject.GetComponent<MeshFilter>() as MeshFilter;
        myMesh = myFilter.mesh; //might be shared mesh
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void OnCollisionEnter(Collision collision)
    {
       // myMesh.colors[0] = Color.white;
    }
    public void OnCollisionExit(Collision collision)
    {
       // myMesh.colors[0] = Color.red;

    }
}
