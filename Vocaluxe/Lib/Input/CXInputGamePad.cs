#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using Vortice.XInput;

namespace Vocaluxe.Lib.Input.XInputGamePad
{
    enum ButtonState
    {
        Released,
        Pressed
    }

    struct SGamePadCapabilities
    {
        public bool IsConnected;
    }

    struct SGamePadDPad
    {
        public bool IsUp;
        public bool IsDown;
        public bool IsLeft;
        public bool IsRight;
    }

    struct SGamePadStick
    {
        public float X;
        public float Y;
    }

    struct SGamePadThumbSticks
    {
        public SGamePadStick Left;
        public SGamePadStick Right;
    }

    struct SGamePadTriggers
    {
        public float Left;
        public float Right;
    }

    struct SGamePadButtons
    {
        public ButtonState A;
        public ButtonState B;
        public ButtonState X;
        public ButtonState Y;
        public ButtonState Start;
        public ButtonState Back;
        public ButtonState LeftShoulder;
        public ButtonState RightShoulder;
        public ButtonState LeftStick;
        public ButtonState RightStick;
    }

    struct GamePadState
    {
        public bool IsConnected;
        public SGamePadDPad DPad;
        public SGamePadThumbSticks ThumbSticks;
        public SGamePadTriggers Triggers;
        public SGamePadButtons Buttons;
    }

    static class GamePad
    {
        public static SGamePadCapabilities GetCapabilities(int index)
        {
            Capabilities capabilities;
            return new SGamePadCapabilities { IsConnected = XInput.GetCapabilities((uint)index, DeviceQueryType.Any, out capabilities) };
        }

        public static GamePadState GetState(int index)
        {
            State state;
            if (!XInput.GetState((uint)index, out state))
            {
                return new GamePadState();
            }

            var pad = state.Gamepad;
            return new GamePadState
            {
                IsConnected = true,
                DPad = new SGamePadDPad
                {
                    IsUp = _IsSet(pad.Buttons, GamepadButtons.DPadUp),
                    IsDown = _IsSet(pad.Buttons, GamepadButtons.DPadDown),
                    IsLeft = _IsSet(pad.Buttons, GamepadButtons.DPadLeft),
                    IsRight = _IsSet(pad.Buttons, GamepadButtons.DPadRight)
                },
                ThumbSticks = new SGamePadThumbSticks
                {
                    Left = new SGamePadStick { X = _Axis(pad.LeftThumbX), Y = _Axis(pad.LeftThumbY) },
                    Right = new SGamePadStick { X = _Axis(pad.RightThumbX), Y = _Axis(pad.RightThumbY) }
                },
                Triggers = new SGamePadTriggers { Left = pad.LeftTrigger / 255.0f, Right = pad.RightTrigger / 255.0f },
                Buttons = new SGamePadButtons
                {
                    A = _Button(pad.Buttons, GamepadButtons.A),
                    B = _Button(pad.Buttons, GamepadButtons.B),
                    X = _Button(pad.Buttons, GamepadButtons.X),
                    Y = _Button(pad.Buttons, GamepadButtons.Y),
                    Start = _Button(pad.Buttons, GamepadButtons.Start),
                    Back = _Button(pad.Buttons, GamepadButtons.Back),
                    LeftShoulder = _Button(pad.Buttons, GamepadButtons.LeftShoulder),
                    RightShoulder = _Button(pad.Buttons, GamepadButtons.RightShoulder),
                    LeftStick = _Button(pad.Buttons, GamepadButtons.LeftThumb),
                    RightStick = _Button(pad.Buttons, GamepadButtons.RightThumb)
                }
            };
        }

        public static bool SetVibration(int index, float left, float right)
        {
            return XInput.SetVibration((uint)index, Math.Clamp(left, 0.0f, 1.0f), Math.Clamp(right, 0.0f, 1.0f));
        }

        private static bool _IsSet(GamepadButtons buttons, GamepadButtons button)
        {
            return (buttons & button) == button;
        }

        private static ButtonState _Button(GamepadButtons buttons, GamepadButtons button)
        {
            return _IsSet(buttons, button) ? ButtonState.Pressed : ButtonState.Released;
        }

        private static float _Axis(short value)
        {
            return Math.Max(-1.0f, value / 32767.0f);
        }
    }
}
