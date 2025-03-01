using System.Linq;
using System.Numerics;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared._Impstation.Cosmiccult;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.CosmicCult.Prototypes;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;

namespace Content.Client._Impstation.CosmicCult.UI.Monument;
[GenerateTypedNameReferences]
public sealed partial class MonumentMenu : FancyWindow
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private readonly SpriteSystem _sprite;
    private readonly ISawmill _sawmill;

    // All glyph prototypes
    private readonly IEnumerable<GlyphPrototype> _glyphPrototypes;
    // All influence prototypes
    private readonly IEnumerable<InfluencePrototype> _influencePrototypes;
    private readonly ButtonGroup _glyphButtonGroup;
    private ProtoId<GlyphPrototype> _selectedGlyphProtoId = string.Empty;
    private HashSet<ProtoId<InfluencePrototype>> _unlockedInfluenceProtoIds = [];
    private HashSet<ProtoId<GlyphPrototype>> _unlockedGlyphProtoIds = [];
    public Action<ProtoId<GlyphPrototype>>? OnSelectGlyphButtonPressed;
    public Action? OnRemoveGlyphButtonPressed;

    public Action<ProtoId<InfluencePrototype>>? OnGainButtonPressed;

    public MonumentMenu()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        _sprite = _entityManager.System<SpriteSystem>();
        _sawmill = _logManager.GetSawmill("monmunentooey");

        // Set the protos. These do not dynamically update so we can just store them right off the bat.
        // If an admin adds a new one in the middle of the round. Too bad*!
        // * You could probably just do this in UpdateState if that is necessary
        _glyphPrototypes = _prototypeManager.EnumeratePrototypes<GlyphPrototype>();
        _influencePrototypes = _prototypeManager.EnumeratePrototypes<InfluencePrototype>();

        _glyphButtonGroup = new ButtonGroup();

        RemoveGlyphButton.OnPressed += _ => OnRemoveGlyphButtonPressed?.Invoke();
        SelectGlyphButton.OnPressed += _ => OnSelectGlyphButtonPressed?.Invoke(_selectedGlyphProtoId);
    }

    public void UpdateState(MonumentBuiState state)
    {
        _selectedGlyphProtoId = state.SelectedGlyph;
        _unlockedInfluenceProtoIds = state.UnlockedInfluences;
        _unlockedGlyphProtoIds = state.UnlockedGlyphs;

        CultProgressBar.BackgroundStyleBoxOverride = new StyleBoxFlat { BackgroundColor = new Color(15, 17, 30) };
        CultProgressBar.ForegroundStyleBoxOverride = new StyleBoxFlat { BackgroundColor = new Color(91, 62, 124) };

        SelectGlyphButton.StyleClasses.Add("ButtonColorPurpleAndCool");
        RemoveGlyphButton.StyleClasses.Add("ButtonColorPurpleAndCool");

        UpdateBar(state);
        UpdateEntropy(state);
        UpdateGlyphs();
        UpdateInfluences(state);
    }

    // Update all the entropy fields
    private void UpdateBar(MonumentBuiState state)
    {
        CultProgressBar.Value = state.PercentageComplete;
        ProgressBarPercentage.Text = Loc.GetString("monument-interface-progress-bar", ("percentage", state.PercentageComplete.ToString("0")));
    }

    // Update all the entropy fields
    private void UpdateEntropy(MonumentBuiState state)
    {
        AvailableEntropy.Text = Loc.GetString("monument-interface-entropy-value", ("infused", state.AvailableEntropy.ToString()));
        EntropyUntilNextStage.Text = Loc.GetString("monument-interface-entropy-value", ("infused", state.EntropyUntilNextStage.ToString()));
        CrewToConvertUntilNextStage.Text = state.CrewToConvertUntilNextStage.ToString();
    }

    // Update all the glyph buttons
    private void UpdateGlyphs()
    {
        var glyphs = _glyphPrototypes.ToList();
        glyphs.Sort((x, y) =>
            string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase));

        GlyphContainer.RemoveAllChildren();
        foreach (var glyph in glyphs)
        {
            var boxContainer = new BoxContainer();
            var unlocked = _unlockedGlyphProtoIds.Contains(glyph.ID);
            var button = new Button
            {
                HorizontalExpand = true,
                StyleClasses = { StyleBase.ButtonSquare },
                ToolTip = Loc.GetString(glyph.Tooltip),
                Group = _glyphButtonGroup,
                Pressed = glyph.ID == _selectedGlyphProtoId,
                Disabled = !unlocked,
                Modulate = !unlocked ? Color.Gray : Color.White,
            };
            button.OnPressed += _ => _selectedGlyphProtoId = glyph.ID;
            var glyphIcon = new TextureRect
            {
                Texture = _sprite.Frame0(glyph.Icon),
                TextureScale = new Vector2(2f, 2f),
                Stretch = TextureRect.StretchMode.KeepCentered,
            };
            boxContainer.AddChild(button);
            button.AddChild(glyphIcon);
            GlyphContainer.AddChild(boxContainer);
        }
    }

    // Update all the influence thingies
    private void UpdateInfluences(MonumentBuiState state)
    {
        var influences = _influencePrototypes
            .OrderByDescending(influence => _unlockedInfluenceProtoIds.Contains(influence.ID))
            .ThenBy<InfluencePrototype, string>(influence => influence.Name);

        InfluencesContainer.RemoveAllChildren();
        foreach (var influence in influences)
        {
            var UIBoxState = GetUIBoxStateForInfluence(influence, state);
            var influenceBox = new InfluenceUIBox(influence, UIBoxState, state);
            influenceBox.OnGainButtonPressed += () => OnGainButtonPressed?.Invoke(influence.ID);
            InfluencesContainer.AddChild(influenceBox);
        }
    }

    private InfluenceUIBox.InfluenceUIBoxState GetUIBoxStateForInfluence(InfluencePrototype influence, MonumentBuiState state)
    {
        var unlocked = _unlockedInfluenceProtoIds.Contains(influence.ID);

        if (!_entityManager.TryGetComponent<CosmicCultComponent>(_playerManager.LocalEntity, out var cultComp)) //this feels wrong but seems to be the correct way to do this?
            return InfluenceUIBox.InfluenceUIBoxState.Locked; //early return with locked if there's somehow no cult comp

        var owned = cultComp.OwnedInfluences.Any(ownedProtoId => ownedProtoId.Id.Equals(influence.ID));

        //more verbose than it needs to be, but it reads nicer
        if (owned)
        {
            return InfluenceUIBox.InfluenceUIBoxState.Owned;
        }

        if (unlocked)
        {
            //if it's unlocked, do we have enough entropy to buy it?
            return influence.Cost > state.AvailableEntropy ? InfluenceUIBox.InfluenceUIBoxState.UnlockedAndNotEnoughEntropy : InfluenceUIBox.InfluenceUIBoxState.UnlockedAndEnoughEntropy;
        }

        return InfluenceUIBox.InfluenceUIBoxState.Locked;
    }
}
