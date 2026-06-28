param(
    [string]$ProjectRoot = (Join-Path $PSScriptRoot "..\RYZECHo.Prototype")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$projectPath = (Resolve-Path $ProjectRoot).Path
$assetsRoot = Join-Path $projectPath "Assets"

$directories = @(
    "Audio/BGM",
    "Audio/SFX/Weapon",
    "Audio/SFX/Impact",
    "Audio/SFX/Footstep",
    "Audio/SFX/UI",
    "Characters/Player",
    "Characters/Enemy",
    "Environment/Materials",
    "Environment/Textures",
    "Environment/Props",
    "Environment/Maps",
    "Weapons/Models",
    "Weapons/Textures",
    "Weapons/Data",
    "UI/Crosshair",
    "UI/HUD",
    "UI/Icons",
    "VFX/MuzzleFlash",
    "VFX/Hit",
    "VFX/Explosion",
    "VFX/Environment",
    "Fonts",
    "Data/Weapons",
    "Data/Enemies",
    "Data/Items"
)

foreach ($directory in $directories) {
    New-Item -ItemType Directory -Path (Join-Path $assetsRoot $directory) -Force | Out-Null
}

function New-Png {
    param(
        [string]$Path,
        [int]$Width,
        [int]$Height,
        [scriptblock]$Draw
    )

    $bitmap = [System.Drawing.Bitmap]::new($Width, $Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.Clear([System.Drawing.Color]::Transparent)

    try {
        & $Draw $graphics $bitmap
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

function New-WavTone {
    param(
        [string]$Path,
        [double]$Frequency,
        [int]$DurationMs,
        [double]$Volume = 0.22,
        [int]$SampleRate = 22050
    )

    $sampleCount = [int]($SampleRate * ($DurationMs / 1000.0))
    $bytesPerSample = 2
    $channels = 1
    $dataLength = $sampleCount * $bytesPerSample * $channels

    $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
    $writer = [System.IO.BinaryWriter]::new($stream)

    try {
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes("RIFF"))
        $writer.Write([int](36 + $dataLength))
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes("WAVE"))
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes("fmt "))
        $writer.Write([int]16)
        $writer.Write([int16]1)
        $writer.Write([int16]$channels)
        $writer.Write([int]$SampleRate)
        $writer.Write([int]($SampleRate * $channels * $bytesPerSample))
        $writer.Write([int16]($channels * $bytesPerSample))
        $writer.Write([int16]($bytesPerSample * 8))
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes("data"))
        $writer.Write([int]$dataLength)

        for ($index = 0; $index -lt $sampleCount; $index++) {
            $t = $index / $SampleRate
            $envelope = 1.0 - ($index / [double]$sampleCount)
            $sample = [math]::Sin(2.0 * [math]::PI * $Frequency * $t) * 32767 * $Volume * $envelope
            $writer.Write([int16][math]::Round($sample))
        }
    }
    finally {
        $writer.Dispose()
        $stream.Dispose()
    }
}

function Write-JsonFile {
    param(
        [string]$Path,
        [object]$Data
    )

    $json = $Data | ConvertTo-Json -Depth 8
    Set-Content -Path $Path -Value $json -Encoding UTF8
}

New-Png -Path (Join-Path $assetsRoot "UI/Crosshair/crosshair_cyan.png") -Width 256 -Height 256 -Draw {
    param($graphics, $bitmap)

    $cyan = [System.Drawing.Color]::FromArgb(255, 86, 229, 247)
    $glow = [System.Drawing.Color]::FromArgb(90, 86, 229, 247)
    $pen = [System.Drawing.Pen]::new($cyan, 8)
    $glowPen = [System.Drawing.Pen]::new($glow, 18)

    $center = 128
    $graphics.DrawEllipse($glowPen, 48, 48, 160, 160)
    $graphics.DrawEllipse($pen, 62, 62, 132, 132)
    $graphics.DrawLine($glowPen, $center, 24, $center, 74)
    $graphics.DrawLine($glowPen, $center, 182, $center, 232)
    $graphics.DrawLine($glowPen, 24, $center, 74, $center)
    $graphics.DrawLine($glowPen, 182, $center, 232, $center)
    $graphics.DrawLine($pen, $center, 32, $center, 76)
    $graphics.DrawLine($pen, $center, 180, $center, 224)
    $graphics.DrawLine($pen, 32, $center, 76, $center)
    $graphics.DrawLine($pen, 180, $center, 224, $center)
    $pen.Dispose()
    $glowPen.Dispose()
}

