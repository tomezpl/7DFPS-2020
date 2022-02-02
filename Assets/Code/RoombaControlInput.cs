using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class RoombaControl
{
    private NativeArray<int> InputDeviceIds { get; set; } = new NativeArray<int>();

    enum DebugDeviceIds
    {
        Keyboard = 1,
        Mouse = 2,
        XboxController = 17
    }

    public void OnForward(InputAction.CallbackContext context)
    {
        if(InputDeviceIds.Contains(context.control.device.deviceId))
        {
            Input.ForwardMovementRaw = Input.ForwardMovement = context.ReadValue<float>();
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
