using UnityEngine;

/*
 * Enables user input
 * TODO: enable highlighting of faces 
 * Enable removal of edges
 * Enable removal of faces
 */
public class Controller : MonoBehaviour
{
    public float movementSpeed;
    public float rotationSpeed;
    public float forceMultiplier;
    // Start is called before the first frame update
    void Start()
    {

    }
    /*
    Controls:
    WASD  : Directional movement
    Shift : Increase speed
    Space : Moves camera up per its local Y-axis
    Cntrl : Moves camera down per its local Y-axis
    left click : apply force push to normal on face
    shift left click : apply force pull to normal on face 
    right click : stop face from moving
    alt click : remove face
    */
    
     

    // Update is called once per frame
    void Update()
    {

        Vector3 c_Velocity = GetBaseMovement() * movementSpeed * Time.deltaTime;
        Vector3 c_Rotation = GetBaseRotation() * rotationSpeed * Time.deltaTime;
        transform.Translate(c_Velocity);
        transform.Rotate(c_Rotation);
        if (Input.GetMouseButtonDown(0)) //not clear if normal calculated is correct
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                
                var go = hit.collider.gameObject;
                if(Input.GetKey(KeyCode.LeftAlt))
                {
                    Destroy(go);
                }
                else if (Input.GetKey(KeyCode.LeftShift))
                {

                    go.GetComponent<Rigidbody>().AddForce(1 * hit.normal * forceMultiplier);
                    
                }
                else
                {
                    go.GetComponent<Rigidbody>().AddForce(-1 * hit.normal * forceMultiplier);

                }
            }
        }
        if (Input.GetMouseButtonDown(1))
        { //right click
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                var go = hit.collider.gameObject;
                var Rb = go.GetComponent<Rigidbody>();
                Rb.angularVelocity= Vector3.zero;
                Rb.velocity = Vector3.zero;

            }
        }
    }
    Vector3 GetBaseRotation()
    {
        Vector3 c_Rotation = new Vector3();

        //rotate X up
        if (Input.GetKey(KeyCode.UpArrow))
            c_Rotation += new Vector3(-1, 0, 0);

        // rotate X down
        if (Input.GetKey(KeyCode.DownArrow))
            c_Rotation += new Vector3(1, 0, 0);

        // rotate y left
        if (Input.GetKey(KeyCode.LeftArrow))
            c_Rotation += new Vector3(0, -1, 0);

        // rotate y right
        if (Input.GetKey(KeyCode.RightArrow))
            c_Rotation += new Vector3(0, 1, 0);

        // rotate z cw c_Rotation += new Vector3(0, 0, 1
        // rotate z cc c_Rotation += new Vector3(0, 0, -1);

        return c_Rotation;
    }

    Vector3 GetBaseMovement() 
    {
        Vector3 c_velocity = new Vector3();

        // Forwards
        if (Input.GetKey(KeyCode.W))
            c_velocity += new Vector3(0, 0, 1);

        // Backwards
        if (Input.GetKey(KeyCode.S))
            c_velocity += new Vector3(0, 0, -1);

        // Left
        if (Input.GetKey(KeyCode.A))
            c_velocity += new Vector3(-1, 0, 0);

        // Right
        if (Input.GetKey(KeyCode.D))
            c_velocity += new Vector3(1, 0, 0);

        // Up
        if (Input.GetKey(KeyCode.Space))
            c_velocity += new Vector3(0, 1, 0);

        // Down
        if (Input.GetKey(KeyCode.LeftControl))
            c_velocity += new Vector3(0, -1, 0);
        return c_velocity;
    }
}
