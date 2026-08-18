using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// High-level phase the battle is currently in; drives which input handler Update() calls.
public enum BattleState { Start, ActionSelection, MoveSelection, TargetSelection, PerformMove, RunningTurn }

// The four actions available from the main action menu. Values map to dialog box selector indices.
public enum ActionType { Bash = 0, Moves = 1, Run = 2, Item = 3 }

// Core turn-based battle controller. Owns battle setup (wild encounters and fixed/boss fights),
// the action/move/target selection state machine, turn-order resolution, and battle-end handling
// (victory + EXP/level-up, defeat, escape).
public class BattleSystem : MonoBehaviour
{
    [SerializeField] BattleUnit player;
    [SerializeField] BattleHud playerHud;

    [Header("Enemy Slots")]
    [SerializeField] BattleUnit enemy1;
    [SerializeField] BattleUnit enemy2;

    [Header("Enemy Prefab Pool")]
    [SerializeField] List<Character> wildEnemies;

    [SerializeField] BattleDialogBox dialogBox;
    [SerializeField] MoveBase bashBase;

    BattleState state;
    ActionType currentAction = ActionType.Bash;
    int currentMove = 0;

    // Target selection state
    int currentTarget = 0;
    Move pendingMove = null;
    bool isBashSelected = false;
    private Coroutine targetDialogCoroutine;

    // Display names shown in dialog/target text (e.g. "Mosquito A", "Mosquito B") when two
    // enemies of the same type are present. Kept external to BattleUnit/Character so those
    // classes don't need to know about disambiguation.
    private Dictionary<BattleUnit, string> enemyDisplayNames = new Dictionary<BattleUnit, string>();

    // Reward tracking
    int accumulatedExp = 0;

    // When true, disables the Run action (used for boss/fixed battles).
    private bool isBossBattle = false;

    // Enemies currently alive and participating in this battle.
    public List<BattleUnit> ActiveEnemies { get; private set; } = new List<BattleUnit>();

    // Fired when the battle ends. Payload is true for a player win/escape-success, false otherwise.
    public event Action<bool> OnBattleOver;

    // Helper method to safely retrieve the target enemy's display name.
    // Defaults to Base.Name if not found in the dictionary.
    private string GetEnemyDisplayName(BattleUnit enemy)
    {
        if (enemy != null && enemyDisplayNames.TryGetValue(enemy, out string displayName))
        {
            return displayName;
        }
        return enemy != null && enemy.battleUnit != null ? enemy.battleUnit.Base.Name : "Enemy";
    }

    // Begins a standard wild encounter with 1–2 randomly chosen enemies from the pool.
    public void StartBattle(CharacterStat playerStat)
    {
        isBossBattle = false;
        StartCoroutine(SetUpBattle(playerStat));
    }

    // Begins a scripted battle against a single specific enemy (e.g. a boss). Disables Run.
    public void StartFixedBattle(CharacterStat playerStat, Character specificEnemy)
    {
        isBossBattle = true;
        StartCoroutine(SetUpFixedBattle(playerStat, specificEnemy));
    }

    // Sets up a fixed/boss battle: one player, one specific enemy, then hands off to action selection.
    private IEnumerator SetUpFixedBattle(CharacterStat playerStat, Character specificEnemy)
    {
        player.Setup(playerStat);

        accumulatedExp = 0;

        ActiveEnemies.Clear();
        enemyDisplayNames.Clear();
        enemy1.SetVisible(false);
        enemy2.SetVisible(false);

        enemy1.Setup(specificEnemy);
        enemy1.SetVisible(true);
        ActiveEnemies.Add(enemy1);
        enemyDisplayNames[enemy1] = specificEnemy.Name;

        playerHud.SetData(player.battleUnit);
        dialogBox.SetMoveNames(player.battleUnit.Moves);

        yield return dialogBox.TypeDialog($"You were challenged by {GetEnemyDisplayName(enemy1)}!");
        yield return new WaitForSeconds(0.4f);

        StartCoroutine(ChooseAction());
    }

