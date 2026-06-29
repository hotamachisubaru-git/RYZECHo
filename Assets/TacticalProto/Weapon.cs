using System;

namespace RYZECHo.TacticalProto
{
    /// <summary>
    /// 豁ｦ蝎ｨ繝｢繝・Ν・亥ｼｾ謨ｰ縲√ム繝｡繝ｼ繧ｸ縲√け繝ｼ繝ｫ繝繧ｦ繝ｳ・峨・    /// 譌｢蟄倥・WeaponStats繧貞盾閠・↓縺励◆邏皮ｲ気#繝｢繝・Ν縲・    /// </summary>
    public sealed class Weapon
    {
        // --- 蝓ｺ譛ｬ諠・ｱ ---
        public WeaponType Type { get; init; }
        public string Label { get; init; } = "";
        public string ShortLabel { get; init; } = "";

        // --- 蠑ｾ謨ｰ ---
        public int MagazineAmmo { get; set; }
        public int MaxMagazineAmmo { get; init; }
        public int ReserveAmmo { get; set; }
        public int TotalAmmo => MagazineAmmo + ReserveAmmo;

        // --- 繝繝｡繝ｼ繧ｸ ---
        public float BaseDamage { get; init; }
        public float HeadshotMultiplier { get; init; }
        public float BodyshotMultiplier => 1.0f;

        // --- 繧ｯ繝ｼ繝ｫ繝繧ｦ繝ｳ / 繝ｪ繝ｭ繝ｼ繝・---
        public float FireCooldown { get; init; }
        public float CurrentCooldown { get; set; }
        public float ReloadTime { get; init; }
        public float CurrentReloadTime { get; set; }
        public bool IsReloading { get; set; }

        // --- 遽・峇 / 遘ｻ蜍・---
        public float VisionRange { get; init; }
        public float HearingMultiplier { get; init; }
        public float MoveSpeedMultiplier { get; init; }
        public float ProjectileRange { get; init; }
        public float ProjectileSpeed { get; init; }

        // --- 繝輔Λ繧ｰ ---
        public bool ScopedFov { get; init; }
        public bool IsReady => CurrentCooldown <= 0f && !IsReloading && MagazineAmmo > 0;
        public bool CanFire => CurrentCooldown <= 0f && MagazineAmmo > 0;

        public Weapon() { }

        public Weapon(WeaponType type, int magazineAmmo, float baseDamage, float fireCooldown)
        {
            Type = type;
            MagazineAmmo = magazineAmmo;
            MaxMagazineAmmo = magazineAmmo;
            BaseDamage = baseDamage;
            FireCooldown = fireCooldown;
        }

        /// <summary>逋ｺ蟆・ょｼｾ謨ｰ縺ｨ繧ｯ繝ｼ繝ｫ繝繧ｦ繝ｳ繧呈ｶ郁ｲｻ縲・/summary>
        public bool Fire(out float damageDealt)
        {
            damageDealt = 0f;
            if (!CanFire) return false;

            MagazineAmmo--;
            CurrentCooldown = FireCooldown;
            damageDealt = BaseDamage;
            return true;
        }

        /// <summary>繝倥ャ繝峨す繝ｧ繝・ヨ逋ｺ蟆・・/summary>
        public bool FireHeadshot(out float damageDealt)
        {
            damageDealt = 0f;
            if (!CanFire) return false;

            MagazineAmmo--;
            CurrentCooldown = FireCooldown;
            damageDealt = BaseDamage * HeadshotMultiplier;
            return true;
        }

        /// <summary>繝ｪ繝ｭ繝ｼ繝蛾幕蟋九・/summary>
        public void StartReload()
        {
            if (IsReloading || MagazineAmmo == MaxMagazineAmmo || ReserveAmmo <= 0) return;
            IsReloading = true;
            CurrentReloadTime = ReloadTime;
        }

        /// <summary>繝ｪ繝ｭ繝ｼ繝蛾ｲ陦後・/summary>
        public void UpdateReload(float delta)
        {
            if (!IsReloading) return;

            CurrentReloadTime -= delta;
            if (CurrentReloadTime <= 0f)
            {
                var needed = MaxMagazineAmmo - MagazineAmmo;
                var available = Math.Min(needed, ReserveAmmo);
                MagazineAmmo += available;
                ReserveAmmo -= available;
                IsReloading = false;
                CurrentReloadTime = 0f;
            }
        }

        /// <summary>繧ｯ繝ｼ繝ｫ繝繧ｦ繝ｳ騾ｲ陦後・/summary>
        public void UpdateCooldown(float delta)
        {
            if (CurrentCooldown > 0f)
            {
                CurrentCooldown -= delta;
                if (CurrentCooldown < 0f) CurrentCooldown = 0f;
            }
        }

        /// <summary>蠑ｾ繧定｣懷・・医Μ繧ｶ繝ｼ繝悶↑縺暦ｼ峨・/summary>
        public void RefillAmmo()
        {
            MagazineAmmo = MaxMagazineAmmo;
            ReserveAmmo = 0;
        }

        /// <summary>豁ｦ蝎ｨ縺ｮ讎りｦ√ｒ譁・ｭ怜・蛹悶・/summary>
        public string Summary() =>
            $"{Label}[{Type}] AMO={MagazineAmmo}/{ReserveAmmo} DMG={BaseDamage:F1} CD={FireCooldown:F1}s";
    }
}

