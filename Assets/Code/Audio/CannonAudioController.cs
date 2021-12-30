using Unity.Netcode;
using UnityEngine;
public class CannonAudioController : NetworkBehaviour
{
    public Cannon Cannon;
    public BulletCylinderFx BulletCylinderFx;
    Transform ShotAudioOrigin;

    public AudioClip[] Shots = new AudioClip[0];
    
    public AudioSource ReloadAudioSrc;

    NetworkVariable<bool> IsFiring = new NetworkVariable<bool>(NetworkVariableReadPermission.Everyone, false);
    bool _isFiring = false;

    int ShotAudioClipIndex = 0;

    /// <summary>
    /// Amount of time (in seconds) that needs to pass without firing in order to reset <see cref="ShotAudioClipIndex"/> to 0.
    /// </summary>
    public float SingleFireCooldown = 0.15f;

    float CooldownProgress = 0f;
    bool SingleFireCooldownPassed = true;

    float TimeSinceLastShot = 0f;

    struct ModulatedAudioConfig
    {
        public float MinPitch, MaxPitch;
        public float BaseVolume;
    }

    ModulatedAudioConfig TurnXAudioConfig = new ModulatedAudioConfig { MinPitch = 0.75f, MaxPitch = 1.25f };
    ModulatedAudioConfig TurnYAudioConfig = new ModulatedAudioConfig { MinPitch = 0.9f, MaxPitch = 1.05f };

    public AudioSource TurnXAudioSrc, TurnYAudioSrc;

    Quaternion lastFrameRotation = Quaternion.identity;

    public float MinYawSpeed = 0.0005f;
    public float MaxYawSpeed = 0.5f;

    public float MinPitchSpeed = 0.0003f;
    public float MaxPitchSpeed = 0.33f;

    void Start()
    {
        Cannon ??= GetComponent<Cannon>();
        BulletCylinderFx ??= Cannon.GetComponentInChildren<BulletCylinderFx>();
        ShotAudioOrigin = (Cannon?.BarrelEnd?.transform ?? Cannon?.transform) ?? transform;
        lastFrameRotation = Cannon.YawBone.transform.localRotation;

        TurnXAudioConfig.BaseVolume = TurnXAudioSrc.volume;
        TurnYAudioConfig.BaseVolume = TurnYAudioSrc.volume;
    }

    private float GetCannonYawSpeed()
    {
        float lastYDeg = lastFrameRotation.eulerAngles.y;
        float yDeg = Cannon.YawBone.transform.localEulerAngles.y;

        return Mathf.Abs(Mathf.Deg2Rad * yDeg - Mathf.Deg2Rad * lastYDeg);
    }

    private float GetCannonPitchSpeed()
    {
        float lastXDeg = lastFrameRotation.eulerAngles.x;
        float xDeg = Cannon.YawBone.transform.localEulerAngles.x;

        return Mathf.Abs(Mathf.Deg2Rad * xDeg - Mathf.Deg2Rad * lastXDeg);
    }

    private void CannonTurningSounds()
    {
        float pitchSpeed = GetCannonPitchSpeed();
        float yawSpeed = GetCannonYawSpeed();

        float pitchT = Mathf.InverseLerp(MinPitchSpeed, MaxPitchSpeed, pitchSpeed);
        float yawT = Mathf.InverseLerp(MinYawSpeed, MaxYawSpeed, yawSpeed);

        TurnXAudioSrc.mute = pitchSpeed == 0f;
        TurnXAudioSrc.volume = pitchT * TurnXAudioConfig.BaseVolume;
        TurnXAudioSrc.pitch = Mathf.Lerp(TurnXAudioConfig.MinPitch, TurnXAudioConfig.MaxPitch, pitchT);

        TurnYAudioSrc.mute = yawSpeed == 0f;
        TurnYAudioSrc.pitch = Mathf.Lerp(TurnYAudioConfig.MinPitch, TurnYAudioConfig.MaxPitch, yawT);
        TurnYAudioSrc.volume = yawT * TurnYAudioConfig.BaseVolume;
    }

    private void Update()
    {
        if(IsOwner)
        {
            _isFiring = Input.GetButton("Fire1");

            if(_isFiring != IsFiring.Value)
            {
                SetIsFiringServerRpc(_isFiring);
            }
        }

        if (!SingleFireCooldownPassed)
        {
            CooldownProgress += Time.deltaTime;
            if (SingleFireCooldown <= CooldownProgress)
            {
                SingleFireCooldownPassed = true;
                ShotAudioClipIndex = 0;
            }
        }

        if(TimeSinceLastShot < Cannon.FireTime)
        {
            TimeSinceLastShot += Time.deltaTime;
        }

        if (IsFiring.Value)
        {
            CooldownProgress = 0f;
            SingleFireCooldownPassed = false;

            if (TimeSinceLastShot >= Cannon.FireTime && Shots.Length != 0)
            {
                AudioSource.PlayClipAtPoint(Shots[ShotAudioClipIndex], ShotAudioOrigin.position);
                ShotAudioClipIndex = ShotAudioClipIndex + 1 == Shots.Length ? 0 : ShotAudioClipIndex + 1;
                TimeSinceLastShot = 0f;
            }
        }

        ReloadAudioSrc.mute = !(BulletCylinderFx?.IsRotating == true) && !IsFiring.Value;

        if (!ReloadAudioSrc.isPlaying && !ReloadAudioSrc.mute)
        {
            ReloadAudioSrc.Play();
        }

        CannonTurningSounds();

        lastFrameRotation = Cannon.YawBone.transform.localRotation;
    }

    [ServerRpc]
    public void SetIsFiringServerRpc(bool isFiring, ServerRpcParams rpcParams = default)
    {
        IsFiring.Value = isFiring;
    }
}