New-Png -Path (Join-Path $assetsRoot "UI/HUD/hud_panel_frame.png") -Width 1536 -Height 300 -Draw {
    param($graphics, $bitmap)

    $backBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        [System.Drawing.Rectangle]::new(0, 0, $bitmap.Width, $bitmap.Height),
        [System.Drawing.Color]::FromArgb(238, 12, 18, 26),
        [System.Drawing.Color]::FromArgb(238, 7, 11, 16),
        90
    )
    $goldPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 173, 145, 88), 6)
    $innerPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(110, 90, 118, 140), 2)
    $graphics.FillRectangle($backBrush, 0, 0, $bitmap.Width, $bitmap.Height)
    $graphics.DrawRectangle($goldPen, 6, 6, $bitmap.Width - 12, $bitmap.Height - 12)
    $graphics.DrawRectangle($innerPen, 20, 20, $bitmap.Width - 40, $bitmap.Height - 40)
    $graphics.FillPolygon([System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 173, 145, 88)), @(
        [System.Drawing.Point]::new(32, 6),
        [System.Drawing.Point]::new(78, 6),
        [System.Drawing.Point]::new(55, -12)
    ))
    $graphics.FillPolygon([System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 173, 145, 88)), @(
        [System.Drawing.Point]::new($bitmap.Width - 80, 6),
        [System.Drawing.Point]::new($bitmap.Width - 34, 6),
        [System.Drawing.Point]::new($bitmap.Width - 57, -12)
    ))
    $backBrush.Dispose()
    $goldPen.Dispose()
    $innerPen.Dispose()
}

New-Png -Path (Join-Path $assetsRoot "UI/Icons/icon_shield.png") -Width 96 -Height 96 -Draw {
    param($graphics, $bitmap)
    $brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 108, 231, 214))
    $pen = [System.Drawing.Pen]::new([System.Drawing.Color]::White, 3)
    $points = @(
        [System.Drawing.Point]::new(48, 10),
        [System.Drawing.Point]::new(76, 22),
        [System.Drawing.Point]::new(68, 60),
        [System.Drawing.Point]::new(48, 84),
        [System.Drawing.Point]::new(28, 60),
        [System.Drawing.Point]::new(20, 22)
    )
    $graphics.FillPolygon($brush, $points)
    $graphics.DrawPolygon($pen, $points)
    $brush.Dispose()
    $pen.Dispose()
}

New-Png -Path (Join-Path $assetsRoot "UI/Icons/icon_credit.png") -Width 96 -Height 96 -Draw {
    param($graphics, $bitmap)
    $gold = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 238, 198, 104))
    $pen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 160, 132, 72), 4)
    $graphics.FillEllipse($gold, 14, 14, 68, 68)
    $graphics.DrawEllipse($pen, 14, 14, 68, 68)
    $font = [System.Drawing.Font]::new("Yu Gothic UI", 26, [System.Drawing.FontStyle]::Bold)
    $brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 48, 38, 12))
    $format = [System.Drawing.StringFormat]::new()
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $format.LineAlignment = [System.Drawing.StringAlignment]::Center
    $graphics.DrawString("G", $font, $brush, [System.Drawing.RectangleF]::new(14, 14, 68, 68), $format)
    $gold.Dispose(); $pen.Dispose(); $font.Dispose(); $brush.Dispose(); $format.Dispose()
}

New-Png -Path (Join-Path $assetsRoot "UI/Icons/icon_audio.png") -Width 96 -Height 96 -Draw {
    param($graphics, $bitmap)
    $pen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 86, 229, 247), 5)
    $graphics.DrawLine($pen, 20, 48, 38, 48)
    $graphics.DrawLine($pen, 38, 48, 54, 32)
    $graphics.DrawLine($pen, 38, 48, 54, 64)
    $graphics.DrawArc($pen, 42, 24, 28, 48, -45, 90)
    $graphics.DrawArc($pen, 50, 16, 40, 64, -45, 90)
    $pen.Dispose()
}

