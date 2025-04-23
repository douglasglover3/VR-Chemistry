using UnityEngine;

[ExecuteInEditMode]
public class Liquid : MonoBehaviour
{
    public enum UpdateMode { Normal, UnscaledTime }
    public UpdateMode updateMode;

    [SerializeField]
    float MaxWobble = 0.03f;
    [SerializeField]
    float WobbleSpeedMove = 1f;

    [SerializeField]
    public float fillAmount = 0.5f;

    [SerializeField]
    public float full = 0.38f;
    [SerializeField]
    public float empty = 0.63f;

    [SerializeField]
    public float Recovery = 1f;
    [SerializeField]
    public float Thickness = 1f;

    Mesh mesh;
    Renderer rend;

    // Added to store instance material
    private Material instanceMaterial;

    Vector3 pos;
    Vector3 lastPos;
    Vector3 velocity;
    Quaternion lastRot;
    Vector3 angularVelocity;
    float wobbleAmountX;
    float wobbleAmountZ;
    float wobbleAmountToAddX;
    float wobbleAmountToAddZ;
    float pulse;
    float sinewave;
    float time = 0.5f;

    // Use this for initialization
    void Start()
    {
        GetMeshAndRend();
        CreateMaterialInstance();
    }

    private void OnValidate()
    {
        GetMeshAndRend();
    }

    // Create a unique instance of the material for this object
    public void CreateMaterialInstance()
    {
        if (rend != null && rend.sharedMaterial != null)
        {
            // Create a unique instance of the material
            instanceMaterial = new Material(rend.sharedMaterial);
            // Assign the instance to this renderer
            rend.material = instanceMaterial;
        }
    }

    // Create a unique instance of the material for this object
    public Material GetMaterialInstance()
    {
        return instanceMaterial;
    }

    public void GetMeshAndRend()
    {
        if (mesh == null)
        {
            mesh = GetComponent<MeshFilter>().sharedMesh;
        }
        if (rend == null)
        {
            rend = GetComponent<Renderer>();
        }
    }

    void Update()
    {
        // Ensure we have our instance material
        if (instanceMaterial == null && rend != null)
        {
            CreateMaterialInstance();
        }

        float deltaTime = 0;
        switch (updateMode)
        {
            case UpdateMode.Normal:
                deltaTime = Time.deltaTime;
                break;

            case UpdateMode.UnscaledTime:
                deltaTime = Time.unscaledDeltaTime;
                break;
        }

        time += deltaTime;

        if (deltaTime != 0)
        {
            // decrease wobble over time
            wobbleAmountToAddX = Mathf.Lerp(wobbleAmountToAddX, 0, (deltaTime * Recovery));
            wobbleAmountToAddZ = Mathf.Lerp(wobbleAmountToAddZ, 0, (deltaTime * Recovery));

            // make a sine wave of the decreasing wobble
            pulse = 2 * Mathf.PI * WobbleSpeedMove;
            sinewave = Mathf.Lerp(sinewave, Mathf.Sin(pulse * time), deltaTime * Mathf.Clamp(velocity.magnitude + angularVelocity.magnitude, Thickness, 10));

            wobbleAmountX = wobbleAmountToAddX * sinewave;
            wobbleAmountZ = wobbleAmountToAddZ * sinewave;

            // velocity
            velocity = (lastPos - transform.position) / deltaTime;

            angularVelocity = GetAngularVelocity(lastRot, transform.rotation);

            // add clamped velocity to wobble
            wobbleAmountToAddX += Mathf.Clamp((velocity.x + (velocity.y * 0.2f) + angularVelocity.z + angularVelocity.y) * MaxWobble, -MaxWobble, MaxWobble);
            wobbleAmountToAddZ += Mathf.Clamp((velocity.z + (velocity.y * 0.2f) + angularVelocity.x + angularVelocity.y) * MaxWobble, -MaxWobble, MaxWobble);
        }

        // send it to the shader - USING INSTANCE MATERIAL
        if (instanceMaterial != null)
        {
            instanceMaterial.SetFloat("_WobbleX", wobbleAmountX);
            instanceMaterial.SetFloat("_WobbleZ", wobbleAmountZ);
        }

        // set fill amount
        UpdatePos(deltaTime);

        // keep last position
        lastPos = transform.position;
        lastRot = transform.rotation;
    }

    void UpdatePos(float deltaTime)
    {
        Vector3 worldPos = transform.TransformPoint(new Vector3(mesh.bounds.center.x, mesh.bounds.center.y, mesh.bounds.center.z));
        pos = worldPos - transform.position - new Vector3(0, 1 - fillAmount, 0);

        // USING INSTANCE MATERIAL
        if (instanceMaterial != null)
        {
            instanceMaterial.SetVector("_FillAmount", pos);
        }
    }

    // Clean up material when object is destroyed
    void OnDestroy()
    {
        // Destroy the instanced material to prevent memory leaks
        if (instanceMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(instanceMaterial);
            }
            else
            {
                DestroyImmediate(instanceMaterial);
            }
        }
    }

    //https://forum.unity.com/threads/manually-calculate-angular-velocity-of-gameobject.289462/#post-4302796
    Vector3 GetAngularVelocity(Quaternion foreLastFrameRotation, Quaternion lastFrameRotation)
    {
        var q = lastFrameRotation * Quaternion.Inverse(foreLastFrameRotation);
        // no rotation?
        // You may want to increase this closer to 1 if you want to handle very small rotations.
        // Beware, if it is too close to one your answer will be Nan
        if (Mathf.Abs(q.w) > 1023.5f / 1024.0f)
            return Vector3.zero;
        float gain;
        // handle negatives, we could just flip it but this is faster
        if (q.w < 0.0f)
        {
            var angle = Mathf.Acos(-q.w);
            gain = -2.0f * angle / (Mathf.Sin(angle) * Time.deltaTime);
        }
        else
        {
            var angle = Mathf.Acos(q.w);
            gain = 2.0f * angle / (Mathf.Sin(angle) * Time.deltaTime);
        }
        Vector3 angularVelocity = new Vector3(q.x * gain, q.y * gain, q.z * gain);

        if (float.IsNaN(angularVelocity.z))
        {
            angularVelocity = Vector3.zero;
        }
        return angularVelocity;
    }
}