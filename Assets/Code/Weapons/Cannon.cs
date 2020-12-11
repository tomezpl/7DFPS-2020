using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cannon : MonoBehaviour
{
    public Camera cam;
    public Transform barrel;
    public RoombaControl owner;

    public Light[] lights;
    public float flashTime = 1f;

    Quaternion _initRotation;

    bool _isFiring = false;
    float _muzzleTimer = 0f;

    // Lights and their associated intensities.
    Dictionary<Light, float> _lights;

    // Start is called before the first frame update
    void Start()
    {
        // Store the intial orientation of the cannon, as per the prefab.
        _initRotation = transform.localRotation;

        if(!owner)
        {
            owner = transform.parent.GetComponent<RoombaControl>();
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
        Debug.Log(camAngleY);
        transform.localRotation = _initRotation * Quaternion.AngleAxis(camAngleY, owner.transform.up);

        if(Input.GetButtonDown("Fire1") && !_isFiring)
        {
            Fire();
        }

        MuzzleFlash();
    }

    void Fire()
    {
        _isFiring = true;
        _muzzleTimer = flashTime;
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
            _isFiring = false;
            _muzzleTimer = 0f;

            foreach(Light light in _lights.Keys)
            {
                light.enabled = false;
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
