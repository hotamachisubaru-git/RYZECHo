namespace RYZECHo;

internal sealed partial class GameModel
{
    private bool ActiveSelection(WeaponType weaponType)
    {
        return _phase switch
        {
            GamePhase.Bet => SelectedLoadoutWeapon() == weaponType,
            _ => _player.Weapon == weaponType,
        };
    }

    private WeaponType DisplayedWeaponType()
    {
        return _phase == GamePhase.Bet ? SelectedLoadoutWeapon() : _player.Weapon;
    }

    private string WeaponDisplayName(WeaponType weaponType)
    {
        return _weaponStats[weaponType].ShortLabel;
    }

    private string WeaponLoadoutLabel(WeaponType weaponType)
    {
        return _weaponStats[weaponType].Code;
    }

    private int CurrentMagazineAmmo()
    {
        return _weaponStats[DisplayedWeaponType()].MagazineAmmo;
    }

    private int CurrentReserveAmmo()
    {
        return _weaponStats[DisplayedWeaponType()].ReserveAmmo;
    }
}
