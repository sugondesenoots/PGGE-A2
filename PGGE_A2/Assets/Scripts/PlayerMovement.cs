using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PGGE;

public class PlayerMovement : MonoBehaviour
{
    //[HideInInspector] 
    //Errors occured such as null CharacterController when hiding
    public CharacterController mCharacterController;
    public Animator mAnimator;

    //Refactor 1
    //Changed public variables to private and use SerializeField instead
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

    private Vector3 mVelocity = new Vector3(0.0f, 0.0f, 0.0f);

    void Start()
    {
        mCharacterController = GetComponent<CharacterController>();
    }

    //Refactor 2 
    //Removed unused methods

    //void Update()
    //{
    //    //HandleInputs();
    //    //Move();
    //}

    //private void FixedUpdate()
    //{
    //    //ApplyGravity();
    //}

    //Refactor 3
    public void HandleInputs()
    {
        //Separated inputs into respective methods
        MovementInput();
        SpeedInput();
        JumpInput();
        CrouchInput();
    }

    //MovementInput handles the inputs separately from the previous HandleInputs method
    private void MovementInput()
    {
#if UNITY_STANDALONE
        hInput = Input.GetAxis("Horizontal");
        vInput = Input.GetAxis("Vertical");
#endif

#if UNITY_ANDROID
    hInput = 2.0f * mJoystick.Horizontal;
    vInput = 2.0f * mJoystick.Vertical;
#endif
    }

    //Did the same for previous methods below
    private void SpeedInput()
    {
        speed = mWalkSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            speed = mWalkSpeed * 2.0f;
        }
    }

    private void JumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jump = true;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            jump = false;
        }
    }

    private void CrouchInput()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleCrouch();
        }
    }

    //Separated the toggle crouch into this method to be handled individually 
    //Rather than using HandleInputs to handle everything
    private void ToggleCrouch()
    {
        crouch = !crouch;
        Crouch();
    }

    public void Move()
    {
        if (crouch) return;

        //Created methods to handle different parts of the Move script
        RotatePlayer();
        Movement();
        UpdateAnimator();

        if (jump)
        {
            Jump();
            jump = false;
        }

        ApplyGravity();
    }

    //Method handles player rotation based on their input
    private void RotatePlayer()
    {
        if (mAnimator == null) return;

        if (mFollowCameraForward)
        {
            RotateTowardsCameraForward();
        }
        else
        {
            transform.Rotate(0.0f, hInput * mRotationSpeed * Time.deltaTime, 0.0f);
        }
    }

    //Rotates player towards camera forward
    private void RotateTowardsCameraForward()
    {
        Vector3 eu = Camera.main.transform.rotation.eulerAngles;
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.Euler(0.0f, eu.y, 0.0f),
            mTurnRate * Time.deltaTime);
    }

    //Applies movement to player
    private void Movement()
    {
        Vector3 forward = transform.TransformDirection(Vector3.forward).normalized;
        forward.y = 0.0f;

        mCharacterController.Move(forward * vInput * speed * Time.deltaTime);
    }

    //Handles the animations for movement
    private void UpdateAnimator()
    {
        mAnimator.SetFloat("PosX", 0);
        mAnimator.SetFloat("PosZ", vInput * speed / (2.0f * mWalkSpeed));
    }

    //Everything else remains the same
    private void Jump()
    {
        mAnimator.SetTrigger("Jump");
        mVelocity.y += Mathf.Sqrt(mJumpHeight * -2f * mGravity);
    }

    private Vector3 halfHeight;
    private Vector3 tempHeight;

    private void Crouch()
    {
        mAnimator.SetBool("Crouch", crouch);

        if (crouch)
        {
            tempHeight = CameraConstants.CameraPositionOffset;
            halfHeight = tempHeight * 0.5f;
            CameraConstants.CameraPositionOffset = halfHeight;
        }
        else
        {
            CameraConstants.CameraPositionOffset = tempHeight;
        }
    }

    private void ApplyGravity()
    {
        mVelocity.x = 0.0f;
        mVelocity.z = 0.0f;

        mVelocity.y += mGravity * Time.deltaTime;
        mCharacterController.Move(mVelocity * Time.deltaTime);

        if (mCharacterController.isGrounded && mVelocity.y < 0)
            mVelocity.y = 0f;
    }

}
