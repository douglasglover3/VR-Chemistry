using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalTransition : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Portal"))
        {
            Scene current_scene = SceneManager.GetActiveScene();

            if (current_scene.buildIndex == 0)
            {
                SceneManager.LoadScene(1);
            }
            else if (current_scene.buildIndex == 1)
            {
                SceneManager.LoadScene(0);
            }
        }
    }
}
