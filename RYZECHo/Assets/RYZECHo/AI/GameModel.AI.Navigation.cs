namespace RYZECHo;

public sealed partial class GameModel
{
    private Structure? GetBlockingStructure(Actor actor, float checkRange)
    {
        if (actor.Path.Count == 0)
        {
            return null;
        }

        var nextTarget = actor.Path.Peek();
        return _structures.FirstOrDefault(structure =>
            StructureCatalog.Get(structure.Kind).BlocksMovement &&
            structure.Health > 0 &&
            Distance(actor.Position, CellCenter(structure.Cell)) < checkRange &&
            WorldToCell(nextTarget) == structure.Cell);
    }
}
