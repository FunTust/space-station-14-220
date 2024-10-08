using System.Linq;
using Content.Server.Body.Systems;
using Content.Shared.Alert;
using Content.Shared.Body.Part;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Throwing;
using Content.Shared.Verbs;

namespace Content.Server.Ensnaring;

public sealed partial class EnsnareableSystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;

    public void InitializeEnsnaring()
    {
        SubscribeLocalEvent<EnsnaringComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<EnsnaringComponent, StepTriggerAttemptEvent>(AttemptStepTrigger);
        SubscribeLocalEvent<EnsnaringComponent, StepTriggeredOffEvent>(OnStepTrigger);
        SubscribeLocalEvent<EnsnaringComponent, ThrowDoHitEvent>(OnThrowHit);
        SubscribeLocalEvent<EnsnaringComponent, AttemptPacifiedThrowEvent>(OnAttemptPacifiedThrow);
        SubscribeLocalEvent<EnsnareableComponent, GetVerbsEvent<Verb>>(GetVerb); //ss220 Ensnareable
        SubscribeLocalEvent<EnsnareableComponent, RemoveEnsnareAlertEvent>(OnRemoveEnsnareAlert);
    }

    //ss220 Ensnareable begin
    private void GetVerb(Entity<EnsnareableComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Hands == null)
            return;

        if (!ent.Comp.IsEnsnared)
            return;

        var target = args.Target;
        var user = args.User;

        Verb verb = new Verb()
        {
            Text = Loc.GetString("ensnare-component-try-free-verb"),
            Act = () =>
            {
                foreach (var entity in ent.Comp.Container.ContainedEntities)
                {
                    if (!TryComp<EnsnaringComponent>(entity, out var ensnaring))
                        continue;

                    TryFree(target, user, entity, ensnaring);
                }
            },
        };
        args.Verbs.Add(verb);
    }
    //ss220 Ensnareable end

    private void OnAttemptPacifiedThrow(Entity<EnsnaringComponent> ent, ref AttemptPacifiedThrowEvent args)
    {
        args.Cancel("pacified-cannot-throw-snare");
    }

    private void OnRemoveEnsnareAlert(Entity<EnsnareableComponent> ent, ref RemoveEnsnareAlertEvent args)
    {
        if (args.Handled)
            return;

        foreach (var ensnare in ent.Comp.Container.ContainedEntities)
        {
            if (!TryComp<EnsnaringComponent>(ensnare, out var ensnaringComponent))
                return;

            TryFree(ent, ent, ensnare, ensnaringComponent);

            args.Handled = true;
            // Only one snare at a time.
            break;
        }
    }

    private void OnComponentRemove(EntityUid uid, EnsnaringComponent component, ComponentRemove args)
    {
        if (!TryComp<EnsnareableComponent>(component.Ensnared, out var ensnared))
            return;

        if (ensnared.IsEnsnared)
            ForceFree(uid, component);
    }

    private void AttemptStepTrigger(EntityUid uid, EnsnaringComponent component, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }

    private void OnStepTrigger(EntityUid uid, EnsnaringComponent component, ref StepTriggeredOffEvent args)
    {
        TryEnsnare(args.Tripper, uid, component);
    }

    private void OnThrowHit(EntityUid uid, EnsnaringComponent component, ThrowDoHitEvent args)
    {
        if (!component.CanThrowTrigger)
            return;

        TryEnsnare(args.Target, uid, component);
    }

    /// <summary>
    /// Used where you want to try to ensnare an entity with the <see cref="EnsnareableComponent"/>
    /// </summary>
    /// <param name="target">The entity that will be ensnared</param>
    /// <paramref name="ensnare"> The entity that is used to ensnare</param>
    /// <param name="component">The ensnaring component</param>
    public void TryEnsnare(EntityUid target, EntityUid ensnare, EnsnaringComponent component)
    {
        //Don't do anything if they don't have the ensnareable component.
        if (!TryComp<EnsnareableComponent>(target, out var ensnareable))
            return;

        var legs = _body.GetBodyChildrenOfType(target, BodyPartType.Leg).Count();
        var ensnaredLegs = (2 * ensnareable.Container.ContainedEntities.Count);
        var freeLegs = legs - ensnaredLegs;

        if (freeLegs <= 0)
            return;

        // Apply stamina damage to target if they weren't ensnared before.
        if (ensnareable.IsEnsnared != true)
        {
            if (TryComp<StaminaComponent>(target, out var stamina))
            {
                _stamina.TakeStaminaDamage(target, component.StaminaDamage, with: ensnare);
            }
        }

        component.Ensnared = target;
        _container.Insert(ensnare, ensnareable.Container);
        ensnareable.IsEnsnared = true;
        Dirty(target, ensnareable);

        UpdateAlert(target, ensnareable);
        var ev = new EnsnareEvent(component.WalkSpeed, component.SprintSpeed);
        RaiseLocalEvent(target, ev);
    }

    /// <summary>
    /// Used where you want to try to free an entity with the <see cref="EnsnareableComponent"/>
    /// </summary>
    /// <param name="target">The entity that will be freed</param>
    /// <param name="user">The entity that is freeing the target</param>
    /// <param name="ensnare">The entity used to ensnare</param>
    /// <param name="component">The ensnaring component</param>
    public void TryFree(EntityUid target, EntityUid user, EntityUid ensnare, EnsnaringComponent component)
    {
        // Don't do anything if they don't have the ensnareable component.
        if (!HasComp<EnsnareableComponent>(target))
            return;

        var freeTime = user == target ? component.BreakoutTime : component.FreeTime;
        var breakOnMove = !component.CanMoveBreakout;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, freeTime, new EnsnareableDoAfterEvent(), target, target: target, used: ensnare)
        {
            BreakOnMove = breakOnMove,
            BreakOnDamage = false,
            NeedHand = true,
            BreakOnDropItem = false,
        };

        if (!_doAfter.TryStartDoAfter(doAfterEventArgs))
            return;

        if (user == target)
            _popup.PopupEntity(Loc.GetString("ensnare-component-try-free", ("ensnare", ensnare)), target, target);
        else
            _popup.PopupEntity(Loc.GetString("ensnare-component-try-free-other", ("ensnare", ensnare), ("user", Identity.Entity(target, EntityManager))), user, user);
    }

    /// <summary>
    /// Used to force free someone for things like if the <see cref="EnsnaringComponent"/> is removed
    /// </summary>
    public void ForceFree(EntityUid ensnare, EnsnaringComponent component)
    {
        if (component.Ensnared == null)
            return;

        if (!TryComp<EnsnareableComponent>(component.Ensnared, out var ensnareable))
            return;

        var target = component.Ensnared.Value;

        _container.Remove(ensnare, ensnareable.Container, force: true);
        ensnareable.IsEnsnared = ensnareable.Container.ContainedEntities.Count > 0;
        Dirty(component.Ensnared.Value, ensnareable);
        component.Ensnared = null;

        UpdateAlert(target, ensnareable);
        var ev = new EnsnareRemoveEvent(component.WalkSpeed, component.SprintSpeed);
        RaiseLocalEvent(ensnare, ev);
    }

    /// <summary>
    /// Update the Ensnared alert for an entity.
    /// </summary>
    /// <param name="target">The entity that has been affected by a snare</param>
    public void UpdateAlert(EntityUid target, EnsnareableComponent component)
    {
        if (!component.IsEnsnared)
            _alerts.ClearAlert(target, component.EnsnaredAlert);
        else
            _alerts.ShowAlert(target, component.EnsnaredAlert);
    }
}
