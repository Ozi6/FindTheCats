using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CatHuntGameController : MonoBehaviour
{
    [Header("UI References")]
    public Text catsFoundText;
    public Text totalCatsText;
    public GameObject winPanel;
    public Button resetButton;
    public Button nextPlanetButton;
    public Button previousPlanetButton;
    public Button mainMenuButton;

    [Header("Game Settings")]
    public PlanetData[] availablePlanets;

    private Planet currentPlanet;
    private List<Cat> allCats = new List<Cat>();
    private List<Cat> foundCats = new List<Cat>();
    private PlanetController planetController;
    private int currentPlanetIndex = 0;
    private float gameStartTime;

    public int CatsFound => foundCats.Count;
    public int TotalCats => allCats.Count;
    public bool IsGameComplete => foundCats.Count >= allCats.Count && allCats.Count > 0;

    void Start()
    {
        SetupGame();
        SetupUI();
        gameStartTime = Time.time;
        if (GameManager.Instance != null)
        {
            currentPlanetIndex = GameManager.Instance.gameData.currentPlanetIndex;
            currentPlanetIndex = Mathf.Clamp(currentPlanetIndex, 0, availablePlanets.Length - 1);
        }
    }

    void SetupUI()
    {
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetPlanet);

        if (nextPlanetButton != null)
            nextPlanetButton.onClick.AddListener(LoadNextPlanet);

        if (previousPlanetButton != null)
            previousPlanetButton.onClick.AddListener(LoadPreviousPlanet);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    void SetupGame()
    {
        currentPlanet = Planet.Instance;
        if (currentPlanet == null)
        {
            var planetGO = new GameObject("Planet");
            currentPlanet = planetGO.AddComponent<Planet>();
            planetController = planetGO.AddComponent<PlanetController>();
        }
        else
        {
            planetController = PlanetController.Instance;
            if (planetController == null)
                planetController = currentPlanet.gameObject.AddComponent<PlanetController>();
        }

        LoadCurrentPlanet();
    }

    void LoadCurrentPlanet()
    {
        if (availablePlanets != null && availablePlanets.Length > 0 && currentPlanet != null)
        {
            currentPlanet.planetData = availablePlanets[currentPlanetIndex];
            currentPlanet.GeneratePlanet();
            RefreshCatsList();
            UpdateUI();
        }
    }

    void RefreshCatsList()
    {
        allCats = currentPlanet.GetAllCats();
        foundCats.Clear();
        foreach (var cat in allCats)
            if (cat != null)
                cat.OnCatClicked += OnCatFound;
    }

    public void OnCatFound(Cat cat)
    {
        if (!foundCats.Contains(cat))
        {
            foundCats.Add(cat);
            UpdateUI();
            SaveProgress();
            if (IsGameComplete)
                OnGameComplete();
        }
    }

    void SaveProgress()
    {
        if (GameManager.Instance != null)
        {
            var gameData = GameManager.Instance.gameData;
            gameData.currentPlanetIndex = currentPlanetIndex;
            gameData.totalCatsFound += 1;
            gameData.totalPlayTime += Time.time - gameStartTime;
            GameManager.Instance.SaveGameData();
        }
    }

    void UpdateUI()
    {
        if (catsFoundText != null)
            catsFoundText.text = foundCats.Count.ToString();
        if (totalCatsText != null)
            totalCatsText.text = allCats.Count.ToString();
        if (previousPlanetButton != null)
            previousPlanetButton.interactable = availablePlanets.Length > 1;
        if (nextPlanetButton != null)
            nextPlanetButton.interactable = availablePlanets.Length > 1;
    }

    public void ResetPlanet()
    {
        if (winPanel != null)
            winPanel.SetActive(false);
        if (planetController != null)
            planetController.StopRotation();
        foreach (var cat in allCats)
            if (cat != null)
                cat.OnCatClicked -= OnCatFound;
        LoadCurrentPlanet();
        gameStartTime = Time.time;
    }

    public void LoadNextPlanet()
    {
        if (availablePlanets != null && availablePlanets.Length > 1)
        {
            currentPlanetIndex = (currentPlanetIndex + 1) % availablePlanets.Length;
            ResetPlanet();
        }
    }

    public void LoadPreviousPlanet()
    {
        if (availablePlanets != null && availablePlanets.Length > 1)
        {
            currentPlanetIndex = (currentPlanetIndex - 1 + availablePlanets.Length) % availablePlanets.Length;
            ResetPlanet();
        }
    }

    public void ReturnToMainMenu()
    {
        SaveProgress();
        if (GameManager.Instance != null)
            GameManager.Instance.LoadMainMenu();
    }

    private void OnGameComplete()
    {
        if (winPanel != null)
            winPanel.SetActive(true);
        if (planetController != null)
            planetController.StopRotation();
        SaveProgress();
    }

    void OnDestroy()
    {
        foreach (var cat in allCats)
            if (cat != null)
                cat.OnCatClicked -= OnCatFound;
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugRevealAllCats()
    {
        foreach (var cat in allCats)
        {
            if (cat != null && !cat.isFound)
            {
                var renderer = cat.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.material.color = Color.red;
            }
        }
    }
}