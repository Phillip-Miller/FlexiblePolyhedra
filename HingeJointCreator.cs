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
class Edge //vertex should be a world vertex
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

    public override bool Equals(object obj)
    {
        Edge other = (Edge)obj;
        if (other.vertex1.Equals(vertex1) && other.vertex2.Equals(vertex2))
            return true;
        if (other.vertex1.Equals(vertex2) && other.vertex2.Equals(vertex1))
            return true;
        return false;
    }
    //if they have one vertex in common
    public bool isConnected(Edge other)
    {
        return (vertex1.Equals(other.vertex1) || vertex1.Equals(other.vertex2) || vertex2.Equals(other.vertex1) || vertex2.Equals(other.vertex2));
    }

    public override string ToString()
    {
        return base.ToString();
    }
}
class Triangle : IEnumerable<Edge>
{
    public Edge edge1;
    public Edge edge2;
    public Edge edge3;
    public double Area;

    public Triangle(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, GameObject go)
    {

        this.edge1 = new Edge(vertex1, vertex2, go);
        this.edge2 = new Edge(vertex2, vertex3, go);
        this.edge3 = new Edge(vertex3, vertex1, go);
        this.Area = Vector3.Cross(vertex2 - vertex1, vertex3 - vertex1).magnitude / 2;
    }
    public Triangle(Edge edge1, Edge edge2, Edge edge3)
    {
        this.edge1 = edge1;
        this.edge2 = edge2;
        this.edge3 = edge3;
        this.Area = Vector3.Cross(edge2.vertex1 - edge1.vertex1, edge3.vertex1 - edge1.vertex1).magnitude / 2;
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

public class MyScript : MonoBehaviour
{
    List<HingeJoint> uniqueHinges = new List<HingeJoint>();
    public GameObject parentModel; //link the parent and unpack it if still a prefab
    public bool useGravity;
    public bool recolour;
    public double hingeTolerance; //TODO: could calculate this as something to do with the width of the shapes imported
    public bool run;
    public double sideArea;
    void Start()
    {
        if (!run)
            return;
        GameObject[] allGameObj = findAllGameObj(); //find and curate list -- working
        configureGameObjs(allGameObj);//color,ridgid,kinematics
        Vector3 inside = CalculateInside(allGameObj);
        var myEdges = FindEdges(allGameObj, inside);
        CreateHingeJoints(allGameObj, myEdges);
        CreateLabels(); //Label Angles, Shapes, and Have an array of angles thats updated each frame
        //TODO: save these presents that were made in the scene so they become permanete after the scene is exited


    }
    void Update()
    {
       // print(uniqueHinges.Count);
        //foreach (HingeJoint hinge in uniqueHinges)
        //{
        //    print(hinge.angle);
        //} //check bounds for collisions,write out all the angles

    }
    /// <summary>
    ///Returns a list of all game objects under the parented object <parentModel> (parent object not included)
    /// </summary>
    GameObject[] findAllGameObj()
    {
        GameObject[] gameObjectArray = new GameObject[parentModel.transform.childCount];

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

        foreach (GameObject go in allGameObjects)
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

        for (int i = 0; i < parentModel.transform.childCount; i++)
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
            avgX += position.x / parentModel.transform.childCount;
            avgY += position.y / parentModel.transform.childCount;
            avgZ += position.z / parentModel.transform.childCount;
        }
        //Calculate Middle of shape
        Vector3 middle = new Vector3(avgX, avgY, avgZ);
        print("Middle of shape" + middle);
        return middle;
    }
    //probably just want to rework to solve for unshared edges and then do some n! method of linking them back up
    List<Triangle> FindEdges(GameObject[] allGameObjects, Vector3 inside)
    {
        Mesh mesh;
        var insideFacesTriangles = new List<Triangle>();
        for (int i = 0; i < parentModel.transform.childCount; i++)
        {
            mesh = allGameObjects[i].GetComponent<MeshFilter>().mesh;
            Vector3[] localVertices = mesh.vertices;
            Vector3[] worldVertices = new Vector3[localVertices.Length];

            int k = 0;
            foreach (Vector3 vert in localVertices)
            {
                worldVertices[k] = allGameObjects[i].transform.TransformPoint(vert);
                k++;
            }
            //filled up world vertices for each game object

            int[] triangles = mesh.GetTriangles(0);
            List<Triangle> triangleStructs = new List<Triangle>();

            for (int j = 0; j < triangles.Length - 2; j += 3) //@FIXME changed -3 to minus 2
            {
                triangleStructs.Add(new Triangle(worldVertices[triangles[j]], worldVertices[triangles[j + 1]], worldVertices[triangles[j + 2]], allGameObjects[i]));
            }
            //created class structs with triangle list given by getTriangles()

            //additional triangle fix
            List<Edge> allEdges = findOutsideEdges(triangleStructs);
            List<Triangle> realTriangles = findConnectedEdges(allEdges);
            //foreach (Triangle tri in realTriangles)
            //{
            //    Debug.DrawLine(tri.edge1.vertex1, tri.edge1.vertex2, Color.red, 100f);
            //    Debug.DrawLine(tri.edge2.vertex1, tri.edge2.vertex2, Color.red, 100f);
            //    Debug.DrawLine(tri.edge3.vertex1, tri.edge3.vertex2, Color.red, 100f);
            //}
            //used to be triangle structs
            Triangle bottomFace = findInsideFace(realTriangles, inside);
            insideFacesTriangles.Add(bottomFace);

        }
        

        return insideFacesTriangles;
    }
    List<Edge> findOutsideEdges(List<Triangle> triangleStructs)
    {
        List<Edge> allEdges = new List<Edge>();
        //I am going to iterate through every single edge and find which edge is unique

        for (int i = 0; i < triangleStructs.Count; i++) //List of all edges
        {
            if (triangleStructs[i].Area < sideArea) //get rid of the side edges
            {
                continue;
            }
            foreach (Edge e in triangleStructs[i])
            {
                allEdges.Add(e);
            }
        }
        for (int j = 0; j < allEdges.Count; j++) //make the list unique @FIXME probably better way but hashset requires ovveriding getHashCode method
        {
            Edge findMatch = allEdges[j];
            for (int k = j + 1; k < allEdges.Count; k++)
            {
                if (findMatch.Equals(allEdges[k]))
                {
                    print("removing edge");
                    allEdges.Remove(findMatch);
                }
            }
        }
        foreach (Edge edge in allEdges)
        {
            Debug.DrawLine(edge.vertex1, edge.vertex2, Color.red, 100f);
        }
        print("Alledges.count: " + allEdges.Count);
        return allEdges;

    }

    //need to iterate through all the edges and remake my triangle objects from scratch by seeing if they have a connected vertex
    List<Triangle> findConnectedEdges(List<Edge> allEdges)
    {
        var finalizedTriangles = new List<Triangle>();
        for(int i = 0; i<allEdges.Count; i++)
        {
            Edge a = allEdges[i];
            Edge b = null;
            Edge c = null;
            bool first = true;

            for (int j = i + 1; j < allEdges.Count; j++) 
            {
                if (first && a.isConnected(allEdges[j])){
                    b = allEdges[j];
                    first = false;
                    continue;
                }
                else if (a.isConnected(allEdges[j])){
                    c = allEdges[j];
                    finalizedTriangles.Add(new Triangle(a, b, c));
                    break;
                }
                allEdges.Remove(a);allEdges.Remove(b);allEdges.Remove(c);
            }
        }
        return finalizedTriangles;
    }
    Triangle findInsideFace(List<Triangle> triangleStructs, Vector3 inside) //@FIXME
    {
        double max = 0;
        int maxIndex1 = 0;
        int maxIndex2 = 0;
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
        double index1AvgDistance = (triangleStructs[maxIndex1].edge1.vertex1 - inside).magnitude / 3;
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
    void CreateHingeJoints(GameObject[] allGameObjects, List<Triangle> triangles)
    {

        List<Edge[]> matchingEdges = findMatchingEdges(triangles);//{[pair1,pair1],[pair2,pair2]}
        foreach (Edge[] entry in matchingEdges)
        {  //only need one hinge per pair
            var hinge1 = entry[0].go.AddComponent<HingeJoint>();
            hinge1.anchor = entry[0].go.transform.InverseTransformPoint((entry[0].vertex1 + entry[0].vertex2) / 2); // this needs to be the bottem edge of the SAME shape (defined in local space)
            hinge1.axis = (entry[0].vertex1 - entry[0].vertex2); //@FIXME this appears to be a world entry not local
            hinge1.connectedBody = entry[1].go.GetComponent<Rigidbody>();

            uniqueHinges.Add(hinge1);
        }
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
                    foreach (Edge edge2 in innerFaces[j])
                    {
                        //TODO: Only checks the start and end verticies might need to change to check all verticies to fix issues vertex match mismatched typically
                        if ((((edge1.vertex1 - edge2.vertex1).magnitude < hingeTolerance && (edge1.vertex2 - edge2.vertex2).magnitude < hingeTolerance) ||
                            ((edge1.vertex1 - edge2.vertex2).magnitude < hingeTolerance && (edge1.vertex2 - edge2.vertex1).magnitude < hingeTolerance)) && edge1.length - edge2.length < 1)
                        {
                            //Debug.DrawLine(edge1.vertex1, edge1.vertex2, Color.red,100f);
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

}
