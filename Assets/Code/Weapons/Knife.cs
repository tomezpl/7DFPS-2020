using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knife : Weapon
{
    public float stabAnimDuration = 0.33f;

    public int dmgPerBackstab = 110;

    public PlayerStats owner;

    public GameObject hit;

    float _stabAnimTimer = 0f;

    Vector3 _initLocalPos;

    PhotonView photonView;

    // Start is called before the first frame update
    void Start()
    {
        _initLocalPos = transform.localPosition;

        photonView = PhotonView.Get(this);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetButtonDown("Fire1") && _stabAnimTimer <= 0f && IsMine)
        {
            _stabAnimTimer = stabAnimDuration;
        }

        if (IsMine)
        {
            StabAnimation();
        }
    }

    void StabAnimation()
    {
        if(_stabAnimTimer <= 0f)
        {
            hit = null;
            //transform.localPosition = _initLocalPos;
            return;
        }

        float stabProgress = Mathf.InverseLerp(stabAnimDuration, stabAnimDuration * .5f, _stabAnimTimer);
        float idleProgress = Mathf.InverseLerp(stabAnimDuration * .5f, 0f, _stabAnimTimer);
        bool stabbed = _stabAnimTimer < stabAnimDuration * .5f;

        transform.localPosition = Vector3.Lerp(_initLocalPos, _initLocalPos + Vector3.forward * .33f, stabbed ? 1f - idleProgress : stabProgress);

        _stabAnimTimer -= Time.deltaTime;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!photonView || !photonView.IsMine)
        {
            return;
        }

        if (PhotonView.Get(other) && !PhotonView.Get(other).IsMine && other.GetComponent<PlayerStats>() && _stabAnimTimer > 0f && hit == null)
        {
            hit = other.gameObject;
            hit.GetComponent<PlayerStats>().lastAttacker = owner.GetComponent<PlayerStats>();
            Events.DealDamage(new DamageData
            {
                AttackerViewId = photonView.ViewID,
                VictimViewId = PhotonView.Get(hit).ViewID,
                DamageDealt = Mathf.RoundToInt(dmgPerBackstab * Mathf.Max(0f, Vector3.Dot(owner.GetComponent<RoombaControl>().roombaCollider.transform.forward, hit.GetComponent<RoombaControl>().roombaCollider.transform.forward)))
            });
        }
    }
}
