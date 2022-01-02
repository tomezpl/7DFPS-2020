using Unity.Netcode;
using UnityEngine;
public class CannonAudioController : NetworkBehaviour
{
    public Cannon Cannon;
    public BulletCylinderFx BulletCylinderFx;

    /// <summary>
    /// The Transform to spawn gun shot sounds at.
    /// </summary>
    Transform ShotAudioOrigin;

    /// <summary>
    /// Gun shot sound effects to cycle through during the loop.
    /// </summary>
    public AudioClip[] Shots = new AudioClip[0];
    
    /// <summary>
    /// Sound effect to use for spinning the bullet cylinder after each shot.
    /// </summary>
    public AudioSource ReloadAudioSrc;

    /// <summary>
    /// Is the gun currently firing?
    /// </summary>
    NetworkVariable<bool> IsFiring = new NetworkVariable<bool>(NetworkVariableReadPermission.Everyone, false);

    /// <summary>
    /// Local copy of <see cref="IsFiring"/> so that <see cref="NetworkVariable{T}.Value"/> doesn't have to be called every frame.
    /// </summary>
    bool _isFiring = false;

    /// <summary>
    /// Current index in the <see cref="Shots"/> cycle.
    /// </summary>
    int ShotAudioClipIndex = 0;

    /// <summary>
    /// Amount of time (in seconds) that needs to pass without firing in order to reset <see cref="ShotAudioClipIndex"/> to 0.
    /// </summary>
    public float SingleFireCooldown = 0.15f;

    /// <summary>
    /// Single fire cooldown elapsed since last shot (in seconds).
    /// </summary>
    float CooldownProgress = 0f;

    /// <summary>
    /// Has the single fire cooldown been reached?
    /// </summary>
    bool SingleFireCooldownPassed = true;

    /// <summary>
    /// Time (in seconds) since last shot was fired.
    /// </summary>
    float TimeSinceLastShot = 0f;

    /// <summary>
    /// Configuration for modulating audio effects based on velocity.
    /// </summary>
    struct ModulatedAudioConfig
    {
        public float MinPitch, MaxPitch;
        public float BaseVolume;
    }

    /// <summary>
    /// Velocity-based modulation config for cannon pitch rotation.
    /// </summary>
    ModulatedAudioConfig TurnXAudioConfig = new ModulatedAudioConfig { MinPitch = 0.75f, MaxPitch = 1.25f };

    /// <summary>
    /// Velocity-based modulation config for cannon yaw rotation.
    /// </summary>
    ModulatedAudioConfig TurnYAudioConfig = new ModulatedAudioConfig { MinPitch = 0.9f, MaxPitch = 1.05f };

    /// <summary>
    /// Sound effect for turning the cannon.
    /// </summary>
    public AudioSource TurnXAudioSrc, TurnYAudioSrc;

    Quaternion lastFrameRotation = Quaternion.identity;

    /// <summary>
    /// Minimal angular velocity yaw component to play yaw turn sound fx (quietly).
    /// </summary>
    public float MinYawSpeed = 0.0005f;

    /// <summary>
    /// Angular velocity yaw component at which yaw turn sound fx will be most audible.
    /// </summary>
    public float MaxYawSpeed = 0.5f;

    /// <summary>
    /// Minimal angular velocity pitch component to play pitch turn sound fx (quietly).
    /// </summary>
    public float MinPitchSpeed = 0.0003f;

    /// <summary>
    /// Angular velocity pitch component at which pitch turn sound fx will be most audible.
    /// </summary>
    public float MaxPitchSpeed = 0.33f;

    void Start()
    {
        Cannon ??= GetComponent<Cannon>();
        BulletCylinderFx ??= Cannon.GetComponentInChildren<BulletCylinderFx>();
        ShotAudioOrigin = (Cannon?.BarrelEnd?.transform ?? Cannon?.transform) ?? transform;
        lastFrameRotation = Cannon.YawBone.transform.localRotation;

        // Copy base volume from component on start.
        TurnXAudioConfig.BaseVolume = TurnXAudioSrc.volume;
        TurnYAudioConfig.BaseVolume = TurnYAudioSrc.volume;
    }

    /// <summary>
    /// Computes the speed of cannon's yaw rotation.
    /// </summary>
    /// <returns>Yaw component of the cannon's angular velocity (in radians).</returns>
    private float GetCannonYawSpeed()
    {
        float lastYDeg = lastFrameRotation.eulerAngles.y;
        float yDeg = Cannon.YawBone.transform.localEulerAngles.y;

        return Mathf.Abs(Mathf.Deg2Rad * yDeg - Mathf.Deg2Rad * lastYDeg);
    }

    /// <summary>
    /// Computes the speed of cannon's pitch rotation.
    /// </summary>
    /// <returns>Pitch component of the cannon's angular velocity (in radians).</returns>
    private float GetCannonPitchSpeed()
    {
        float lastXDeg = lastFrameRotation.eulerAngles.x;
        float xDeg = Cannon.YawBone.transform.localEulerAngles.x;

        return Mathf.Abs(Mathf.Deg2Rad * xDeg - Mathf.Deg2Rad * lastXDeg);
    }

    /// <summary>
    /// Modulates & plays the cannon turning sounds.
    /// </summary>
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
