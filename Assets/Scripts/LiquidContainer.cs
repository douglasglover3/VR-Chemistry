using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEngine;


public class LiquidContainer : MonoBehaviour
{

    Liquid liquid;
    MeshRenderer liquidMesh;
    ParticleSystemRenderer pouringParticlesRenderer;
    Rigidbody container;

    Vector3 lastVelocity;
    float mixSpeed;
    int frameCount = 0;
    float temperature = 0;

    Vector4 trailVector = Vector4.zero;
    Texture2D trailBaseMap;

    public Dictionary<string, float> chemicalComposition;

    [SerializeField]
    string chemicalName;
    [SerializeField]
    float fullness = 1.0f;

    public int volumeMilliliters = 1000;

    bool firstUpdate = false;

    // list of chemicals = list of reduction ratios, new list of chemicals, minimum temperature, maximum temperature, temperature change, mixing multiplier, reaction time, slow down rate, needs mixing
    readonly Dictionary<List<string>, (List<float> reductions, Dictionary<string, float> newChemicals, float minTemperature, float maxTemperature, float temperatureChange, float mixingMultiplier, float reactionTime, float slowDownRate, bool needsMixing)> chemicalReactions = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        container = GetComponent<Rigidbody>();

        lastVelocity = container.linearVelocity;
        mixSpeed = 0;
        chemicalComposition = new();
        trailBaseMap = Texture2D.normalTexture;

        //Purple Dyed Water
        chemicalReactions.Add(new List<string> { "Purple Food Coloring", "Water" }, (new List<float> { 0.08f, 1f }, new Dictionary<string, float> { { "Purple Dyed Water", 1f } }, 0f, 100f, 0f, 10f, 30f, 0f, false));
        chemicalReactions.Add(new List<string> { "Purple Food Coloring", "Salt Water" }, (new List<float> { 0.08f, 1f }, new Dictionary<string, float> { { "Purple Dyed Salt Water", 1f } }, 0f, 100f, 0f, 100f, 30f, 0f, false));

        //Salt Water
        chemicalReactions.Add(new List<string> { "Salt Powder", "Water" }, (new List<float> { 0.08f, 1f }, new Dictionary<string, float> { { "Salt Water", 1f } }, 0f, 100f, 0f, 300f, 5f, 0f, true));
        chemicalReactions.Add(new List<string> { "Salt Powder", "Purple Dyed Water" }, (new List<float> { 0.08f, 1f }, new Dictionary<string, float> { { "Purple Dyed Salt Water", 1f } }, 0f, 100f, 0f, 300f, 5f, 0f, true));

        //Traffic Light Reaction
        chemicalReactions.Add(new List<string> { "Sodium Hydroxide Powder", "Water" }, (new List<float> { 0.05f, 1f }, new Dictionary<string, float> { { "Sodium Hydroxide Solution", 1f } }, 0f, 100f, 0f, 100f, 10f, 0f, true));
        chemicalReactions.Add(new List<string> { "Glucose Powder", "Water" }, (new List<float> { 0.03f, 0.5f }, new Dictionary<string, float> { { "Glucose Solution", 0.5f } }, 0f, 100f, 0f, 100f, 10f, 0f, true));
        chemicalReactions.Add(new List<string> { "Indigo Carmine Dye Powder", "Water" }, (new List<float> { 0.02f, 1f }, new Dictionary<string, float> { { "Indigo Carmine Dye Solution", 1f } }, 0f, 100f, 0f, 10f, 10f, 0f, false));
        chemicalReactions.Add(new List<string> { "Indigo Carmine Dye Solution", "Sodium Hydroxide Solution" }, (new List<float> { 0.5f, 0.5f }, new Dictionary<string, float> {{ "Indigo Carmine Dye Solution (Basic)", 1}}, 0f, 100f, 0f, 1f, 10f, 5f, false));
        chemicalReactions.Add(new List<string> { "Indigo Carmine Dye Solution (Basic)", "Glucose Solution" }, (new List<float> { 1f, 0.1f }, new Dictionary<string, float> { { "Indigo Carmine Dye Solution (Reduction 1)", 1f }, { "Water", 0.1f } }, 0f, 100f, 0f, 1f, 50f, 6f, false));
        chemicalReactions.Add(new List<string> { "Indigo Carmine Dye Solution (Reduction 1)", "Glucose Solution" }, (new List<float> { 1f, 0.1f }, new Dictionary<string, float> { { "Indigo Carmine Dye Solution (Reduction 2)", 1f }, { "Water", 0.1f } }, 0f, 100f, 0f, 1f, 100f, 14f, false));
        chemicalReactions.Add(new List<string> { "Indigo Carmine Dye Solution (Reduction 1)"}, (new List<float> { 10f }, new Dictionary<string, float> { { "Indigo Carmine Dye Solution (Basic)", 10 } }, 0f, 100f, 0f, 20f, 120f, 10f, true));
        chemicalReactions.Add(new List<string> { "Indigo Carmine Dye Solution (Reduction 2)" }, (new List<float> { 10f }, new Dictionary<string, float> { { "Indigo Carmine Dye Solution (Reduction 1)", 10 } }, 0f, 100f, 0f, 20f, 60f, 0f, true));

