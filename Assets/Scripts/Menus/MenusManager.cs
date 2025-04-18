using LMCore.AbstractClasses;
using LMCore.DevConsole;
using LMCore.IO;
using LMCore.TiledDungeon.Narrative;
using LMCore.UI;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenusManager : Singleton<MenusManager, MenusManager>, IOnLoadSave
{
    [SerializeField]
    PauseMenu pauseMenu;

    [SerializeField]
    PlayerInput playerInput;

    [SerializeField]
    bool allowPauseFromStart;

    public bool AllowPause { get; set; }

    private void Start()
    {
        AllowPause = allowPauseFromStart;
    }

    private enum ActionMaps { Crawler, MenuUI };

    void ToggleActionMap(ActionMaps map)
    {
        switch (map)
        {
            case ActionMaps.Crawler:
                ActionMapToggler.instance.ToggleByName(playerInput, "Crawling", "MenuUI");
                break;
            case ActionMaps.MenuUI:
                ActionMapToggler.instance.ToggleByName(playerInput, "MenuUI", "Crawling");
                break;
        }
    }

    private void OnEnable()
    {
        AbsMenu.OnShowMenu += AbsMenu_OnShowMenu;
        AbsMenu.OnHideMenus += AbsMenu_OnHideMenus;

        StartPositionCustom.OnCapturePlayer += StartPositionCustom_OnCapturePlayer;
        StartPositionCustom.OnReleasePlayer += StartPositionCustom_OnReleasePlayer;
    }

    private void OnDisable()
    {
        AbsMenu.OnShowMenu -= AbsMenu_OnShowMenu;
        AbsMenu.OnHideMenus -= AbsMenu_OnHideMenus;

        StartPositionCustom.OnCapturePlayer -= StartPositionCustom_OnCapturePlayer;
        StartPositionCustom.OnReleasePlayer -= StartPositionCustom_OnCapturePlayer;
    }

    private void StartPositionCustom_OnCapturePlayer(LMCore.Crawler.GridEntity player)
    {
        AllowPause = false;
    }

    private void StartPositionCustom_OnReleasePlayer(LMCore.Crawler.GridEntity player)
    {
        AllowPause = true;
    }

    private void AbsMenu_OnShowMenu(AbsMenu menu)
    {
        ToggleActionMap(ActionMaps.MenuUI);
    }

    private void AbsMenu_OnHideMenus()
    {
        ToggleActionMap(ActionMaps.Crawler);
    }

    float showMenuTime;

    public void HandleShowMenu(InputAction.CallbackContext context)
    {
        if (!AllowPause || StoryManager.instance.Playing || DevConsole.focused) return;

        if (context.performed)
        {
            if (AbsMenu.ShowingMenus)
            {
                HandleExitMenu(context);
            }
            else
            {
                showMenuTime = Time.timeSinceLevelLoad;
                pauseMenu.Show();
                ToggleActionMap(ActionMaps.MenuUI);
            }
        }
    }

    public void HandleExitMenu(InputAction.CallbackContext context)
    {
        if (StoryManager.instance.Playing || DevConsole.focused || Time.timeSinceLevelLoad - showMenuTime < 0.25f) return;

        if (context.performed)
        {
            AbsMenu.FocusedMenu?.Exit();
            if (!AbsMenu.ShowingMenus)
            {
                ToggleActionMap(ActionMaps.Crawler);
            }
        }
    }


    public int OnLoadPriority => 10000;

    public void OnLoad<T>(T save) where T : new()
    {
        AllowPause = true;
    }
}
