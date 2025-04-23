using UnityEngine;
using System.Collections;

public class Experiment : MonoBehaviour
{
    GameObject currentExperiment;
    static string experimentName = "Menu";

    public void LoadExperiment(string experiment)
    {
        currentExperiment = Instantiate(Resources.Load<GameObject>("Experiments/" + experiment), gameObject.transform.parent.position, gameObject.transform.parent.rotation, gameObject.transform.parent);
        experimentName = experiment;
        Destroy(gameObject);
    }

    public void ResetExperiment()
    {
        currentExperiment = Instantiate(Resources.Load<GameObject>("Experiments/" + experimentName), gameObject.transform.parent.position, gameObject.transform.parent.rotation, gameObject.transform.parent);
        Destroy(gameObject);
    }
}
