using UnityEngine;

public class TriggerBoxFx : FxController
{
    public Texture2D BaseTexture, ActiveTexture, FiringTexture;

    public int MaterialToChangeIndex;

    /// <summary>
    /// Time in seconds to disable <see cref="FiringTexture"/>.
    /// </summary>
    public float ResetFiringStateAfter = 0.25f;

    public Color MuzzleFlashColour = new Color();
    public float MuzzleFlashIntensity = 0.3f;
    public float MuzzleFlashRange = 2f;

    public Cannon Owner = null;

    Light muzzleFlashLight = null;

    bool countTime = false;
    float timeElapsedSinceTrigger = 0f;

    public override void Trigger()
    {
        SetTexture(FiringTexture);
        countTime = true;
        timeElapsedSinceTrigger = 0f;

        if (muzzleFlashLight == null)
        {
            muzzleFlashLight = Instantiate(new GameObject("MuzzleFlashLight"), Owner.BarrelEnd).AddComponent<Light>();
            muzzleFlashLight.color = MuzzleFlashColour;
            muzzleFlashLight.range = MuzzleFlashRange;
            muzzleFlashLight.intensity = MuzzleFlashIntensity;
        }
    }

    private void Start()
    {
        SetTexture(ActiveTexture);

        if(Owner == null)
        {
            Owner = GetComponentInParent<Cannon>();
        }
    }

    private void Update()
    {
        if(countTime)
        {
            timeElapsedSinceTrigger += Time.deltaTime;

            if(timeElapsedSinceTrigger >= ResetFiringStateAfter)
            {
                ResetFiringState();
            }
        }
    }

    private void ResetFiringState()
    {
        SetTexture(ActiveTexture);
        countTime = false;
        timeElapsedSinceTrigger = 0f;

        if(muzzleFlashLight != null)
        {
            Destroy(muzzleFlashLight);
            muzzleFlashLight = null;
        }
    }

    private void SetTexture(Texture2D texture)
    {
        Material materialToChange = GetComponent<MeshRenderer>().materials[MaterialToChangeIndex];

        if (materialToChange)
        {
            if (texture)
            {
                materialToChange.mainTexture = texture;
                countTime = true;
            }
        }
    }
}