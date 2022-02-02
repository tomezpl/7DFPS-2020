using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class RoombaControl
{
    public int[] InputDeviceIds = new int[0];

    public enum DebugDeviceIds
    {
        Keyboard = 1,
        Mouse = 2,
        XboxController = 17
    }

    private bool CheckIfInputIsOurs(int deviceId)
    {
        foreach(int inputDeviceId in InputDeviceIds)
        {
            if(inputDeviceId == deviceId)
            {
                return true;
            }
        }

        return false;
    }

    public void OnForward(InputAction.CallbackContext context)
    {
        if(CheckIfInputIsOurs(context.control.device.deviceId))
        {
            Input.ForwardMovementRaw = Input.ForwardMovement = context.ReadValue<float>();
        }

        Debug.Log($"Getting input from device {context.control.device.deviceId}");
    }

    public InputState Input { get; } = new InputState();

    public class InputState
    {
        public bool IsFiring;

        public float ForwardMovement, ForwardMovementRaw;

        public float Turning, TurningRaw;

        public Vector2 Look;

        public bool IsJumping;

        public bool IsSuiciding;

        public bool FiredJustNow;
    }
}
