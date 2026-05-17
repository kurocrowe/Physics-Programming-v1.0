using UnityEngine;

public class Player : MonoBehaviour
{
    private Rigidbody2D rb;
    private float horForce = 10.0f;
    private float verForce = 10.0f;
    private bool rightArrDown = false;
    private bool leftArrDown = false;
    private bool spacePressed = false;
    private bool shouldJump = false;
    private bool isGrounded = false;
    private bool isStairs = false;

    private Vector2 groundTangent= Vector2.zero;
    private Transform xform = null;
    private float zAngle = 0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb= GetComponent<Rigidbody2D>();
        xform= GetComponent<Transform>();

    }

    // Update is called once per frame
    void Update()
    {
        rightArrDown= Input.GetKey(KeyCode.RightArrow);
        leftArrDown = Input.GetKey(KeyCode.LeftArrow);
        spacePressed= Input.GetKeyDown(KeyCode.Space);
        if (spacePressed && !shouldJump && (isGrounded || isStairs))
        {
            shouldJump = true;
        }
        var tangentAngle= Vector2.SignedAngle(Vector2.left, groundTangent);
        zAngle = Mathf.LerpAngle(zAngle, tangentAngle, Time.deltaTime * 10f);
        xform.rotation = Quaternion.Euler(0, 0, zAngle);
        // boolean make sure is aligned with the framerate, so that the player can only jump once per space press, and not multiple times if the space is held down.
    }
    void FixedUpdate()
    {
        Vector2 rayCastDir= isGrounded ? Vector2.Perpendicular(groundTangent) : Vector2.down;
        RaycastHit2D hitRes= Physics2D.Raycast(transform.position, rayCastDir, 1.0f,LayerMask.GetMask("Ground"));
        isGrounded= hitRes.collider != null;
        groundTangent= isGrounded ? Vector2.Perpendicular(hitRes.normal) : Vector2.zero;
        if (rightArrDown)
        {
            rb.AddForce(-groundTangent * horForce);
        }
        if (leftArrDown)
        {
            rb.AddForce(groundTangent * horForce);
        }
        if (shouldJump)
        {
            shouldJump = false;
            rb.AddForce(Vector2.up * verForce, ForceMode2D.Impulse);
        }
        RaycastHit2D stairRes = Physics2D.Raycast(transform.position, Vector2.down, 0.6f, LayerMask.GetMask("Stairs"));
        isStairs = stairRes.collider != null;
        if (isStairs)
        {
            
            verForce = 5.0f;
           
        }
        else
        {
          
            verForce = 10.0f;
        }

    }
}
