using System.Collections;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class Callout : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The tag Transform associated with this Callout.")]
    Transform m_LazyTag;

    [SerializeField]
    [Tooltip("The line curve GameObject associated with this Callout.")]
    GameObject m_Curve;

    [SerializeField]
    [Tooltip("The composition text GameObject associated with this Callout.")]
    GameObject m_Composition;
    TMP_Text textMesh;

    [SerializeField]
    [Tooltip("Whether the associated tooltip and curve will be disabled on Start.")]
    bool m_TurnOffAtStart = true;


    void Start()
    {
        if (m_TurnOffAtStart)
            TurnOffStuff();
        textMesh = m_Composition.GetComponent<TMP_Text>();
    }

    public void TurnOnStuff()
    {
        if (m_LazyTag != null)
            m_LazyTag.gameObject.SetActive(true);
        if (m_Curve != null)
            m_Curve.SetActive(true);
    }

    public void TurnOffStuff()
    {
        if (m_LazyTag != null)
            m_LazyTag.gameObject.SetActive(false);
        if (m_Curve != null)
            m_Curve.SetActive(false);
    }

    void SetCompositionText(string text)
    {
        if (m_Composition != null)
            textMesh.SetText(text);
    }

    void Update()
    {
        LiquidContainer container = transform.GetComponentInParent<LiquidContainer>();

        float volume = container.chemicalComposition.Values.Sum();
        string text = "<b>Volume:</b>\n" + volume.ToString() + " mL / " + container.volumeMilliliters.ToString() + " mL\n\n<b>Composition:</b>\n";
        
        foreach (var chemical in container.chemicalComposition)
        {
            text = text + chemical.Key + "\n";
        }

        text = text + "\n";
        
        SetCompositionText(text);
    }
}
