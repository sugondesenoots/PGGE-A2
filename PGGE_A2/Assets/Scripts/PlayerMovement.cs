using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PGGE;

public class PlayerMovement : MonoBehaviour
{
    [HideInInspector]
    public CharacterController mCharacterController;
    public Animator mAnimator;
     
    // Refactor Change 2 - Changed public variables to private and use SerializeField instead
    [SerializeField]
    private float mWalkSpeed = 1.5f;

    [SerializeField]
    private float mRotationSpeed = 50.0f;

    [SerializeField]
    private bool mFollowCameraForward = false;

    [SerializeField]
    private float mTurnRate = 10.0f;

    [SerializeField]
    public float mGravity = -30.0f;

    [SerializeField]
    public float mJumpHeight = 1.0f;

#if UNITY_ANDROID
    public FixedJoystick mJoystick;
#endif

    private float hInput;
    private float vInput;
    private float speed; 

    private bool jump = false;
    private bool crouch = false;  

    private PlayerJump jumpComponent;
    private PlayerGravity gravityComponent;

    public Vector3 mVelocity = new Vector3(0.0f, 0.0f, 0.0f); // Made it public so other classes can access it

    

    void Start()
    {
        mCharacterController = GetComponent<CharacterController>();
        jumpComponent = GetComponent<PlayerJump>();
        gravityComponent = GetComponent<PlayerGravity>();
    }

    void Update()
    {
        //HandleInputs();
        //Move(); 
    }

    private void FixedUpdate()
    {
        //ApplyGravity();
    }

    public void HandleInputs()
    {
        // We shall handle our inputs here.
#if UNITY_STANDALONE
        hInput = Input.GetAxis("Horizontal");
        vInput = Input.GetAxis("Vertical");
#endif

#if UNITY_ANDROID
        hInput = 2.0f * mJoystick.Horizontal;
        vInput = 2.0f * mJoystick.Vertical;
#endif

        speed = mWalkSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            speed = mWalkSpeed * 2.0f;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jump = true;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            jump = false;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            crouch = !crouch;
            Crouch();
        }
    }

    public void Move()
    {
        if (crouch) return;

        // We shall apply movement to the game object here.
        if (mAnimator == null) return;
        if (mFollowCameraForward)
        {
            // rotate Player towards the camera forward.
            Vector3 eu = Camera.main.transform.rotation.eulerAngles;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.Euler(0.0f, eu.y, 0.0f),
                mTurnRate * Time.deltaTime);
        }
        else
        {
            transform.Rotate(0.0f, hInput * mRotationSpeed * Time.deltaTime, 0.0f);
        }

        Vector3 forward = transform.TransformDirection(Vector3.forward).normalized;
        forward.y = 0.0f;

        mCharacterController.Move(forward * vInput * speed * Time.deltaTime);
        mAnimator.SetFloat("PosX", 0);
        mAnimator.SetFloat("PosZ", vInput * speed / (2.0f * mWalkSpeed));

        if (jump)
        {
            jumpComponent.Jump();
            jump = false;
        }
        gravityComponent.ApplyGravity();
    }

    private Vector3 HalfHeight;
    private Vector3 tempHeight; 

    void Crouch()
    {
        mAnimator.SetBool("Crouch", crouch);
        if (crouch)
        {
            tempHeight = CameraConstants.CameraPositionOffset;
            HalfHeight = tempHeight;
            HalfHeight.y *= 0.5f;
            CameraConstants.CameraPositionOffset = HalfHeight;
        }
        else
        {
            CameraConstants.CameraPositionOffset = tempHeight;
        }
    }
}

// Refactor Change 1 - 'Separation of Concerns' for my Jump function
public class PlayerJump : MonoBehaviour
{
    private PlayerMovement movementController;

    private void Start()
    {
        movementController = GetComponent<PlayerMovement>();
    }

    public void Jump()
    {
        movementController.mAnimator.SetTrigger("Jump");
        movementController.mVelocity.y += Mathf.Sqrt(movementController.mJumpHeight * -2f * movementController.mGravity);
    }
}

// Refactor Change 3 - 'Separation of Concerns' for my ApplyGravity function
public class PlayerGravity : MonoBehaviour
{
    private PlayerMovement movementController;

    private void Start()
    {
        movementController = GetComponent<PlayerMovement>();
    }
     
    public void ApplyGravity()
    {
        // apply gravity.
        movementController.mVelocity.x = 0.0f;
        movementController.mVelocity.z = 0.0f;

        movementController.mVelocity.y += movementController.mGravity * Time.deltaTime;
        movementController.mCharacterController.Move(movementController.mVelocity * Time.deltaTime); 

        if (movementController.mCharacterController.isGrounded && movementController.mVelocity.y < 0)
            movementController.mVelocity.y = 0f;
    }
}
