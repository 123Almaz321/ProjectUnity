using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private int[] _board = new int[9];
    private int _currentPlayer = 1;
    private bool _gameOver = false;
    public bool GameOver => _gameOver;

    private void Awake()
    {
        Instance = this;
    }

    public int GetCurrentPlayer()
    {
        return _currentPlayer;
    }

    public void MakeMove(int index)
    {
        if (_gameOver || _board[index] != 0)
            return;

        _board[index] = _currentPlayer;

        if (CheckWin())
        {
            Debug.Log("Player " + _currentPlayer + " wins!");
            _gameOver = true;
            return;
        }

        if (CheckDraw())
        {
            Debug.Log("Draw!");
            _gameOver = true;
            return;
        }

        _currentPlayer = _currentPlayer == 1 ? 2 : 1;
    }

    private bool CheckWin()
    {
        int[,] combinations =
        {
            {0, 1, 2},
            {3, 4, 5},
            {6, 7, 8},
            {0, 3, 6},
            {1, 4, 7},
            {2, 5, 8},
            {0, 4, 8},
            {2, 4, 6}
        };

        for (int i = 0; i < 8; i++)
        {
            int a = combinations[i, 0];
            int b = combinations[i, 1];
            int c = combinations[i, 2];

            if (_board[a] == _currentPlayer &&
                _board[b] == _currentPlayer &&
                _board[c] == _currentPlayer)
            {
                return true;
            }
        }

        return false;
    }

    private bool CheckDraw()
    {
        foreach (int cell in _board)
        {
            if (cell == 0)
                return false;
        }

        return true;
    }
}