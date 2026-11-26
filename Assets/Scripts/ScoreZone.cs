using UnityEngine;

public class ScoreZone : MonoBehaviour
{
    [SerializeField] private BasketballCourt court;

    private void OnTriggerEnter(Collider other)
    {
        Ball ball = other.GetComponent<Ball>();
        if (ball == null || ball.State == Ball.BallState.Held || ball.LastShooter == null)
            return;

        if (ball.Rb.linearVelocity.y < 0f)
            court.OnScored();
    }
}