using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
/* Need to have parented, no prefab, no rigid bodies, no other modifiers
 * Joints are picked based on how close together they are using the tolerance indicator
 * Make sure made in fusion in meters, 1to1 unity scale factor
 * Make sure read/write is enabled
 * @ROADMAP:
 * Make upacking function
 * Removal of faces,edges,hinges
 * Display of angles
 * Display of volume
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
        return base.ToString() + this.vertex1.ToString() + this.vertex2.ToString();
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
        return Vector3.Cross(side1, side2).normalized;
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
    public override string ToString()
    {
        return base.ToString() + ": " + edge1.ToString() + edge2.ToString() + edge3.ToString();
    }
}
class Polygon : IEnumerable
{
    private List<Vector3> Vertices = new List<Vector3>(); 
    public List<Edge> EdgeList = new List<Edge>();
    private List<Triangle> TriangleList = new List<Triangle>();
    public Vector3 extrudeDirection;
    public int numTriangles;

    public Polygon(List<Triangle> triangles)
    {
        this.TriangleList = triangles;
    }
    public Polygon()
    {

    }
    /// <summary>
    ///Create triangles from vertex[1]
    /// </summary>
    /// <returns></returns>
    public List<Triangle> createTriangles() 
    {
       
        getVerticies();
        List<Triangle> t = new List<Triangle>();
        foreach(Edge e in this.EdgeList)
        {
            if(e.vertex1 != this.Vertices[0] && e.vertex2 != this.Vertices[0])
            { 
                t.Add(new Triangle(this.Vertices[0], e.vertex1, e.vertex2, e.go));
            }
        }
        
        return t; 
    }
    public List<Vector3> getVerticies() 
    {
        if (this.Vertices.Count > 3)
            return this.Vertices;
        else
        {
            foreach(Edge edge in EdgeList)
            {
                this.Vertices.Add(edge.vertex1);
            } 
        }
        return this.Vertices;
    }
    public Vector3 getNormal()
    {
        return (new Triangle(EdgeList[0], EdgeList[1], EdgeList[2])).Normal;
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
    public bool enableColliders; 
    public bool hideColliders;
    public bool hideOutside;
    public bool updateHingeMotion;
    void Start()
    {
        if (!run)
            return;
        GameObject[] allGameObj = findAllGameObj(); //find and curate list of all viewable faces
        GameObject[] allColliderGameObjects = new GameObject[allGameObj.Length]; //empty list of colliders that the allGameObj will be parented under
        configureGameObjs(allGameObj,true);//Randomly assigns colour
        List<Polygon> myPolygons = FindFacePolygons(allGameObj,0);//finds all outside edges of shape using shared edges and area methods
        FindMatchingEdges(ref myPolygons); //edits ref myPolygons to just the inner polygons by finding the matching edges

        CreateColliderPlanes(ref myPolygons); //Create my collider planes from the myPolygons and reassigns the game object attached to my polygon
        for (int i = 0; i < myPolygons.Count; i++)
        {
            allColliderGameObjects[i] = myPolygons[i].EdgeList[0].go;
        } //filling up the allColliderGameObject list
        configureGameObjs(allColliderGameObjects,false); //configure but for physics system, no colouring (could change this to include coloring)
        
        myPolygons = FindFacePolygons(allColliderGameObjects,myPolygons[0].numTriangles*3); //@run a second time to proof that colliders are accurate
        List<Edge[]> matchingEdges = FindMatchingEdges(ref myPolygons); //@FIXME somehow this is not working on the second iteration
        CreateHingeJoints(matchingEdges); 
        
        foreach(GameObject go in allColliderGameObjects) //bool hide colliders
        {
            go.GetComponent<MeshRenderer>().forceRenderingOff = hideColliders;
        }
        foreach (GameObject go in allGameObj)  //bool hide faces
        {
            go.SetActive(!hideOutside);
        }


        CreateLabels(); //Label Angles, Shapes, and Have an array of angles thats updated each frame @TODO:
        

    }
    void Update()
    {
        float [] previousHinges = new float[uniqueHinges.Count];
        if (updateHingeMotion)
        {
            for(int i = 0; i<uniqueHinges.Count; i++)
            {
                HingeJoint hinge = uniqueHinges[i];
                print("AngleDelta: "+  Math.Abs(hinge.angle - previousHinges[i])); ;
                previousHinges[i] = hinge.angle;
            }
        }
        

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
    void configureGameObjs(GameObject[] allGameObjects,bool face) //TODO: have an assigned list that way colors dont get reused
    {
        
        bool first = true; //we want the first one to be kinematic such that it stays in place (equivalent of grounding in fusion360)

        foreach (GameObject go in allGameObjects)
        {
            if (!face)
            {
                Rigidbody rb = go.GetComponent<Rigidbody>();
                rb.useGravity = useGravity;
                rb.isKinematic = first;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                if(!first)
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                else
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                if (enableColliders)
                {
                    MeshCollider c = go.GetComponent<MeshCollider>();
                    c.convex = true;
                    c.enabled = true;
                }
                first = false;
            }
            else if (recolour)
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
    /// <param name="GameObjects"></param>
    /// <param name="collider"></param> Used to skip half the verticies on a two faced plane to aid in making hinges
    /// <returns></returns>
    List<Polygon> FindFacePolygons(GameObject[] GameObjects,int edges)
    {
        Mesh mesh;
        var facePolygons = new List<Polygon>();
        for (int i = 0; i < GameObjects.Length; i++) //foreach game object
        {
            mesh = GameObjects[i].GetComponent<MeshFilter>().mesh;
            Vector3[] localVertices = mesh.vertices;
            Vector3[] worldVertices = new Vector3[localVertices.Length];

            int k = 0;
            foreach (Vector3 vert in localVertices) //Convert all local verts to world verts
            {
                worldVertices[k] = GameObjects[i].transform.TransformPoint(vert);
                k++;
            }
            
            int[] triangles = mesh.GetTriangles(0);
            List<Triangle> worldTriangles = new List<Triangle>();
            int loopStop = triangles.Length;
            if (edges!=0)
                loopStop = edges; //numTriangles? @FIXME could be wrong
            for (int j = 0; j < loopStop - 2; j += 3) //all triangles in world space
            {
                worldTriangles.Add(new Triangle(worldVertices[triangles[j]], worldVertices[triangles[j + 1]], worldVertices[triangles[j + 2]], GameObjects[i]));
            }
            
            List<Edge> allEdges = FindOutsideEdges(worldTriangles); //find all the edges of a shape
            List<Polygon> realPolygons = findConnectedEdges(allEdges); //this list should always be of size two

            foreach (Polygon poly in realPolygons) 
            {
                facePolygons.Add(poly);
            }

        }
        return facePolygons;
    }
    List<Edge> FindOutsideEdges(List<Triangle> worldTriangles)
    {
        List<Edge> allEdges = new List<Edge>();
        //I am going to iterate through every single edge and find which edge is unique
        int sideTriangleCount = 0;
        for (int i = 0; i < worldTriangles.Count; i++) //List of all edges
        {
            if (worldTriangles[i].Area < sideArea) //get rid of the side edges
            {
                sideTriangleCount++;
                continue;
            }
            foreach (Edge e in worldTriangles[i])
            {
                allEdges.Add(e);
            }
        }
        //36 edges (12 per face * 3 shapes)
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
        return allEdges; //all edges returning correct value here
    }

    /// <summary>
    /// Iterates through all the edges to create polygon objects from scratch by seeing if they have a connected vertex
    /// </summary>
    /// <param name="allEdges"></param>
    /// <returns></returns>
    List<Polygon> findConnectedEdges(List<Edge> allEdges)
    {
        var finalizedPolygons = new List<Polygon>();
        while (allEdges.Count != 0)
        {
            Polygon myPolygon = new Polygon();
            myPolygon.EdgeList.Add(allEdges[0]);

            for (int j = 1; j < allEdges.Count; j++) //build up polygon
            {
                foreach (Edge e in myPolygon)
                {
                    if (e.isConnected(allEdges[j]))
                    {
                        myPolygon.EdgeList.Add(allEdges[j]);
                        break;
                    }
                }
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
    void CreateHingeJoints(List<Edge[]> matchingEdges)
    {
        //@FIXME are child objects created at this point

        print("Number of hinges: "+ matchingEdges.Count);
        foreach (Edge[] entry in matchingEdges)
        {

            var hinge = entry[0].go.AddComponent<HingeJoint>();
            GameObject go = entry[0].go;
            hinge.anchor = entry[0].go.transform.InverseTransformPoint((entry[0].vertex1 + entry[0].vertex2) / 2);
            hinge.axis = go.transform.InverseTransformPoint(entry[0].vertex1) - go.transform.InverseTransformPoint(entry[0].vertex2);
            hinge.connectedBody = entry[1].go.GetComponent<Rigidbody>();
            hinge.enableCollision = true;
            uniqueHinges.Add(hinge);
            Physics.IgnoreCollision(entry[0].go.GetComponent<MeshCollider>(), entry[1].go.GetComponent<MeshCollider>());
        }
    }

    /// <summary>
    /// Finds matching edges using global hingeTolerance, and finding edges that match in length. This also determines which polygons are inward facing replacing the old findInside method.
    /// </summary>
    /// <param name="realPolygons"> List of finalized polygons </param>
    List<Edge[]> FindMatchingEdges(ref List<Polygon> realPolygons) 
    {
        //index out of bounds on first iteration....realPolygons has bad input
        var returnList = new List<Edge[]>();
        HashSet<Polygon> insideFacePolygons = new HashSet<Polygon>(); //to avoid duplicates
        for (int i = 0; i < realPolygons.Count; i++)//pick a shape
        {
            for (int j = i + 1; j < realPolygons.Count; j++)//check other shapes
            {
                foreach (Edge edge1 in realPolygons[i])
                {
                    foreach (Edge edge2 in realPolygons[j])
                    {
                        if ((((edge1.vertex1 - edge2.vertex1).magnitude < hingeTolerance && (edge1.vertex2 - edge2.vertex2).magnitude < hingeTolerance) ||
                            ((edge1.vertex1 - edge2.vertex2).magnitude < hingeTolerance && (edge1.vertex2 - edge2.vertex1).magnitude < hingeTolerance))) 
                        {
                            if (Vector3.Cross(realPolygons[i].getNormal(),realPolygons[j].getNormal()).Equals(Vector3.zero)) //If vectors are parallel their cross product should be 0
                                print("shared Normal"); //probably never gets called because of shared edges algorithm
                            returnList.Add(new Edge[] { edge1, edge2 });
                            insideFacePolygons.Add(realPolygons[i]);
                            insideFacePolygons.Add(realPolygons[j]);
                        }
                    }
                }

            }
        }
        realPolygons = insideFacePolygons.ToList();
        print("Number of matching edges: " + returnList.Count);
        return returnList;
    }
    /// <summary>
    /// Creates planes, enables mesh colliders, sets them in the correct place, edits the myPolygons to refer to the collider now
    /// </summary>
    /// <param name="myPolygons"></param>
     void CreateColliderPlanes(ref List<Polygon> myPolygons) // List<Edge> edges
     {
        print("Number of polygons (colliders): " + myPolygons.Count);

        foreach (Polygon p in myPolygons) {
            List<Vector3> verticiesList = p.getVerticies();
            int[] triangles = CreateTriangleArray(p,ref verticiesList).ToArray();
            Vector3[] vertices = p.getVerticies().ToArray();
            GameObject colliderGo = new GameObject("Mesh", typeof(MeshFilter),typeof(MeshCollider),typeof(MeshRenderer),typeof(Rigidbody));
            var newChild = p.EdgeList[0].go.transform;

            Mesh mesh = new Mesh();
            colliderGo.GetComponent<MeshFilter>().mesh = mesh;
            colliderGo.GetComponent<MeshCollider>().sharedMesh = mesh;
            mesh.vertices = vertices;
            mesh.triangles = triangles;

            foreach(Edge e in p)
            {
                e.go = colliderGo;
            }
            newChild.transform.SetParent(colliderGo.transform);//gets rid of parent
        }
     }

    List<int> CreateTriangleArray(Polygon p,ref List<Vector3> verticies) //Assuming winding order does not matter for colliders -- if needed uncomment out the flipped code to make figure double sided
    {
        List<Triangle> tList = p.createTriangles();
        List<int> indexList = new List<int>();
        int indexTracker = 0;
        foreach (Triangle t in tList)
        {
            foreach(Edge e in t)
            {
                indexList.Add(verticies.IndexOf(e.vertex1)); 
                
                indexTracker++;
            }
        }
        p.numTriangles = tList.Count;
        //Want my code to ignore everything in the list after these triangles in order to make hinges only on the plane

        //I want to extrude mesh outward and sqew inwards into 4 sided figure
        Vector3 planeInterior = Vector3.zero;
        foreach (Vector3 v in verticies)
            planeInterior += v;
        planeInterior /= verticies.Count; 

        Vector3 shapeInterior = Vector3.zero;
        foreach (Vector3 v in p.EdgeList[0].go.GetComponent<MeshFilter>().mesh.vertices) 
            shapeInterior += v;
        shapeInterior /= p.EdgeList[0].go.GetComponent<MeshFilter>().mesh.vertices.Length;
        float extrudeDistance = .001F;
        Vector3 extrudeDirection = (shapeInterior - planeInterior).normalized; //@FIXME I want to go in diretion of shape interior so I think I subtract
        verticies.Add(planeInterior + extrudeDirection * extrudeDistance);
        int extrudeVertexIndex = verticies.IndexOf(planeInterior + extrudeDirection * extrudeDistance);
        
        foreach(Edge e in p)
        {
            indexList.Add(verticies.IndexOf(e.vertex1));
            indexList.Add(verticies.IndexOf(e.vertex2));
            indexList.Add(extrudeVertexIndex);
        }
        //@FIXME Colliders not quite working as intended
        //int[] flipped = new int[indexList.Length];
        //Array.Copy(indexList, flipped, indexList.Length);
        //Array.Reverse(flipped,0,indexList.Length);
        //var combined = new int[2*indexList.Length];
        //indexList.CopyTo(combined, 0);
        //flipped.CopyTo(combined, indexList.Length);
        return indexList;
    }

    double CalculateVolume(List<Triangle> faces)
    {
        return 0.0;
    }
    void CreateLabels()
    {
        return;
    }
    

}
 
