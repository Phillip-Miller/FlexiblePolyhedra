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

//inside faces triangles is not working I should instead just see which one is closest to joint
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

    public override int GetHashCode() //no idea if this works
    {
        int hashCode = -1704521559;
        hashCode = hashCode + -1521134295 * vertex1.GetHashCode();
        hashCode = hashCode + -1521134295 * vertex2.GetHashCode();
        return hashCode;
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
    public Vector3 Normal;

    public Triangle(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, GameObject go)
    {
        this.edge1 = new Edge(vertex1, vertex2, go);
        this.edge2 = new Edge(vertex2, vertex3, go);
        this.edge3 = new Edge(vertex3, vertex1, go);
        this.Area = Vector3.Cross(vertex2 - vertex1, vertex3 - vertex1).magnitude / 2;
        this.Normal = GetNormal();
    }
    public Triangle(Edge edge1, Edge edge2, Edge edge3)
    {
        this.edge1 = edge1;
        this.edge2 = edge2;
        this.edge3 = edge3;
        this.Area = Vector3.Cross(edge2.vertex1 - edge1.vertex1, edge3.vertex1 - edge1.vertex1).magnitude / 2;
        this.Normal = GetNormal();
    }
    private Vector3 GetNormal()
    {
        Vector3 side1 = edge2.vertex1 - edge1.vertex1;
        Vector3 side2 = edge3.vertex1 - edge1.vertex1;
        return Vector3.Cross(side1, side2);
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
    public void Draw()
    {
        Debug.DrawLine(this.edge1.vertex1, this.edge1.vertex2, Color.red, 10f);
        Debug.DrawLine(this.edge2.vertex1, this.edge2.vertex2, Color.red, 10f);
        Debug.DrawLine(this.edge3.vertex1, this.edge3.vertex2, Color.red, 10f);
    }
}
class Polygon : IEnumerable
{
    public List<Edge> EdgeList = new List<Edge>();
    public List<Triangle> TriangleList = new List<Triangle>();
   

    public Polygon(List<Triangle> triangles)
    {
        this.TriangleList = triangles;
    }
    public Polygon()
    {

    }
    /// <summary>
    /// Create triangles from one vertex picked at random
    /// </summary>
    private void createTriangles()
    {

    }
    public Vector3 getNormal()
    {
        if (this.TriangleList == null)
            createTriangles();
        return this.TriangleList[0].Normal;
    } 
    public double getArea()
    {
        if (this.TriangleList == null)
            createTriangles();
        double area = 0;
        foreach (Triangle tri in TriangleList)
        {
            area += tri.Area;
        }
        return area;
    }
    public IEnumerator GetEnumerator()
    {
        return EdgeList.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}




public class MyScript : MonoBehaviour
{
    List<HingeJoint> uniqueHinges = new List<HingeJoint>();
    public GameObject parentModel; //On run: link the parent and unpack it if still a prefab
    public bool useGravity; //false by default
    public bool recolour;
    public double hingeTolerance; //TODO: could calculate this as something to do with the width of the shapes imported
    public bool run;
    public double sideArea;
    public bool enableColliders; //TODO: make custom colliders just on the inside face of the shape and space out the hinges slightly
    void Start()
    {
        if (!run)
            return;
        GameObject[] allGameObj = findAllGameObj(); //find and curate list -- working
        configureGameObjs(allGameObj);//color,ridgid,kinematics,collider
        Vector3 inside = CalculateInside(allGameObj);//average insides -- extranous method
        var myEdges = FindEdges(allGameObj, inside);//finds all outside edges of shape using shared edges and area methods
        CreateHingeJoints(allGameObj, myEdges); //creates hinge joints
        CreateLabels(); //Label Angles, Shapes, and Have an array of angles thats updated each frame @TODO:

        for (int i = 0; i < allGameObj.Length; i++)
        {
            for (int j = i + 1; j < allGameObj.Length; j++)
            {
                Physics.IgnoreCollision(allGameObj[i].GetComponent<MeshCollider>(), allGameObj[j].GetComponent<MeshCollider>(), true);

            }
        }
    }
    void Update()
    { 
        // print(uniqueHinges.Count);
        //foreach (HingeJoint hinge in uniqueHinges)
        //{
        //    print(hinge.angle);
        //} //check bounds for collisions,write out all the angles
        //print(calculate volume)

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
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb == null)
                rb = go.AddComponent<Rigidbody>();
            rb.useGravity = useGravity;
            rb.isKinematic = first; //ground the first one @FIXME
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            if (enableColliders)  
            {
                MeshCollider c = go.GetComponent<MeshCollider>();
                if (c == null)
                {
                    c = go.AddComponent<MeshCollider>();
                    c.convex = true;
                }
                
            }
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
        return middle;
    }
    /// <summary>
    /// Finds all the outside edges of a given face 2*ngon (front and back)
    /// </summary>
    /// <param name="allGameObjects"></param>
    /// <param name="inside"></param>
    /// <returns></returns>
    List<Polygon> FindEdges(GameObject[] allGameObjects, Vector3 inside)
    {
        Mesh mesh;
        var facePolygons = new List<Polygon>();
        for (int i = 0; i < parentModel.transform.childCount; i++) //foreach game object
        {
            mesh = allGameObjects[i].GetComponent<MeshFilter>().mesh;
            Vector3[] localVertices = mesh.vertices;
            Vector3[] worldVertices = new Vector3[localVertices.Length];

            int k = 0;
            foreach (Vector3 vert in localVertices) //Convert all local verts to world verts
            {
                worldVertices[k] = allGameObjects[i].transform.TransformPoint(vert);
                k++;
            }
            

            int[] triangles = mesh.GetTriangles(0);
            List<Triangle> worldTriangles = new List<Triangle>();
            for (int j = 0; j < triangles.Length - 2; j += 3) //all triangles in world space
            {
                worldTriangles.Add(new Triangle(worldVertices[triangles[j]], worldVertices[triangles[j + 1]], worldVertices[triangles[j + 2]], allGameObjects[i]));
            }

            List<Edge> allEdges = findOutsideEdges(worldTriangles); //find all the edges of a shape
            List<Polygon> realPolygons = findConnectedEdges(allEdges); //this list should always be of size two

            foreach (Polygon poly in realPolygons) //@FIXME Polygon
            {
                facePolygons.Add(poly);
            }

        }
        return facePolygons;
    }
    List<Edge> findOutsideEdges(List<Triangle> worldTriangles)
    { 
        List<Edge> allEdges = new List<Edge>();
        //I am going to iterate through every single edge and find which edge is unique

        for (int i = 0; i < worldTriangles.Count; i++) //List of all edges
        {
            if (worldTriangles[i].Area < sideArea) //get rid of the side edges
            {
                continue;
            }
            foreach (Edge e in worldTriangles[i])
            {
                allEdges.Add(e);
            }
        }
        for (int j = 0; j < allEdges.Count; j++) //if a duplicate exists, remove both as they are interior triangles
        {
            Edge findMatch = allEdges[j];
            for (int k = j + 1; k < allEdges.Count; k++)
            {
                if (findMatch.Equals(allEdges[k]))
                {
                    while (allEdges.Contains(findMatch))
                        allEdges.Remove(findMatch);
                    j--;
                    break;
                }
            }
        }
        return allEdges;
    }

    /// <summary>
    /// Iterates through all the edges to create polygon objects from scratch by seeing if they have a connected vertex
    /// </summary>
    /// <param name="allEdges"></param>
    /// <returns></returns>
    List<Polygon> findConnectedEdges(List<Edge> allEdges)
    {
        var finalizedPolygons = new List<Polygon>();
        while(allEdges.Count !=0) 
        {
            Polygon myPolygon = new Polygon();
            myPolygon.EdgeList.Add(allEdges[0]);

            for (int j = 1; j < allEdges.Count; j++) //build up polygon
            {
                if (myPolygon.EdgeList[myPolygon.EdgeList.Count - 1].isConnected(allEdges[j]))
                    myPolygon.EdgeList.Add(allEdges[j]);
            }
            foreach (Edge edge in myPolygon)
            {
                allEdges.Remove(edge);
            }
            finalizedPolygons.Add(myPolygon);
        }
        return finalizedPolygons;
    }
    /// <summary>
    /// Do not use
    /// Calculates closest edge to middle out of 2 possible triangles
    /// </summary>
    Triangle findInsideFace(List<Triangle> triangleStructs, Vector3 inside) 
    {
       
        double index1AvgDistance = (triangleStructs[0].edge1.vertex1 - inside).magnitude / 3;
        double index2AvgDistance = (triangleStructs[1].edge1.vertex1 - inside).magnitude / 3;
        index1AvgDistance += (triangleStructs[0].edge2.vertex1 - inside).magnitude / 3;
        index2AvgDistance += (triangleStructs[1].edge2.vertex1 - inside).magnitude / 3;
        index1AvgDistance += (triangleStructs[0].edge3.vertex1 - inside).magnitude / 3;
        index2AvgDistance += (triangleStructs[1].edge3.vertex1 - inside).magnitude / 3;

        if (index1AvgDistance > index2AvgDistance)
        {
            return triangleStructs[1];
        }
        return triangleStructs[0];
    }
    void CreateHingeJoints(GameObject[] allGameObjects, List<Polygon> polygons)
    {
        
        List<Edge[]> matchingEdges = findMatchingEdges(polygons);//{[pair1,pair1],[pair2,pair2]}
        print(matchingEdges.Count);
        foreach (Edge[] entry in matchingEdges)
        {  
            
            var hinge = entry[0].go.AddComponent<HingeJoint>();
            GameObject go = entry[0].go;
            hinge.anchor = entry[0].go.transform.InverseTransformPoint((entry[0].vertex1 + entry[0].vertex2) / 2); 
            hinge.axis = go.transform.InverseTransformPoint(entry[0].vertex1) - go.transform.InverseTransformPoint(entry[0].vertex2); 
            hinge.connectedBody = entry[1].go.GetComponent<Rigidbody>();
            hinge.enableCollision = true;
            //Physics.IgnoreCollision(entry[0].go.GetComponent<MeshCollider>(), entry[1].go.GetComponent<MeshCollider>(),true);
            uniqueHinges.Add(hinge);
        }
    }

    /// <summary>
    /// Finds matching edges using global hingeTolerance, and finding edges that match in length
    /// </summary>
    /// <param name="realPolygons"> List of finalized polygons (size 2) </param>
    List<Edge[]> findMatchingEdges(List<Polygon> realPolygons) //@FIXME Polygon
    {
        var returnList = new List<Edge[]>(); 
        for (int i = 0; i < realPolygons.Count; i++)//pick a shape
        {
            for (int j = i + 1; j < realPolygons.Count; j++)//check other shapes
            {
                foreach (Edge edge1 in realPolygons[i])
                {
                    foreach (Edge edge2 in realPolygons[j])
                    {
                        if ((((edge1.vertex1 - edge2.vertex1).magnitude < hingeTolerance && (edge1.vertex2 - edge2.vertex2).magnitude < hingeTolerance) ||
                            ((edge1.vertex1 - edge2.vertex2).magnitude < hingeTolerance && (edge1.vertex2 - edge2.vertex1).magnitude < hingeTolerance)) && edge1.length - edge2.length < 1)
                        {
                            //check if they share the same normal
                            //if(edge1.go.No) @FIXME Check for normal
                            returnList.Add(new Edge[] { edge1, edge2 });
                        }
                    }
                }

            }
        }
        return returnList;
    }

    double CalculateArea(List<Triangle> faces)
    {
        return 0.0;
    }
    void CreateLabels()
    {
        return;
    }
    // Update is called once per frame

}
