namespace RYZECHo;

public sealed partial class GameModel
{
    private static WeaponType[] PrimaryWeaponSelectionOrder()
    {
        return new[]
        {
            WeaponType.Blitz,
            WeaponType.Monster,
            WeaponType.Melt,
            WeaponType.Fairy,
            WeaponType.Giant,
            WeaponType.Juggernaut,
            WeaponType.Violet,
            WeaponType.Changer,
            WeaponType.Howl,
        };
    }

    private static WeaponType[] SidearmSelectionOrder()
    {
        return new[]
        {
            WeaponType.Pulse,
            WeaponType.Shard,
        };
    }

    private static bool IsSidearmWeapon(WeaponType weaponType)
    {
        return weaponType is WeaponType.Pulse or WeaponType.Shard;
    }

    private bool IsPrimaryWeapon(WeaponType weaponType)
    {
        return !IsSidearmWeapon(weaponType) && _weaponStats.ContainsKey(weaponType);
    }

    private WeaponType SelectedLoadoutWeapon()
    {
        return _selectedLoadoutFocus == LoadoutFocus.Primary ? _selectedWeapon : _selectedSidearmWeapon;
    }

    private void ToggleLoadoutFocus()
    {
        _selectedLoadoutFocus = _selectedLoadoutFocus == LoadoutFocus.Primary ? LoadoutFocus.Sidearm : LoadoutFocus.Primary;
    }

    private void CycleLoadoutWeapon(int direction)
    {
        var order = _selectedLoadoutFocus == LoadoutFocus.Primary ? PrimaryWeaponSelectionOrder() : SidearmSelectionOrder();
        var current = SelectedLoadoutWeapon();
        var index = Array.IndexOf(order, current);
        if (index < 0)
        {
            index = 0;
        }

        var next = order[(index + direction + order.Length) % order.Length];
        if (_selectedLoadoutFocus == LoadoutFocus.Primary)
        {
            _selectedWeapon = next;
        }
        else
        {
            _selectedSidearmWeapon = next;
        }
    }
}
