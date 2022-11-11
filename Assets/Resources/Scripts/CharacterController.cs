using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController : MonoBehaviour
{
    [Header("Horizontal Movement")]
    public float moveSpeed;
    public float maxSpeed;

    [Space(15)]public float linearDrag = 1f;
    public float airControl = 0.2f;
    public float airDrag = 0.05f;
    public float wallJumpPower = 10;

    [Header("Vertical Movement")]
    [Min(1)]public float jumpPower;
    public float mass;
    public float downMultiplier = 1.5f;

    [Space(15)]public float coyoteTime;
    public float jumpBufferTime;

    public float maxFallSpeed;
    public float yAcceleration;

    [Header("Collision")]
    public LayerMask whatIsGround;

    [Header("References")]
    public Gravity gravScript;

    [Space(15)]public ParticleSystem landingEffect;
    public ParticleSystem airJumpEffect;
    public ParticleSystem roofHitEffect;

    [Header("Booleans")]
    public bool grounded;
    public bool hitRoof;

    private RaycastHit _rayHit;

    private Vector3 _groundRay;
    private Vector3 _roofRay;

    private Vector3 _rightRay;
    private Vector3 _leftRay;
    private Vector3 _boxScale;

    private Vector3 _previousPos;
    private Vector3 _movement;

    private Vector3 _groundEffectPos;
    private Vector3 _roofEffectPos;

    private float _coyoteTimeCounter;
    private float _jumpBufferCounter;

    private float _xSpeed;
    private float _moveDir;
    private float _directionModifier;
    private float _wallDirection;
    private float _wallPower;

    private bool _isTouchingWall;

    private void Start()
    {
        _previousPos = transform.position;
        _groundEffectPos = new Vector3(0, -0.9f, 0);
        _roofEffectPos = new Vector3(0, 0.9f, 0);

        _boxScale = new Vector3(1, 2, 1);

        _roofRay = new Vector3(0, 1, 0);
        _groundRay = new Vector3(0, -1, 0);
        _rightRay = new Vector3(1, 0, 0);
        _leftRay = new Vector3(-1, 0, 0);

        _directionModifier = 1;
    }

    private void Update()
    {
        CoyoteAndBuffer();
        HorizontalMovement();

        Jump();

        ApplyMovement();

        VerticalCollisionCheck();
        HorizontalCollisionCheck();

        _previousPos = transform.position;
    }

    #region Collision Checks
    private void HorizontalCollisionCheck()
    {
        bool wasHit = false;
        bool direction = _moveDir > 0;
        Vector3 rayDir = direction ? _rightRay : _leftRay;

        for (int i = 0; i < 3; i++)
        {
            Vector3 pos = transform.position + new Vector3(0, Mathf.Lerp(-0.95f, 0.95f, i / 2.0f), 0);
            
            if (Physics.Raycast(pos, rayDir, out var hit, 0.5f, whatIsGround))
            {
                wasHit = true;
                _directionModifier = direction ? _moveDir > 0 ? 0 : 1 : _moveDir < 0 ? 0 : 1;
                if (!_isTouchingWall)
                {
                    _wallDirection = direction ? -1 : 1;
                    _coyoteTimeCounter = coyoteTime * 3;
                    _isTouchingWall = true;
                    _wallPower = 0;

                    transform.position = new Vector3(hit.point.x + (direction ? -0.5f : 0.5f), transform.position.y, 0);
                }
                break;
            }
        }

        if (!wasHit & _isTouchingWall)
        {
            _isTouchingWall = false;
            _directionModifier = 1;
        }
    }

    private void VerticalCollisionCheck()
    {
        if (hitRoof && Physics.Linecast(_previousPos, transform.position + _roofRay * 1 + _roofRay * (1.0f / 60), out _rayHit))
        {
            transform.position = _rayHit.point + _rayHit.normal * 1;

            yAcceleration = 0;
            _movement.y = 0;

            Instantiate(roofHitEffect, transform.position + _roofEffectPos, Quaternion.identity);
            hitRoof = false;
        }

        if (!grounded && Physics.Linecast(_previousPos, transform.position + _groundRay * 1 + _groundRay * (1.0f / 60), out _rayHit))
        {
            transform.position = _rayHit.point + _rayHit.normal * 1;

            yAcceleration = 0;
            _movement.y = 0;
            grounded = true;
            hitRoof = true;

            Instantiate(landingEffect, transform.position + _groundEffectPos, Quaternion.identity);
        }

        if (grounded && !Physics.Raycast(transform.position, _groundRay, 1.1f))
            grounded = false;
    }
#endregion

    private void CoyoteAndBuffer()
    {
        if (grounded)
            _coyoteTimeCounter = coyoteTime;
        else
            _coyoteTimeCounter -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space))
            _jumpBufferCounter = jumpBufferTime;
        else
            _jumpBufferCounter -= Time.deltaTime;
    }

    private void Jump()
    {
        if (_jumpBufferCounter > 0f && _coyoteTimeCounter > 0f)
        {
            if (!grounded)
            {
                Instantiate(airJumpEffect, transform.position + _groundEffectPos, Quaternion.identity);
                if (_jumpBufferCounter > 0 && _isTouchingWall)
                {
                    _directionModifier = 1;
                    _wallPower = _wallDirection * wallJumpPower;
                
                }
            }

            transform.position += new Vector3(0, 0.0001f, 0);
            grounded = false;

            _jumpBufferCounter = 0;
            _coyoteTimeCounter = 0;

            _movement.y = 0;
            yAcceleration = jumpPower * mass;
        }
    }

    private void HorizontalMovement()
    {
        if (_xSpeed > 0)
        {
            _xSpeed -=  (grounded ? linearDrag : airDrag * linearDrag) * Time.deltaTime;
            if (_xSpeed < 0)
                { _xSpeed = 0; }
        }

        float absWallPw = Mathf.Abs(_wallPower);
        float wallPwSign = Mathf.Sign(_wallPower);
        if (absWallPw > 0)
        {
            absWallPw -= (grounded ? linearDrag : airDrag * linearDrag) * Time.deltaTime;
            if (absWallPw < 0)
            { absWallPw = 0; }
            _wallPower = absWallPw * wallPwSign;
        }

        float h = Input.GetAxisRaw("Horizontal");

        _moveDir = Mathf.Approximately(h, 0) ? _moveDir : h;
        _xSpeed += moveSpeed * Mathf.Abs(h) * (grounded ? 1 : airControl) * Time.deltaTime;
        _xSpeed = Mathf.Min(_xSpeed, maxSpeed);

        _movement.x = _xSpeed * _moveDir * _directionModifier + _wallPower;
    }

    private void ApplyMovement()
    {
        if (!grounded)
        {
            if (_movement.y > maxFallSpeed)
            {
                _movement = gravScript.ApplyGravity(mass, _movement);
            } 
            else
            {
                _movement.y = maxFallSpeed;
            }

        }

        Debug.Log(_movement.y);
        this.transform.position += _movement * Time.deltaTime;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, _groundRay * 1.25f);
    }
}