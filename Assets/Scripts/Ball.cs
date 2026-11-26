using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class Ball : MonoBehaviour
{
    public enum BallState { Free, Held, Flying }

    [SerializeField] private float pickupRadius = 1.1f;
    [SerializeField] private float maxPickupHeight = 2.3f;  
    [SerializeField] private float pickupBlockTime = 0.4f;  
    [SerializeField] private float fallResetHeight = -3f;  

    public BallState State { get; private set; } = BallState.Free;
    public PlayerController Holder { get; private set; }
    public PlayerController LastShooter { get; private set; }
    public Rigidbody Rb { get; private set; }

    private Collider _collider;
    private BasketballCourt _court;
    private float _pickupBlockedUntil;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
    }

    public void Init(BasketballCourt court)
    {
        _court = court;
    }

    private void Update()
    {
        if (_court == null || State == BallState.Held)
            return;

        if (transform.position.y < fallResetHeight)
        {
            _court.ResetBall();
            return;
        }

        if (Time.time < _pickupBlockedUntil || transform.position.y > maxPickupHeight)
            return;

        foreach (PlayerController player in _court.Players)
        {
            if (!player.CanPickUp) continue;

            Vector3 diff = player.transform.position - transform.position;
            diff.y = 0f;

            if (diff.magnitude < pickupRadius)
            {
                player.GiveBall(this);
                break;
            }
        }
    }

    private void LateUpdate()
    {
        if (State == BallState.Held && Holder != null)
            transform.position = Holder.HoldPoint.position;
    }

    public void Attach(PlayerController holder)
    {
        Holder = holder;
        State = BallState.Held;
        Rb.isKinematic = true;
        _collider.enabled = false;
        transform.position = holder.HoldPoint.position;
    }

    public void Launch(Vector3 velocity, PlayerController shooter)
    {
        Holder = null;
        LastShooter = shooter;
        State = BallState.Flying;

        _collider.enabled = true;
        Rb.isKinematic = false;
        Rb.linearVelocity = velocity;
        Rb.angularVelocity = Vector3.zero;

        _pickupBlockedUntil = Time.time + pickupBlockTime;
        StartCoroutine(IgnoreShooterBriefly(shooter.Col));
    }

    public void ResetTo(Vector3 position)
    {
        StopAllCoroutines();
        Holder = null;
        LastShooter = null;
        State = BallState.Free;

        _collider.enabled = true;
        Rb.isKinematic = false;
        Rb.linearVelocity = Vector3.zero;
        Rb.angularVelocity = Vector3.zero;
        Rb.position = position;
        transform.position = position;

        _pickupBlockedUntil = 0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (State == BallState.Flying)
            State = BallState.Free;
    }

    private IEnumerator IgnoreShooterBriefly(Collider shooterCollider)
    {
        Physics.IgnoreCollision(_collider, shooterCollider, true);
        yield return new WaitForSeconds(0.3f);
        Physics.IgnoreCollision(_collider, shooterCollider, false);
    }
}