    // Sets up a random wild encounter: rolls 1 or 2 enemies from wildEnemies,
    // disambiguates display names with "A"/"B" suffixes if both enemies share a name,
    // shows the encounter message, then hands off to action selection.
    public IEnumerator SetUpBattle(CharacterStat playerStat)
    {
        player.Setup(playerStat);

        accumulatedExp = 0;

        int enemyCount = UnityEngine.Random.Range(1, 3);
        ActiveEnemies.Clear();
        enemyDisplayNames.Clear();

        enemy1.SetVisible(false);
        enemy2.SetVisible(false);

        // Pick enemy base assets.
        List<Character> selectedBases = new List<Character>();
        selectedBases.Add(wildEnemies[UnityEngine.Random.Range(0, wildEnemies.Count)]);

        if (enemyCount == 2 && wildEnemies.Count > 0)
        {
            selectedBases.Add(wildEnemies[UnityEngine.Random.Range(0, wildEnemies.Count)]);
        }

        // Check if both enemies are the same character type.
        bool isDuplicate = selectedBases.Count > 1 && selectedBases[0].Name == selectedBases[1].Name;

        // Setup Enemy 1.
        enemy1.Setup(selectedBases[0]);
        enemy1.SetVisible(true);
        ActiveEnemies.Add(enemy1);
        enemyDisplayNames[enemy1] = isDuplicate ? $"{selectedBases[0].Name} A" : selectedBases[0].Name;

        string encounterMessage = $"You encountered a {enemyDisplayNames[enemy1]}.";

        // Setup Enemy 2 (if present).
        if (selectedBases.Count > 1)
        {
            enemy2.Setup(selectedBases[1]);
            enemy2.SetVisible(true);
            ActiveEnemies.Add(enemy2);
            enemyDisplayNames[enemy2] = isDuplicate ? $"{selectedBases[1].Name} B" : selectedBases[1].Name;

            encounterMessage = $"A {enemyDisplayNames[enemy1]} and a {enemyDisplayNames[enemy2]} appeared!";
        }

        playerHud.SetData(player.battleUnit);
        dialogBox.SetMoveNames(player.battleUnit.Moves);

        yield return dialogBox.TypeDialog(encounterMessage);
        yield return new WaitForSeconds(0.4f);

        StartCoroutine(ChooseAction());
    }

    // Entry point for each new round: checks for player defeat, prompts "What will X do?",
    // then opens the action selector.
    public IEnumerator ChooseAction()
    {
        if (playerHud.DisplayedHP <= 0)
        {
            StartCoroutine(HandlePlayerDefeated());
            yield break;
        }

        state = BattleState.PerformMove;
        dialogBox.EnableActionSelector(false);

        yield return dialogBox.TypeDialog($"What will {player.battleUnit.Base.Name} do?");

        state = BattleState.ActionSelection;
        dialogBox.EnableActionSelector(true);
    }

    // Shows the player-defeat message, then ends the game. Does not fire
    // OnBattleOver — a loss isn't a "battle over, resume overworld"
    // outcome, it's a full game-over.
    IEnumerator HandlePlayerDefeated()
    {
        state = BattleState.PerformMove;
        dialogBox.EnableActionSelector(false);
        yield return dialogBox.TypeDialog($"{player.battleUnit.Base.Name} got hurt and collapsed...");
        yield return new WaitForSeconds(1.5f);

        EndGame();
    }

