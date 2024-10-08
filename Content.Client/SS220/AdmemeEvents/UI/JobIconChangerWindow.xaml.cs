// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using System.Numerics;
using Content.Client.Stylesheets;
using Content.Shared.SS220.AdmemeEvents;
using Content.Shared.StatusIcon;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.AdmemeEvents.UI;

[GenerateTypedNameReferences]
public sealed partial class JobIconChangerWindow : DefaultWindow
{
    private static readonly string[] JobIconFilter = { "IOT", "NT", "USSP" };
    private static readonly int JobIconColumnCount = 5;
    private static readonly Vector2 BtnMaxSize = new Vector2(42, 28);
    private static readonly Vector2 TexScale = new Vector2(2.5f, 2.5f);

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;

    private readonly SpriteSystem _spriteSystem;
    private readonly JobIconChangerBoundUserInterface _bui;

    public JobIconChangerWindow(JobIconChangerBoundUserInterface bui)
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _spriteSystem = _entitySystem.GetEntitySystem<SpriteSystem>();
        _bui = bui;
    }

    public void OnUpdateState(EventRoleIconFilterGroup filter)
    {
        SetAllowedIcons(GetIconsList(filter));
    }

    public List<FactionIconPrototype> GetIconsList(EventRoleIconFilterGroup filter)
    {
        if (filter == EventRoleIconFilterGroup.None)
        {
            var allEventIcons = _prototypeManager.EnumeratePrototypes<FactionIconPrototype>()
                .Where(icon => JobIconFilter.Any(filter => icon.ID.StartsWith(filter)))
                .OrderBy(icon => icon.ID)
                .ToList();

            return allEventIcons;
        }

        var jobIconList = _prototypeManager.EnumeratePrototypes<FactionIconPrototype>()
            .Where(icon => icon.ID.StartsWith(filter.ToString()))
            .OrderBy(icon => icon.ID)
            .ToList();

        return jobIconList;
    }

    public void SetAllowedIcons(List<FactionIconPrototype> jobIconList)
    {
        IconGrid.DisposeAllChildren();

        var jobIconBtnGroup = new ButtonGroup();

        string styleBase;
        Button jobIconBtn;
        TextureRect jobIconTex;

        short i = 0;
        foreach (var jobIcon in jobIconList)
        {
            styleBase = GetStyleBase(i);

            jobIconBtn = new Button
            {
                Access = AccessLevel.Public,
                StyleClasses = { styleBase },
                MaxSize = BtnMaxSize,
                Group = jobIconBtnGroup,
            };

            jobIconTex = new TextureRect
            {
                Texture = _spriteSystem.Frame0(jobIcon.Icon),
                TextureScale = TexScale,
                Stretch = TextureRect.StretchMode.KeepCentered,
            };

            jobIconBtn.AddChild(jobIconTex);
            jobIconBtn.OnPressed += _ => _bui.OnJobIconChanged(jobIcon.ID);
            IconGrid.AddChild(jobIconBtn);

            i++;
        }
    }

    private string GetStyleBase(int counter)
    {
        var modulo = counter % JobIconColumnCount;
        return (modulo == 0) ? StyleBase.ButtonOpenRight :
            (modulo == JobIconColumnCount - 1) ? StyleBase.ButtonOpenLeft : StyleBase.ButtonOpenBoth;
    }
}
