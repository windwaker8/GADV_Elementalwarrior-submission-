using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
//done

/// <summary>
/// Handles overworld grid-based player movement, animation, interaction with
/// nearby objects (via <see cref="Interface"/>), and random encounter checks
/// while walking through grass. Also owns the player's persistent
/// <see cref="CharacterStat"/>, created once from <see cref="playerBase"/>/<see cref="playerLevel"/>.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed;
    public bool isMoving;
    public Vector2 input;

    [SerializeField] Character playerBase;
    [SerializeField] int playerLevel = 1;

    /// <summary>
    /// The player's persistent battle stats, built once in <see cref="Awake"/> 
    ///and reused across battles.
    /// </summary>
    public CharacterStat CharacterStat { get; private set; }

    private Animator animator;

    public LayerMask solidObjects;
    public LayerMask Interactable;

    //uses the layer EncounterArea
    public LayerMask grassLayer; 

    /// <summary>
    ///Fired when a random encounter triggers, so GameManager (or similar) can start a battle.
    ///</summary>
    public event Action OnEncountered;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (CharacterStat == null)
        {
            // Initialize Jeff's persistent stats.
            CharacterStat = new CharacterStat(playerBase, playerLevel);
        }
    }

    /// <summary>
    /// Called every frame from GameManager when the game is in FreeRoam state,
    /// while the player is allowed to act. Reads movement input, kicks off a grid step
    /// if the destination is walkable, and checks for an interact key press.
    /// </summary>
    public void HandleUpdate()
    {
        if (!isMoving)
        {
            if (Keyboard.current.wKey.isPressed) input += (Vector2)transform.up;
            if (Keyboard.current.sKey.isPressed) input -= (Vector2)transform.up;
            if (Keyboard.current.aKey.isPressed) input -= (Vector2)transform.right;
            if (Keyboard.current.dKey.isPressed) input += (Vector2)transform.right;

            if (input != Vector2.zero)
            {
                animator.SetFloat("moveX", input.x);
                animator.SetFloat("moveY", input.y);

                var targetPos = transform.position;
                targetPos.x += input.x;
                targetPos.y += input.y;

                if (isWalkable(targetPos))
                {
                    StartCoroutine(Move(targetPos));
                }
                input = Vector2.zero;
            }

            if (Keyboard.current.jKey.wasPressedThisFrame)
            {
                Interact();
            }
        }
        animator.SetBool("isMoving", isMoving);
    }

    /// <summary>
    /// Checks one tile in front of the player (based on the last-faced direction stored
    /// in the animator's moveX/moveY floats) for anything on the Interactable layer,
    /// and calls its <see cref="Interface.Interact"/> if found.
    /// </summary>
    void Interact()
    {
        var facingDir = new Vector3(animator.GetFloat("moveX"), animator.GetFloat("moveY"));
        var interactPos = transform.position + facingDir;
        var collider = Physics2D.OverlapCircle(interactPos, 0.2f, Interactable);
        if (collider != null)
        {
            collider.GetComponent<Interface>()?.Interact();
        }
    }

    /// <summary>
    ///Smoothly slides the player from its current position to a single adjacent grid tile, 
    ///then checks for an encounter.
    ///</summary>
    IEnumerator Move(Vector3 targetPos)
    {
        isMoving = true;
        while ((targetPos - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;
        isMoving = false;

        CheckForEncounters();
    }

    /// <summary>
    /// A tile is walkable if nothing occupies it on either the solid-objects or interactable layers.
    /// </summary>
    private bool isWalkable(Vector3 targetPos)
    {
        return Physics2D.OverlapCircle(targetPos, 0.2f, solidObjects | Interactable) == null;
    }

    /// <summary>If standing in grass, rolls a 10% chance per step to trigger a random encounter.
    ///</summary>
    private void CheckForEncounters()
    {
        if (Physics2D.OverlapCircle(transform.position, 0.1f, grassLayer) != null)
        {
            if (UnityEngine.Random.Range(1, 101) <= 10)
            {
                animator.SetBool("isMoving", false);
                OnEncountered?.Invoke();
            }
        }
    }
}