    // Ends the play session on defeat: stops Play Mode in the Editor, or quits the
    // built executable in a real build. Mirrors the same pattern used by ExitStairs,
    // since Application.Quit has no effect in the Editor.
    private void EndGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Stops play mode in Unity Editor.
#else
        Application.Quit(); // Closes the built game executable.
#endif
    }

    // Handles the full victory sequence: syncs the HUD's animated HP back into the real stat,
    // shows the win message, awards accumulated EXP, handles level-up (and any move learned),
    // then closes the battle and fires OnBattleOver(true).
    IEnumerator HandleBattleVictory()
    {
        state = BattleState.PerformMove;

        int survivingHP = playerHud.FreezeAndSyncHP();
        player.battleUnit.HP = Mathf.Max(1, survivingHP);

        yield return dialogBox.TypeDialog("You won!");
        yield return new WaitForSeconds(1f);

        if (accumulatedExp > 0)
        {
            yield return dialogBox.TypeDialog($"{player.battleUnit.Base.Name} gained {accumulatedExp} EXP!");
            yield return new WaitForSeconds(0.5f);

            bool leveledUp = player.battleUnit.GainExp(accumulatedExp, out bool didLevelUp);
            if (didLevelUp)
            {
                yield return dialogBox.TypeDialog($"{player.battleUnit.Base.Name} grew to Level {player.battleUnit.Level}!");
                yield return new WaitForSeconds(0.5f);

                var newMove = player.battleUnit.GetMoveAtLevel();
                if (newMove != null)
                {
                    player.battleUnit.LearnMove(newMove);
                    yield return dialogBox.TypeDialog($"{player.battleUnit.Base.Name} learned {newMove.Base.Name}!");
                    dialogBox.SetMoveNames(player.battleUnit.Moves);
                }
                yield return new WaitForSeconds(1.2f);
            }
        }

        yield return new WaitForSeconds(1f);

        gameObject.SetActive(false);
        OnBattleOver?.Invoke(true);
    }

    // Switches from the action menu into the move-selection submenu.
    public void MoveSelection()
    {
        state = BattleState.MoveSelection;
        dialogBox.EnableActionSelector(false);
        dialogBox.EnableDialogText(false);
        dialogBox.EnableMoveSelector(true);

        dialogBox.SetMoveNames(player.battleUnit.Moves);
        dialogBox.EnableMoveSelector(true);
    }

    // Begins target selection for the chosen action. If only one enemy is active, skips the
    // selection UI entirely and executes the turn immediately against that enemy.
    // isBash: True if the pending action is a basic Bash rather than a Move.
    // move: The move to use, or null if this is a Bash.
    void StartTargetSelection(bool isBash, Move move = null)
    {
        dialogBox.EnableActionSelector(false);
        dialogBox.EnableMoveSelector(false);
        dialogBox.EnableDialogText(true);

        isBashSelected = isBash;
        pendingMove = move;
        currentTarget = Mathf.Clamp(currentTarget, 0, ActiveEnemies.Count - 1);

        if (ActiveEnemies.Count == 1)
        {
            StartCoroutine(ExecuteTurn(ActiveEnemies[0], move, isBash));
            return;
        }

        state = BattleState.TargetSelection;
        UpdateTargetDialog();
    }

    // Refreshes the "Target: X?" prompt for the currently highlighted enemy, cancelling any in-flight typing.
    void UpdateTargetDialog()
    {
        if (targetDialogCoroutine != null)
        {
            StopCoroutine(targetDialogCoroutine);
        }

        // Retrieves custom name from dictionary (e.g. Target: Slime A?).
        string targetName = GetEnemyDisplayName(ActiveEnemies[currentTarget]);
        targetDialogCoroutine = StartCoroutine(dialogBox.TypeDialog($"Target: {targetName}?"));
    }

    // Input handler for BattleState.TargetSelection. Left/right cycles the target,
    // J confirms and starts the turn, K/Escape cancels back to the action or move menu.
    public void HandleTargetSelection()
    {
        if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame ||
            Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            currentTarget = (currentTarget + 1) % ActiveEnemies.Count;
            UpdateTargetDialog();
        }

        if (Keyboard.current.jKey.wasPressedThisFrame)
        {
            if (targetDialogCoroutine != null) StopCoroutine(targetDialogCoroutine);
            BattleUnit selectedEnemy = ActiveEnemies[currentTarget];

            StartCoroutine(ExecuteTurn(selectedEnemy, pendingMove, isBashSelected));
        }
        else if (Keyboard.current.kKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (targetDialogCoroutine != null) StopCoroutine(targetDialogCoroutine);

            if (isBashSelected)
            {
                StartCoroutine(ChooseAction());
            }
            else
            {
                MoveSelection();
            }
        }
    }

    // Lightweight record used only to sort combatants into speed-based turn order for a single round.
    private struct TurnParticipant
    {
        public BattleUnit Unit;
        public int Speed;
        public bool IsPlayer;
    }

    // Resolves one full round: builds a speed-sorted turn order (player ties win over enemies),
    // then has each participant act in turn. Bails out early to victory/defeat handling if the
    // battle ends mid-round; otherwise loops back into ChooseAction.
    IEnumerator ExecuteTurn(BattleUnit playerTarget, Move playerMove, bool isBash)
    {
        state = BattleState.PerformMove;

        List<TurnParticipant> turnOrder = new List<TurnParticipant>
        {
            new TurnParticipant { Unit = player, Speed = player.battleUnit.speed, IsPlayer = true }
        };

        foreach (var enemy in ActiveEnemies)
        {
            turnOrder.Add(new TurnParticipant { Unit = enemy, Speed = enemy.battleUnit.speed, IsPlayer = false });
        }

        // Higher speed acts first; on a tie, the player goes first.
        turnOrder.Sort((a, b) =>
        {
            if (a.Speed != b.Speed) return b.Speed.CompareTo(a.Speed);
            return a.IsPlayer ? -1 : 1;
        });

        foreach (var participant in turnOrder)
        {
            if (participant.IsPlayer)
            {
                if (isBash)
                {
                    yield return PerformPlayerBash(playerTarget);
                }
                else
                {
                    yield return PlayerMove(playerTarget, playerMove);
                }

                if (ActiveEnemies.Count == 0)
                {
                    yield return HandleBattleVictory();
                    yield break;
                }
            }
            else
            {
                // Skip enemies that were defeated earlier this same round.
                if (!ActiveEnemies.Contains(participant.Unit)) continue;

                var enemyMove = participant.Unit.battleUnit.ChooseMove();
                if (enemyMove != null && participant.Unit.battleUnit.PP >= enemyMove.Pbase.Cost)
                {
                    yield return EnemyMove(participant.Unit, enemyMove);
                }

                if (playerHud.DisplayedHP <= 0)
                {
                    yield return HandlePlayerDefeated();
                    yield break;
                }
            }
        }

        StartCoroutine(ChooseAction());
    }

    // Executes the player's chosen move against a target: deducts PP, rolls accuracy,
    // applies damage, and removes the target from battle if defeated (banking its EXP yield).
    // Falls back to the first active enemy if the original target was defeated earlier this round.
    IEnumerator PlayerMove(BattleUnit targetEnemy, Move move)
    {
        state = BattleState.PerformMove;
        if (!ActiveEnemies.Contains(targetEnemy))
        {
            if (ActiveEnemies.Count == 0) yield break;
            targetEnemy = ActiveEnemies[0];
        }

        player.battleUnit.DeductPP(move.Pbase.Cost);
        playerHud.UpdatePP(player.battleUnit.PP);

        string enemyName = GetEnemyDisplayName(targetEnemy);

        yield return dialogBox.TypeDialog($"{player.battleUnit.Base.Name} used {move.Pbase.Name} on {enemyName}!");
        yield return new WaitForSeconds(0.4f);

        int randomRoll = UnityEngine.Random.Range(1, 101);
        if (randomRoll > move.Pbase.Accuracy)
        {
            yield return dialogBox.TypeDialog($"{player.battleUnit.Base.Name}'s attack missed!");
            yield return new WaitForSeconds(0.4f);
            yield break;
        }

        bool isDefeated = targetEnemy.battleUnit.TakeDamage(move, player.battleUnit, out int damageDealt, out bool isSuperEffective);

        if (isSuperEffective)
        {
            yield return dialogBox.TypeDialog("It's super effective!");
            yield return new WaitForSeconds(0.1f);
        }

        if (damageDealt > 0)
        {
            yield return dialogBox.TypeDialog($"{enemyName} took {damageDealt} damage!");
            yield return new WaitForSeconds(0.4f);
        }

        if (isDefeated)
        {
            accumulatedExp += targetEnemy.battleUnit.ExpYield;

            yield return dialogBox.TypeDialog($"{enemyName} became tame!");
            yield return new WaitForSeconds(0.4f);

            targetEnemy.SetVisible(false);
            ActiveEnemies.Remove(targetEnemy);
            enemyDisplayNames.Remove(targetEnemy);
        }
    }

    // Executes the player's basic Bash attack (no PP cost, uses bashBase) against a target.
    // Mirrors PlayerMove but skips the PP deduction and accuracy roll.
    IEnumerator PerformPlayerBash(BattleUnit targetEnemy)
    {
        state = BattleState.PerformMove;
        if (!ActiveEnemies.Contains(targetEnemy))
        {
            if (ActiveEnemies.Count == 0) yield break;
            targetEnemy = ActiveEnemies[0];
        }

        string enemyName = GetEnemyDisplayName(targetEnemy);

        yield return dialogBox.TypeDialog($"{player.battleUnit.Base.Name} attacked {enemyName}!");
        yield return new WaitForSeconds(0.4f);

        Move bashMove = new Move(bashBase);
        bool isDefeated = targetEnemy.battleUnit.TakeDamage(bashMove, player.battleUnit, out int damageDealt, out bool isSuperEffective);

        if (isSuperEffective)
        {
            yield return dialogBox.TypeDialog("It's super effective!");
            yield return new WaitForSeconds(0.4f);
        }

        if (damageDealt > 0)
        {
            yield return dialogBox.TypeDialog($"{enemyName} took {damageDealt} damage!");
            yield return new WaitForSeconds(0.4f);
        }

        if (isDefeated)
        {
            accumulatedExp += targetEnemy.battleUnit.ExpYield;

            yield return dialogBox.TypeDialog($"{enemyName} became tame!");
            yield return new WaitForSeconds(0.4f);

            targetEnemy.SetVisible(false);
            ActiveEnemies.Remove(targetEnemy);
            enemyDisplayNames.Remove(targetEnemy);
        }
    }

    // Executes a single enemy's move against the player: deducts its PP, rolls accuracy,
    // applies damage to the player, and updates the HUD. Player defeat is checked by the caller.
    IEnumerator EnemyMove(BattleUnit activeEnemy, Move move)
    {
        state = BattleState.PerformMove;
        activeEnemy.battleUnit.DeductPP(move.Pbase.Cost);

        string enemyName = GetEnemyDisplayName(activeEnemy);

        yield return dialogBox.TypeDialog($"{enemyName} used {move.Pbase.Name}!");
        yield return new WaitForSeconds(0.4f);

        int randomRoll = UnityEngine.Random.Range(1, 101);
        if (randomRoll > move.Pbase.Accuracy)
        {
            yield return dialogBox.TypeDialog($"{enemyName}'s attack missed!");
            yield return new WaitForSeconds(0.4f);
            yield break;
        }

        bool isDefeated = player.battleUnit.TakeDamage(move, activeEnemy.battleUnit, out int damageDealt, out bool _);

        if (damageDealt > 0)
        {
            yield return dialogBox.TypeDialog($"{player.battleUnit.Base.Name} took {damageDealt} damage!");
            playerHud.ApplyDamage(damageDealt);
        }

        yield return new WaitForSeconds(0.4f);
    }

    // Routes per-frame input to the correct handler based on the current battle state.
    private void Update()
    {
        if (state == BattleState.ActionSelection)
        {
            HandleActionSelection();
        }
        else if (state == BattleState.MoveSelection)
        {
            HandleMoveSelectionSelection();
        }
        else if (state == BattleState.TargetSelection)
        {
            HandleTargetSelection();
        }
    }

    // Input handler for BattleState.ActionSelection. WASD/arrows move a cursor
    // through the 2x2 Bash/Moves/Item/Run grid; J confirms the highlighted action.
    public void HandleActionSelection()
    {
        if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            if (currentAction == ActionType.Bash) currentAction = ActionType.Moves;
            else if (currentAction == ActionType.Item) currentAction = ActionType.Run;
        }
        else if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            if (currentAction == ActionType.Moves) currentAction = ActionType.Bash;
            else if (currentAction == ActionType.Run) currentAction = ActionType.Bash;
        }
        else if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            if (currentAction == ActionType.Bash) currentAction = ActionType.Run;
            else if (currentAction == ActionType.Moves) currentAction = ActionType.Run;
        }
        else if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            if (currentAction == ActionType.Item) currentAction = ActionType.Bash;
            else if (currentAction == ActionType.Run) currentAction = ActionType.Moves;
        }

        dialogBox.UpdateActionSelection((int)currentAction);

        if (Keyboard.current.jKey.wasPressedThisFrame)
        {
            if (currentAction == ActionType.Bash)
            {
                StartTargetSelection(isBash: true);
            }
            else if (currentAction == ActionType.Moves)
            {
                MoveSelection();
            }
            else if (currentAction == ActionType.Run)
            {
                StartCoroutine(TrytoEscape());
            }
        }
    }

    // Input handler for BattleState.MoveSelection. Moves are laid out as a
    // 2-column grid (even index = left column, odd = right column); WASD/arrows navigate it,
    // J confirms (blocked if insufficient PP), K/Escape backs out to the action menu.
    public void HandleMoveSelectionSelection()
    {
        int moveCount = player.battleUnit.Moves.Count;
        if (moveCount == 0) return;

        if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            if (currentMove % 2 == 0 && currentMove + 1 < moveCount)
                currentMove++;
        }
        else if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            if (currentMove % 2 == 1)
                currentMove--;
        }
        else if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            if (currentMove + 2 < moveCount)
                currentMove += 2;
        }
        else if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            if (currentMove - 2 >= 0)
                currentMove -= 2;
        }

        currentMove = Mathf.Clamp(currentMove, 0, moveCount - 1);
        dialogBox.UpdateMoveSelection(currentMove, player.battleUnit.Moves[currentMove]);

        if (Keyboard.current.jKey.wasPressedThisFrame)
        {
            var selectedMove = player.battleUnit.Moves[currentMove];

            if (player.battleUnit.PP < selectedMove.Pbase.Cost)
            {
                StartCoroutine(dialogBox.TypeDialog($"Not enough PP to use {selectedMove.Pbase.Name}!"));
                return;
            }

            StartTargetSelection(isBash: false, move: selectedMove);
        }
        else if (Keyboard.current.kKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            dialogBox.EnableMoveSelector(false);
            dialogBox.EnableDialogText(true);
            StartCoroutine(ChooseAction());
        }
    }

    // Handles the Run action: always fails in boss battles. Otherwise rolls a 50% chance to
    // escape; on success syncs HP and ends the battle (OnBattleOver(false)), on failure every
    // active enemy gets a free attack before returning control to the player.
    IEnumerator TrytoEscape()
    {
        state = BattleState.PerformMove;
        dialogBox.EnableActionSelector(false);

        if (isBossBattle)
        {
            yield return dialogBox.TypeDialog("You can't run from a boss battle!");
            yield return new WaitForSeconds(0.1f);
            StartCoroutine(ChooseAction());
            yield break;
        }

        int escapeChance = UnityEngine.Random.Range(1, 101);
        if (escapeChance <= 50)
        {
            int survivingHP = playerHud.FreezeAndSyncHP();
            player.battleUnit.HP = Mathf.Max(1, survivingHP);

            yield return dialogBox.TypeDialog("You successfully escaped!");
            yield return new WaitForSeconds(0.2f);

            gameObject.SetActive(false);
            OnBattleOver?.Invoke(false);
        }
        else
        {
            yield return dialogBox.TypeDialog("Escape failed!");
            yield return new WaitForSeconds(0.1f);

            // Iterate over a snapshot since enemies may be removed mid-loop (though none should
            // be defeated by a failed escape, this guards against the collection changing).
            foreach (var enemy in ActiveEnemies.ToArray())
            {
                if (!ActiveEnemies.Contains(enemy)) continue;

                var enemyMove = enemy.battleUnit.ChooseMove();
                if (enemyMove != null && enemy.battleUnit.PP >= enemyMove.Pbase.Cost)
                {
                    yield return EnemyMove(enemy, enemyMove);
                }

                if (playerHud.DisplayedHP <= 0)
                {
                    yield return HandlePlayerDefeated();
                    yield break;
                }
            }

            StartCoroutine(ChooseAction());
        }
    }
}