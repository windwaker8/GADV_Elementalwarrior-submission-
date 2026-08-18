using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
//Done

/// High-level mode the game is currently in; determines which subsystem receives per-frame updates.
public enum GameState { FreeRoam, Dialog, Battle }


public class GameManager : MonoBehaviour
{
    //Singleton instance set in awake
    public static GameManager Instance { get; private set; }

    [SerializeField] PlayerMovement playerMovement;
    [SerializeField] BattleSystem battleSystem;
    [SerializeField] Camera worldCamera;

   
    //exposes the player's battle stats from PlayerMovement.CharacterStat 
    //so files like Healer can read/modify it without referencing it
    
    public CharacterStat PlayerStat => playerMovement != null ? playerMovement.CharacterStat : null;

    private GameState state;

    
    //stored here because the parameter itself goes out of scope the moment StartFixedBattle returns
    private System.Action currentBattleOverCallback;

    private void Awake()
    {
        Instance = this;
    }

    //all cross system listeners are wired up here
    private void Start()
    {
        // Dialogue listeners.
        DialogManager.Instance.OnShowDialog += () =>
        {
            state = GameState.Dialog;
        };

        DialogManager.Instance.OnHideDialog += () =>
        {
            if (state == GameState.Dialog)
            {
                state = GameState.FreeRoam;
            }
        };

        // Wild encounter listener
        playerMovement.OnEncountered += StartBattle;

        // End battle listener.
        battleSystem.OnBattleOver += EndBattle;
    }

    
    private void Update()
    {   
        //no movement if state != freeroam, freezing the position of the player
        if (state == GameState.FreeRoam)
        {
            playerMovement.HandleUpdate();
        }
        else if (state == GameState.Dialog)
        {
            DialogManager.Instance.HandleUpdate();
        }
    }

    
    //switches the state to Battle, swaps the camera from world camera to battle camera
    //player stats gets moved to BattleSystem.
    public void StartBattle()
    {
        state = GameState.Battle;
        battleSystem.gameObject.SetActive(true);
        if (worldCamera != null) worldCamera.gameObject.SetActive(false);

        battleSystem.StartBattle(playerMovement.CharacterStat);
    }

    //starts a fixed battle with the specificEnemy, eg a boss.
    public void StartFixedBattle(Character specificEnemy, System.Action onBattleOverCallback = null)
    {
        state = GameState.Battle;
        battleSystem.gameObject.SetActive(true);
        if (worldCamera != null) worldCamera.gameObject.SetActive(false);

        currentBattleOverCallback = onBattleOverCallback;
        battleSystem.StartFixedBattle(playerMovement.CharacterStat, specificEnemy);
    }

    

    //once a battle ends that is not a loss, return back to overworld via freeroam 
    private void EndBattle(bool won)
    {
        state = GameState.FreeRoam;
        battleSystem.gameObject.SetActive(false);
        if (worldCamera != null) worldCamera.gameObject.SetActive(true);

        currentBattleOverCallback?.Invoke();
        currentBattleOverCallback = null;
    }
}