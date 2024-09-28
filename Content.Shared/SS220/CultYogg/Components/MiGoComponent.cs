// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.SS220.CultYogg.EntitySystems;
using Robust.Shared.Audio;

namespace Content.Shared.SS220.CultYogg.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedMiGoSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
public sealed partial class MiGoComponent : Component
{
    /// ABILITIES ///
    [DataField]
    public EntProtoId MiGoEnslavementAction = "ActionMiGoEnslavement";

    [DataField]
    public EntProtoId MiGoHealAction = "ActionMiGoHeal";

    [DataField]
    public EntProtoId MiGoAstralAction = "ActionMiGoAstral";

    [DataField]
    public EntProtoId MiGoErectAction = "ActionMiGoErect";

    [DataField]
    public EntProtoId MiGoSacrificeAction = "ActionMiGoSacrifice";

    [DataField, AutoNetworkedField]
    public EntityUid? MiGoEnslavementActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? MiGoHealActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? MiGoAstralActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? MiGoErectActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? MiGoSacrificeActionEntity;

    /// <summary>
    ///Enlsavement variables
    /// <summary>
    public string RequiedEffect = "Rave";//Required effect for enslavement

    [DataField]
    public SoundSpecifier? EnslavingSound = new SoundPathSpecifier("/Audio/SS220/CultYogg/migo_slave.ogg");

    /// <summary>
    ///Erect variables
    /// <summary>
    public float HealingEffectTime = 15;//How long heal effect will occure

    /// <summary>
    ///Astral variables
    /// <summary>
    [ViewVariables, AutoNetworkedField]
    public bool PhysicalForm = true;//Is MiGo in phisycal form?

    [DataField]
    public SoundSpecifier? SoundMaterialize = new SoundPathSpecifier("/Audio/SS220/CultYogg/migo_astral_out.ogg");

    [DataField]
    public SoundSpecifier? SoundDeMaterialize = new SoundPathSpecifier("/Audio/SS220/CultYogg/migo_astral_in.ogg");

    public TimeSpan CooldownAfterMaterialize = TimeSpan.FromSeconds(3);

    /// How long reaper can stay dematerialized, depending on stage
    [ViewVariables, DataField, AutoNetworkedField]
    public List<TimeSpan> MaterializeDurations = new()
    {
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(20),
        TimeSpan.FromSeconds(40)
    };

    [ViewVariables]
    public TimeSpan? DeMaterializedStart;

    [ViewVariables, DataField, AutoNetworkedField]
    public float MaterialMovementSpeed = 6f; //ToDo check this thing

    [ViewVariables, DataField, AutoNetworkedField]
    public float UnMaterialMovementSpeed = 18f;//ToDo check this thing

    /// <summary>
    ///Erect variables
    /// <summary>
    [ViewVariables, DataField]
    public float ErectDoAfterSeconds = 3f;

    /// <summary>
    ///Replacement variables
    /// <summary>

    //Marking if entity can be gibbed and replaced
    public bool MayBeReplaced = false;

    //Should the timer count down the time
    public bool ShouldBeCounted = false;

    /// <summary>
    /// How long it takes to unlock another destination once one is taken.
    /// </summary>
    [DataField]
    public TimeSpan BeforeReplacementCooldown = TimeSpan.FromSeconds(300);

    /// <summary>
    /// Buffer to markup when time has come
    /// </summary>
    [DataField]
    public TimeSpan? ReplacementEventTime;
}

//Visual
[Serializable, NetSerializable]
public enum MiGoVisual
{
    Base
}