using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
/* Need to have parented, no prefab, no rigid bodies, no other modifiers
 * 
 * @Author Phillip MIller
 * @Date 6/21/2021
 */
class Edge 
{
    public Vector3 vertex1;
    public Vector3 vertex2;
    public GameObject go;
    public double length;
    public Edge(Vector3 vertex1, Vector3 vertex2, GameObject go)
    {   
        this.go = go;
        this.vertex1 = vertex1;
        this.vertex2 = vertex2;
        this.length = (vertex1 - vertex2).magnitude;
    }
}
class Triangle : IEnumerable<Edge> 
{
    public Edge edge1;
    public Edge edge2;
    public Edge edge3;
    public double Area;
    
    public Triangle(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3,GameObject go)
    {
        
        this.edge1 = new Edge(vertex1, vertex2,go);
        this.edge2 = new Edge(vertex2, vertex3,go);
        this.edge3 = new Edge(vertex3, vertex1,go);
        this.Area = Vector3.Cross(vertex2-vertex1, vertex3-vertex1).magnitude / 2 ; 
    }
    public Triangle(Edge edge1, Edge edge2, Edge edge3)
    {
        this.edge1 = edge1;
        this.edge2 = edge2;
        this.edge3 = edge3;
        this.Area = Vector3.Cross(edge2.vertex1 - edge1.vertex1, edge3.vertex1 - edge1.vertex1).magnitude /2;
    }
    public IEnumerator<Edge> GetEnumerator()
    {
        yield return edge1;
        yield return edge2;
        yield return edge3;
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class HingeJointCreator : MonoBehaviour
{ 
    ArrayList allAngles = new ArrayList(); 
    public GameObject parentModel; //link the parent and unpack it if still a prefab
    public int numGameObjects; //the number of go's under the parentModel TODO: change parentModel.transform.childCount; 
    public bool useGravity;
    public bool recolour;
    public int hingeTolerance; //TODO: could calculate this as something to do with the width of the shapes imported
    void Start()
    {
        GameObject[] allGameObj = findAllGameObj(); //find and curate list -- working
        configureGameObjs(allGameObj);//color,ridgid,kinematics
        Vector3 inside = CalculateInside(allGameObj);
        var myEdges = FindEdges(allGameObj, inside);
        CreateHingeJoints(allGameObj,myEdges);
        CreateLabels(); //Label Angles, Shapes, and Have an array of angles thats updated each frame
        //TODO: save these presents that were made in the scene so they become permanete after the scene is exited


    }
    /// <summary>
    ///Returns a list of all game objects under the parented object <parentModel> (parent object not included)
    /// </summary>
    GameObject[] findAllGameObj()
    {
        GameObject[] gameObjectArray = new GameObject[numGameObjects];
        
        for (int i = 0; i < parentModel.transform.childCount; i++)
        {
            GameObject child = parentModel.transform.GetChild(i).gameObject;
            gameObjectArray[i] = child;
        }
        return gameObjectArray; 
    }
    /// <summary>
    /// applies ridgid body, enables is kinematic,applys random colors 
    /// </summary>
    /// <param name="allGameObjects"> allGameObject to be configured </param>
    void configureGameObjs(GameObject[] allGameObjects) //TODO: have an assigned list that way colors dont get reused
    {
        bool first = true; //we want the first one to be kinematic such that it stays in place (equivalent of grounding in fusion360)

        foreach(GameObject go in allGameObjects) 
        {

            Rigidbody rb = go.AddComponent<Rigidbody>(); //TODO: make sure none of them have rigid bodies first
            rb.useGravity = useGravity;
            rb.isKinematic = first; //ground the first one
            first = false;

            if (recolour)
            {
                var colorChange = go.GetComponent<Renderer>(); //randomizing the color attached to get easy to view multicolor faces
                colorChange.material.SetColor("_Color", UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f));
            }
        }
    }

    Vector3 CalculateInside(GameObject[] allGameObjects)
    {
         float avgX = 0; float avgY = 0; float avgZ = 0;
        
        for (int i = 0; i < numGameObjects; i++)
        {
            Vector3 position = Vector3.zero;
            Mesh mesh = allGameObjects[i].GetComponent<MeshFilter>().mesh;
            Vector3[] localVertices = mesh.vertices;
            Vector3[] worldVertices = new Vector3[localVertices.Length];

            int k = 0;
            foreach (Vector3 vert in localVertices)
            {
                worldVertices[k] = allGameObjects[i].transform.TransformPoint(vert);
                position.x += worldVertices[k].x / localVertices.Length;
                position.y += worldVertices[k].y / localVertices.Length;
                position.z += worldVertices[k].z / localVertices.Length;
                k++;
            }

            //position = allGameObjects[i].transform.position; //TODO: make sure positions inside the fusion360 are marked correctly @QUESTION
            avgX += position.x / numGameObjects;
            avgY += position.y / numGameObjects;
            avgZ += position.z / numGameObjects;
        }
        //Calculate Middle of shape
        Vector3 middle = new Vector3(avgX,avgY,avgZ);
        print("Middle of shape"+middle);
        return middle;
    }
    
    List<Triangle> FindEdges(GameObject[] allGameObjects, Vector3 inside) 
    {
        Mesh mesh;
        var insideFacesTriangles = new List<Triangle>();
        for (int i = 0; i < numGameObjects; i++)
        {
            mesh = allGameObjects[i].GetComponent<MeshFilter>().mesh;
            Vector3 [] localVertices = mesh.vertices;
            Vector3 [] worldVertices = new Vector3[localVertices.Length];
            
            int k = 0;
            foreach (Vector3 vert in localVertices)
            {
                worldVertices[k] = allGameObjects[i].transform.TransformPoint(vert);
                k++;
            }

            int[] triangles = mesh.GetTriangles(0); 
            List<Triangle> triangleStructs = new List<Triangle>();
            
            for (int j = 0; j < triangles.Length-3; j+=3)
            {
                triangleStructs.Add(new Triangle(worldVertices[triangles[j]], worldVertices[triangles[j+1]], worldVertices[triangles[j+2]],allGameObjects[i]));
            }
            Triangle bottomFace = findFace(triangleStructs,inside);
            insideFacesTriangles.Add(bottomFace);

        }

        //foreach (Triangle tri in insideFacesTriangles)
        //{
        //    Debug.DrawLine(tri.edge1.vertex1, tri.edge1.vertex2, Color.red, 100f);
        //    Debug.DrawLine(tri.edge2.vertex1, tri.edge2.vertex2, Color.red, 100f);
        //    Debug.DrawLine(tri.edge3.vertex1, tri.edge3.vertex2, Color.red, 100f);
        //}

        return insideFacesTriangles;
    }

    Triangle findFace(List<Triangle >triangleStructs,Vector3 inside) //@FIXME
    {
        double max = 0;
        int maxIndex1=0;
        int maxIndex2=0;
        for (int i = 0; i < triangleStructs.Count; i++)
        {
            if (triangleStructs[i].Area > max)
            {
                max = triangleStructs[i].Area;
                maxIndex1 = i;
            }
            else if (Math.Abs(triangleStructs[i].Area - max) < 8)
            {
                maxIndex2 = i;
            }
        }

        //If this causes errors switch to Max cross product normal idea
        double index1AvgDistance = (triangleStructs[maxIndex1].edge1.vertex1 - inside).magnitude /3;
        double index2AvgDistance = (triangleStructs[maxIndex2].edge1.vertex1 - inside).magnitude / 3;
        index1AvgDistance += (triangleStructs[maxIndex1].edge2.vertex1 - inside).magnitude / 3;
        index2AvgDistance += (triangleStructs[maxIndex2].edge2.vertex1 - inside).magnitude / 3;
        index1AvgDistance += (triangleStructs[maxIndex1].edge3.vertex1 - inside).magnitude / 3;
        index2AvgDistance += (triangleStructs[maxIndex2].edge3.vertex1 - inside).magnitude / 3;

        if (index1AvgDistance > index2AvgDistance)
        {
            return triangleStructs[maxIndex2];
        }
        return triangleStructs[maxIndex1];
    }
    void CreateHingeJoints(GameObject[] allGameObjects, List<Triangle> edges)
    {
        List<Edge[]> matchingEdges = findMatchingEdges(edges);//{[pair1,pair1],[pair2,pair2]}
        GameObject go = allGameObjects[0];
        var hinge = go.AddComponent<HingeJoint>();

        hinge.anchor = new Vector3(0, 0, 0); // this needs to be the bottem edge of the SAME shape (defined in local space)
        hinge.axis = new Vector3(0, 0, 0); //again for the same shape this is defined in local space
        return;
    }

    /// <summary>
    /// Finds matching edges using global hingeTolerance, and finding edges that match in length
    /// </summary>
    /// <param name="innerFaces"> List of Triangles that are facing the inside </param>
    List<Edge[]> findMatchingEdges(List<Triangle> innerFaces)
    {
        var returnList = new List<Edge[]>(); //TODO: check for duplicates
        //beauitufl code
        for (int i = 0; i < innerFaces.Count; i++)//pick a shape
        {
            for (int j = i + 1; j < innerFaces.Count; j++)//check other shapes
            {
                foreach (Edge edge1 in innerFaces[i])
                {
                    foreach(Edge edge2 in innerFaces[j])
                    {
                        //TODO: Only checks the start and end verticies might need to change to check all verticies to fix issues vertex match mismatched typically
                        if((((edge1.vertex1 - edge2.vertex1).magnitude < hingeTolerance && (edge1.vertex2 - edge2.vertex2).magnitude < hingeTolerance) ||
                            ((edge1.vertex1 - edge2.vertex2).magnitude < hingeTolerance && (edge1.vertex2 - edge2.vertex1).magnitude < hingeTolerance)) && edge1.length - edge2.length < 1)
                        {
                            Debug.DrawLine(edge1.vertex1, edge1.vertex2, Color.red, 100f);
                            Debug.DrawLine(edge2.vertex1, edge2.vertex2, Color.red, 100f);
                            returnList.Add(new Edge[] { edge1, edge2 });
                        }
                    }
                }

            }
        }
        return returnList;
    }
    void CreateLabels()
    {
        return;
    }
    // Update is called once per frame
    void Update()
    {

        
    }
}
