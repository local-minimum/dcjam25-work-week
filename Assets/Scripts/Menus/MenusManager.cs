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

    [SerializeField]
    bool stopTimeWhenPaused;

    public bool AllowPause { get; set; }

    private void Start()
    {
        if (!AllowPause)
        {
            Debug.Log($"MenusManager: Setting pausing allowed to {allowPauseFromStart} in startup");
            AllowPause = allowPauseFromStart;
        }
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
        Debug.Log($"MenusManager: Disables pausing because player captured in start position");
        AllowPause = false;
    }

    private void StartPositionCustom_OnReleasePlayer(LMCore.Crawler.GridEntity player)
    {
        Debug.Log($"MenusManager: Enables player because released from start position");
        AllowPause = true;
    }

    private void AbsMenu_OnShowMenu(AbsMenu menu)
    {
        ToggleActionMap(ActionMaps.MenuUI);
    }

    private void AbsMenu_OnHideMenus()
    {
        ToggleActionMap(ActionMaps.Crawler);

        if (Time.timeScale == 0 && stopTimeWhenPaused)
        {
            Time.timeScale = timeScale;
            timeScale = 0f;
        }
    }

    float showMenuTime;
    float timeScale;

    public void HandleShowMenu(InputAction.CallbackContext context)
    {
        if (!AllowPause || StoryManager.instance != null && StoryManager.instance.Playing || DevConsole.focused)
        {
            Debug.LogWarning($"MenusManager: Pausing allowed {AllowPause} " +
                $"Story playing {StoryManager.instance != null && StoryManager.instance.Playing} " +
                $"Dev console in focus {DevConsole.focused}");
            return;
        }

        if (context.performed)
        {
            if (AbsMenu.ShowingMenus)
            {
                HandleExitMenu(context);
            }
            else
            {
                showMenuTime = Time.realtimeSinceStartup;
                if (stopTimeWhenPaused)
                {
                    timeScale = Time.timeScale;
                    Time.timeScale = 0f;
                }
                pauseMenu.Show();
                ToggleActionMap(ActionMaps.MenuUI);
            }
        }
    }

    public void HandleExitMenu(InputAction.CallbackContext context)
    {
        if (StoryManager.instance != null && StoryManager.instance.Playing 
            || DevConsole.focused 
            || Time.realtimeSinceStartup - showMenuTime < 0.25f) return;

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
        Debug.Log($"MenusManager: Game loaded assuming pausing allowed");
        AllowPause = true;
    }
}
