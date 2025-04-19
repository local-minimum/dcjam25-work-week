using LMCore.AbstractClasses;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.IO
{
    public class InputBindingsManager : Singleton<InputBindingsManager, InputBindingsManager>
    {
        [SerializeField]
        bool includeInactiveBindings;

        List<ActionBindingConf> _actionBindings;
        List<ActionBindingConf> actionBindings
        {
            get
            {
                if (_actionBindings == null)
                {
                    _actionBindings = GetComponentsInChildren<ActionBindingConf>(includeInactiveBindings).ToList();
                }
                return _actionBindings;
            }
        }

        List<CustomBindingConf> _customBindings;
        List<CustomBindingConf> customBindings
        {
            get
            {
                if (_customBindings == null)
                {
                    _customBindings = GetComponentsInChildren<CustomBindingConf>(includeInactiveBindings).ToList();
                }
                return _customBindings;
            }
        }

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        public string GetActiveActionHint(
            GamePlayAction action,
            string decoration = "[%HINT%]",
            string missing = "<UNSET>")
        {
            var device = ActionMapToggler.LastDevice;

            var options = actionBindings
                .Where(a => a.Defines(action, 0) && a.For(device))
                .ToList();

            if (options.Count == 0)
            {
                Debug.LogWarning($"No binding for {action} and device {device} found among {actionBindings.Count} bindings!");
                return missing;
            }

            return decoration.Replace("%HINT%", options[0].HumanizedBinding());
        }

        public string GetActiveCustomHint(
            string customId,
            string decoration = "[%HINT%]",
            string missing = "<UNSET>")
        {
            var device = ActionMapToggler.LastDevice;

            var options = customBindings 
                .Where(c => c.Defines(customId, 0) && c.For(device))
                .ToList();

            if (options.Count == 0)
            {
                Debug.LogWarning($"No binding for '{customId}' and device {device} found among {actionBindings.Count} bindings!");
                return missing;
            }

            return decoration.Replace("%HINT%", options[0].HumanizedBinding());
        }
    }
}
