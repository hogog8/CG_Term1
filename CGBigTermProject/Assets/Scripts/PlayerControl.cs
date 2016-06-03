using UnityEngine;
using System.Collections;

public class PlayerControl : MonoBehaviour {
    public float speed;
    private Rigidbody rb;
    bool isJumping;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    void Update()
    {
        if (Input.GetButtonDown("Jump"))
            isJumping = true;
    }
    void FixedUpdate()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveHorizontal,0.0f, moveVertical);

        rb.AddForce(movement*speed);
        if(isJumping)
            rb.AddForce(Vector3.up*10,ForceMode.Impulse);
        isJumping = false;
    }
}
