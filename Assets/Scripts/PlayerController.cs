using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private int playerId = 1; 

    [Header("Control")]
    [SerializeField] private KeyCode upKey = KeyCode.W;
    [SerializeField] private KeyCode downKey = KeyCode.S;
    [SerializeField] private KeyCode leftKey = KeyCode.A;
    [SerializeField] private KeyCode rightKey = KeyCode.D;
    [SerializeField] private KeyCode actionKey = KeyCode.Space; 

    [Header("Moving")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float carrySpeedMultiplier = 0.85f;  
    [SerializeField] private float chargeSpeedMultiplier = 0.35f;
    [SerializeField] private float turnSpeed = 14f;

    [Header("Dash or steal")]
    [SerializeField] private float dashSpeed = 14f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 0.8f;
    [SerializeField] private float stealRadius = 1.4f;
    [SerializeField] private float stealImmunity = 1f; 

    [Header("Throw")]
    [SerializeField] private Transform holdPoint;
    [SerializeField] private float chargeSpeed = 1.4f; 
    [SerializeField] private float launchAngle = 55f;
    [SerializeField] private float minPowerMultiplier = 0.85f;
    [SerializeField] private float maxPowerMultiplier = 1.15f;
    [SerializeField] private Slider chargeBar;        

    public int PlayerId => playerId;
    public Transform HoldPoint => holdPoint;
    public Collider Col { get; private set; }
    public bool HasBall => _ball != null;
    public bool CanPickUp => !HasBall && _controlEnabled;
    public bool IsImmune => Time.time < _immuneUntil;

    private BasketballCourt _court;
    private Rigidbody _rb;
    private Ball _ball;

    private Vector3 _input;
    private bool _controlEnabled;
    private bool _charging;
    private float _chargeTime;
    private float _power;

    private bool _dashing;
    private float _dashEnd;
    private float _nextDashTime;
    private float _immuneUntil;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        Col = GetComponent<Collider>();
        _rb.freezeRotation = true;

        if (chargeBar != null)
        {
            chargeBar.minValue = 0f;
            chargeBar.maxValue = 1f;
            chargeBar.gameObject.SetActive(false);
        }
    }

    public void Init(BasketballCourt court)
    {
        _court = court;
    }

    public void SetControl(bool enabled)
    {
        _controlEnabled = enabled;
        if (!enabled)
        {
            _input = Vector3.zero;
            StopCharging();
            _dashing = false;
        }
    }


    private void Update()
    {
        if (!_controlEnabled)
            return;

        ReadMovement();

        if (HasBall)
            HandleShooting();
        else if (Input.GetKeyDown(actionKey) && Time.time >= _nextDashTime)
            StartDash();
    }

    private void ReadMovement()
    {
        float h = (Input.GetKey(rightKey) ? 1f : 0f) - (Input.GetKey(leftKey) ? 1f : 0f);
        float v = (Input.GetKey(upKey) ? 1f : 0f) - (Input.GetKey(downKey) ? 1f : 0f);

        Transform cam = Camera.main != null ? Camera.main.transform : null;
        Vector3 forward = cam != null ? cam.forward : Vector3.forward;
        Vector3 right = cam != null ? cam.right : Vector3.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        _input = Vector3.ClampMagnitude(forward * v + right * h, 1f);

        if (_input.sqrMagnitude > 0.01f && !_dashing)
        {
            Quaternion target = Quaternion.LookRotation(_input);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
        }
    }

    private void HandleShooting()
    {
        if (Input.GetKeyDown(actionKey))
        {
            _charging = true;
            _chargeTime = 0f;
            if (chargeBar != null) chargeBar.gameObject.SetActive(true);
        }

        if (!_charging)
            return;

        _chargeTime += Time.deltaTime;
        _power = Mathf.PingPong(_chargeTime * chargeSpeed, 1f);
        if (chargeBar != null) chargeBar.value = _power;

        if (Input.GetKeyUp(actionKey))
            Shoot();
    }


    private void FixedUpdate()
    {
        Vector3 velocity;

        if (_dashing)
        {
            velocity = transform.forward * dashSpeed;
            TrySteal();

            if (Time.time >= _dashEnd)
                _dashing = false;
        }
        else
        {
            float speed = moveSpeed;
            if (HasBall) speed *= _charging ? chargeSpeedMultiplier : carrySpeedMultiplier;
            velocity = _input * speed;
        }

        velocity.y = _rb.linearVelocity.y; 
        _rb.linearVelocity = velocity;
    }


    private void StartDash()
    {
        if (_input.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(_input);

        _dashing = true;
        _dashEnd = Time.time + dashDuration;
        _nextDashTime = Time.time + dashCooldown;
    }

    private void TrySteal()
    {
        if (_court == null || HasBall) return;

        foreach (PlayerController other in _court.Players)
        {
            if (other == this || !other.HasBall || other.IsImmune)
                continue;

            if (Vector3.Distance(transform.position, other.transform.position) < stealRadius)
            {
                Ball stolen = other.TakeBall();
                GiveBall(stolen);
                _immuneUntil = Time.time + stealImmunity;
                _dashing = false;
                return;
            }
        }
    }


    public void GiveBall(Ball ball)
    {
        _ball = ball;
        ball.Attach(this);
    }

    public Ball TakeBall()
    {
        Ball ball = _ball;
        _ball = null;
        StopCharging();
        return ball;
    }

    private void Shoot()
    {
        float multiplier = Mathf.Lerp(minPowerMultiplier, maxPowerMultiplier, _power);
        Vector3 velocity = _court.CalculateShotVelocity(holdPoint.position, launchAngle, multiplier);

        Ball ball = _ball;
        _ball = null;
        StopCharging();

        ball.Launch(velocity, this);
    }

    private void StopCharging()
    {
        _charging = false;
        if (chargeBar != null) chargeBar.gameObject.SetActive(false);
    }

    public void ResetTo(Transform spawn)
    {
        _ball = null;
        StopCharging();
        _dashing = false;
        _immuneUntil = 0f;

        _rb.linearVelocity = Vector3.zero;
        _rb.position = spawn.position;
        transform.SetPositionAndRotation(spawn.position, spawn.rotation);
    }
}