using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knife : Weapon
{
    /// <summary>
    /// Duration of the stabbing animation.
    /// </summary>
    public float StabAnimDuration = 0.33f;

    /// <summary>
    /// Damage dealt when the attacker is exactly behind the victim and at the same angle.
    /// </summary>
    public int DamagePerBackstab = 110;

    /// <summary>
    /// Owner player of this weapon.
    /// </summary>
    public PlayerStats Owner;

    /// <summary>
    /// The victim hit by this player's attack.
    /// </summary>
    public GameObject Hit;

    /// <summary>
    /// Timer to track the stabbing animation.
    /// </summary>
    float stabAnimTimer = 0f;

    /// <summary>
    /// Initial position of the knife in the player object (for interpolating in the animation).
    /// </summary>
    Vector3 initLocalPosition;

    // Start is called before the first frame update
    void Start()
    {
        initLocalPosition = transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetButtonDown("Fire1") && stabAnimTimer <= 0f && IsMine)
        {
            // Start the timer when attack input is triggered.
            stabAnimTimer = StabAnimDuration;
        }

        if (IsMine)
        {
            StabAnimation();
        }
    }

    /// <summary>
    /// Perform the stabbing animation (just an interpolation of the knife's position).
    /// </summary>
    void StabAnimation()
    {
        if(stabAnimTimer <= 0f)
        {
            Hit = null;
            return;
        }

        float stabProgress = Mathf.InverseLerp(StabAnimDuration, StabAnimDuration * .5f, stabAnimTimer);
        float idleProgress = Mathf.InverseLerp(StabAnimDuration * .5f, 0f, stabAnimTimer);

        // Is the stab lunge complete now (and we're recovering to idle position)?
        bool stabbed = stabAnimTimer < StabAnimDuration * .5f;

        // Interpolate knife position.
        transform.localPosition = Vector3.Lerp(initLocalPosition, initLocalPosition + Vector3.forward * .33f, stabbed ? 1f - idleProgress : stabProgress);

        // Update timer.
        stabAnimTimer -= Time.deltaTime;
    }

    private void OnTriggerStay(Collider other)
    {
        // TODO: Needs rewriting

        /*if (!photonView || !photonView.IsMine)
        {
            return;
        }*/

        /*if (PhotonView.Get(other) && !PhotonView.Get(other).IsMine && other.GetComponent<PlayerStats>() && _stabAnimTimer > 0f && hit == null)
        {
            hit = other.gameObject;
            hit.GetComponent<PlayerStats>().lastAttacker = owner.GetComponent<PlayerStats>();
            Events.DealDamage(new DamageData
            {
                AttackerViewId = photonView.ViewID,
                VictimViewId = PhotonView.Get(hit).ViewID,
                DamageDealt = Mathf.RoundToInt(dmgPerBackstab * Mathf.Max(0f, Vector3.Dot(owner.GetComponent<RoombaControl>().roombaCollider.transform.forward, hit.GetComponent<RoombaControl>().roombaCollider.transform.forward)))
            });
        }*/
    }
}
