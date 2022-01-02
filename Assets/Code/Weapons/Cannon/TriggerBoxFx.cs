using UnityEngine;

/// <summary>
/// <see cref="FxController"/> used for the little box connecting the cannon to the back of the Roomba.
/// </summary>
public class TriggerBoxFx : FxController
{
    /// <summary>
    /// Texture for the corresponding state.
    /// </summary>
    public Texture2D BaseTexture, ActiveTexture, FiringTexture;

    /// <summary>
    /// Emissive maps for the corresponding states.
    /// </summary>
    public Texture2D BaseEmissive, ActiveEmissive, FiringEmissive;

    /// <summary>
    /// Index of the material on the box's <see cref="MeshRenderer"/> containing the texture.
    /// </summary>
    public int MaterialToChangeIndex;

    /// <summary>
    /// Time in seconds to disable <see cref="FiringTexture"/>.
    /// </summary>
    public float ResetFiringStateAfter = 0.1f;

    /// <summary>
    /// Muzzle flash light colour.
    /// </summary>
    public Color MuzzleFlashColour = new Color();
    
    /// <summary>
    /// Muzzle flash light intensity.
    /// </summary>
    public float MuzzleFlashIntensity = 0.3f;

    /// <summary>
    /// Muzzle flash point light range.
    /// </summary>
    public float MuzzleFlashRange = 2f;

    /// <summary>
    /// <see cref="MeshRenderer"/> used by the muzzle flash sprite.
    /// </summary>
    public MeshRenderer MuzzleFlashRenderer;

    /// <summary>
    /// Weapon object controlling this fx.
    /// </summary>
    public Cannon Owner = null;

    /// <summary>
    /// Light component to trigger to illuminate the area with the muzzle flash light.
    /// </summary>
    Light muzzleFlashLight = null;

    /// <summary>
    /// Whether <see cref="muzzleFlashLight"/> should be active.
    /// </summary>
    bool illuminateMuzzleFlash = false;

    /// <summary>
    /// True if timers should be updated.
    /// </summary>
    bool countTime = false;

    /// <summary>
    /// Time passed since <see cref="Trigger"/> was last invoked. Only updated if <see cref="countTime"/> is true.
    /// </summary>
    float timeElapsedSinceTrigger = 0f;

    /// <summary>
    /// Sets the box texture to firing, activates the muzzle flash light and sprite, and starts the timer.
    /// </summary>
    public override void Trigger()
    {
        // Set cannon's triggerbox texture.
        SetTexture(FiringTexture, FiringEmissive);

        // Start timer.
        countTime = true;
        timeElapsedSinceTrigger = 0f;

        // Activate the muzzle flash light.
        if (!illuminateMuzzleFlash)
        {
            if(muzzleFlashLight == null)
            {
                muzzleFlashLight = Owner.BarrelEnd.gameObject.AddComponent<Light>();
                muzzleFlashLight.color = MuzzleFlashColour;
                muzzleFlashLight.range = MuzzleFlashRange;
                muzzleFlashLight.intensity = MuzzleFlashIntensity;
            }

            muzzleFlashLight.enabled = true;
            illuminateMuzzleFlash = true;
        }

        // Activate the muzzle flash sprite.
        if(MuzzleFlashRenderer != null)
        {
            MuzzleFlashRenderer.enabled = true;
        }
    }

    private void Start()
    {
        SetTexture(ActiveTexture, ActiveEmissive);

        if(Owner == null)
        {
            Owner = GetComponentInParent<Cannon>();
        }

        // Disable muzzle flash sprite by default.
        if (MuzzleFlashRenderer != null)
        {
            MuzzleFlashRenderer.enabled = false;
        }
    }

    private void Update()
    {
        if(countTime)
        {
            timeElapsedSinceTrigger += Time.deltaTime;

            // Reset the state if enough time passed.
            if(timeElapsedSinceTrigger >= ResetFiringStateAfter)
            {
                ResetFiringState();
            }
        }
    }

    private void ResetFiringState()
    {
        SetTexture(ActiveTexture, ActiveEmissive);
        countTime = false;
        timeElapsedSinceTrigger = 0f;

        if(illuminateMuzzleFlash)
        {
            if (muzzleFlashLight)
            {
                muzzleFlashLight.enabled = false;
            }

            illuminateMuzzleFlash = false;
        }

        if (MuzzleFlashRenderer != null)
        {
            MuzzleFlashRenderer.enabled = false;
        }
    }

    private void SetTexture(Texture2D mainTex, Texture2D emissiveTex = null)
    {
        Material materialToChange = GetComponent<MeshRenderer>().materials[MaterialToChangeIndex];

        if (materialToChange)
        {
            if (mainTex)
            {
                materialToChange.mainTexture = mainTex;
            }

            if(emissiveTex)
            {
                materialToChange.SetTexture("_EmissionMap", emissiveTex);
            }
        }
    }
}