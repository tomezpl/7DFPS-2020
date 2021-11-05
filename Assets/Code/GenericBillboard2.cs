using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class GenericBillboard2 : MonoBehaviour
{
    public void Update()
    {
        Camera camera = GameManager.Singleton?.SpawnedPlayer?.Cam ?? Camera.main ?? Camera.current;

        if(camera)
        {
            Vector3 forwards = (camera.transform.position - transform.position).normalized;
            Vector3 up = camera.transform.up;
            transform.rotation = Quaternion.LookRotation(forwards, up);
            transform.Rotate(Vector3.right, 90f, Space.Self);
        }
    }
}
