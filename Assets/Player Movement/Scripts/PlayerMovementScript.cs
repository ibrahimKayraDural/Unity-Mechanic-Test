using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerMovement
{
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerMovementScript : MonoBehaviour
    {
        float TurnSpeed => _TurnSpeedRaw / 100;

        [SerializeField] float _Speed = 1;
        [SerializeField] float _TurnSpeedRaw = 25;
        [SerializeField] LayerMask _CollisionLayers;

        const float Rounder = .02f;

        CapsuleCollider _collider;
        DefaultActions _input;
        InputAction _iaMovement;
        InputAction _iaLook;
        void Awake()
        {
            _collider = GetComponent<CapsuleCollider>();
            _input = new();
            _iaMovement = _input.Player.Move;
            _iaLook = _input.Player.Look;
        }
        void OnEnable()
        {
            _iaMovement.Enable();
            _iaLook.Enable();
        }
        void OnDisable()
        {
            _iaMovement.Disable();
            _iaLook.Disable();
        }
        void FixedUpdate()
        {
            Vector3 position = transform.position;

            //MOVE PLAYER

            Vector3 moveVector = ReadAndImplementInput();
            moveVector = CheckCollisionAndSlide(moveVector);
            Vector3 targetPos = position + moveVector;

            //Set position
            transform.position = targetPos;


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
                Vector3 moveVector = new Vector3(temp.x, 0, temp.y).normalized * _Speed * Time.fixedDeltaTime;

                //Cast vector forwards
                var rot = Quaternion.LookRotation(transform.forward, Vector3.up);
                moveVector = rot * moveVector;

                return moveVector;
            }

            Vector3 CheckCollisionAndSlide(Vector3 moveVector)
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
                        hitInfo, maxDist, _CollisionLayers, QueryTriggerInteraction.Ignore))
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
                        hitInfo, slideVector.magnitude, _CollisionLayers, QueryTriggerInteraction.Ignore))
                        {
                            slideVector = slideVector.normalized * (hitInfo.distance - Rounder);
                        }

                        moveVector = newMovement + slideVector;
                    }
                }

                return moveVector;
            }
        }
    }
}
