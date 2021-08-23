using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/* No Hinge Pure Math Model
 * Set square with 90 degree angles and faces with non 90
 * Angle D will be tracked as inside and converted upon inputs if needed
 * 
 * TODO LIST
 * Hinge axis is tracking correctly issue is with altParentTracker being created too late and then being in the wrong place
 * I will have all the hinges done from the corner I just need to just fix the axis to rotate around
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
    public void Draw()
    {
        Debug.DrawLine(vertex1, vertex2, Color.red, 50f);
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
    public Vector3 getMiddle()
    {
        var verts = this.getVerticies();
        Vector3 avg = Vector3.zero;

        foreach(Vector3 vert in verts)
        {
            avg += vert;
        }
        avg /= verts.Count;
        return avg;
    }
    public IEnumerator GetEnumerator()
    {
        return EdgeList.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public override bool Equals(object obj) 
    {
        Polygon p = (Polygon) obj;
        return this.getVerticies().All(p.getVerticies().Contains) && this.getVerticies().Count == p.getVerticies().Count; 
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string ToString()
    {
        return base.ToString();
    }
    public void Draw()
    {
        foreach(Edge edge in EdgeList){
            edge.Draw();
        }
    }
}
class MyHinge //everything in here is in world space !! 
    //hinge normal 1 and 2 will be continously updated as go1, and go2 are rotated respectively
{
    public readonly Vector3 origin;
    public readonly Polygon polygon1;
    public readonly Polygon polygon2;
    public readonly Vector3 moveDirection;
    

    //Updated during runtime
    public Vector3 axis;
    public Vector3 anchor;
    public Vector3 axisPointA;
    public Vector3 axisPointB;
    public Vector3 orthogonalV1;
    public Vector3 orthogonalV2;
    public List<MyHinge> connectedHinges = new List<MyHinge>();
    public Vector3 normalToCenter1;
    
    public Vector3 normalToCenter2;

    public static List<Polygon> lockedFaces = new List<Polygon>();
    public GameObject go1;
    public GameObject go2;

    public float updatedAngle;
    
    public MyHinge(Polygon Polygon1, Polygon Polygon2,Vector3 axisPointA, Vector3 axisPointB)
    {
        polygon1 = Polygon1;
        polygon2 = Polygon2;
        this.go1 = Polygon1.EdgeList[0].go;
        this.go2 = Polygon2.EdgeList[0].go;
        this.axis = axisPointA - axisPointB; //should I actually be subtracting here
        this.anchor = (axisPointA + axisPointB) / 2;
        this.origin = this.anchor;
        this.axisPointA = axisPointA;
        this.axisPointB = axisPointB;
        calculateOrthogonalHinge();
        this.updatedAngle = this.GetAngle();
        this.moveDirection = ((orthogonalV1 + orthogonalV2) / 2).normalized;
    }
    public void updateGo()
    {
        this.go1 = polygon1.EdgeList[0].go;
        this.go2 = polygon2.EdgeList[0].go;

    }
    /// <summary>
    /// in order to rotate to rotate around someone elses anchor.
    /// Only works for one alternate anchor, assuming further uses want the same anchor
    /// this will be the outermost parent altTrackingParent,TrackingParent,GameObject
    ///@FIXME make sure to run normal getTranslateParent Before hand
    /// </summary>
    /// <param name="firstGo"></param>
    /// <param name="altAnchor"></param> anchor of another game object you wish to rotate around 
    /// <returns></returns>
    private GameObject GetTranslateParent(bool firstGo, Vector3 altAnchor)
    {
        
        GameObject child;
        if (firstGo)
        {
            if (this.go1.transform.parent != null && this.go1.transform.parent.name.Equals("AltTrackingParent"))
            { 
                return this.go1.transform.parent.gameObject;
            }
            child = this.go1;
        }
        else
        {
            if (this.go2.transform.parent != null && this.go2.transform.parent.name.Equals("AltTrackingParent"))
                return this.go2.transform.parent.gameObject;
            child = this.go2;
        }
        GameObject parent = new GameObject("AltTrackingParent");
        parent.transform.Translate(altAnchor);
        child.transform.parent = parent.transform;
        return parent; 
    }
    private GameObject GetTranslateParent(bool firstGo) 
    {

        GameObject child;
        GameObject oldParent;
        if (this.go1.transform.parent != null && !this.go1.transform.parent.name.Contains("Parent") )
        {
            oldParent = this.go1.transform.parent.gameObject;
            this.go1.transform.parent = null;
            GameObject.Destroy(oldParent);
        }
        if (this.go2.transform.parent != null && !this.go2.transform.parent.name.Contains("Parent"))
        {
            oldParent = this.go2.transform.parent.gameObject;
            this.go2.transform.parent = null;
            GameObject.Destroy(oldParent);
        }
        if (firstGo)
        {
            if (this.go1.transform.parent != null) {
                return this.go1.transform.parent.gameObject;
            }
            child = this.go1;

        }
        else 
        {
            if (this.go2.transform.parent != null)
            {
                return this.go2.transform.parent.gameObject;
            }
                child = this.go2;
        }
        GameObject parent = new GameObject("TrackingParent");
        //GameObject parent = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //parent.name = "TrackingParent";
        //parent.transform.localScale = .2f * Vector3.one;
        parent.transform.Translate(this.anchor);
        child.transform.parent = parent.transform;
        return parent;
        
    }
    private void calculateOrthogonalHinge() //want to find orthogonal line between axis and point on polygon. Might want to calculate the axis inside of this function
    {
        this.orthogonalV1 = this.polygon1.getMiddle() - this.anchor;
        this.orthogonalV2 = this.polygon2.getMiddle() - this.anchor;
     
       
    }

    private float GetAngle()
    {
        
        double magAxB = orthogonalV1.magnitude * orthogonalV2.magnitude;
        double dotProduct = Vector3.Dot(orthogonalV1, orthogonalV2);
        //Vector3.SignedAngle(orthogonalV1, orthogonalV2); has weird result where it will always return value less than 180
        return (float)(Math.Acos((dotProduct / magAxB)) * (180 / Math.PI));
    }
    public override bool Equals(object obj)
    {
        MyHinge other = (MyHinge)obj; //sees if the anchor is the same
        if (this.anchor.Equals(other.anchor))
            return true;
        return false;
    }
    public bool sharesPolygon(MyHinge hinge)
    {
       
        return this.polygon1.Equals(hinge.polygon1) || this.polygon1.Equals(hinge.polygon2) || this.polygon2.Equals(hinge.polygon1) || this.polygon2.Equals(hinge.polygon2);
    }
    /// <summary>
    /// Moves GO's, updates 
    /// </summary>
    /// <param name="rad"></param>
    public float updateAngle(float deg) //need to adjust the flaps here too...locked faces not really used
    {
        this.updatedAngle += deg;

        if (lockedFaces.Contains(this.polygon1))
        {
            RotateAroundPivot(false, deg);
        }
        else if (lockedFaces.Contains(this.polygon2))
        {
            RotateAroundPivot(true, deg);
        }
        else
        {
            
            RotateAroundPivot(true, deg/2);
            RotateAroundPivot(false,-1*deg/2);
        }
        return this.updatedAngle;
    }
    

    private void RotateAroundPivot(bool firstGo, float deg, bool updateValueOnly = false) 
    {
        //IF FLAP THEN ROTATE AROUND PREEXISTING PARENT (alt parent) instead of triny to create a new one @FIXME is using wrong translate parent causing issues?
        
        GameObject parent = this.GetTranslateParent(firstGo);
        parent.transform.Rotate(this.axis, deg);
        Quaternion rotation = Quaternion.AngleAxis(-deg, this.axis);
        
       
        //zfighting
        if (firstGo)
        {
            this.normalToCenter1 = rotation.normalized * this.normalToCenter1;
        }
        if (!firstGo)
        {
            this.normalToCenter2 = rotation.normalized * this.normalToCenter2;
        }
        //DrawNormalToCenters();

        if ((this.go1.name.Contains("abcd") || this.go2.name.Contains("abcd")) && firstGo) //added first go to make sure this only gets executed one time
        {
            foreach (MyHinge hinge in this.connectedHinges)
            {
                
                bool FlapIsFirstGo = !this.go1.name.Contains("abcd"); //we want to move the non abcd face (flapA and flapB)
                hinge.GetTranslateParent(FlapIsFirstGo, this.anchor).transform.Rotate(this.axis, -deg); //want to update axisPointA,AxisPointB and then recalculate anchor as well
                //hinge.axisPointA = rotation.normalized * hinge.axisPointA;
                //hinge.axisPointB = rotation.normalized * hinge.axisPointB;
                //hinge.axis = rotation.normalized * hinge.axis;
                //hinge.anchor = (hinge.axisPointA + hinge.axisPointB) / 2;
                if (FlapIsFirstGo) //update ortho
                {
                    hinge.orthogonalV1 = rotation.normalized * hinge.orthogonalV1; //order matters 
                }
                else
                {
                    hinge.orthogonalV2 = rotation.normalized * hinge.orthogonalV2; //order matters 
                }

            }
        }

        if (firstGo) //update ortho
        {
            orthogonalV1 = rotation.normalized * orthogonalV1; //order matters 
        }
        else
        {
            orthogonalV2 = rotation.normalized * orthogonalV1; //order matters 
        }

        

    }
    public void TranslateHinge(float dX, bool updateValueOnly) //@FIXME this is where i am messing up the axis
    {
        dX *= -1;
        orthogonalV1 += ((orthogonalV1 + orthogonalV2) / 2).normalized * dX; 
        orthogonalV2 += ((orthogonalV2 + orthogonalV2) / 2).normalized * dX;
        
        this.axisPointA += this.moveDirection * dX;
        this.axisPointB += this.moveDirection * dX;
        this.axis = this.axisPointA - this.axisPointB; 
        this.anchor = (this.axisPointA + this.axisPointB) / 2;

        if (updateValueOnly)
            return;
        this.GetTranslateParent(true).transform.Translate(this.moveDirection * dX, Space.World);
        this.GetTranslateParent(false).transform.Translate(this.moveDirection * dX, Space.World);
        if (this.go1.name.Contains("a") || this.go2.name.Contains("a"))
        { //then we will have to adjust other hinges

            foreach (MyHinge hinge in this.connectedHinges)
            {
                
                //if we are adjusting hinge c 
                //orthogonalV1 += this.moveDirection * dX;
                //orthogonalV2 += this.moveDirection * dX;
                //hinge.axisPointA += (this.moveDirection * dX);
                //hinge.axisPointB += (this.moveDirection * dX);
                //hinge.axis = hinge.axisPointA - hinge.axisPointB; //should I actually be subtracting here @FIXME @FIXME @FIXME
                //hinge.anchor = (hinge.axisPointA + hinge.axisPointB) / 2;
                bool first = !hinge.go1.name.Equals("abcd");
                hinge.GetTranslateParent(first,this.anchor).transform.Translate(this.moveDirection * dX, Space.World);
            }
            
        }
    }

    /// <summary>
    /// Draws in green
    /// </summary>
    public void Draw() //need to update these points more often
    {
        Debug.DrawLine(axisPointA, axisPointB, Color.green, 1f);
    }
    /// <summary>
    /// Draws the Normal to Center Ray
    /// </summary>
    /// <param name="go"></param> 0 means to draw both, 1/2 will specify which game object to draw
    public void DrawNormalToCenters(int go = 0)
    {
        if (go == 0)
        {
            Debug.DrawLine(this.polygon1.getMiddle(), normalToCenter1, Color.yellow, 50f);
            Debug.DrawLine(this.polygon2.getMiddle(), normalToCenter2, Color.yellow, 50f);
        }
        if (go == 1)
        {
            Debug.DrawLine(this.polygon1.getMiddle(), normalToCenter1, Color.yellow, 50f);
        }
        if (go == 2)
        {
            Debug.DrawLine(this.polygon2.getMiddle(), normalToCenter2, Color.yellow, 50f);
        }
    }
    /// <summary>
    /// Designed for use specefically in updating the values of the axis for the flaps to rotate around
    /// </summary>
    /// <param name="axisPointA"></param>
    /// <param name="axisPointB"></param>
    public void updateFromAxisPoints(Vector3 axisPointA, Vector3 axisPointB)
    {
        this.axisPointA = axisPointA;
        this.axisPointB = axisPointB;
        this.anchor = (axisPointA + axisPointB) / 2;
        this.axis = axisPointA - axisPointB;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string ToString()
    {
        return base.ToString();
    }
}

public class MyScript : MonoBehaviour
{
    List<MyHinge> uniqueHinges;
    private GameObject parentModel;
    public bool autoUnpack;
    public bool useGravity;
    public float userAngleD;
    public float userAngleC;
    public float userAngleA;
    public float userAngleB;

    private double hingeTolerance = .001;
    public double sideArea;

    private double squareLength;
    List<GameObject> hiddenGo = new List<GameObject>();
    GameObject[] allGameObj;
    List<MyHinge> interiorHinges = new List<MyHinge>();
    List<MyHinge> DandOppHinges = new List<MyHinge>();
    MyHinge hingeA;
    MyHinge hingeB;
    MyHinge hingeC;
    MyHinge hingeD;
    GameObject faceA;
    GameObject faceB;
    GameObject faceC;
    GameObject faceDandOpp;
    GameObject faceABCD;
    GameObject faceOpp;
    GameObject AaxisPoint1;
    GameObject AaxisPoint2;
    GameObject BaxisPoint1;
    GameObject BaxisPoint2;

    public float Z_DISPLACEMENT;

    List<GameObject> myStack1 = new List<GameObject>(); //the stack that the flaps can be added to (abcd to center)
    List<GameObject> myStack2 = new List<GameObject>();
    List<GameObject> prevMyStack1 = new List<GameObject>();
    List<GameObject> prevMyStack2 = new List<GameObject>();

    Vector3 stack1UpVector; //also known as abcdToCenter
    Vector3 stack2UpVector;
    Vector3 prevStack1UpVector = Vector3.zero;
    Vector3 prevStack2UpVector = Vector3.zero;
    [System.NonSerialized]
    public bool gameObjectDestroyed;



    void Start()
    {
        //myPlygon changes go but hinges are made before that....

        parentModel = this.gameObject;
        allGameObj = FindAllGameObjects(); //find and curate list of all viewable faces
        List<Polygon> myPolygons = FindFacePolygons(allGameObj, null);//finds all outside edges of shape using shared edges and area methods
        List<Edge[]> matchingEdges = FindMatchingEdges(ref myPolygons); //edits ref myPolygons to just the inner polygons by finding the matching edges
        foreach (GameObject go in allGameObj)
            go.transform.parent = null;
        ReSizeShape(ref myPolygons);
        foreach (MyHinge hinge in uniqueHinges)
        {
            hinge.updateGo();
        }
        



        squareLength = ((uniqueHinges[0].axisPointA - uniqueHinges[0].axisPointB).magnitude);

        Time.fixedDeltaTime = 0.01f;
        //c and angle across from it
        hingeC = uniqueHinges.Where(hinge => hinge.go1.name.Contains("c") && hinge.go2.name.Contains("c")).First(); //hopefully there is only 1
        hingeA = uniqueHinges.Where(hinge => hinge.go1.name.Contains("a") && hinge.go2.name.Contains("a")).First();
        hingeB = uniqueHinges.Where(hinge => hinge.go1.name.Contains("b") && hinge.go2.name.Contains("b")).First();
        hingeD = uniqueHinges.Where(hinge => hinge.go1.name.Contains("d") && hinge.go2.name.Contains("d")).First();

        interiorHinges.Add(hingeC);
        foreach (MyHinge hinge in uniqueHinges)
        {
            if (hinge.go1.name.Contains("opp") && hinge.go2.name.Contains("opp")) //helper hinge...not getting triggered
            {
                interiorHinges.Add(hinge);
            }
        }

        DandOppHinges.Add(hingeD);
        foreach (MyHinge hinge in uniqueHinges)
        {
            if (hinge.go1.name.Equals("opp") && hinge.go2.name.Equals("c") || hinge.go1.name.Equals("c") && hinge.go2.name.Equals("opp")) //helper hinge...not getting triggered
            {
                DandOppHinges.Add(hinge);
            }
        }
        hingeC.connectedHinges.Add(hingeA);
        hingeC.connectedHinges.Add(hingeB);

        faceA = allGameObj.Where(go => go.name.Equals("a")).First();
        faceB = allGameObj.Where(go => go.name.Equals("b")).First();
        faceC = allGameObj.Where(go => go.name.Equals("c")).First();
        faceDandOpp = allGameObj.Where(go => go.name.Equals("d&opp")).First();
        faceABCD = allGameObj.Where(go => go.name.Equals("abcd")).First();
        faceOpp = allGameObj.Where(go => go.name.Equals("opp")).First();

        findNormals();

        AaxisPoint1 = new GameObject("AaxisPoint1");
        AaxisPoint2 = new GameObject("AaxisPoint2");
        BaxisPoint1 = new GameObject("BaxisPoint1");
        BaxisPoint2 = new GameObject("BaxisPoint2");

        AaxisPoint1.transform.Translate(hingeA.axisPointA);
        AaxisPoint2.transform.Translate(hingeA.axisPointB);
        BaxisPoint1.transform.Translate(hingeB.axisPointA);
        BaxisPoint2.transform.Translate(hingeB.axisPointB);

        AaxisPoint1.transform.SetParent(faceABCD.transform);
        AaxisPoint2.transform.SetParent(faceABCD.transform);
        BaxisPoint1.transform.SetParent(faceABCD.transform);
        BaxisPoint2.transform.SetParent(faceABCD.transform);
                                        



        //I want to attach axisA and axisB to face abcd and then I can just track that and update it in the fixed update
        //UpdateAngleA(userAngleA - flapA.updatedAngle);
        //UpdateAngleB(userAngleB - flapB.updatedAngle);
        //UpdateAngleC(userAngleC - hingeC.updatedAngle); //pass in a delta
        //UpdateAngleD((360 - userAngleD) - hingeD.updatedAngle);
        //UpdateAngleD(-hingeD.updatedAngle);
    }
    String stackPrint(List<GameObject> myStack)
    {
        String returnString = "";
        foreach(GameObject go in myStack)
        {
            returnString += go.name + " < ";
        }
        return returnString;
    }
    private void Update()
    {
        hingeA.updateFromAxisPoints(AaxisPoint1.transform.position, AaxisPoint2.transform.position);
        hingeB.updateFromAxisPoints(BaxisPoint1.transform.position, BaxisPoint2.transform.position);

    }

    void FixedUpdate() //update commands are all in deltas (how much you want them to move)
    {
        
        if(myStack1.Count > 1)
            print(stackPrint(myStack1));
        Debug.DrawRay(hingeA.anchor, hingeA.axis, Color.cyan, 1f);
        //zOffset();



    }
    public float UpdateAngleA(float deg)
    {

        float returnAngle = UpdateFlap(deg, hingeA);
        if(hingeA.updatedAngle < 1)
        {
            if (!myStack1.Contains(faceA))
                myStack1.Insert(0, faceA);
        }
        if (hingeA.updatedAngle > 359)
        {
            if (!myStack1.Contains(faceA))
                myStack1.Add(faceA);
        }
        else
            myStack1.Remove(faceA);
        return returnAngle;
    }
    public float UpdateAngleB(float deg)
    {
        float returnAngle = UpdateFlap( deg, hingeB);
        if (hingeB.updatedAngle < 1)
        {
            if (!myStack1.Contains(faceB))
                myStack1.Insert(0, faceB);
        }
        if (hingeB.updatedAngle > 359)
        {
            if (!myStack1.Contains(faceB))
                myStack1.Add(faceB);
        }
        else
            myStack1.Remove(faceB);
        return returnAngle;
    }
    private float UpdateFlap(float deg, MyHinge hinge)
    {
        if (hinge.updatedAngle + deg > 360)
        {
            
            deg = 360 - hinge.updatedAngle;
        }
        if (hinge.updatedAngle + deg < 0)
        {
            deg = 0 - hinge.updatedAngle;
        }
        //figure out which side
        if (hinge.go1.name.Equals("abcd"))
            MyHinge.lockedFaces.Add(hinge.polygon1);
        else
            MyHinge.lockedFaces.Add(hinge.polygon2);
        Debug.DrawRay(hinge.anchor, hinge.axis, Color.green, 1f);

        float returnAngle = hinge.updateAngle(deg);
        MyHinge.lockedFaces.RemoveAt(MyHinge.lockedFaces.Count - 1); //removes the polyygon we just added
        return returnAngle;
    }
    public float UpdateAngleD(float deg)
    {
        
        if (hingeC.updatedAngle > 180)
        {
            Debug.Log("D Bound Condition");
            //hingeC.updateAngle(-deg);
            //interiorHinges[1].updateAngle(deg);
        }
        if (hingeC.updatedAngle == 0)//@FIXME need to track DandOpp[1] here and adjust hinge location
        {
            //Iff deg is pos that means opp will be on top
            //neg deg means c will be on top
            foreach(MyHinge hinge in DandOppHinges) //wnat to lock c and abcd
            {
                if (hinge.go1.name.Contains("c"))
                    MyHinge.lockedFaces.Add(hinge.polygon1);
                if (hinge.go2.name.Contains("c"))
                    MyHinge.lockedFaces.Add(hinge.polygon2);
            }
            hingeD.updateAngle(deg);
            DandOppHinges[1].updateAngle(deg);
            MyHinge.lockedFaces.Clear();

           
        }
        else //otherwise d is directly dependent on C
        {
            UpdateAngleC(-deg);
        }

        if (hingeD.updatedAngle < 1)
        {
            if (!myStack1.Contains(faceOpp) && !myStack1.Contains(faceDandOpp))
            {
                myStack1.Insert(0, faceOpp);
                myStack1.Insert(0, faceDandOpp);
            }
        }
        if (hingeD.updatedAngle > 359)
        {
            if (!myStack1.Contains(faceOpp) && !myStack1.Contains(faceDandOpp))
            {
                myStack1.Add( faceDandOpp);
                myStack1.Add(faceOpp);
            }
        }
        else
        {
            myStack2.Clear();
            myStack1.Remove(faceOpp);
            myStack1.Remove(faceDandOpp);
        }
        return hingeD.updatedAngle;
    }

    public float UpdateAngleC(float deg) //distance apart is length*cos(pheta/2)-> dX/dPheta = length*-sin(pheta/2) *.5
                                  //double translateAmmount = -1* (-.5 * squareLength * Math.Sin(Mathf.Deg2Rad*1*deg/2));
                                 //I will have interior angles[0] always be c
    {
        
        float angleC = hingeC.updatedAngle;
        if ((angleC == 0 && deg < 0) || (angleC == 360 && deg > 0)) //cant move
            return 0;
        if (angleC + deg < 0) //snap to 0
        {
            print("BOUND CONDITION 1");
            deg = 0 - angleC;
        }
        if(angleC + deg > 360) //snap to 360
        {
            print("BOUND CONDITION 2");
            deg = 360 - angleC;
        }
        if (angleC >= 180) // @ fixme need to track dandopp[1]
        {
            hingeC.updateAngle(deg);
            interiorHinges[1].updateAngle(-deg);
        }
        else
        {
            if (angleC + deg > 180) //only doing one type of movement at a time
                deg = 180 - angleC;

            

            foreach (MyHinge hinge in interiorHinges)
            {
                hinge.updateAngle(deg);
                float dX = (float)(squareLength * Math.Cos(Mathf.Deg2Rad * hinge.updatedAngle / 2)) - (float)(squareLength * Math.Cos(Mathf.Deg2Rad * (hinge.updatedAngle - deg) / 2));
                if (angleC > 0 && angleC < 180) //might be an unncesary if statement
                {
                   hinge.TranslateHinge(dX, false);

                }
            }
            foreach (MyHinge hinge in DandOppHinges)
            {
                float dX = (float)(squareLength * Math.Cos(Mathf.Deg2Rad * hinge.updatedAngle / 2)) - (float)(squareLength * Math.Cos(Mathf.Deg2Rad * (hinge.updatedAngle - deg) / 2));
                if (angleC > 0 && angleC < 180)
                {
                    hinge.TranslateHinge(-dX, true);
                }
            }
            float adjAngle = 180 - hingeC.updatedAngle; 
            hingeD.updatedAngle = adjAngle;
            DandOppHinges[1].updatedAngle = adjAngle;
        }       
        
        if(hingeC.updatedAngle > 359 && !myStack1.Contains(faceC) && !myStack1.Contains(faceOpp))
        {
            if (!myStack1.Contains(faceDandOpp) && !myStack1.Contains(faceABCD))
            {
                myStack1.Clear();
                myStack1.Add(faceDandOpp);
                myStack1.Add(faceABCD);
            }

            myStack2.Clear();
            myStack1.Add(faceC);
            myStack1.Add(faceOpp);
            
        }
        if(hingeC.updatedAngle < 359 && hingeC.updatedAngle > 179 && !myStack2.Contains(faceOpp)) //180 usecase
        {
            myStack2.Clear();
            myStack2.Add(faceOpp);
            myStack2.Add(faceC);

            if (!myStack1.Contains(faceDandOpp))
                myStack1.Insert(0, faceDandOpp);
            if (!myStack1.Contains(faceABCD))
                myStack1.Add(faceABCD);

        }
        if ( hingeC.updatedAngle < 1)
        {
            if(!myStack1.Contains(faceC))
                myStack1.Insert(0,faceC);
            if (!myStack1.Contains(faceABCD))
                myStack1.Add(faceABCD);
            if (hingeD.updatedAngle < 1 && !myStack1.Contains(faceDandOpp) && !myStack1.Contains(faceOpp))
            {
                myStack1.Insert(0, faceOpp);
                myStack1.Insert(0, faceDandOpp);
            }
            else if(!myStack1.Contains(faceC) && !myStack2.Contains(faceOpp))
            {
                //stack1 needs to add c
                myStack2.Clear();
                myStack2.Add(faceOpp);
                myStack2.Add(faceDandOpp);
               
            }
        }
        if(hingeC.updatedAngle>1 && hingeC.updatedAngle < 180)
        {
            myStack2.Clear();
            myStack1.RemoveAll(go => go != faceA && go != faceB && go != faceABCD);
        }
        if (hingeC.go1.Equals(faceABCD))
            stack1UpVector = hingeC.normalToCenter1;
        if (hingeC.go2.Equals(faceABCD))
            stack1UpVector = hingeC.normalToCenter2;
        return hingeC.updatedAngle;
    }
    /// <summary>
    /// Given the order of the stacks I need to move the elements at the top of the stack and the bottom of the stack apart from eachother ever so slightly
    /// TODO: make middle ones invisible in bigger stacks if needed
    /// </summary>
    public Boolean zOffset() //I think this is causing issues in the rotation
    {

        if (hingeC.updatedAngle > 90)
        {
            stack2UpVector = hingeC.go1.Equals(faceC) ? hingeC.normalToCenter1 : hingeC.normalToCenter2;
        }
        if (hingeC.updatedAngle < 90)
        {
            stack2UpVector = interiorHinges[1].go1.Equals(faceDandOpp) ? interiorHinges[1].normalToCenter1 : interiorHinges[1].normalToCenter2;
        }

        ///attach parents and fix that kind of stuff up
        if (myStack1 != null && myStack1.Count > 1)
        {
            if (!prevMyStack1.SequenceEqual(myStack1))
            {
                Debug.Log("STACK CHANGE");
                prevMyStack1[0].transform.Translate(-prevStack1UpVector.normalized * Z_DISPLACEMENT, Space.World);
                prevMyStack1[prevMyStack1.Count -1].transform.Translate(prevStack1UpVector.normalized * Z_DISPLACEMENT, Space.World);
                prevStack1UpVector = Vector3.zero; //gotta make sure it moves foward first

            }
            myStack1[0].transform.Translate(-prevStack1UpVector.normalized * Z_DISPLACEMENT, Space.World);
            myStack1[myStack1.Count-1].transform.Translate(prevStack1UpVector.normalized * Z_DISPLACEMENT, Space.World);

            myStack1[0].transform.Translate(stack1UpVector.normalized * Z_DISPLACEMENT , Space.World);
            myStack1[myStack1.Count - 1].transform.Translate(-stack1UpVector.normalized * Z_DISPLACEMENT, Space.World);

            prevStack1UpVector = stack1UpVector;

        }
        prevMyStack1.Clear();
        foreach (GameObject go in myStack1)
        {
            prevMyStack1.Add(go);
        }

        if(myStack2 != null && myStack2.Count > 1)
        {
            if (!prevMyStack1.SequenceEqual(myStack1))
            {
                Debug.Log("STACK CHANGE");
                prevMyStack2[0].transform.Translate(-prevStack2UpVector.normalized * Z_DISPLACEMENT, Space.World);
                prevMyStack2[prevMyStack2.Count - 1].transform.Translate(prevStack2UpVector.normalized * Z_DISPLACEMENT, Space.World);
                prevStack2UpVector = Vector3.zero; //gotta make sure it moves foward first

            }
            myStack2[0].transform.Translate(-prevStack2UpVector.normalized * Z_DISPLACEMENT, Space.World);
            myStack2[myStack1.Count - 1].transform.Translate(prevStack2UpVector.normalized * Z_DISPLACEMENT, Space.World);

            myStack2[0].transform.Translate(stack2UpVector.normalized * Z_DISPLACEMENT, Space.World);
            myStack2[myStack2.Count - 1].transform.Translate(-stack2UpVector.normalized * Z_DISPLACEMENT, Space.World);

            prevStack2UpVector = stack2UpVector;
        }

        if(myStack1.Count > 2) //hide middle stuff
            for (int i = 0; i < myStack1.Count; i++)
            {
                if (i != 0 && i != myStack1.Count - 1)
                {
                    myStack1[i].GetComponent<MeshRenderer>().enabled = false;
                    hiddenGo.Add(myStack1[i]);
                }
            }
        else
        {
            foreach(GameObject go in hiddenGo)
            {
                go.GetComponent<MeshRenderer>().enabled = true;
            }
            hiddenGo.Clear();

        }
        return true;
    
        
    }
    void findNormals()
    {

        Vector3[] faceCenters = new Vector3[4]; //(same order as below)
        
        GameObject[] square = { faceC, faceOpp, faceDandOpp, faceABCD };
        List<Vector3> allVerticies = new List<Vector3>();
        for(int i = 0; i<square.Length; i++)
        {
            GameObject go = square[i];
            List<Vector3> myVerticies = new List<Vector3>();
            go.GetComponent<MeshFilter>().mesh.GetVertices(myVerticies);
            Vector3 faceCenter = Vector3.zero;
            foreach (Vector3 v in myVerticies) {
                allVerticies.Add(go.transform.TransformPoint(v));
                faceCenter += go.transform.TransformPoint(v);
            }
            faceCenters[i] = faceCenter / myVerticies.Count;
        }
        Vector3 interior = Vector3.zero;
        foreach (Vector3 v in allVerticies)
        {
            interior += v;
        }
        interior /= allVerticies.Count;

     
        Vector3 abcdToCenter = interior - faceCenters[3];
        Vector3 dandOppToCenter = interior - faceCenters[2];
        Vector3 cToCenter = interior - faceCenters[0];

        if (hingeC.go1.name.Equals("abcd"))
            hingeC.normalToCenter1 = abcdToCenter;
        if (hingeC.go2.name.Equals("abcd"))
            hingeC.normalToCenter2 = abcdToCenter;
        if(interiorHinges[1].go1.name.Equals("d&opp"))
            interiorHinges[1].normalToCenter1 = dandOppToCenter;
        if (interiorHinges[1].go2.name.Equals("d&opp"))
            interiorHinges[1].normalToCenter2 = dandOppToCenter;
        if (hingeC.go1.name.Equals("c"))
            hingeC.normalToCenter1 = cToCenter;
        if (hingeC.go2.name.Equals("c"))
            hingeC.normalToCenter2 = cToCenter;


    }


    /// <summary>
    ///Returns a list of all game objects under the parented object <parentModel> (parent object not included)
    /// </summary>
    GameObject[] FindAllGameObjects()
    {
        //TODO: autounpack doesnt appear to be working any more
        GameObject[] gameObjectArray = new GameObject[parentModel.transform.childCount];
        if (!autoUnpack)
        {
            for (int i = 0; i < parentModel.transform.childCount; i++)
            {
                GameObject child = parentModel.transform.GetChild(i).gameObject;
                gameObjectArray[i] = child;
            }

            return gameObjectArray;
        }

       // PrefabUtility.UnpackPrefabInstance(parentModel, PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);
        int childCount = parentModel.transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            gameObjectArray[i] = parentModel.transform.GetChild(i).transform.GetChild(0).transform.GetChild(0).gameObject;
            
        }
        return gameObjectArray;
        
    }
    /// <summary>
    /// applies ridgid body, enables is kinematic,applys random colors 
    /// </summary>
    /// <param name="allGameObjects"> allGameObject to be configured </param>
    void ConfigureGameObjects(GameObject[] allGameObjects) //TODO: have an assigned list that way colors dont get reused
    {
        print(allGameObjects.Length);
        //bool first = true; //we want the first one to be kinematic such that it stays in place (equivalent of grounding in fusion360)
        Color[] faceColors = new Color[] { new Color(231, 76, 60), new Color(155, 89, 182), new Color(41, 128, 185), new Color(26, 188, 156), new Color(243, 156, 18), new Color(44, 62, 80) };
        for (int i = 0; i<allGameObjects.Length; i++) {
            GameObject go = allGameObjects[i];
            var colorChange = go.GetComponent<Renderer>(); //randomizing the color attached to get easy to view multicolor faces
            colorChange.material.SetColor("_Color", faceColors[1]);
               // UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f));
        }
    }

    /// <summary>
    /// Finds all the outside edges of a given face 2*ngon (front and back)
    /// </summary>
    /// <param name="GameObjects"></param>
    /// <param name="collider"></param> Used to skip half the verticies on a two faced plane to aid in making hinges
    /// <returns></returns>
    List<Polygon> FindFacePolygons(GameObject[] GameObjects, List<Polygon> myPolygons)
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
            for (int j = 0; j < loopStop - 2; j += 3) //all triangles in world space
            {
                worldTriangles.Add(new Triangle(worldVertices[triangles[j]], worldVertices[triangles[j + 1]], worldVertices[triangles[j + 2]], GameObjects[i]));
            }
            
            List<Edge> allEdges = FindOutsideEdges(worldTriangles); //find all the edges of a shape
            List<Polygon> realPolygons = FindConnectedEdges(allEdges); //this list should always be of size two

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
    List<Polygon> FindConnectedEdges(List<Edge> allEdges)
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
    /// Finds matching edges using global hingeTolerance, and finding edges that match in length. This also determines which polygons are inward facing replacing the old findInside method.
    /// </summary>
    /// <param name="realPolygons"> List of finalized polygons </param>
    List<Edge[]> FindMatchingEdges(ref List<Polygon> realPolygons) 
    {
        uniqueHinges = new List<MyHinge>();
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
                            //if (Vector3.Cross(realPolygons[i].getNormal(),realPolygons[j].getNormal()).Equals(Vector3.zero)) //If vectors are parallel their cross product should be 0
                            //    print("shared Normal"); //probably never gets called because of shared edges algorithm
                            returnList.Add(new Edge[] { edge1, edge2 });
                            insideFacePolygons.Add(realPolygons[i]);
                            insideFacePolygons.Add(realPolygons[j]);
                            Vector3 avgp1;
                            Vector3 avgp2;
                            if(Vector3.Distance(edge1.vertex1,edge2.vertex1) < Vector3.Distance(edge1.vertex1, edge2.vertex2))
                            {
                                avgp1 = (edge1.vertex1 + edge2.vertex1) / 2;
                                avgp2 = (edge1.vertex2 + edge2.vertex2) / 2;
                            }
                            else
                            {
                                avgp1 = (edge1.vertex1 + edge2.vertex2) / 2;
                                avgp2 = (edge1.vertex2 + edge2.vertex1) / 2;
                            }
                            MyHinge hinge = new MyHinge(realPolygons[i], realPolygons[j], avgp1, avgp2);

                            //MyHinge hinge = new MyHinge(realPolygons[i], realPolygons[j], edge1.vertex1, edge1.vertex2);

                            if (!uniqueHinges.Contains(hinge)) //slow but oh well
                                uniqueHinges.Add(hinge);
                        }
                    }
                }

            }
        }
        realPolygons = insideFacePolygons.ToList();
        return returnList;
    }
    void ReSizeShape(ref List<Polygon> myPolygons) // List<Edge> edges
    {
        Color[] faceColors = new Color[] { new Color32(231, 76, 60,255), new Color32(155, 89, 182,255), new Color32(41, 128, 185,255), new Color32(163, 228, 215, 255), new Color32(243, 156, 18,255), new Color32(20, 90, 50,255) };
        
        for(int i = 0; i< myPolygons.Count; i++) 
        {
            Polygon p = myPolygons[i];
            List<Vector3> verticiesList = p.getVerticies();
            
            int[] triangles = CreateTriangleArray(p,verticiesList).ToArray();
            Vector3[] vertices = p.getVerticies().ToArray();
            GameObject newGo = new GameObject(p.EdgeList[0].go.name, typeof(MeshFilter), typeof(MeshRenderer));
            var oldPlace = p.EdgeList[0].go;
            Mesh mesh = new Mesh();
            newGo.GetComponent<MeshFilter>().mesh = mesh;
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            foreach (Edge e in p)
            {
                e.go = newGo;
            }
            var colorChange = newGo.GetComponent<Renderer>(); 
            colorChange.material.SetColor("_Color", faceColors[i]);
            Destroy(oldPlace);
            allGameObj[i] = newGo;

        }
    }

    List<int> CreateTriangleArray(Polygon p, List<Vector3> verticies) 
    {
        List<Triangle> tList = p.createTriangles();
        List<int> indexList = new List<int>();
        int indexTracker = 0;
        foreach (Triangle t in tList)
        {
            foreach (Edge e in t)
            {
                indexList.Add(verticies.IndexOf(e.vertex1));

                indexTracker++;
            }
        }

        //double sided does not quite appear to be working properly as of yet @FIXME

        int[] flipped = new int[indexList.Count];
        Array.Copy(indexList.ToArray(), flipped, indexList.Count);
        Array.Reverse(flipped, 0, indexList.Count);
        var combined = new int[2 * indexList.Count];
        indexList.CopyTo(combined, 0);
        flipped.CopyTo(combined, indexList.Count);
        
        return combined.ToList<int>();
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
 
