# FSM

Namespace: `DataKeeper.FSM`

A generic finite state machine keyed by an enum, with an owner reference (`Self`), condition-driven transitions, per-transition cooldowns, and an editor-only transition history (viewable in `Tools > Windows > FSM Debugger`).

## Quick start

```csharp
public enum EnemyState { Idle, Chase, Attack }

public class Enemy : MonoBehaviour
{
    private StateMachine<EnemyState, Enemy> _fsm;

    private void Awake()
    {
        _fsm = new StateMachine<EnemyState, Enemy>(this);

        _fsm.AddState(EnemyState.Idle, new IdleState());
        _fsm.AddState(EnemyState.Chase, new ChaseState());
        _fsm.AddState(EnemyState.Attack, new AttackState());

        _fsm.AddTransition(EnemyState.Idle, EnemyState.Chase, () => CanSeePlayer)
            .OnTransition(() => Debug.Log("spotted!"));

        // Checked from every state, before regular transitions
        _fsm.AddAnyStateTransition(EnemyState.Idle, () => IsDead)
            .Cooldown(1f);

        _fsm.SetInitialState(EnemyState.Idle);
    }

    private void Update() => _fsm.OnUpdate();
    private void FixedUpdate() => _fsm.OnFixedUpdate();
    private void LateUpdate() => _fsm.OnLateUpdate();
}

public class IdleState : State<EnemyState, Enemy>
{
    public override void OnEnter() { /* Self is the Enemy instance */ }
    public override void OnUpdate() { }
    public override void OnExit() { }
}
```

## States

`State<TState, TSelfType>` exposes the machine (`StateMachine`) and its owner (`Self`), plus overridable lifecycle hooks:

| Hook | Called |
| --- | --- |
| `OnEnter` / `OnExit` | on state change |
| `OnUpdate` | from `StateMachine.OnUpdate()` when active |
| `OnLateUpdate` / `OnFixedUpdate` / `OnAnimatorMove` | forwarded from the matching machine methods |

## Transitions

- `AddTransition(from, to, condition)` — evaluated each `OnUpdate` while `from` is active.
- `AddAnyStateTransition(to, condition)` — evaluated each `OnUpdate` regardless of current state, **before** regular transitions.
- Both return the `Transition<TState>`, so you can chain:
  - `.OnTransition(Action)` — callback fired when the transition triggers.
  - `.Cooldown(seconds)` — minimum time between triggers of this transition.
- `ChangeState(state)` — force a state change directly (no condition needed).

## Observing

- `OnStateChanged` — a `Signal<TState>` invoked after each state change.
- `CurrentStateType` / `PreviousStateType` — serialized, inspectable at runtime.

## Debugging

In the editor the machine records recent transitions into `FSMHistory<TState>` (default 10 entries). Inspect them live via `Tools > Windows > FSM Debugger (Beta)` or `GetStateHistory()`.
