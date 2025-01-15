using Content.Shared._EinsteinEngines.Supermatter.Monitor;
using Content.Shared.Atmos;
using Content.Shared.DoAfter;
using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._EinsteinEngines.Supermatter.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SupermatterComponent : Component
{
    #region Base

    [DataField]
    public bool Activated = true;

    /// <summary>
    /// The current status of the singularity, used for alert sounds and the monitoring console
    /// </summary>
    [DataField]
    public SupermatterStatusType Status = SupermatterStatusType.Inactive;

    [DataField]
    public string SliverPrototype = "SupermatterSliver";

    [DataField]
    public string[] LightningPrototypes =
    {
        "SupermatterLightning",
        "SupermatterLightningCharged",
        "SupermatterLightningSupercharged"
    };

    [DataField]
    public string SingularitySpawnPrototype = "Singularity";

    [DataField]
    public string TeslaSpawnPrototype = "TeslaEnergyBall";

    [DataField]
    public string KudzuSpawnPrototype = "SupermatterKudzu";

    [DataField]
    public string AnomalyBluespaceSpawnPrototype = "AnomalyBluespace";

    [DataField]
    public string AnomalyGravitySpawnPrototype = "AnomalyGravity";

    [DataField]
    public string AnomalyPyroSpawnPrototype = "AnomalyPyroclastic";

    /// <summary>
    /// What spawns in the place of an unfortunate entity that got removed by the SM.
    /// </summary>
    [DataField]
    public string CollisionResultPrototype = "Ash";

    #endregion

    #region Sounds

    [DataField]
    public SoundSpecifier DustSound = new SoundPathSpecifier("/Audio/_EinsteinEngines/Supermatter/supermatter.ogg");

    [DataField]
    public SoundSpecifier DistortSound = new SoundPathSpecifier("/Audio/_EinsteinEngines/Supermatter/charge.ogg");

    [DataField]
    public SoundSpecifier CalmLoopSound = new SoundPathSpecifier("/Audio/_EinsteinEngines/Supermatter/calm.ogg");

    [DataField]
    public SoundSpecifier DelamLoopSound = new SoundPathSpecifier("/Audio/_EinsteinEngines/Supermatter/delamming.ogg");

    [DataField]
    public SoundSpecifier CurrentSoundLoop = new SoundPathSpecifier("/Audio/_EinsteinEngines/Supermatter/calm.ogg");

    [DataField]
    public SoundSpecifier CalmAccent = new SoundCollectionSpecifier("SupermatterAccentNormal");

    [DataField]
    public SoundSpecifier DelamAccent = new SoundCollectionSpecifier("SupermatterAccentDelam");

    [DataField]
    public string StatusWarningSound = "SupermatterWarning";

    [DataField]
    public string StatusDangerSound = "SupermatterDanger";

    [DataField]
    public string StatusEmergencySound = "SupermatterEmergency";

    [DataField]
    public string StatusDelamSound = "SupermatterDelaminating";

    [DataField]
    public string? StatusCurrentSound = null;

    #endregion

    #region Processing

    [DataField]
    public float Power;

    [DataField]
    public float Temperature;

    [DataField]
    public float WasteMultiplier;

    [DataField]
    public float MatterPower;

    [DataField]
    public float MatterPowerConversion = 10f;

    /// <summary>
    /// The portion of the gasmix we're on
    /// </summary>
    [DataField]
    public float GasEfficiency = 0.15f;

    /// <summary>
    /// Uses PowerlossDynamicScaling and GasStorage to lessen the effects of our powerloss functions
    /// </summary>
    [DataField]
    public float PowerlossInhibitor = 1;

    /// <summary>
    /// Based on CO2 percentage, this slowly moves between 0 and 1.
    /// We use it to calculate PowerlossInhibitor.
    /// </summary>
    [DataField]
    public float PowerlossDynamicScaling;

    /// <summary>
    /// Affects the amount of damage and minimum point at which the SM takes heat damage
    /// </summary>
    [DataField]
    public float DynamicHeatResistance = 1;

    /// <summary>
    /// Multiplier on damage the core takes from absorbing hot gas.
    /// </summary>
    [DataField]
    public float MoleHeatPenalty = 350f;

    /// <summary>
    /// Inverse of <see cref="MoleHeatPenalty" />
    /// </summary>
    [DataField]
    public float MoleHeatThreshold = 350f;

    /// <summary>
    /// 
    /// </summary>
    [DataField]
    public float ReactionPowerModifier = 0.55f;

    /// <summary>
    /// Acts as a multiplier on the amount that reactions increase the supermatter core temperature
    /// </summary>
    [DataField]
    public float ThermalReleaseModifier = 5f;

    /// <summary>
    /// Multiplier on how much plasma is released during supermatter reactions
    /// Default is ~1/750
    /// </summary>
    [DataField]
    public float PlasmaReleaseModifier = 0.001333f;

    /// <summary>
    /// Multiplier on how much oxygen is released during supermatter reactions.
    /// Default is ~1/325
    /// </summary>
    [DataField]
    public float OxygenReleaseEfficiencyModifier = 0.0031f;

    /// <summary>
    /// The chance for supermatter lightning to strike random coordinates instead of an entity
    /// </summary>
    [DataField]
    public float ZapHitCoordinatesChance = 0.75f;

    /// <summary>
    /// The lifetime of a supermatter-spawned anomaly.
    /// </summary>
    [DataField]
    public float AnomalyLifetime = 60f;

    /// <summary>
    /// The minimum distance from the supermatter that anomalies will spawn at
    /// </summary>
    [DataField]
    public float AnomalySpawnMinRange = 5f;

    /// <summary>
    /// The maximum distance from the supermatter that anomalies will spawn at
    /// </summary>
    [DataField]
    public float AnomalySpawnMaxRange = 10f;

    /// <summary>
    /// The chance for a bluespace anomaly to spawn when power or damage is high
    /// Default is ~1/150
    /// </summary>
    [DataField]
    public float AnomalyBluespaceChance = 0.006667f;

    /// <summary>
    /// The chance for a gravity anomaly to spawn when power or damage is high, and the severe power penalty threshold is exceeded
    /// Default is ~1/150
    /// </summary>
    [DataField]
    public float AnomalyGravityChanceSevere = 0.006667f;

    /// <summary>
    /// The chance for a gravity anomaly to spawn when power or damage is high
    /// Default is ~1/750
    /// </summary>
    [DataField]
    public float AnomalyGravityChance = 0.001333f;

    /// <summary>
    /// The chance for a pyroclastic anomaly to spawn when power or damage is high, and the severe power penalty threshold is exceeded
    /// Default is ~1/375
    /// </summary>
    [DataField]
    public float AnomalyPyroChanceSevere = 0.002667f;

    /// <summary>
    /// The chance for a gravity anomaly to spawn when power or damage is high, and the power penalty threshold is exceeded
    /// Default is ~1/2500
    /// </summary>
    [DataField]
    public float AnomalyPyroChance = 0.0004f;

    #endregion

    #region Timing

    /// <summary>
    /// We yell if over 50 damage every YellTimer Seconds
    /// </summary>
    [DataField]
    public TimeSpan YellTimer;

    /// <summary>
    /// Last time the supermatter's damage was announced
    /// </summary>
    [DataField]
    public TimeSpan YellLast;

    /// <summary>
    /// Time when the delamination will occur
    /// </summary>
    [DataField]
    public TimeSpan DelamEndTime;

    /// <summary>
    /// How long it takes in seconds for the supermatter to delaminate after reaching zero integrity
    /// </summary>
    [DataField]
    public float DelamTimer = 30f;

    /// <summary>
    /// Last time a supermatter accent sound was triggered
    /// </summary>
    [DataField]
    public TimeSpan AccentLastTime;

    /// <summary>
    /// Minimum time in seconds between supermatter accent sounds
    /// </summary>
    [DataField]
    public float AccentMinCooldown = 2f;

    [DataField]
    public TimeSpan ZapLast = TimeSpan.Zero;

    #endregion

    #region Thresholds

    /// <summary>
    /// Percentage of inhibitor gas needed before the charge inertia chain reaction effect starts.
    /// </summary>
    [DataField]
    public float PowerlossInhibitionGasThreshold = 0.20f;

    /// <summary>
    /// Moles of the gas needed before the charge inertia chain reaction effect starts.
    /// Scales powerloss inhibition down until this amount of moles is reached.
    /// </summary>
    [DataField]
    public float PowerlossInhibitionMoleThreshold = 20f;

    /// <summary>
    /// Bonus powerloss inhibition boost if this amount of moles is reached
    /// </summary>
    [DataField]
    public float PowerlossInhibitionMoleBoostThreshold = 500f;

    /// <summary>
    /// Above this value we can get a singulo and independent mol damage, below it we can heal damage
    /// </summary>
    [DataField]
    public float MolePenaltyThreshold = 1800f;

    /// <summary>
    /// More moles of gases are harder to heat than fewer, so let's scale heat damage around them
    /// </summary>
    [DataField]
    public float MoleHeatPenaltyThreshold;

    /// <summary>
    /// The cutoff on power properly doing damage, pulling shit around,
    /// and delamming into a tesla. Spawns anomalies, +2 bolts of electricity
    /// </summary>
    [DataField]
    public float PowerPenaltyThreshold = 5000f;

    /// <summary>
    /// Increased anomaly spawns, +1 bolt of electricity
    /// </summary>
    [DataField]
    public float SeverePowerPenaltyThreshold = 7000f;

    /// <summary>
    /// +1 bolt of electricity
    /// </summary>
    [DataField]
    public float CriticalPowerPenaltyThreshold = 9000f;

    /// <summary>
    /// Maximum safe operational temperature in degrees Celsius.
    /// Supermatter begins taking damage above this temperature.
    /// </summary>
    [DataField]
    public float HeatPenaltyThreshold = 40f;

    #endregion

    #region Damage

    /// <summary>
    /// The amount of damage taken
    /// </summary>
    [DataField]
    public float Damage = 0f;

    /// <summary>
    /// The damage from before this cycle.
    /// Used to limit the damage we can take each cycle, and for safe alert.
    /// </summary>
    [DataField]
    public float DamageArchived = 0f;

    /// <summary>
    /// Is multiplied by ExplosionPoint to cap evironmental damage per cycle
    /// </summary>
    [DataField]
    public float DamageHardcap = 0.002f;

    /// <summary>
    /// Environmental damage is scaled by this
    /// </summary>
    [DataField]
    public float DamageIncreaseMultiplier = 0.25f;

    /// <summary>
    /// Max space damage the SM will take per cycle
    /// </summary>
    [DataField]
    public float MaxSpaceExposureDamage = 2;

    /// <summary>
    /// The point at which we should start sending radio messages about the damage.
    /// </summary>
    [DataField]
    public float DamageWarningThreshold = 50;

    /// <summary>
    /// The point at which we start sending station announcements about the damage.
    /// </summary>
    [DataField]
    public float DamageEmergencyThreshold = 500;

    /// <summary>
    /// The point at which the SM begins shooting lightning.
    /// </summary>
    [DataField]
    public int DamagePenaltyPoint = 550;

    /// <summary>
    /// The point at which the SM begins delaminating.
    /// </summary>
    [DataField]
    public int DamageDelaminationPoint = 900;

    /// <summary>
    /// The point at which the SM begins showing warning signs.
    /// </summary>
    [DataField]
    public int DamageDelamAlertPoint = 300;

    [DataField]
    public bool Delamming = false;

    [DataField]
    public DelamType PreferredDelamType = DelamType.Explosion;

    #endregion

    #region Announcements

    [DataField]
    public bool DelamAnnounced = false;

    /// <summary>
    /// The radio channel for supermatter alerts
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> Channel = "Engineering";

    /// <summary>
    /// The common radio channel for severe supermatter alerts
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> ChannelGlobal = "Common";

    #endregion

    #region Gases

    /// <summary>
    ///     How much gas is in the SM
    /// </summary>
    [DataField]
    public Dictionary<Gas, float> GasStorage = new Dictionary<Gas, float>()
    {
        { Gas.Oxygen,        0f },
        { Gas.Nitrogen,      0f },
        { Gas.CarbonDioxide, 0f },
        { Gas.Plasma,        0f },
        { Gas.Tritium,       0f },
        { Gas.WaterVapor,    0f },
        { Gas.Frezon,        0f },
        { Gas.Ammonia,       0f },
        { Gas.NitrousOxide,  0f },
    };

    /// <summary>
    ///     Stores information about how every gas interacts with the SM
    /// </summary>
    //TODO: Replace this with serializable GasFact array something
    public readonly Dictionary<Gas, (float TransmitModifier, float HeatPenalty, float PowerMixRatio, float HeatResistance)> GasDataFields = new()
    {
        { Gas.Oxygen,        (1.5f, 1f,    1f,  1f) },
        { Gas.Nitrogen,      (0f,   -1.5f, -1f, 1f) },
        { Gas.CarbonDioxide, (0f,   0.1f,  1f,  1f) },
        { Gas.Plasma,        (4f,   15f,   1f,  1f) },
        { Gas.Tritium,       (30f,  10f,   1f,  1f) },
        { Gas.WaterVapor,    (2f,   12f,   1f,  1f) },
        { Gas.Frezon,        (0f,   -10f,  -1f, 1f) },
        { Gas.Ammonia,       (0f,   .5f,   1f,  1f) },
        { Gas.NitrousOxide,  (0f,   -5f,   -1f, 6f) },
    };

    #endregion
}


public enum DelamType : int
{
    Explosion = 0,
    Singulo = 1,
    Tesla = 2,
    Cascade = 3
}

[Serializable, DataDefinition]
public sealed partial class GasFact
{
    [DataField]
    public float TransmitModifier;

    [DataField]
    public float HeatPenalty;

    [DataField]
    public float PowerMixRatio;

    public GasFact(float transmitModifier, float heatPenalty, float powerMixRatio)
    {
        TransmitModifier = transmitModifier;
        HeatPenalty = heatPenalty;
        PowerMixRatio = powerMixRatio;
    }
}

[Serializable, NetSerializable]
public sealed partial class SupermatterDoAfterEvent : SimpleDoAfterEvent
{
}
