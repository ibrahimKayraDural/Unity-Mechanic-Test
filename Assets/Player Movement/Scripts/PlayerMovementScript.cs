using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerMovement
{
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerMovementScript : MonoBehaviour
    {
        float ActualGravity => Gravity * _GravityMultiplier;
        float TurnSpeed => _TurnSpeedRaw / 100;
        bool IsGrounded => _lastGroundedTime >= Time.time - _CoyoteTime;

        [SerializeField] LayerMask _GroundLayers = -1;
        [SerializeField] float _Speed = 1;
        [SerializeField] float _TurnSpeedRaw = 25;
        [SerializeField] float _GroundCheckDistance = .1f;
        [SerializeField] float _CoyoteTime = .15f;
        [SerializeField] float _JumpForce = 5;
        [SerializeField] float _GravityMultiplier = 1;

        const float Rounder = .02f;
        const float Gravity = 9.8f;

        CapsuleCollider _collider;
        DefaultActions _input;
        InputAction _iaMovement;
        InputAction _iaLook;
        InputAction _iaJump;
        float _verticalVelocity = 0;
        float _lastGroundedTime = -1;
        bool _hasJumpedOnce = false;
        void Awake()
        {
            _collider = GetComponent<CapsuleCollider>();
            _input = new();
            _iaMovement = _input.Player.Move;
            _iaLook = _input.Player.Look;
            _iaJump = _input.Player.Jump;
        }
        void OnEnable()
        {
            _iaMovement.Enable();
            _iaLook.Enable();
            _iaJump.Enable();
            _iaJump.performed += OnJumpPressed;
        }
        void OnDisable()
        {
            _iaMovement.Disable();
            _iaLook.Disable();
            _iaJump.Disable();
            _iaJump.performed -= OnJumpPressed;
        }
        void FixedUpdate()
        {
            Vector3 position = transform.position;

            //MOVE PLAYER

            Vector3 moveVector = Vector3.zero;
            Vector3 groundNormal = Vector3.zero;

            //Start with vertical movement

            //Get vertical movement
            float vertical = HandleVerticality();

            //Handle collision
            vertical = HandleVerticalCollision(vertical);

            //Set position
            position.y += vertical;
            transform.position = position;

            //Now do the horizontal movement.
            //They are done seperately because
            //the player gets stuck at walls otherwise.

            //Read input for horizontal movement
            var input = ReadAndImplementInput();
            moveVector += input;

            //Handle collision
            moveVector = HandleCollisionAndSlide(moveVector);

            //Set position
            position = position + moveVector;
            transform.position = position;

            //TURN PLAYER

            //Read input and create target rotation
            Vector2 lookVector = _iaLook.ReadValue<Vector2>();
            Quaternion targetRot = Quaternion.Euler(0, lookVector.x * TurnSpeed, 0);
            targetRot = transform.rotation * targetRot;

            //Set rotation
            transform.rotation = targetRot;


            Vector3 ReadAndImplementInput()
            {
                //Read input and create movement vector
                var temp = _iaMovement.ReadValue<Vector2>();
                var dir = new Vector3(temp.x, 0, temp.y);

                //Cast vector forwards
                var rot = Quaternion.LookRotation(transform.forward, Vector3.up);
                dir = rot * dir;
                dir = Vector3.ProjectOnPlane(dir, groundNormal);

                Vector3 moveVector = dir.normalized * _Speed * Time.fixedDeltaTime;

                return moveVector;
            }

            Vector3 HandleCollisionAndSlide(Vector3 moveVector)
            {
                if (moveVector.sqrMagnitude > 0.001f)
                {
                    float rad = _collider.radius;
                    float distToSphere = _collider.height / 2 - rad;
                    Vector3 center = _collider.center + _collider.transform.position;
                    Vector3 pos1 = center, pos2 = center;
                    pos1.y += distToSphere;
                    pos2.y -= distToSphere;
                    float maxDist = moveVector.magnitude;
                    var dir = moveVector.normalized;

                    if (Physics.CapsuleCast(pos1, pos2, rad, dir, out RaycastHit
                        hitInfo, maxDist, -1, QueryTriggerInteraction.Ignore))
                    {
                        float dist = hitInfo.distance;
                        var remaining = maxDist - dist;
                        var newMovement = dir * (dist - Rounder);

                        Vector3 rotatedNorm = Quaternion.AngleAxis(90, Vector3.up) * hitInfo.normal;
                        float dot = Vector3.Dot(rotatedNorm, dir);
                        Vector3 slideVector = (rotatedNorm * dot) * remaining;

                        pos1 += newMovement;
                        pos2 += newMovement;

                        if (Physics.CapsuleCast(pos1, pos2, rad, slideVector.normalized, out
                        hitInfo, slideVector.magnitude, -1, QueryTriggerInteraction.Ignore))
                        {
                            slideVector = slideVector.normalized * (hitInfo.distance - Rounder);
                        }

                        moveVector = newMovement + slideVector;
                    }
                }

                return moveVector;
            }

            float HandleVerticalCollision(float velThisFrame)
            {
                float finalVel = velThisFrame;
                float velMag = Mathf.Abs(velThisFrame);

                if (velMag > 0.0001f)
                {
                    float rad = _collider.radius;
                    float distToSphere = _collider.height / 2 - rad;
                    Vector3 center = _collider.center + _collider.transform.position;
                    Vector3 pos1 = center, pos2 = center;
                    pos1.y += distToSphere;
                    pos2.y -= distToSphere;
                    var dir = (Vector3.up * velThisFrame).normalized;

                    if (Physics.CapsuleCast(pos1, pos2, rad, dir, out RaycastHit
                        hitInfo, velMag, -1, QueryTriggerInteraction.Ignore))
                    {
                        float magnitude = Mathf.Max(0, hitInfo.distance - Rounder);
                        finalVel = magnitude * Mathf.Sign(velThisFrame);

                        if (Mathf.Sign(finalVel) == Mathf.Sign(_verticalVelocity))
                            _verticalVelocity = 0;
                    }
                }

                return finalVel;
            }

            float HandleVerticality()
            {
                float rad = _collider.radius;
                float distToSphere = _collider.height / 2 - rad;
                Vector3 colPos = _collider.center + _collider.transform.position;
                colPos.y -= distToSphere;

                if (Physics.SphereCast(colPos, rad, Vector3.down, out RaycastHit hitInfo,
                    _GroundCheckDistance, _GroundLayers, QueryTriggerInteraction.Ignore))
                {
                    _lastGroundedTime = Time.time;
                    groundNormal = hitInfo.normal;
                }

                if (IsGrounded == false)
                {
                    _verticalVelocity -= ActualGravity * Time.fixedDeltaTime;
                    _hasJumpedOnce = false;
                }

                return _verticalVelocity * Time.fixedDeltaTime;
            }
        }

        void OnJumpPressed(InputAction.CallbackContext obj)
        {
            if (IsGrounded && _hasJumpedOnce == false)
            {
                _verticalVelocity += _JumpForce;
                _hasJumpedOnce = true;
            }
        }
    }
}
