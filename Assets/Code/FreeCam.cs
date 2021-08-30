using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeCam : MonoBehaviour
{
    float yawTheta = 0f, pitchTheta = 0f;

    public float movementSpeed = 5f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float forward = Input.GetAxis("Forward") + Input.GetAxis("Backward"), horizontal = Input.GetAxis("Horizontal"), mouseX = Input.GetAxis("Mouse X"), mouseY = Input.GetAxis("Mouse Y");

        yawTheta += mouseX;
        pitchTheta += -mouseY;

        transform.localRotation = Quaternion.AngleAxis(yawTheta, Vector3.up);
        transform.localRotation *= Quaternion.AngleAxis(pitchTheta, Vector3.right);

        transform.Translate(new Vector3(horizontal, 0f, forward) * Time.deltaTime * movementSpeed);
    }
}
