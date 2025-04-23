using UnityEngine;

public class Spear : MonoBehaviour
{
    Rigidbody rb;
    Vector3 original_position;
    Quaternion original_rotation;
    RigidbodyConstraints freeze_constraints;
    RigidbodyConstraints unfreeze_constraints;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        freeze_constraints = RigidbodyConstraints.FreezeAll;
        unfreeze_constraints = RigidbodyConstraints.None;
        original_position = transform.position;
        original_rotation = transform.rotation;
        rb.position = new Vector3(-17.833f, 1.116f, 4.934f);
    }

    public void resetSpear()
    {
        rb.position = original_position;
        rb.rotation = original_rotation;
        rb.angularVelocity = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
        rb.constraints = unfreeze_constraints;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "Target")
        {
            rb.constraints = freeze_constraints;
        }
    }
}
