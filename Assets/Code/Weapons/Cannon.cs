using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cannon : Weapon
{
    /// <summary>
    /// Player camera, usually same one the owner's RoombaControl uses.
    /// </summary>
    public Camera Cam;

    /// <summary>
    /// Point at the end of the barrel to spawn the bullet projectile.
    /// </summary>
    public Transform BarrelEnd;

    /// <summary>
    /// Bone to control rotation.
    /// </summary>
    public Transform YawBone, PitchBone;

    /// <summary>
    /// Player this weapon belongs to.
    /// </summary>
    public RoombaControl Owner;

    /// <summary>
    /// <para>Lights to use for the muzzle flash.</para>
    /// <para>Each light needs to have a pre-set intensity that acts as their peak intensity.</para>
    /// </summary>
    public Light[] Lights;

    /// <summary>
    /// <para>The duration of the flash animation.</para>
    /// <para>The shorter, the more sudden it appears.</para>
    /// </summary>
    public float FlashTime = 0.09f;

    /// <summary>
    /// The time it takes for a single shot to complete (essentially a cooldown).
    /// </summary>
    public float FireTime = 0.1f;

    /// <summary>
    /// Bullet projectile prefab to spawn. Used for hitreg (no hitscan here folks).
    /// </summary>
    public GameObject CannonShellPrefab;

    /// <summary>
    /// Limit on the X-axis rotation of the PitchBone.
    /// </summary>
    public const float PitchLimit = 0.4f;

    /// <summary>
    /// FxController scripts to activate on <see cref="Fire"/>.
    /// </summary>
    public FxController[] FxObjects = new FxController[0];

    /// <summary>
    /// Firing modes supported by the Cannon.
    /// </summary>
    public enum FiringMode
    {
        /// <summary>
        /// Single tap fire (semi-automatic).
        /// </summary>
        Single = 0,

        /// <summary>
        /// Short round burst.
        /// </summary>
        Burst,

        /// <summary>
        /// Fully automatic (hold to continuously fire).
        /// </summary>
        FullAuto
    }

    /// <summary>
    /// Currently active firing mode.
    /// </summary>
    public FiringMode CurrentFiringMode = FiringMode.FullAuto;

    /// <summary>
    /// Initial orientation of the <see cref="YawBone"/>.
    /// </summary>
    Quaternion initYawBoneRotation;

    /// <summary>
    /// Initial orientation of the <see cref="PitchBone"/>.
    /// </summary>
    Quaternion initPitchBoneRotation;

    /// <summary>
    /// <para>Is the gun being fired currently?</para>
    /// <para>This is set to true when <see cref="Fire"/> is called, then back to false once <see cref="FireTime"/> has elapsed.</para>
    /// </summary>
    bool isFiring = false;

    /// <summary>
    /// Time remaining on the fire cooldown.
    /// </summary>
    float fireTimer = 0f;

    /// <summary>
    /// Last fired shell - we keep track of this to despawn it after it hits a target.
    /// </summary>
    CannonBullet firedShell;

    // Start is called before the first frame update
    void Start()
    {
        // Store the intial orientation of the yaw bone.
        if (YawBone)
        {
            initYawBoneRotation = YawBone.localRotation;
        }

        // Store the initial orientation of the pitch bone.
        if (PitchBone)
        {
            initPitchBoneRotation = PitchBone.localRotation;
        }

        // Find the owner if not assigned before runtime.
        if (!Owner)
        {
            Owner = transform.parent.GetComponent<RoombaControl>();
        }

        // Find the camera if not assigned before runtime.
        if (!Cam)
        {
            Cam = Owner.GetComponentInChildren<Camera>();
        }
    }

    /// <summary>
    /// Performs Quaternion rotations to align the gun with the camera direction.
    /// </summary>
    void AlignGunWithCam()
    {
        // Align the gun orientation with the camera.
        float camAngleY = Cam.transform.localEulerAngles.y;
        YawBone.localRotation = initYawBoneRotation;

        YawBone.localRotation *= Quaternion.AngleAxis(camAngleY, Vector3.up);

        // Align the barrel with the camera pitch.
        float camAngleX = Cam.transform.localEulerAngles.x;
        float pitchSin = Mathf.Sin(Mathf.Deg2Rad * camAngleX);
        Quaternion newPitch = (YawBone == PitchBone ? YawBone.localRotation : initPitchBoneRotation) * Quaternion.AngleAxis(camAngleX, Vector3.right);

        // Clamp pitch.
        if (Mathf.Abs(pitchSin) > PitchLimit)
        {
            newPitch *= Quaternion.AngleAxis(-Mathf.Asin(Mathf.Sign(pitchSin) * (Mathf.Abs(pitchSin) - PitchLimit)) * Mathf.Rad2Deg, Vector3.right);
        }

        PitchBone.transform.localRotation = newPitch * GunRecoil();
    }

    /// <summary>
    /// Applies recoil effect. A <see cref="RecoilFx"/> needs to be defined in <see cref="FxObjects"/>.
    /// </summary>
    /// <returns>A <see cref="Quaternion"/> to apply in <see cref="AlignGunWithCam"/>.</returns>
    Quaternion GunRecoil()
    {
        RecoilFx recoilFx = null;

        foreach(FxController fx in FxObjects)
        {
            if(fx is RecoilFx)
            {
                recoilFx = (RecoilFx)fx;
                break;
            }
        }

        if(recoilFx != null)
        {
            return recoilFx.RecoilPitchExternal;
        }
        else
        {
            return Quaternion.identity;
        }
    }

    // Update is called once per frame
    void Update()
    {
        AlignGunWithCam();

        // Get the fire input based on the firing mode.
        bool acceptFireInput = CurrentFiringMode == FiringMode.FullAuto ? Owner.Input.IsFiring : Owner.Input.FiredJustNow;

        // Listen for fire inputs from the local player.
        if (Owner.PlayerControlled && acceptFireInput && !isFiring)
        {
            Fire();

            // Play firing FX for other clients by triggering it on the server.
            FireServerRpc();
        }

        // Update values for fire rate cooldown.
        ControlFireRate();

        // Check that a fired shell exists.
        if (firedShell)
        {
            // Check that the shell hit a target.
            if (firedShell.Hit)
            {
                Debug.Log(firedShell.Hit);
                RoombaControl roombaHit = firedShell.Hit.GetComponent<RoombaControl>();
                // Check if we hit a player.
                if (roombaHit)
                {
                    if (Owner.PlayerControlled)
                    {
                        Debug.Log("Hit!");
                        Debug.Log($"Dealt {firedShell.DamageDealt} damage");
                    }
                    firedShell = null;
                }
            }
        }
    }

    /// <summary>
    /// Fires a bullet projectile in the current aiming direction and activates muzzle FX & cooldown.
    /// </summary>
    void Fire()
    {
        isFiring = true;

        fireTimer = FireTime;

        // Spawn the cannon shell over network.
        firedShell = Instantiate(CannonShellPrefab, BarrelEnd.position, BarrelEnd.rotation * CannonShellPrefab.transform.rotation).GetComponent<CannonBullet>();

        // Trigger any FxControllers.
        if(FxObjects?.Length > 0)
        {
            foreach(FxController fxController in FxObjects)
            {
                fxController.Trigger();
            }
        }

        // TODO: This probably doesn't sync across clients, 
        // but might not need to as the damage event will be raised on the attacker's end anyway.
        firedShell.Owner = gameObject;

        // very rough approximation
        float deltaPitch = Vector3.Dot(-BarrelEnd.transform.up, Owner.Cam.transform.up);
        //Debug.Log($"delta pitch: {deltaPitch}");
        Vector3 launchDir = deltaPitch <= 0.997f ? Owner.Cam.transform.forward : BarrelEnd.transform.forward;
        if(Physics.Raycast(Owner.Cam.transform.position, Owner.Cam.transform.forward, out RaycastHit cameraRaycastHit, 20f, ~(1 << LayerMask.NameToLayer("LocalPlayer"))))
        {
            launchDir = (cameraRaycastHit.point - BarrelEnd.transform.position).normalized;
        }

        // Launch the cannon shell in the direction we're aiming.
        firedShell.GetComponent<Rigidbody>().AddForce(launchDir * 1000f);
        firedShell.GetComponent<Rigidbody>().useGravity = false;
    }

    /// <summary>
    /// Triggers a player's firing FX for other clients.
    /// </summary>
    /// <param name="serverRpcParams"></param>
    [ServerRpc]
    void FireServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // Number of target players (ie. excluding the firing player).
        int nbTargetPlayers = NetworkManager.Singleton.ConnectedClientsList.Count - 1;
        if (nbTargetPlayers >= 1)
        {
            ulong[] targetClients = new ulong[nbTargetPlayers];
            int counter = 0;
            foreach(ulong clientId in NetworkManager.Singleton.ConnectedClients.Keys)
            {
                if(clientId != serverRpcParams.Receive.SenderClientId)
                {
                    targetClients[counter++] = clientId;
                }
            }

            FireClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = targetClients } });
        }

    }

    /// <summary>
    /// Triggers another player's firing FX for the local client.
    /// </summary>
    /// <param name="clientRpcParams"></param>
    [ClientRpc]
    void FireClientRpc(ClientRpcParams clientRpcParams = default)
    {
        Fire();
    }

    /// <summary>
    /// Caps fire rate to the set interval.
    /// </summary>
    void ControlFireRate()
    {
        if (fireTimer > 0f)
        {
            // Update the firing cooldown (time between shots).
            fireTimer -= Time.deltaTime;
        }
        else
        {
            // If cooldown was reached, reset state to allow next shot.
            isFiring = false;
            fireTimer = 0f;
        }
    }
}
