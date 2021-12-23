using Unity.Netcode;
using UnityEngine;
public class CannonAudioController : NetworkBehaviour
{
    public Cannon Cannon;
    Transform ShotAudioOrigin;

    public AudioClip[] Shots = new AudioClip[0];

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

    void Start()
    {
        Cannon ??= GetComponent<Cannon>();
        ShotAudioOrigin = (Cannon?.BarrelEnd?.transform ?? Cannon?.transform) ?? transform;
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

            if (TimeSinceLastShot >= Cannon.FireTime)
            {
                AudioSource.PlayClipAtPoint(Shots[ShotAudioClipIndex], ShotAudioOrigin.position);
                ShotAudioClipIndex = ShotAudioClipIndex + 1 == Shots.Length ? 0 : ShotAudioClipIndex + 1;
                TimeSinceLastShot = 0f;
            }
        }
    }

    [ServerRpc]
    public void SetIsFiringServerRpc(bool isFiring, ServerRpcParams rpcParams = default)
    {
        IsFiring.Value = isFiring;
    }
}
