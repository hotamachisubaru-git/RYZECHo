using System;
using RYZECHo;

namespace RYZECHo.TacticalProto;

/// <summary>
/// タクティカルシューター用武器モデル（既存のWeaponStatsを参照）
/// MagazineAmmo、ReserveAmmo、Damage、FireCooldown、VisionRange
/// 弾数管理（発射→Magazine減少→リロード→Reserveから補充）
/// </summary>
public sealed class TacticalWeapon
{
    public required WeaponType Type { get; init; }
    public required string Label { get; init; }
    public required string ShortLabel { get; init; }

    public int MagazineAmmo { get; set; }
    public int MaxMagazineAmmo { get; set; }
    public int ReserveAmmo { get; set; }
    public int MaxReserveAmmo { get; set; }

    public required float Damage { get; init; }
    public required float FireCooldown { get; init; }
    public required float VisionRange { get; init; }
    public required float MoveSpeedModifier { get; init; }
    public required int Cost { get; init; }

    public float CurrentCooldown { get; set; }
    public bool IsReloading { get; private set; }
    public float ReloadTime { get; set; }
    private int _reloadStartMagazine;

    public TacticalWeapon()
    {
        CurrentCooldown = 0f;
        IsReloading = false;
        ReloadTime = 0f;
    }

    public void Initialize(int magAmmo, int reserveAmmo)
    {
        MagazineAmmo = magAmmo;
        MaxMagazineAmmo = magAmmo;
        ReserveAmmo = reserveAmmo;
        MaxReserveAmmo = reserveAmmo;
        CurrentCooldown = 0f;
        IsReloading = false;
        ReloadTime = 0f;
    }

    public bool TryFire()
    {
        if (IsReloading) return false;
        if (CurrentCooldown > 0f) return false;
        if (MagazineAmmo <= 0) return false;
        MagazineAmmo--;
        CurrentCooldown = FireCooldown;
        return true;
    }

    public void UpdateCooldown(float deltaSeconds)
    {
        if (CurrentCooldown > 0f) CurrentCooldown = Math.Max(0f, CurrentCooldown - deltaSeconds);
        if (IsReloading)
        {
            ReloadTime -= deltaSeconds;
            if (ReloadTime <= 0f) FinishReload();
        }
    }

    public bool StartReload()
    {
        if (IsReloading) return false;
        if (MagazineAmmo == MaxMagazineAmmo) return false;
        if (ReserveAmmo <= 0) return false;
        _reloadStartMagazine = MagazineAmmo;
        IsReloading = true;
        ReloadTime = Type switch
        {
            WeaponType.Blitz => 1.8f,
            WeaponType.Monster => 3.0f,
            WeaponType.Melt => 2.5f,
            WeaponType.Fairy => 2.0f,
            WeaponType.Giant => 2.5f,
            WeaponType.Juggernaut => 3.5f,
            WeaponType.Violet => 2.2f,
            WeaponType.Changer => 2.0f,
            WeaponType.Howl => 2.8f,
            WeaponType.Pulse => 1.5f,
            WeaponType.Shard => 2.0f,
            _ => 2.5f,
        };
        return true;
    }

    private void FinishReload()
    {
        IsReloading = false;
        int needed = MaxMagazineAmmo - _reloadStartMagazine;
        int toAdd = Math.Min(needed, ReserveAmmo);
        MagazineAmmo += toAdd;
        ReserveAmmo -= toAdd;
        ReloadTime = 0f;
    }

    public void RefillReserve()
    {
        ReserveAmmo = MaxReserveAmmo;
    }
}
