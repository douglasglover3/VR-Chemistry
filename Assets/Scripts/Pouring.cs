using System;
using System.Collections.Generic;
using UnityEngine;

public class Pouring : MonoBehaviour
{
    ParticleSystem particles;
    Transform containerTransform;
    LiquidContainer container;

    float particleMilliliters = 0.5f;

    public float pourLimit = 0.3f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        particles = GetComponent<ParticleSystem>();
        containerTransform = GetComponent<Transform>().parent;
        container = GetComponentInParent<LiquidContainer>();
        particles.Stop();
    }

    void OnParticleCollision(GameObject other)
    {
        if (other.transform != containerTransform)
        {
            Dictionary<string, float> dropComposition = new Dictionary<string, float>();
            foreach (var chemical in container.chemicalComposition)
            {
                dropComposition.Add(chemical.Key, chemical.Value * particleMilliliters / container.GetFullness());
            }
            if (other.CompareTag("Container"))
            {
                other.GetComponent<LiquidContainer>().AddLiquid(dropComposition);
            }
            else if (other.CompareTag("Funnel"))
            {
                if (other.GetComponent<LabObject>().lastGhost != null)
                {
                    other.GetComponent<LabObject>().lastGhost.GetComponentInParent<LiquidContainer>().AddLiquid(dropComposition);
                }
            }
        }
        container.ReduceAllLiquid(particleMilliliters);
    }


    // Update is called once per frame
    void Update()
    {
        float containerOrientation = Vector3.Dot(containerTransform.up, Vector3.up);

        float currentVolume = container.GetFullness();
        float pourThreshold = currentVolume / container.volumeMilliliters - pourLimit;
        if (containerOrientation < pourThreshold && currentVolume > 0)
        {
            int amount = (int)(((currentVolume / container.volumeMilliliters) - containerOrientation) / particleMilliliters);
            particles.Emit(amount);
            
            container.gameObject.layer = 6;
        }
        else
        {
            container.gameObject.layer = 0;
        }
    }
}
