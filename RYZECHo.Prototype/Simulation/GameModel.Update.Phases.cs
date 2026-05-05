namespace RYZECHo.Prototype;

internal sealed partial class GameModel
{
    public void UpdateConstructPhase(InputSnapshot input)
    {
        if (input.Press1)
        {
            _selectedBuildTool = BuildToolKind.BlastDoor;
        }
        else if (input.Press2)
        {
            _selectedBuildTool = BuildToolKind.HoneyTrap;
        }
        else if (input.Press3)
        {
            _selectedBuildTool = BuildToolKind.StaticNest;
        }
        else if (input.Press4)
        {
            _selectedBuildTool = BuildToolKind.ReconBeacon;
        }
        else if (input.Press5)
        {
            _selectedBuildTool = BuildToolKind.ShieldRelay;
        }

        if (input.PressQ)
        {
            CycleStructureSkin(-1);
        }
        else if (input.PressE)
        {
            CycleStructureSkin(1);
        }

        if (input.PressR)
        {
            CycleAdTheme();
        }

        if (input.Confirm)
        {
            _sideSwapConstructPending = false;
            BeginBetPhase();
        }
    }

    public void UpdateBetPhase(InputSnapshot input)
    {
        EnsureFriendlyEconomyState();

        if (input.Press1)
        {
            TrySelectBoss(RosterCatalog.PlayerName);
        }
        else if (input.Press2)
        {
            TrySelectBoss(RosterCatalog.NorthAnchorName);
        }
        else if (input.Press3)
        {
            TrySelectBoss(RosterCatalog.SouthAnchorName);
        }
        else if (input.Press4)
        {
            TrySelectBoss(RosterCatalog.CenterLinkName);
        }

        if (input.PressQ)
        {
            CycleLoadoutWeapon(-1);
        }
        else if (input.PressE)
        {
            CycleLoadoutWeapon(1);
        }

        if (input.PressR)
        {
            ToggleLoadoutFocus();
        }

        if (input.AdjustBetLeft)
        {
            AdjustSelectedInvestment(-25);
        }
        else if (input.AdjustBetRight)
        {
            AdjustSelectedInvestment(25);
        }

        SyncSelectedBetTotal();

        if (input.Confirm)
        {
            StartRound();
        }
    }

}