New-Png -Path (Join-Path $assetsRoot "Characters/Player/player_operator.png") -Width 256 -Height 256 -Draw {
    param($graphics, $bitmap)
    $back = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 44, 188, 232))
    $rim = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 222, 192, 116), 8)
    $inner = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 20, 48, 72))
    $emblem = [System.Drawing.Pen]::new([System.Drawing.Color]::White, 6)
    $graphics.FillEllipse($back, 24, 24, 208, 208)
    $graphics.FillEllipse($inner, 40, 40, 176, 176)
    $graphics.DrawEllipse($rim, 24, 24, 208, 208)
    $graphics.DrawLine($emblem, 128, 76, 128, 180)
    $graphics.DrawLine($emblem, 76, 128, 180, 128)
    $back.Dispose(); $rim.Dispose(); $inner.Dispose(); $emblem.Dispose()
}

New-Png -Path (Join-Path $assetsRoot "Characters/Enemy/enemy_raider.png") -Width 256 -Height 256 -Draw {
    param($graphics, $bitmap)
    $brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 242, 102, 82))
    $pen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 255, 210, 190), 6)
    $shadow = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(92, 0, 0, 0))
    $graphics.FillEllipse($shadow, 52, 160, 152, 52)
    $points = @(
        [System.Drawing.Point]::new(128, 36),
        [System.Drawing.Point]::new(214, 184),
        [System.Drawing.Point]::new(42, 184)
    )
    $graphics.FillPolygon($brush, $points)
    $graphics.DrawPolygon($pen, $points)
    $brush.Dispose(); $pen.Dispose(); $shadow.Dispose()
}

New-Png -Path (Join-Path $assetsRoot "Environment/Textures/holo_floor_grid.png") -Width 1024 -Height 1024 -Draw {
    param($graphics, $bitmap)
    $graphics.Clear([System.Drawing.Color]::FromArgb(255, 10, 28, 30))
    $finePen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(38, 86, 229, 247), 1)
    $majorPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(72, 86, 229, 247), 2)
    for ($i = 0; $i -le 1024; $i += 32) {
        $pen = if ($i % 128 -eq 0) { $majorPen } else { $finePen }
        $graphics.DrawLine($pen, $i, 0, $i, 1024)
        $graphics.DrawLine($pen, 0, $i, 1024, $i)
    }
    $finePen.Dispose(); $majorPen.Dispose()
}

New-Png -Path (Join-Path $assetsRoot "Environment/Props/blast_door.png") -Width 256 -Height 256 -Draw {
    param($graphics, $bitmap)
    $body = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 74, 132, 170))
    $edge = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 156, 236, 248), 5)
    $graphics.FillRectangle($body, 42, 48, 172, 152)
    $graphics.DrawRectangle($edge, 42, 48, 172, 152)
    $graphics.DrawLine($edge, 128, 48, 128, 200)
    $body.Dispose(); $edge.Dispose()
}

New-Png -Path (Join-Path $assetsRoot "Environment/Props/honey_trap.png") -Width 256 -Height 256 -Draw {
    param($graphics, $bitmap)
    $brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 240, 188, 72))
    $pen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 255, 230, 160), 5)
    $graphics.FillEllipse($brush, 42, 60, 172, 132)
    $graphics.DrawEllipse($pen, 42, 60, 172, 132)
    $brush.Dispose(); $pen.Dispose()
}

New-Png -Path (Join-Path $assetsRoot "Environment/Props/static_nest.png") -Width 256 -Height 256 -Draw {
    param($graphics, $bitmap)
    $brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 122, 212, 120))
    $pen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 222, 255, 182), 5)
    $aura = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(72, 122, 212, 120), 6)
    $graphics.DrawEllipse($aura, 26, 30, 204, 196)
    $graphics.FillEllipse($brush, 56, 60, 144, 136)
    $graphics.DrawEllipse($pen, 56, 60, 144, 136)
    $brush.Dispose(); $pen.Dispose(); $aura.Dispose()
}

