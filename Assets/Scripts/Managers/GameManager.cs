using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    [Header("Game State")]
    public GameData gameData;

    [Header("Scene Management")]
    public const string mainMenuScene = "MainMenuScene";
    public const string gameplayScene = "GameScene";
    public const string settingsScene = "SettingsScene";

    private GameState currentGameState = GameState.MainMenu;

    public GameState CurrentGameState => currentGameState;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
        if (gameData == null)
            gameData = new GameData();
        LoadGameData();
    }

    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (scene.name)
        {
            case mainMenuScene:
                currentGameState = GameState.MainMenu;
                break;
            case gameplayScene:
                currentGameState = GameState.Playing;
                break;
            case settingsScene:
                currentGameState = GameState.Settings;
                break;
        }
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene(mainMenuScene);
    }

    public void LoadGameplay()
    {
        SceneManager.LoadScene(gameplayScene);
    }

    public void LoadSettings()
    {
        SceneManager.LoadScene(settingsScene);
    }

    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        SaveGameData();
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }

    public void SaveGameData()
    {
        if (gameData != null)
        {
            string jsonData = JsonUtility.ToJson(gameData, true);
            PlayerPrefs.SetString("GameData", jsonData);
            PlayerPrefs.Save();
        }
    }

    public void LoadGameData()
    {
        if (PlayerPrefs.HasKey("GameData"))
        {
            string jsonData = PlayerPrefs.GetString("GameData");
            gameData = JsonUtility.FromJson<GameData>(jsonData);
        }
        else
            gameData = new GameData();
    }

    public void ResetGameData()
    {
        PlayerPrefs.DeleteKey("GameData");
        gameData = new GameData();
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
        currentGameState = GameState.Paused;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        currentGameState = GameState.Playing;
    }
}