using System.Collections.Generic;
using UnityEngine;
using CTHeadless;
using TacticsWarMud.TypeDefinitions;
using Piece = CTCommon.Piece;

public interface IGameState
{
    void Enter();
    void Update();
    void Exit();
}

public class GameStateMachine : MonoBehaviour
{
    private IGameState currentState;

    public void SetState(IGameState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    private void Update()
    {
        currentState?.Update();
    }
}

public class SelectingState : IGameState
{
    private GameStateMachine stateMachine;
    private CTContext ctContext;
    private Piece selectedPiece;

    public SelectingState(GameStateMachine sm, CTContext context)
    {
        stateMachine = sm;
        ctContext = context;
    }

    public void Enter() { Debug.Log("Entering Selecting State"); }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            stateMachine.SetState(new MovingState(stateMachine, ctContext, selectedPiece));
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            stateMachine.SetState(new AttackingState(stateMachine, ctContext, selectedPiece));
        }
    }

    public void Exit() { Debug.Log("Exiting Selecting State"); }
}

public class MovingState : IGameState
{
    private GameStateMachine stateMachine;
    private CTContext ctContext;
    private Piece selectedPiece;
    private List<PositionData> path = new List<PositionData>();

    public MovingState(GameStateMachine sm, CTContext context, Piece piece)
    {
        stateMachine = sm;
        ctContext = context;
        selectedPiece = piece;
    }

    public void Enter() { Debug.Log("Entering Moving State"); }

    public async void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (await ctContext.Move(selectedPiece, path))
            {
                stateMachine.SetState(new SelectingState(stateMachine, ctContext));
            }
        }
    }

    public void Exit() { Debug.Log("Exiting Moving State"); }
}

public class AttackingState : IGameState
{
    private GameStateMachine stateMachine;
    private CTContext ctContext;
    private Piece selectedPiece;

    public AttackingState(GameStateMachine sm, CTContext context, Piece piece)
    {
        stateMachine = sm;
        ctContext = context;
        selectedPiece = piece;
    }

    public void Enter() { Debug.Log("Entering Attacking State"); }

    public async void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Piece targetPiece = ctContext.GetCellAtPosition(0, 0).piece;
            if (targetPiece != null && await ctContext.Attack(selectedPiece, targetPiece))
            {
                stateMachine.SetState(new SelectingState(stateMachine, ctContext));
            }
        }
    }

    public void Exit() { Debug.Log("Exiting Attacking State"); }
}

public class OpponentTurnState : IGameState
{
    private GameStateMachine stateMachine;

    public OpponentTurnState(GameStateMachine sm)
    {
        stateMachine = sm;
    }

    public void Enter() { Debug.Log("Opponent Turn"); }

    public void Update()
    {
        // Simulate opponent logic and switch back
        stateMachine.SetState(new SelectingState(stateMachine, null));
    }

    public void Exit() { Debug.Log("Exiting Opponent Turn"); }
}