New-Png -Path (Join-Path $assetsRoot "Weapons/Textures/rifle_hud_card.png") -Width 512 -Height 128 -Draw {
    param($graphics, $bitmap)
    $back = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        [System.Drawing.Rectangle]::new(0, 0, $bitmap.Width, $bitmap.Height),
        [System.Drawing.Color]::FromArgb(255, 18, 26, 34),
        [System.Drawing.Color]::FromArgb(255, 10, 14, 18),
        90
    )
    $pen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 170, 146, 92), 4)
    $graphics.FillRectangle($back, 0, 0, $bitmap.Width, $bitmap.Height)
    $graphics.DrawRectangle($pen, 4, 4, $bitmap.Width - 8, $bitmap.Height - 8)
    $weaponPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 220, 196, 122), 7)
    $graphics.DrawLine($weaponPen, 56, 72, 318, 72)
    $graphics.DrawLine($weaponPen, 112, 52, 170, 52)
    $graphics.DrawLine($weaponPen, 244, 72, 278, 44)
    $graphics.DrawLine($weaponPen, 278, 44, 332, 44)
    $graphics.DrawLine($weaponPen, 306, 72, 356, 84)
    $weaponPen.Dispose(); $pen.Dispose(); $back.Dispose()
}

New-Png -Path (Join-Path $assetsRoot "VFX/MuzzleFlash/muzzle_flash.png") -Width 256 -Height 256 -Draw {
    param($graphics, $bitmap)
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $path.AddPolygon(@(
        [System.Drawing.Point]::new(128, 18),
        [System.Drawing.Point]::new(186, 108),
        [System.Drawing.Point]::new(236, 128),
        [System.Drawing.Point]::new(186, 148),
        [System.Drawing.Point]::new(128, 238),
        [System.Drawing.Point]::new(70, 148),
        [System.Drawing.Point]::new(20, 128),
        [System.Drawing.Point]::new(70, 108)
    ))
    $brush = [System.Drawing.Drawing2D.PathGradientBrush]::new($path)
    $brush.CenterColor = [System.Drawing.Color]::FromArgb(255, 255, 240, 180)
    $brush.SurroundColors = @([System.Drawing.Color]::FromArgb(0, 255, 140, 80))
    $graphics.FillPath($brush, $path)
    $brush.Dispose(); $path.Dispose()
}

New-Png -Path (Join-Path $assetsRoot "VFX/Hit/hit_spark.png") -Width 256 -Height 256 -Draw {
    param($graphics, $bitmap)
    $pen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 255, 180, 92), 6)
    foreach ($pair in @(
        @(128, 20, 128, 236),
        @(20, 128, 236, 128),
        @(44, 44, 212, 212),
        @(212, 44, 44, 212)
    )) {
        $graphics.DrawLine($pen, $pair[0], $pair[1], $pair[2], $pair[3])
    }
    $pen.Dispose()
}

New-Png -Path (Join-Path $assetsRoot "VFX/Explosion/explosion_ring.png") -Width 256 -Height 256 -Draw {
    param($graphics, $bitmap)
    $pen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 255, 124, 92), 10)
    $inner = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(165, 255, 220, 180), 5)
    $graphics.DrawEllipse($pen, 24, 24, 208, 208)
    $graphics.DrawEllipse($inner, 64, 64, 128, 128)
    $pen.Dispose(); $inner.Dispose()
}

New-Png -Path (Join-Path $assetsRoot "VFX/Environment/audio_ripple.png") -Width 256 -Height 256 -Draw {
    param($graphics, $bitmap)
    $pen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(220, 86, 229, 247), 4)
    $graphics.DrawEllipse($pen, 32, 32, 192, 192)
    $graphics.DrawEllipse($pen, 64, 64, 128, 128)
    $graphics.DrawEllipse($pen, 96, 96, 64, 64)
    $pen.Dispose()
}

New-WavTone -Path (Join-Path $assetsRoot "Audio/BGM/prototype_holo_theme.wav") -Frequency 196 -DurationMs 2800 -Volume 0.12
New-WavTone -Path (Join-Path $assetsRoot "Audio/SFX/Weapon/rifle_fire.wav") -Frequency 520 -DurationMs 140
New-WavTone -Path (Join-Path $assetsRoot "Audio/SFX/Impact/hit_ping.wav") -Frequency 720 -DurationMs 180
New-WavTone -Path (Join-Path $assetsRoot "Audio/SFX/Footstep/hard_step.wav") -Frequency 120 -DurationMs 90
New-WavTone -Path (Join-Path $assetsRoot "Audio/SFX/UI/confirm_beep.wav") -Frequency 880 -DurationMs 120

