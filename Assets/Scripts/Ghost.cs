using UnityEngine;

public class Ghost : MonoBehaviour
{
    MeshRenderer ghostMesh;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ghostMesh = GetComponent<MeshRenderer>();
        ghostMesh.enabled = false;
        if (gameObject.transform.childCount != 0)
        {
            GameObject container = gameObject.transform.GetChild(0).gameObject;
            container.GetComponent<LabObject>().ghosts.Insert(0, gameObject);
            container.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            container.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (gameObject.transform.childCount == 0 && other.gameObject.name + " Ghost" == gameObject.name && other.gameObject.GetComponent<LabObject>().held)
        {
            ghostMesh.enabled = true;
            other.gameObject.GetComponent<LabObject>().ghosts.Insert(0, gameObject);
        }
    }
    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.name + " Ghost" == gameObject.name && other.gameObject.GetComponent<LabObject>().held)
        {
            if (other.gameObject.GetComponent<LabObject>().ghosts.Count > 0 && other.gameObject.GetComponent<LabObject>().ghosts[0] != gameObject)
            {
                ghostMesh.enabled = false;
            }
            else
            {
                ghostMesh.enabled = true;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        ghostMesh.enabled = false;
        if (other.gameObject.name + " Ghost" == gameObject.name)
        {
            other.gameObject.GetComponent<LabObject>().ghosts.Remove(gameObject);
        }
    }
}
