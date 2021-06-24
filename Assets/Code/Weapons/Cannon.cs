using MLAPI;
using MLAPI.Messaging;
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
    public float FlashTime = 1f;

    /// <summary>
    /// The time it takes for a single shot to complete (essentially a cooldown).
    /// </summary>
    public float FireTime = 1.5f;

    /// <summary>
    /// Bullet projectile prefab to spawn. Used for hitreg (no hitscan here folks).
    /// </summary>
    public GameObject CannonShellPrefab;

    /// <summary>
    /// Initial aiming orientation of the gun.
    /// </summary>
    Quaternion initRotation;

    /// <summary>
    /// Initial aiming orientation of the barrel - this will mainly be used to correct the barrel's pitch.
    /// </summary>
    Quaternion initBarrelRotation;

    /// <summary>
    /// <para>Is the gun being fired currently?</para>
    /// <para>This is set to true when <see cref="Fire"/> is called, then back to false once <see cref="FireTime"/> has elapsed.</para>
    /// </summary>
    bool isFiring = false;

    /// <summary>
    /// Time remaining on the muzzle flash animation.
    /// </summary>
    float muzzleTimer = 0f;

    /// <summary>
    /// Time remaining on the fire cooldown.
    /// </summary>
    float fireTimer = 0f;

    /// <summary>
    /// Lights mapped to their peak intensities.
    /// </summary>
    Dictionary<Light, float> lights;

    /// <summary>
    /// Last fired shell - we keep track of this to despawn it after it hits a target.
    /// </summary>
    CannonBullet firedShell;

    // Start is called before the first frame update
    void Start()
    {
        // Store the intial orientation of the cannon, as per the prefab.
        initRotation = transform.localRotation;

        // Store the initial orientation of the barrel.
        if (BarrelEnd)
        {
            initBarrelRotation = BarrelEnd.parent.transform.localRotation;
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

        // Store all provided lights and their peak intensities in a Dictionary.
        lights = new Dictionary<Light, float>();
        if (Lights?.Length > 0)
        {
            foreach (Light light in Lights)
            {
                lights.Add(light, light.intensity);
                light.intensity = 0f;
                light.enabled = false;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Align the gun orientation with the camera.
        float camAngleY = Cam.transform.localEulerAngles.y;
        transform.localRotation = initRotation * Quaternion.AngleAxis(camAngleY, Vector3.up);

        // Align the barrel with the camera pitch.
        float camAngleX = Cam.transform.localEulerAngles.x;
        Quaternion newBarrelRot = initBarrelRotation * Quaternion.AngleAxis(-camAngleX, Vector3.right) * Quaternion.AngleAxis(-camAngleX, Vector3.right);

        float newBarrelPitch = Mathf.Deg2Rad * newBarrelRot.eulerAngles.x;

        // Clamp the barrel pitch.
        // TODO: Add tweakable parameters for these bounds.
        if(newBarrelRot.x < -0.56f & newBarrelRot.x > -0.765f)
        {
            BarrelEnd.parent.transform.localRotation = newBarrelRot;
        }

        // Listen for fire inputs from the local player.
        if (Owner.PlayerControlled && Input.GetButtonDown("Fire1") && !isFiring)
        {
            Fire();

            // Play firing FX for other clients by triggering it on the server.
            FireServerRpc();
        }

        // Animate the muzzle flash if needed.
        MuzzleFlash();

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

        // Start timers.
        muzzleTimer = FlashTime;
        fireTimer = FireTime;

        // Spawn the cannon shell over network.
        firedShell = Instantiate(CannonShellPrefab, BarrelEnd.position, BarrelEnd.rotation * CannonShellPrefab.transform.rotation).GetComponent<CannonBullet>();

        // TODO: This probably doesn't sync across clients, 
        // but might not need to as the damage event will be raised on the attacker's end anyway.
        firedShell.Owner = gameObject;

        // very rough approximation
        Vector3 launchDir = (BarrelEnd.transform.forward + Cam.transform.forward) / 2f;
        if(Physics.Raycast(Owner.Cam.transform.position, Owner.Cam.transform.forward, out RaycastHit cameraRaycastHit, 20f, ~(1 << LayerMask.NameToLayer("LocalPlayer"))))
        {
            launchDir = (cameraRaycastHit.point - BarrelEnd.transform.position);
            Debug.Log("FOUND POINT");
            Debug.DrawLine(BarrelEnd.transform.position, BarrelEnd.transform.position + launchDir, Color.red, 5f);
            launchDir = launchDir.normalized;
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
    /// <para>Animate the muzzle flash & update muzzle and cooldown timers.</para>
    /// <para>In short:</para>
    /// <para>at t=0, each light provided in <see cref="Lights"/> will have its intensity at 0.</para>
    /// <para>at t=<see cref="FlashTime"/>*0.5f, each light will be set to the intensity they were assigned in the Inspector.</para>
    /// <para>at t=<see cref="FlashTime"/>, each light will have its intensity at 0 again.</para>
    /// </summary>
    void MuzzleFlash()
    {
        if (muzzleTimer > 0f)
        {
            // Find the interpolant value based on the flash timer & duration.
            float flashProgress = Mathf.InverseLerp(FlashTime, FlashTime * .5f, muzzleTimer);
            float fadeProgress = Mathf.InverseLerp(FlashTime * .5f, 0f, muzzleTimer);

            // Use flash progress if we've not reached t=FlashTime/2 yet.
            if (muzzleTimer > FlashTime * .5f)
            {
                SetMuzzleFlashLights(flashProgress);
            }
            // Switch to the fade progress value if we're past the peak.
            else
            {
                SetMuzzleFlashLights(1f - fadeProgress);
            }

            // Update the timer.
            muzzleTimer -= Time.deltaTime;
        }
        else
        {
            // Limit the timer to avoid any unexpected results.
            muzzleTimer = 0f;

            // Disable all lights when they're not used.
            foreach (Light light in lights.Keys)
            {
                light.enabled = false;
            }
        }

        // Update the firing cooldown as well.
        if (fireTimer > 0f)
        {
            fireTimer -= Time.deltaTime;
        }
        else
        {
            isFiring = false;
            fireTimer = 0f;
        }
    }

    /// <summary>
    /// Scale the intensity of all <see cref="Lights"/>.
    /// </summary>
    /// <param name="scale">Scale for the lights' peak intensities.</param>
    void SetMuzzleFlashLights(float scale)
    {
        foreach (Light light in lights.Keys)
        {
            light.enabled = true;
            light.intensity = lights[light] * scale;
        }
    }
}
