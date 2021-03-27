using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Phone : Weapon
{
    public PlayerStats owner;

    public float radiusMultiplier = 1.5f;
    public Light[] explosionLights;
    public TextMeshPro timerText;

    // Damage taken being right next to the explosion.
    public int closeUpDmg = 200;

    // Damage dropoff radius (at this distance, players will take 0 damage from the explosion).
    public float dmgRadius = 3.5f;

    public float explosionFxTime = 1f;

    // TODO: these shouldn't be public but oh well
    public bool _isExploding = false;
    public float _explosionFxTimer = 0f;

    // Time before battery detonates.
    public double timerLength = 20;

    DateTime _detonationTime;

    // Start is called before the first frame update
    void Start()
    {
        foreach(Light light in explosionLights)
        {
            light.range *= radiusMultiplier;
            light.enabled = false;
        }
        
        _detonationTime = DateTime.Now + TimeSpan.FromSeconds(timerLength);
    }

    // Update is called once per frame
    void Update()
    {
        if(!_isExploding && (DateTime.Now > _detonationTime || Input.GetButtonDown("Fire1")) && IsMine)
        {
            //PhotonView.Get(owner).RPC("DetonateLithiumBomb", RpcTarget.All);
        }

        if(_detonationTime != null)
        {
            timerText.text = (_detonationTime - DateTime.Now).ToString("ss");
        }

        if(_isExploding)
        {
            _explosionFxTimer -= Time.deltaTime;

            float inv = 1f - Mathf.InverseLerp(explosionFxTime, 0f, _explosionFxTimer);
            if(inv >= 0f)
            {
                explosionLights[3].enabled = true;
            }
            if(inv >= 0.25f)
            {
                explosionLights[2].enabled = true;
            }
            if (inv >= 0.5f)
            {
                explosionLights[1].enabled = true;
            }
            if (inv >= 0.75f)
            {
                explosionLights[0].enabled = true;
            }

            if (_explosionFxTimer <= 0f)
            {
                _isExploding = false;

                foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
                {
                    /*if (PhotonView.Get(player).IsMine)
                    {
                        // Don't bother damaging ourselves as the explosion is a suicide anyway.
                        continue;
                    }*/

                    float dmgMult = Mathf.InverseLerp(dmgRadius, 0f, Vector3.Distance(player.transform.position, transform.position));

                    Debug.Log($"Dealing {Mathf.RoundToInt(dmgMult * closeUpDmg)}");
                    /*Events.DealDamage(new DamageData
                    {
                        AttackerViewId = PhotonView.Get(owner).ViewID,
                        VictimViewId = PhotonView.Get(player).ViewID,
                        DamageDealt = Mathf.RoundToInt(dmgMult * closeUpDmg)
                    });*/
                }

                if (IsMine)
                {
                    owner.Die();
                }
            }
        }
    }
}
