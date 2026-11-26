using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public event Action<int, int> OnMarkPlaced; 

    private enum State
    {
        Ready,     
        MiniGame,   
        Placing,   
        GameOver 
    }

    [Header("Mini games")]
    [SerializeField] private string[] miniGameScenes; 
    [SerializeField] private string[] miniGameHints; 

    [Header("Links")]
    [SerializeField] private GameObject boardRoot; 
    [SerializeField] private GameObject readyPanel;
    [SerializeField] private TMP_Text readyText;

    [Header("Ready screen")]
    [SerializeField] private float goDuration = 1f;
    [SerializeField] private Color readyColor = Color.white;
    [SerializeField] private Color goColor = Color.green;
    [SerializeField] private float readyFontSize = 60f;
    [SerializeField] private float goFontSize = 120f;

    private int[] _board = new int[9];
    private State _state;
    private int _placingPlayer;    
    private string _currentScene;

    public bool GameOver => _state == State.GameOver;
    public bool CanPlace => _state == State.Placing;
    public int PlacingPlayer => _placingPlayer;
    public Transform BoardRootTransform => boardRoot != null ? boardRoot.transform : null;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable() => MiniGameBridge.OnFinished += HandleMiniGameFinished;
    private void OnDisable() => MiniGameBridge.OnFinished -= HandleMiniGameFinished;

    private void Start()
    {
        ShowReady();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Space))
            return;

        if (_state == State.Ready)
        {
            _state = State.MiniGame;
            StartCoroutine(GoAndLoad());
        }
        else if (_state == State.GameOver)
        {
            SceneManager.LoadScene(gameObject.scene.name);
        }
    }


    private void ShowReady()
    {
        if (miniGameScenes == null || miniGameScenes.Length == 0)
        {
            Debug.LogError("GameManager: No Mini Game Scenes");
            return;
        }

        int gameIndex = UnityEngine.Random.Range(0, miniGameScenes.Length);
        _currentScene = miniGameScenes[gameIndex];

        string hint = (miniGameHints != null && gameIndex < miniGameHints.Length
                       && !string.IsNullOrEmpty(miniGameHints[gameIndex]))
            ? miniGameHints[gameIndex]
            : _currentScene;

        SetPanelText(hint + "\n\nReady? Press SPACE to start", readyColor, readyFontSize);
        _state = State.Ready;
    }

    private void SetPanelText(string text, Color color, float size)
    {
        readyText.text = text;
        readyText.color = color;
        readyText.fontSize = size;
        readyPanel.SetActive(true);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private IEnumerator GoAndLoad()
    {
        SetPanelText("GO!", goColor, goFontSize);
        yield return new WaitForSeconds(goDuration);

        readyPanel.SetActive(false);
        yield return LoadMiniGame();
    }


    private IEnumerator LoadMiniGame()
    {
        if (boardRoot != null)
            boardRoot.SetActive(false);

        yield return SceneManager.LoadSceneAsync(_currentScene, LoadSceneMode.Additive);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(_currentScene));
    }

    private void HandleMiniGameFinished(int winner)
    {
        if (_state != State.MiniGame)
            return;

        StartCoroutine(ReturnToBoard(winner));
    }

    private IEnumerator ReturnToBoard(int winner)
    {
        SceneManager.SetActiveScene(gameObject.scene);
        yield return SceneManager.UnloadSceneAsync(_currentScene);

        if (boardRoot != null)
            boardRoot.SetActive(true);

        readyPanel.SetActive(false);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        _placingPlayer = winner;
        _state = State.Placing;
        Debug.Log("Order: " + PlayerName(winner));
    }


    public void PlaceMark(int index)
    {
        if (_state != State.Placing || _board[index] != 0)
            return;

        int player = _placingPlayer;
        _board[index] = player;
        OnMarkPlaced?.Invoke(index, player);

        if (CheckWin(player))
        {
            _state = State.GameOver;
            SetPanelText(PlayerName(player) + " wins!\n\n<size=60%>Press SPACE to restart</size>",
                         goColor, goFontSize);
            return;
        }

        if (CheckDraw())
        {
            _state = State.GameOver;
            SetPanelText("Draw!\n\n<size=60%>Press SPACE to restart</size>",
                         readyColor, goFontSize);
            return;
        }

        ShowReady();
    }

    private string PlayerName(int player) => player == 1 ? "X" : "O";

    private bool CheckWin(int player)
    {
        int[,] combinations =
        {
            {0, 1, 2}, {3, 4, 5}, {6, 7, 8},
            {0, 3, 6}, {1, 4, 7}, {2, 5, 8},
            {0, 4, 8}, {2, 4, 6}
        };

        for (int i = 0; i < 8; i++)
        {
            if (_board[combinations[i, 0]] == player &&
                _board[combinations[i, 1]] == player &&
                _board[combinations[i, 2]] == player)
                return true;
        }
        return false;
    }

    private bool CheckDraw()
    {
        foreach (int cell in _board)
            if (cell == 0) return false;
        return true;
    }
}