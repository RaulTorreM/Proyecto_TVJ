using UnityEngine;

public class MovementController : MonoBehaviour {
    private Rigidbody2D rigidbody;
    private Vector3 velocity;

    public float speed;

    void Start() {
        rigidbody = GetComponent<Rigidbody2D>();
    }

    void Update() {
        float hor = Input.GetAxisRaw("Horizontal");
        float ver = Input.GetAxisRaw("Vertical");

        if (hor != 0 || ver != 0) {
            Vector3 direction = (Vector3.up * ver + Vector3.right * hor).normalized;
            velocity = direction * speed;
        } else {
            velocity = Vector3.zero;
        }
    }

    void FixedUpdate() {
        rigidbody.MovePosition(transform.position + velocity * Time.fixedDeltaTime);
    }
}