        SetLiquid(chemicalName, fullness * volumeMilliliters);
    }

    void OnValidate()
    {
        chemicalComposition = new();
        SetLiquid(chemicalName, fullness * volumeMilliliters);
    }

    void Update()
    {
        frameCount = frameCount + 1;
        if (frameCount >= 10 && firstUpdate)
        {
            UpdateLiquid();
            frameCount = 0;
        }
        mixSpeed = (container.linearVelocity - lastVelocity).magnitude;
        lastVelocity = container.linearVelocity;
    }

    Dictionary<string, float> ReactChemicals(Dictionary<string, float> chemicalComposition)
    {
        
        foreach (var reactants in chemicalReactions.Keys)
        {
            bool reacting = true;
            foreach (var reactant in reactants) {
                if (!chemicalComposition.ContainsKey(reactant))
                {
                    reacting = false;
                    break;
                }
            }
            if (reacting)
            {
                //list of reduction ratios, new list of chemicals, minimum temperature, maximum temperature, temperature change, mixing multiplier, reaction time, mixing needed
                (List<float> reductions, Dictionary<string, float> newChemicals, float minTemperature, float maxTemperature, float temperatureChange, float mixingMultiplier, float reactionTime, float slowDownRate, bool needsMixing) reaction = chemicalReactions[reactants];


                if (reaction.needsMixing && mixSpeed < 0.1f) //Skip if not mixing when needed
                    continue;
                if (temperature >= reaction.minTemperature && temperature <= reaction.maxTemperature)//Skip if temperature is out of range
                {
                    var reducedChemicals = reactants.Zip(reaction.reductions, (first, second) => new { first, second }).ToDictionary(x => x.first, x => x.second);
                    float requirement_ratio = 0;
                    float worst_requirement_ratio = float.PositiveInfinity;
                    foreach (var chemicalRequirements in reducedChemicals.ToList())
                    {
                        requirement_ratio = chemicalComposition[chemicalRequirements.Key] / chemicalRequirements.Value;

                        if (requirement_ratio < 1)
                            break;
                        else if (requirement_ratio < worst_requirement_ratio)
                        {
                            worst_requirement_ratio = requirement_ratio;
                        }
                    }
                    if (requirement_ratio < 1)
                        continue;
                    if (reaction.mixingMultiplier * (1 + mixSpeed) * 10 * worst_requirement_ratio / GetFullness() > UnityEngine.Random.Range(-20 + reaction.slowDownRate, reaction.reactionTime))
                    {
                        temperature = temperature + reaction.temperatureChange;
                        ReduceLiquid(reducedChemicals, false);
                        AddLiquid(reaction.newChemicals, false);
                    }
                }
            }
        }
        return chemicalComposition;
    }

    void SetLiquid(string newChemical, float newAmount)
    {
        if (newChemical == "Empty")
        {
            fullness = 0;
            liquid.fillAmount = liquid.empty;
            return;
        }
        chemicalComposition.Add(newChemical, newAmount);
        liquid = GetComponentInChildren<Liquid>();
        Material liquidMaterial = Resources.Load("Liquid Materials/" + newChemical, typeof(Material)) as Material;
        Material trailMaterial = Resources.Load("Pour Materials/" + newChemical + " Pour", typeof(Material)) as Material;
        liquidMesh = liquid.gameObject.GetComponent<MeshRenderer>();
        liquidMesh.material = liquidMaterial;
        pouringParticlesRenderer = GetComponentInChildren<ParticleSystemRenderer>();
        pouringParticlesRenderer.trailMaterial = trailMaterial;
        liquid.fillAmount = (liquid.full - liquid.empty) * (newAmount / volumeMilliliters) + liquid.empty;
        if (newChemical.Contains("Powder"))
        {
            liquid.Thickness = 10;
            liquid.Recovery = 10;
        }
        liquid.GetMeshAndRend();
        liquid.CreateMaterialInstance();
    }

    void UpdateLiquid()
    {
        firstUpdate = true;
        liquid = GetComponentInChildren<Liquid>();

        chemicalComposition = ReactChemicals(chemicalComposition);

        Vector4 tintVector = Vector4.zero;
        Vector4 topVector = Vector4.zero;
        Vector4 foamVector = Vector4.zero;
        Vector4 rimVector = Vector4.zero;

        Material newTrailMaterial = new Material(Resources.Load("Pour Materials/Default Pour", typeof(Material)) as Material);


        float currentVolume = GetFullness();

        if (currentVolume != 0)
        {
            trailVector = Vector4.zero;
            trailBaseMap = Texture2D.normalTexture;
            foreach (var chemical in chemicalComposition.ToList())
            {
                Material liquidMaterial = Resources.Load("Liquid Materials/" + chemical.Key, typeof(Material)) as Material;
                Material trailMaterial = Resources.Load("Pour Materials/" + chemical.Key + " Pour", typeof(Material)) as Material;
                tintVector += liquidMaterial.GetVector("_Tint") * chemical.Value / currentVolume;
                topVector += liquidMaterial.GetVector("_TopColor") * chemical.Value / currentVolume;
                foamVector += liquidMaterial.GetVector("_FoamColor") * chemical.Value / currentVolume;
                rimVector += liquidMaterial.GetVector("_RimColor") * chemical.Value / currentVolume;
                trailVector += trailMaterial.GetVector("_BaseColor") * chemical.Value / currentVolume;

                if (chemical.Key.Contains("Powder") && chemical.Value / currentVolume > 0.5f) //If the majority of the fluid is a powder, use powder texture for pouring trail
                    trailBaseMap = ((Texture2D)trailMaterial.GetTexture("_BaseMap"));
            }
        }

        Material liquidMaterialInstance = liquid.GetMaterialInstance();
        liquidMaterialInstance.SetVector("_Tint", tintVector);
        liquidMaterialInstance.SetVector("_TopColor", topVector);
        liquidMaterialInstance.SetVector("_FoamColor", foamVector);
        liquidMaterialInstance.SetVector("_RimColor", rimVector);

        newTrailMaterial.SetVector("_BaseColor", trailVector);
        newTrailMaterial.SetTexture("_BaseMap", trailBaseMap);
        pouringParticlesRenderer = GetComponentInChildren<ParticleSystemRenderer>();
        pouringParticlesRenderer.trailMaterial = newTrailMaterial;

        liquid.fillAmount = (liquid.full - liquid.empty) * (currentVolume / volumeMilliliters) + liquid.empty;
        return;
    }

    public float GetFullness()
    {
        return chemicalComposition.Values.Sum();
    }

    public void AddLiquid(Dictionary<string, float> addedLiquid, bool update = true)
    {
        if (GetFullness() + addedLiquid.Values.Sum() > volumeMilliliters) //Container is full
        {
            return;
        }

        foreach (var chemical in addedLiquid.ToList())
        {
            if (chemicalComposition.ContainsKey(chemical.Key))
            {
                chemicalComposition[chemical.Key] += chemical.Value;
            }
            else
            {
                chemicalComposition.Add(chemical.Key, chemical.Value);
            }
        }
        if (update)
            UpdateLiquid();
    }
    public void ReduceAllLiquid(float reducedVolume, bool update = true)
    {
        float filledVolume = GetFullness();
        if (filledVolume - reducedVolume <= 0)
        {
            chemicalComposition.Clear();
            liquid.fillAmount = liquid.empty;
        }
        else
        {
            foreach (var chemical in chemicalComposition.ToList())
            {
                float newChemicalVolume = chemical.Value - reducedVolume * (chemical.Value / filledVolume);

                if (newChemicalVolume < 0.1f)
                    chemicalComposition.Remove(chemical.Key);
                else
                    chemicalComposition[chemical.Key] = newChemicalVolume;
            }

            liquid.fillAmount = (liquid.full - liquid.empty) * (filledVolume / volumeMilliliters) + liquid.empty;
        }
        if (update)
            UpdateLiquid();
    }

    public void ReduceLiquid(Dictionary<string, float> removedLiquid, bool update = true)
    {
        foreach (var chemical in removedLiquid)
        {
            float newChemicalVolume = chemicalComposition[chemical.Key] - chemical.Value;

            if (newChemicalVolume < 0.1f)
                chemicalComposition.Remove(chemical.Key);
            else
                chemicalComposition[chemical.Key] = newChemicalVolume;
        }
        if (update)
            UpdateLiquid();
    }
}
