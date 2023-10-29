using HuntAndPeck.NativeMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static HuntAndPeck.NativeMethods.User32;

namespace HuntAndPeck.Services
{
    internal class ActionHandlerService
    {
        private MouseMoveHandlerService _mouseMoveHandlerService;
        private MouseClickHandlerService _mouseClickHandlerService;

        public ActionHandlerService(KeysBeingHeld keysBeingHeld)
        {
            _mouseMoveHandlerService = new MouseMoveHandlerService(keysBeingHeld);
            _mouseClickHandlerService = new MouseClickHandlerService(keysBeingHeld);
        }
    }

    internal class MouseClickHandlerService
    {
        private readonly KeysBeingHeld _keysBeingHeld;

        // Move to NativeMethods?
        const int INPUT_MOUSE = 0;
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        const uint MOUSEEVENTF_RIGHTUP = 0x0010;



        public MouseClickHandlerService(KeysBeingHeld keysBeingHeld)
        {
            _keysBeingHeld = keysBeingHeld;
            _keysBeingHeld.IsActionKeyHeld[Action.MouseLeftButton].ValueChanged += HandleLeftMouseButtonStateChanged;
            _keysBeingHeld.IsActionKeyHeld[Action.MouseRightButton].ValueChanged += HandleRightMouseButtonStateChanged;

        }

        private void HandleRightMouseButtonStateChanged()
        {
            var newValue = _keysBeingHeld.IsActionKeyHeld[Action.MouseRightButton].Value;
            var state = newValue ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_RIGHTUP;

            INPUT[] inputs = new INPUT[2];

            GetCursorPos(out POINT point);

            inputs[0] = new INPUT();
            inputs[0].type = INPUT_MOUSE;
            inputs[0].mi.dx = point.X;
            inputs[0].mi.dy = point.Y;
            inputs[0].mi.dwFlags = state;

            // Send the input events
            SendInput(2, inputs, Marshal.SizeOf(inputs[0]));
        }

        private void HandleLeftMouseButtonStateChanged()
        {
            var newValue = _keysBeingHeld.IsActionKeyHeld[Action.MouseLeftButton].Value;
            var state = newValue ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP;

            INPUT[] inputs = new INPUT[2];

            GetCursorPos(out POINT point);

            inputs[0] = new INPUT();
            inputs[0].type = INPUT_MOUSE;
            inputs[0].mi.dx = point.X;
            inputs[0].mi.dy = point.Y;
            inputs[0].mi.dwFlags = state;

            // Send the input events
            SendInput(2, inputs, Marshal.SizeOf(inputs[0]));
        }
    }


    internal class MouseMoveHandlerService
    {
        private DefaultKeyMappings _keyMappings;
        private KeysBeingHeld _keysBeingHeld;
        private CancellationTokenSource _cancellationTokenSource;

        // Define the base speed and speed modifiers
        const double BaseSpeed = 8;
        const double SpeedUpModifier = 3.0;
        const double SpeedDownModifier = 0.3;


        public MouseMoveHandlerService(KeysBeingHeld keysBeingHeld)
        {

            _keysBeingHeld = keysBeingHeld;
            _keysBeingHeld.IsActionKeyHeld[Action.MouseMoveUp].ValueChanged += HandleMouseMoveKeyHeldChanged;
            _keysBeingHeld.IsActionKeyHeld[Action.MouseMoveDown].ValueChanged += HandleMouseMoveKeyHeldChanged;
            _keysBeingHeld.IsActionKeyHeld[Action.MouseMoveLeft].ValueChanged += HandleMouseMoveKeyHeldChanged;
            _keysBeingHeld.IsActionKeyHeld[Action.MouseMoveRight].ValueChanged += HandleMouseMoveKeyHeldChanged;

        }

        private bool IsMouseBeingMoved() 
        {
            return _keysBeingHeld.IsActionKeyHeld[Action.MouseMoveUp].Value ||
                   _keysBeingHeld.IsActionKeyHeld[Action.MouseMoveDown].Value ||
                   _keysBeingHeld.IsActionKeyHeld[Action.MouseMoveLeft].Value ||
                   _keysBeingHeld.IsActionKeyHeld[Action.MouseMoveRight].Value;


        }

        private void HandleMouseMoveKeyHeldChanged()
        {
            var isMouseBeingMoved = IsMouseBeingMoved();

            if (isMouseBeingMoved && _cancellationTokenSource == null)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _cancellationTokenSource.Token;
                Task.Run(() => MouseMoveLoop(cancellationToken), cancellationToken);

                return;
            }

            if(!isMouseBeingMoved && _cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void MouseMoveLoop(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;


                double speedModifier = 1; // Default speed modifier

                if (_keysBeingHeld.IsActionKeyHeld[Action.MouseSpeedUp].Value)
                {
                    speedModifier *= SpeedUpModifier;
                }
                if (_keysBeingHeld.IsActionKeyHeld[Action.MouseSpeedDown].Value)
                {
                    speedModifier *= SpeedDownModifier;
                }

                if (_keysBeingHeld.IsActionKeyHeld[Action.MouseMoveUp].Value)
                {
                    MoveMouse(0, -BaseSpeed * speedModifier);
                }
                if (_keysBeingHeld.IsActionKeyHeld[Action.MouseMoveLeft].Value)
                {
                    MoveMouse(-BaseSpeed * speedModifier, 0);
                }
                if (_keysBeingHeld.IsActionKeyHeld[Action.MouseMoveDown].Value)
                {
                    MoveMouse(0, BaseSpeed * speedModifier);
                }
                if (_keysBeingHeld.IsActionKeyHeld[Action.MouseMoveRight].Value)
                {
                    MoveMouse(BaseSpeed * speedModifier, 0);
                }

                //if (IsKeyDown(VK_F17))
                //{
                //    // Scroll up when F17 key is held down
                //    ScrollMouse(120);
                //}
                //if (IsKeyDown(VK_F18))
                //{
                //    // Scroll down when F18 key is held down
                //    ScrollMouse(-120);
                //}


                Thread.Sleep(10);
            }
        }

        private void MoveMouse(double deltaX, double deltaY)
        {
            GetCursorPos(out POINT currentPosition);

            int newX = (int)Math.Round(currentPosition.X + deltaX);
            int newY = (int)Math.Round(currentPosition.Y + deltaY);

            SetCursorPos(newX, newY);
        }

    }
}
