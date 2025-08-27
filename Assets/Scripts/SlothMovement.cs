using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

[RequireComponent(typeof(CharacterController))]

public class SlothMovement : MonoBehaviour
{
    [Header("Física")]
    [SerializeField] float gravity = -30f;      // m/s² (negativa)
    [SerializeField] float jumpHeight = 2.6f;   // metros
    [SerializeField] float maxFallSpeed = -25f; // límite de caída
    [SerializeField] float dropBoost = 10f;     // impulso extra hacia abajo (swipe ↓)

    [Header("Carriles (−1,0,+1)")]
    [SerializeField] float laneWidth = 1.6f;
    [SerializeField] float laneChangeSpeed = 10f;
    int lane = 0;

    [Header("Gestos")]
    [SerializeField] float minSwipePixels = 80f;
    Vector2 swipeStart;
    bool trackingSwipe;

    CharacterController cc;
    float yVelocity;

    void OnEnable(){ EnhancedTouchSupport.Enable(); TouchSimulation.Enable(); }
    void OnDisable(){ TouchSimulation.Disable(); EnhancedTouchSupport.Disable(); }

    void Awake(){ cc = GetComponent<CharacterController>(); Application.targetFrameRate = 60; }

    void Update()
    {
        HandleSwipe();

        // Lateral hacia el carril objetivo
        float targetX = lane * laneWidth;
        float newX = Mathf.MoveTowards(transform.position.x, targetX, laneChangeSpeed * Time.deltaTime);
        float deltaX = newX - transform.position.x;

        // Gravedad y salto (sin auto-movimiento vertical)
        if (cc.isGrounded && yVelocity < 0f) yVelocity = -2f;
        yVelocity += gravity * Time.deltaTime;
        yVelocity = Mathf.Max(yVelocity, maxFallSpeed);
        float deltaY = yVelocity * Time.deltaTime;

        cc.Move(new Vector3(deltaX, deltaY, 0f));
    }

    void HandleSwipe()
    {
        foreach (var t in Touch.activeTouches)
        {
            switch (t.phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    swipeStart = t.screenPosition; trackingSwipe = true; break;
                case UnityEngine.InputSystem.TouchPhase.Ended:
                    if (!trackingSwipe) break;
                    Vector2 delta = t.screenPosition - swipeStart;
                    if (delta.magnitude >= minSwipePixels)
                    {
                        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                            lane = Mathf.Clamp(lane + (delta.x > 0 ? +1 : -1), -1, +1);
                        else if (delta.y > 0) TryJump();
                        else QuickDrop();
                    }
                    trackingSwipe = false;
                    break;
            }
        }

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.A)) lane = Mathf.Clamp(lane - 1, -1, +1);
        if (Input.GetKeyDown(KeyCode.D)) lane = Mathf.Clamp(lane + 1, -1, +1);
        if (Input.GetKeyDown(KeyCode.Space)) TryJump();
        if (Input.GetKeyDown(KeyCode.S)) QuickDrop();
#endif
    }

    void TryJump()
    {
        if (cc.isGrounded)
            yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity); // kinemática estándar
    }

    void QuickDrop()
    {
        if (yVelocity > 0f) yVelocity = 0f;
        yVelocity -= dropBoost;
    }

}
