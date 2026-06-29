namespace RYZECHo;

public sealed partial class GameModel
{
    public void CycleBuildTool()
    {
        _selectedBuildTool = _selectedBuildTool switch
        {
            BuildToolKind.BlastDoor => BuildToolKind.HoneyTrap,
            BuildToolKind.HoneyTrap => BuildToolKind.StaticNest,
            BuildToolKind.StaticNest => BuildToolKind.ReconBeacon,
            BuildToolKind.ReconBeacon => BuildToolKind.ShieldRelay,
            BuildToolKind.ShieldRelay => BuildToolKind.PortableCover,
            BuildToolKind.PortableCover => BuildToolKind.VisorWall,
            BuildToolKind.VisorWall => BuildToolKind.HoloDecoy,
            _ => BuildToolKind.BlastDoor,
        };
    }

    public void ToggleBriefing()
    {
        _showBriefing = !_showBriefing;
    }

    public void HandleLeftClick(Point location)
    {
        if (_phase == GamePhase.Construct)
        {
            TryPlaceStructure(location);
        }
    }

    public void HandleRightClick(Point location)
    {
        if (_phase == GamePhase.Construct)
        {
            TryRemoveStructure(location);
        }
    }
}
