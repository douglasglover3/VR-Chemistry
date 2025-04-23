using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LabObject : MonoBehaviour
{
    public List<GameObject> ghosts;
    public GameObject lastGhost = null;
    public bool held = false;

    Transform originalparent;

    private void Start()
    {
        originalparent = transform.parent;
    }

    private void Update()
    {
        if(lastGhost != null)
        {
            gameObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }
    public void StickToGhost()
    {
        held = false;
        if (ghosts.Count > 0)
        {
            lastGhost = ghosts[0];
            gameObject.transform.SetParent(lastGhost.transform);
            gameObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            Rigidbody body = gameObject.GetComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.FreezeAll;
            lastGhost.GetComponent<MeshRenderer>().enabled = false;
            lastGhost.GetComponent<CapsuleCollider>().enabled = false;
            Physics.IgnoreCollision(gameObject.GetComponent<MeshCollider>(), lastGhost.GetComponentInParent<MeshCollider>(), true);
        }
        else
        {
            gameObject.transform.SetParent(originalparent);
        }
    }

    public void UnStick()
    {
        held = true;
        if (ghosts.Count > 0)
        {
            Rigidbody body = gameObject.GetComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.None;
            body.isKinematic = false;
            Physics.IgnoreCollision(gameObject.GetComponent<MeshCollider>(), lastGhost.GetComponentInParent<MeshCollider>(), false);
            lastGhost.GetComponent<CapsuleCollider>().enabled = true;
            ghosts.Clear();
            lastGhost = null;
        }
    }

    public void SelfDestruct()
    {
        Destroy(gameObject);
    }
}
