using LMCore.AbstractClasses;
using LMCore.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LMCore.IO
{

    public delegate void ChangeControlsEvent(PlayerInput input, string controlScheme, SimplifiedDevice device);
    public delegate void ToggleActionMapEvent(PlayerInput input, InputActionMap enabled, InputActionMap disabled, SimplifiedDevice device);

    public class ActionMapToggler : Singleton<ActionMapToggler, ActionMapToggler>
    {
        /// <summary>
        /// Fired when an action map gets swapped out for another
        /// </summary>
        public static event ToggleActionMapEvent OnToggleActionMap;

        /// <summary>
        /// Fired when control scheme changes (e.g. keyboard to controller)
        /// </summary>
        public static event ChangeControlsEvent OnChangeControls;

        InputActionMap enabledMap;
        List<PlayerInput> connectedInputs = new List<PlayerInput>();

        public static InputActionMap EnabledMap => instance.enabledMap;

        SimplifiedDevice lastDevice = SimplifiedDevice.MouseAndKeyboard;
        public static SimplifiedDevice LastDevice
        {
            get
            {
                if (instance == null)
                {
                    Debug.LogWarning("ActionMapToggler: There's no instance, guessing Mouse and keyboard");
                    return SimplifiedDevice.MouseAndKeyboard;
                }

                return instance.lastDevice;
            }
        } 

        private void SetupPlayerInput(PlayerInput playerInput)
        {
            if (!connectedInputs.Contains(playerInput))
            {
                playerInput.controlsChangedEvent.AddListener(PlayerInput_onControlsChanged);
                connectedInputs.Add(playerInput);
            }
        }

        private new void OnDestroy()
        {
            base.OnDestroy();
            foreach (var connected in connectedInputs)
            {
                connected.onControlsChanged -= PlayerInput_onControlsChanged;
            }
        }

        public void ToggleByName(PlayerInput playerInput, string enable, string disable = null)
        {
            SetupPlayerInput(playerInput);

            if (playerInput == null)
            {
                Debug.LogError("ActionMapToggler: No player input in scene");
                return;
            }

            lastDevice = GetDevice(playerInput);

            var enableMap = playerInput.actions.FindActionMap(enable);
            var disableMap = disable == null ? null : playerInput.actions.FindActionMap(disable);

            if (enableMap != null)
            {
                enabledMap = enableMap;
                enableMap.Enable();
            }

            if (disableMap == enabledMap)
            {
                enabledMap = null;
            }

            if (disableMap != null)
            {
                disableMap.Disable();
            }

            OnToggleActionMap?.Invoke(playerInput, enabledMap, disableMap, lastDevice);
        }

        SimplifiedDevice GetDevice(PlayerInput input)
        {
            return input.devices
                .OrderByDescending(d => d.lastUpdateTime)
                .FirstOrDefault()
                .SimpleDevice(input);
        }

        private void PlayerInput_onControlsChanged(PlayerInput obj)
        {
            lastDevice = GetDevice(obj);
            Debug.Log($"ActionMapToggler: New controls are {lastDevice} & {obj.currentControlScheme}");
            OnChangeControls?.Invoke(obj, obj.currentControlScheme, lastDevice);
        }

        private void Start()
        {
            var pi = FindFirstObjectByType<PlayerInput>();
            if (pi != null)
            {
                SetupPlayerInput(pi);
                OnChangeControls?.Invoke(pi, pi.currentControlScheme, lastDevice);
            }
        }
    }
}
