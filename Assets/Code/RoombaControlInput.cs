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
    }

    public void OnTurning(InputAction.CallbackContext context)
    {
        if (CheckIfInputIsOurs(context.control.device.deviceId))
        {
            Input.TurningRaw = Input.Turning = context.ReadValue<float>();
        }
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (CheckIfInputIsOurs(context.control.device.deviceId))
        {
            Input.Look = context.ReadValue<Vector2>();
        }
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        if (CheckIfInputIsOurs(context.control.device.deviceId))
        {
            Input.FiredJustNow = !Input.IsFiring && context.performed;
            Input.IsFiring = context.performed;
        }
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
