using LMCore.IO;
using LMCore.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KeyboardBindingsUI : MonoBehaviour
{
    [SerializeField]
    GameObject[] bindingRoots;

    IEnumerable<KeyBinderUI> binders
    {
        get
        {
            return bindingRoots == null ? null : bindingRoots.SelectMany(br => br.GetComponentsInChildren<KeyBinderUI>());
        }
    }

    public void SetPrimaryBinding(Movement movement, string key)
    {
        var binder = binders.FirstOrDefault(b =>
        {
            var c = b.Conf;

            if (c is MovementBindingConf)
            {
                return ((MovementBindingConf)c).Defines(movement, 0);
            }
            return false;
        });

        if (binder != null)
        {
            binder.SetBindingOverride(key);
        }
        else
        {
            Debug.LogError($"Found no binder for {movement}");
        }
    }
    public void SetPrimaryBinding(GamePlayAction action, string key)
    {
        var binder = binders.FirstOrDefault(b =>
        {
            var c = b.Conf;

            if (c is ActionBindingConf)
            {
                return ((ActionBindingConf)c).Defines(action, 0);
            }
            return false;
        });

        if (binder != null)
        {
            binder.SetBindingOverride(key);
        }
        else
        {
            Debug.LogError($"Found no binder for {action}");
        }
    }

    public void SetWasdPreset()
    {
        SetPrimaryBinding(Movement.Forward, "w");
        SetPrimaryBinding(Movement.Backward, "s");
        SetPrimaryBinding(Movement.StrafeLeft, "a");
        SetPrimaryBinding(Movement.StrafeRight, "d");
        SetPrimaryBinding(Movement.YawCCW, "q");
        SetPrimaryBinding(Movement.YawCW, "e");

        SetPrimaryBinding(GamePlayAction.Interact, "space");
    }

    public void SetNumpadPreset()
    {
        SetPrimaryBinding(Movement.Forward, "numpad8");
        SetPrimaryBinding(Movement.Backward, "numpad5");
        SetPrimaryBinding(Movement.StrafeLeft, "numpad4");
        SetPrimaryBinding(Movement.StrafeRight, "numpad6");
        SetPrimaryBinding(Movement.YawCCW, "numpad7");
        SetPrimaryBinding(Movement.YawCW, "numpad9");

        SetPrimaryBinding(GamePlayAction.Interact, "space");
    }

    public void RestoreDefaults()
    {
        foreach (var binder in binders)
        {
            binder.RemoveBindingOverride();
        }
    }
}