$weaponData = @(
    @{
        id = "smg_earline"
        displayName = "SMG / Audio Focus"
        cost = 50
        visionRange = 225
        hearingMultiplier = 1.35
        fireCooldown = 0.12
        damage = 8
        moveSpeed = 230
        projectileRange = 205
    },
    @{
        id = "rifle_balance"
        displayName = "Rifle / Balanced"
        cost = 100
        visionRange = 320
        hearingMultiplier = 1.0
        fireCooldown = 0.22
        damage = 15
        moveSpeed = 210
        projectileRange = 300
    },
    @{
        id = "sr_eyeline"
        displayName = "SR / Vision Focus"
        cost = 150
        visionRange = 470
        hearingMultiplier = 0.75
        fireCooldown = 0.68
        damage = 36
        moveSpeed = 185
        projectileRange = 430
    }
)

foreach ($weapon in $weaponData) {
    Write-JsonFile -Path (Join-Path $assetsRoot "Weapons/Data/$($weapon.id).json") -Data $weapon
    Write-JsonFile -Path (Join-Path $assetsRoot "Data/Weapons/$($weapon.id).json") -Data $weapon
}

Write-JsonFile -Path (Join-Path $assetsRoot "Data/Enemies/raider.json") -Data @{
    id = "raider"
    displayName = "Raider"
    archetypes = @("SMG", "Rifle", "Sniper")
    health = @{
        smg = 48
        rifle = 58
        sniper = 44
    }
    intent = "DestroyDataCore"
}

Write-JsonFile -Path (Join-Path $assetsRoot "Data/Items/honey_trap.json") -Data @{
    id = "honey_trap"
    displayName = "Honey Trap"
    effect = "slow_and_amplify"
    apCost = 3
}

Write-JsonFile -Path (Join-Path $assetsRoot "Data/Items/static_nest.json") -Data @{
    id = "static_nest"
    displayName = "Static Nest"
    effect = "vision_noise_and_fake_ripples"
    apCost = 4
}

Write-JsonFile -Path (Join-Path $assetsRoot "Environment/Materials/material_manifest.json") -Data @{
    palette = @{
        bg = "#081018"
        cyan = "#56E5F7"
        gold = "#B68E58"
        red = "#F26652"
        green = "#60C47A"
    }
    style = "holographic_tactical_board"
}

Write-JsonFile -Path (Join-Path $assetsRoot "Environment/Maps/prototype_holo_lane.json") -Data @{
    mapId = "prototype_holo_lane"
    grid = @{
        columns = 18
        rows = 12
        cellSize = 56
    }
    coreCell = @{ x = 14; y = 6 }
    spawnCells = @(
        @{ x = 1; y = 2 },
        @{ x = 1; y = 5 },
        @{ x = 1; y = 9 }
    )
}

Write-JsonFile -Path (Join-Path $assetsRoot "Fonts/font_manifest.json") -Data @{
    primary = "Yu Gothic UI"
    fallback = @("Segoe UI", "Bahnschrift")
    note = "Temporary system-font manifest until production font assets arrive."
}

$objContent = @"
o rifle_placeholder
v 0.0 0.0 0.0
v 1.8 0.0 0.0
v 1.8 0.2 0.0
v 0.0 0.2 0.0
v 1.2 0.4 0.0
v 1.7 0.4 0.0
f 1 2 3 4
f 3 6 5
"@
Set-Content -Path (Join-Path $assetsRoot "Weapons/Models/rifle_placeholder.obj") -Value $objContent -Encoding UTF8

$readme = @"
# Placeholder Assets

This directory is scaffolded for incoming production assets.
Generated files are temporary stand-ins so the prototype can evolve without blocking on art/audio delivery.
"@
Set-Content -Path (Join-Path $assetsRoot "README.md") -Value $readme -Encoding UTF8

Write-Host "Placeholder assets generated under $assetsRoot"
