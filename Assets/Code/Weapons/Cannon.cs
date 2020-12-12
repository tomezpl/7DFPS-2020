using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cannon : MonoBehaviour
{
    public Camera cam;
    public Transform barrelEnd;
    public RoombaControl owner;

    public Light[] lights;
    public float flashTime = 1f;
    public float fireTime = 1.5f;

    public GameObject cannonShell;

    Quaternion _initRotation;

    bool _isFiring = false;
    float _muzzleTimer = 0f;
    float _fireTimer = 0f;

    // Lights and their associated intensities.
    Dictionary<Light, float> _lights;

    CannonBullet _firedShell;

    // Start is called before the first frame update
    void Start()
    {
        // Store the intial orientation of the cannon, as per the prefab.
        _initRotation = transform.localRotation;

        if(!owner)
        {
            owner = transform.parent.GetComponent<RoombaControl>();
        }

        if(!cam)
        {
            cam = owner.GetComponentInChildren<Camera>();
        }

        _lights = new Dictionary<Light, float>();
        if (lights?.Length > 0)
        {
            foreach(Light light in lights)
            {
                _lights.Add(light, light.intensity);
                light.intensity = 0f;
                light.enabled = false;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        float camAngleY = cam.transform.localEulerAngles.y;
        transform.localRotation = _initRotation * Quaternion.AngleAxis(camAngleY, owner.transform.up);

        if(owner.playerControlled && Input.GetButtonDown("Fire1") && !_isFiring)
        {
            Fire();
        }

        MuzzleFlash();
    }

    void Fire()
    {
        _isFiring = true;
        _muzzleTimer = flashTime;
        _fireTimer = fireTime;

        _firedShell = Instantiate(cannonShell, barrelEnd).GetComponent<CannonBullet>();
        _firedShell.owner = gameObject;
        _firedShell.GetComponent<Rigidbody>().AddForce(cam.transform.forward * 1000f);
    }

    void MuzzleFlash()
    {
        if(_muzzleTimer > 0f)
        {
            float flashProgress = Mathf.InverseLerp(flashTime, flashTime * .5f, _muzzleTimer);
            float fadeProgress = Mathf.InverseLerp(flashTime * .5f, 0f, _muzzleTimer);

            if(_muzzleTimer > flashTime * .5f)
            {
                SetMuzzleFlashLights(flashProgress);
            }
            else
            {
                SetMuzzleFlashLights(1f - fadeProgress);
            }

            _muzzleTimer -= Time.deltaTime;
        }
        else
        {
            _muzzleTimer = 0f;

            foreach(Light light in _lights.Keys)
            {
                light.enabled = false;
            }
        }

        if (_fireTimer > 0f)
        {
            _fireTimer -= Time.deltaTime;
        }
        else
        {
            _isFiring = false;
            _fireTimer = 0f;
        }

        if (_firedShell)
        {
            if (_firedShell.hit)
            {
                Debug.Log(_firedShell.hit);
                RoombaControl roombaHit = _firedShell.hit.GetComponent<RoombaControl>();
                if (roombaHit)
                {
                    Debug.Log("Hit!");
                    Destroy(_firedShell.gameObject);
                    _firedShell = null;
                }
            }
        }
    }

    void SetMuzzleFlashLights(float scale)
    {
        foreach(Light light in _lights.Keys)
        {
            light.enabled = true;
            light.intensity = _lights[light] * scale;
        }
    }
}
