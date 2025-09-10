using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    public Button playButton;
    public TextMeshProUGUI planetText;
    public TextMeshProUGUI catsFoundText;

    void Start()
    {
        playButton.onClick.AddListener(() => { GameManager.Instance.PlayLatestPlanet(); });
        UpdateUI();
    }

    void UpdateUI()
    {
        var gameData = GameManager.Instance.gameData;
        var planetDatabase = GameManager.Instance.planetDatabase;

        if (planetDatabase != null)
            planetText.text = $"Planet: {gameData.currentPlanetIndex + 1}/{planetDatabase.planets.Count}";
        catsFoundText.text = $"Cats Found: {gameData.totalCatsFound}";
        int hours = Mathf.FloorToInt(gameData.totalPlayTime / 3600);
        int minutes = Mathf.FloorToInt((gameData.totalPlayTime % 3600) / 60);
    }
}