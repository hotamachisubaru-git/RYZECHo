namespace RYZECHo;

internal sealed partial class GameModel
{
    private void TryPlaceStructure(Point location)
    {
        if (!TryGetWorldPointFromScreen(location, out _))
        {
            return;
        }

        var cell = ScreenToCell(location);
        var candidate = CreateStructure(_selectedBuildTool, cell);
        var placementRule = ValidateStructurePlacement(candidate);
        if (placementRule is not null)
        {
            SetResultMessage(placementRule);
            return;
        }

        var apTransaction = MapEditApRules.TrySpend(_buildPoints, candidate.APCost, MaxBuildPoints);
        if (!apTransaction.Accepted)
        {
            SetResultMessage(apTransaction.Reason ?? "AP が不足しています。");
            return;
        }

        _buildPoints = apTransaction.AfterAp;
        _structures.Add(candidate);
        SetResultMessage($"{candidate.Label} を {cell.X},{cell.Y} に設置。AP {apTransaction.BeforeAp}->{apTransaction.AfterAp}。");
        // T005-3: 構造物設置イベントの Emit
        GameEventBus.Emit(new StructureBuiltEvent(candidate.Kind, cell, CellCenter(cell), "Player"));
    }

    private void TryRemoveStructure(Point location)
    {
        if (!TryGetWorldPointFromScreen(location, out _))
        {
            return;
        }

        var cell = ScreenToCell(location);
        var structure = _structures.FirstOrDefault(candidate => candidate.Cell == cell);
        if (structure is null)
        {
            return;
        }

        var apTransaction = MapEditApRules.RefundForRemoval(_buildPoints, structure, MaxBuildPoints);
        _buildPoints = apTransaction.AfterAp;
        _structures.Remove(structure);
        SetResultMessage(apTransaction.DeltaAp > 0
            ? $"{structure.Label} を撤去。AP +{apTransaction.DeltaAp} ({apTransaction.BeforeAp}->{apTransaction.AfterAp})。"
            : $"{structure.Label} を撤去。{apTransaction.Reason}");
    }
}
