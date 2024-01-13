using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PGGE.Patterns;

public class PlayerState_Multiplayer : FSMState
{
    protected Player_Multiplayer mPlayer = null;

    public PlayerState_Multiplayer(Player_Multiplayer player) 
        : base()
    {
        mPlayer = player;
        mFsm = mPlayer.mFsm;
    }

    public override void Enter()
    {
        base.Enter();
    }
    public override void Exit()
    {
        base.Exit();
    }
    public override void Update()
    {
        base.Update();
    }
    public override void FixedUpdate()
    {
        base.FixedUpdate();
    }
}

public class PlayerState_Multiplayer_MOVEMENT : PlayerState_Multiplayer
{
    public PlayerState_Multiplayer_MOVEMENT(Player_Multiplayer player) : base(player)
    {
        mId = (int)(PlayerStateType.MOVEMENT);
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Update()
    {
        base.Update();

        // For Student ---------------------------------------------------//
        // Implement the logic of player movement. 
        //----------------------------------------------------------------//
        // Hint:
        //----------------------------------------------------------------//
        // You should remember that the logic for movement
        // has already been implemented in PlayerMovement.cs.
        // So, how do we make use of that?
        // We certainly do not want to copy and paste the movement 
        // code from PlayerMovement to here.
        // Think of a way to call the Move method. 
        //
        // You should also
        // check if fire buttons are pressed so that 
        // you can transit to ATTACK state.

        mPlayer.Move();

        for (int i = 0; i < mPlayer.mAttackButtons.Length; ++i)
        {
            if (mPlayer.mAttackButtons[i])
            {
                if (mPlayer.mBulletsInMagazine > 0)
                {
                    PlayerState_Multiplayer_ATTACK attack =
                  (PlayerState_Multiplayer_ATTACK)mFsm.GetState(
                            (int)PlayerStateType.ATTACK);

                    attack.AttackID = i;
                    mPlayer.mFsm.SetCurrentState(
                        (int)PlayerStateType.ATTACK);
                }
                else
                {
                    Debug.Log("No more ammo left");
                }
            }
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
    }
}

public class PlayerState_Multiplayer_ATTACK : PlayerState_Multiplayer
{
    private int mAttackID = 0;
    private string mAttackName;

    public int AttackID
    {
        get
        {
            return mAttackID;
        }
        set
        {
            mAttackID = value;
            mAttackName = "Attack" + (mAttackID + 1).ToString();
        }
    }

    public PlayerState_Multiplayer_ATTACK(Player_Multiplayer player) : base(player)
    {
        mId = (int)(PlayerStateType.ATTACK);
    }

    public override void Enter()
    {
        mPlayer.mAnimator.SetBool(mAttackName, true);
    }
    public override void Exit()
    {
        mPlayer.mAnimator.SetBool(mAttackName, false);
    }
    public override void Update()
    {
        base.Update();

        // For Student ---------------------------------------------------//
        // Implement the logic of attack, reload and revert to movement. 
        //----------------------------------------------------------------//
        // Hint:
        //----------------------------------------------------------------//
        // 1. Transition to RELOAD
        // Notice that we have three variables, viz., 
        // mAmunitionCount
        // mBulletsInMagazine
        // mMaxAmunitionBeforeReload
        // You will need to make use of these variables while
        // implementing the transition to RELOAD.
        //
        // 2. Staying in ATTACK state
        // You should stay in ATTACK state as long as the 
        // Fire buttons are pressed. During ATTACK state
        // you should trigger the correct ATTACK animation
        // based on which button is pressed and shoot bullets.
        // Every bullet shot should reduce the count of mAmunitionCount
        // and mBulletsInMagazine.
        // Once mBulletsInMagazine reaches to 0 you should 
        // transit to RELOAD state.
        //
        // 3. Transition to MOVEMENT state
        // You should transit to MOVEMENT state when any of the 
        // following two situations happen.
        // First you have exhausted all your bullets, that means your
        // mAmunitionCount is 0 or if you do not press any of the
        // Fire buttons.
        // Discuss with your tutor if you find any difficulties
        // in implementing this section.        
        
        // For tutor - start ---------------------------------------------//
        Debug.Log("Ammo count: " + mPlayer.mAmunitionCount + ", In Magazine: " + mPlayer.mBulletsInMagazine);
        if (mPlayer.mBulletsInMagazine == 0 && mPlayer.mAmunitionCount > 0)
        {
            mPlayer.mFsm.SetCurrentState((int)PlayerStateType.RELOAD);
            return;
        }

        if (mPlayer.mAmunitionCount <= 0 && mPlayer.mBulletsInMagazine <= 0)
        {
            mPlayer.mFsm.SetCurrentState((int)PlayerStateType.MOVEMENT);
            mPlayer.NoAmmo();
            return;
        }

        if (mPlayer.mAttackButtons[mAttackID])
        {
            mPlayer.mAnimator.SetBool(mAttackName, true);
            mPlayer.Fire(AttackID);
        }
        else
        {
            mPlayer.mAnimator.SetBool(mAttackName, false);
            mPlayer.mFsm.SetCurrentState((int)PlayerStateType.MOVEMENT);
        }
        // For tutor - end   ---------------------------------------------//
    }
}

public class PlayerState_Multiplayer_RELOAD : PlayerState_Multiplayer
{
    public float ReloadTime = 3.0f;
    float dt = 0.0f;
    public int previousState;
    public PlayerState_Multiplayer_RELOAD(Player_Multiplayer player) : base(player)
    {
        mId = (int)(PlayerStateType.RELOAD);
    }

    public override void Enter()
    {
        mPlayer.mAnimator.SetTrigger("Reload");
        mPlayer.Reload();
        dt = 0.0f;
    }
    public override void Exit()
    {
        if (mPlayer.mAmunitionCount > mPlayer.mMaxAmunitionBeforeReload)
        {
            mPlayer.mBulletsInMagazine += mPlayer.mMaxAmunitionBeforeReload;
            mPlayer.mAmunitionCount -= mPlayer.mBulletsInMagazine;
        }
        else if (mPlayer.mAmunitionCount > 0 && mPlayer.mAmunitionCount < mPlayer.mMaxAmunitionBeforeReload)
        {
            mPlayer.mBulletsInMagazine += mPlayer.mAmunitionCount;
            mPlayer.mAmunitionCount = 0;
        }
    }

    public override void Update()
    {
        dt += Time.deltaTime;
        if (dt >= ReloadTime)
        {
            mPlayer.mFsm.SetCurrentState((int)PlayerStateType.MOVEMENT);
        }
    }

    public override void FixedUpdate()
    {
    }
}
