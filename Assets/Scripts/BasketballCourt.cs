using System.Collections;
using UnityEngine;
using TMPro;

public class BasketballCourt : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private PlayerController[] players;  
    [SerializeField] private Transform[] spawnPoints;   
    [SerializeField] private Ball ball;
    [SerializeField] private Transform ballSpawn;
    [SerializeField] private Transform hoopTarget;      

    [Header("Rules")]
    [SerializeField] private int pointsToWin = 3;
    [SerializeField] private float matchTime = 60f;
    [SerializeField] private float pauseAfterScore = 1.5f;

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text infoText;

    public PlayerController[] Players => players;

    private readonly int[] _score = new int[3]; 
    private float _timeLeft;
    private bool _playing;
    private bool _goldenPoint;
    private bool _finished;

    private void Start()
    {
        foreach (PlayerController p in players)
            p.Init(this);
        ball.Init(this);

        _timeLeft = matchTime;
        UpdateUI();
        StartCoroutine(Kickoff());
    }

    private void Update()
    {
        if (!_playing || _goldenPoint)
            return;

        _timeLeft -= Time.deltaTime;

        if (_timeLeft <= 0f)
        {
            _timeLeft = 0f;

            if (_score[1] != _score[2])
            {
                EndMatch(_score[1] > _score[2] ? 1 : 2);
            }
            else
            {
                _goldenPoint = true;
                infoText.text = "Golden ball! Next basket wins";
            }
        }

        UpdateUI();
    }


    private IEnumerator Kickoff()
    {
        SetControl(false);
        ResetPositions();

        for (int i = 3; i > 0; i--)
        {
            infoText.text = i.ToString();
            yield return new WaitForSeconds(0.6f);
        }

        infoText.text = "GO!";
        SetControl(true);
        _playing = true;

        yield return new WaitForSeconds(0.6f);
        if (infoText.text == "GO!")
            infoText.text = _goldenPoint ? "Golden ball!" : "";
    }

    private void ResetPositions()
    {
        for (int i = 0; i < players.Length; i++)
            players[i].ResetTo(spawnPoints[i]);

        ResetBall();
    }

    public void ResetBall()
    {
        if (ball.Holder != null)
            ball.Holder.TakeBall();

        ball.ResetTo(ballSpawn.position);
    }

    private void SetControl(bool enabled)
    {
        foreach (PlayerController p in players)
            p.SetControl(enabled);
    }

  
    public void OnScored()
    {
        if (!_playing || _finished || ball.LastShooter == null)
            return;

        int scorer = ball.LastShooter.PlayerId;
        _score[scorer]++;
        _playing = false;
        UpdateUI();

        StartCoroutine(AfterScore(scorer));
    }

    private IEnumerator AfterScore(int scorer)
    {
        SetControl(false);
        infoText.text = Name(scorer) + " scores!";
        yield return new WaitForSeconds(pauseAfterScore);

        if (_score[scorer] >= pointsToWin || _goldenPoint)
            EndMatch(scorer);
        else
            StartCoroutine(Kickoff());
    }

    private void EndMatch(int winner)
    {
        if (_finished) return;

        _finished = true;
        _playing = false;
        SetControl(false);
        infoText.text = Name(winner) + " wins!";
        StartCoroutine(FinishAfterDelay(winner));
    }

    private IEnumerator FinishAfterDelay(int winner)
    {
        yield return new WaitForSeconds(2f);
        MiniGameBridge.Finish(winner);
    }

 
    public Vector3 CalculateShotVelocity(Vector3 start, float angle, float multiplier)
    {
        Vector3 d = hoopTarget.position - start;
        Vector3 flatDir = new Vector3(d.x, 0f, d.z);
        float distance = flatDir.magnitude;
        float height = d.y;
        flatDir = distance > 0.001f ? flatDir / distance : Vector3.forward;

        float g = -Physics.gravity.y;

     
        for (float a = angle; a <= 85f; a += 5f)
        {
            float rad = a * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float denominator = 2f * cos * cos * (distance * Mathf.Tan(rad) - height);

            if (denominator > 0f)
            {
                float speed = Mathf.Sqrt(g * distance * distance / denominator) * multiplier;
                Vector3 direction = flatDir * cos + Vector3.up * Mathf.Sin(rad);
                return direction * speed;
            }
        }

        return Vector3.up * 8f * multiplier; 
    }



    private void UpdateUI()
    {
        scoreText.text = "X: " + _score[1] + "   O: " + _score[2];
        timerText.text = _goldenPoint ? "∞" : Mathf.CeilToInt(_timeLeft).ToString();
    }

    private string Name(int player) => player == 1 ? "X" : "O";
}