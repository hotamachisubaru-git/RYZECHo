using System.Drawing.Drawing2D;

namespace RYZECHo.Prototype;

internal sealed partial class GameModel
{
    private bool CanPlayerDefuse()
    {
        return _bombPlanted &&
               _player.IsAlive &&
               _armedBombSiteId is not null &&
               IsInsideBombSite(_player.Position, _armedBombSiteId.Value, 10f);
    }

    private bool IsInsideBombSite(PointF position, float padding = 0f)
    {
        return GetBombSites().Any(site => IsInsideBombSite(position, site.Id, padding));
    }

    private bool IsInsideBombSite(PointF position, ObjectiveSiteId siteId, float padding = 0f)
    {
        return Distance(position, BombSitePosition(siteId)) <= BombSiteRadius + padding;
    }

    private void BuildMapGeometry()
    {
        _permanentWalls.Clear();
        _buildSlots.Clear();
        _noBuildZones.Clear();
        _spawnCells.Clear();

        for (var x = 0; x < GridColumns; x++)
        {
            _permanentWalls.Add(new Point(x, 0));
            _permanentWalls.Add(new Point(x, GridRows - 1));
        }

        for (var y = 0; y < GridRows; y++)
        {
            _permanentWalls.Add(new Point(0, y));
            _permanentWalls.Add(new Point(GridColumns - 1, y));
        }

        AddWallLine(6, 2, 6, 4);
        AddWallLine(6, 7, 6, 9);
        AddWallLine(9, 2, 9, 4);
        AddWallLine(9, 7, 9, 9);
        AddWallLine(12, 2, 12, 3);
        AddWallLine(12, 8, 12, 9);
        AddWallLine(3, 5, 4, 5);
        AddWallLine(10, 5, 11, 5);

        foreach (var slot in new[]
        {
            new Point(3, 4), new Point(3, 6), new Point(5, 5), new Point(6, 5), new Point(6, 6),
            new Point(7, 4), new Point(7, 7), new Point(8, 5), new Point(8, 6), new Point(9, 5),
            new Point(10, 6), new Point(11, 4), new Point(11, 7), new Point(13, 5), new Point(13, 7),
        })
        {
            if (!_permanentWalls.Contains(slot))
            {
                _buildSlots.Add(slot);
            }
        }

        _spawnCells.AddRange([new Point(1, 2), new Point(1, 4), new Point(1, 7), new Point(1, 9)]);

        foreach (var protectedCell in new[]
        {
            new Point(14, 4), new Point(13, 4), new Point(14, 5), new Point(14, 3),
            new Point(14, 8), new Point(13, 8), new Point(14, 7), new Point(14, 9),
            MirrorCellHorizontally(new Point(14, 4)), MirrorCellHorizontally(new Point(13, 4)), MirrorCellHorizontally(new Point(14, 5)), MirrorCellHorizontally(new Point(14, 3)),
            MirrorCellHorizontally(new Point(14, 8)), MirrorCellHorizontally(new Point(13, 8)), MirrorCellHorizontally(new Point(14, 7)), MirrorCellHorizontally(new Point(14, 9)),
            new Point(1, 2), new Point(2, 2), new Point(1, 4), new Point(2, 4), new Point(1, 7), new Point(2, 7), new Point(1, 9), new Point(2, 9),
            new Point(13, 6), new Point(13, 4), new Point(13, 8), new Point(12, 6),
            new Point(12, 6), new Point(12, 4), new Point(12, 8), new Point(11, 6),
        })
        {
            if (protectedCell.X >= 0 && protectedCell.X < GridColumns && protectedCell.Y >= 0 && protectedCell.Y < GridRows)
            {
                _noBuildZones.Add(protectedCell);
            }
        }
    }

    private void AddWallLine(int startX, int startY, int endX, int endY)
    {
        for (var x = Math.Min(startX, endX); x <= Math.Max(startX, endX); x++)
        {
            for (var y = Math.Min(startY, endY); y <= Math.Max(startY, endY); y++)
            {
                _permanentWalls.Add(new Point(x, y));
            }
        }
    }

    private static bool IsPerimeterCell(Point cell)
    {
        return cell.X == 0 || cell.Y == 0 || cell.X == GridColumns - 1 || cell.Y == GridRows - 1;
    }

    private Structure CreateStructure(BuildToolKind tool, Point cell)
    {
        return tool switch
        {
            BuildToolKind.BlastDoor => new Structure
            {
                Kind = StructureKind.BlastDoor,
                Cell = cell,
                APCost = 2,
                Label = "防壁ドア",
                Health = 120f,
                MaxHealth = 120f,
                PulseCooldown = 0f,
            },
            BuildToolKind.HoneyTrap => new Structure
            {
                Kind = StructureKind.HoneyTrap,
                Cell = cell,
                APCost = 3,
                Label = "ハチミツトラップ",
                Health = 1f,
                MaxHealth = 1f,
                PulseCooldown = 0f,
            },
            BuildToolKind.StaticNest => new Structure
            {
                Kind = StructureKind.StaticNest,
                Cell = cell,
                APCost = 4,
                Label = "スタティックネスト",
                Health = 1f,
                MaxHealth = 1f,
                PulseCooldown = 0.3f,
            },
            BuildToolKind.ReconBeacon => new Structure
            {
                Kind = StructureKind.ReconBeacon,
                Cell = cell,
                APCost = 4,
                Label = "リコンビーコン",
                Health = 1f,
                MaxHealth = 1f,
                PulseCooldown = 0.45f,
            },
            _ => new Structure
            {
                Kind = StructureKind.ShieldRelay,
                Cell = cell,
                APCost = 5,
                Label = "シールドリレー",
                Health = 90f,
                MaxHealth = 90f,
                PulseCooldown = 0.6f,
            },
        };
    }

    private static Dictionary<WeaponType, WeaponStats> CreateWeaponStats()
    {
        return new Dictionary<WeaponType, WeaponStats>
        {
            [WeaponType.Blitz] = new()
            {
                Type = WeaponType.Blitz,
                Label = "ブリッツ / 高速連射 SMG",
                ShortLabel = "ブリッツ",
                Code = "BLZ",
                Category = "近距離特化",
                VisionClass = "狭",
                Cost = 220,
                MagazineAmmo = 25,
                ReserveAmmo = 50,
                VisionRange = 220f,
                HearingMultiplier = 1.25f,
                FireCooldown = 0.09f,
                Damage = 7f,
                MoveSpeed = 234f,
                ProjectileRange = 210f,
                ScopedFov = false,
            },
            [WeaponType.Monster] = new()
            {
                Type = WeaponType.Monster,
                Label = "モンスター / 装弾数特化 SMG",
                ShortLabel = "モンスター",
                Code = "MON",
                Category = "近距離特化",
                VisionClass = "中",
                Cost = 310,
                MagazineAmmo = 40,
                ReserveAmmo = 80,
                VisionRange = 300f,
                HearingMultiplier = 1.1f,
                FireCooldown = 0.11f,
                Damage = 8f,
                MoveSpeed = 220f,
                ProjectileRange = 225f,
                ScopedFov = false,
            },
            [WeaponType.Melt] = new()
            {
                Type = WeaponType.Melt,
                Label = "メルト / 高火力 SG",
                ShortLabel = "メルト",
                Code = "MLT",
                Category = "近距離特化",
                VisionClass = "狭",
                Cost = 260,
                MagazineAmmo = 6,
                ReserveAmmo = 12,
                VisionRange = 205f,
                HearingMultiplier = 0.95f,
                FireCooldown = 0.58f,
                Damage = 34f,
                MoveSpeed = 214f,
                ProjectileRange = 122f,
                ScopedFov = false,
            },
            [WeaponType.Fairy] = new()
            {
                Type = WeaponType.Fairy,
                Label = "フェアリー / 軽量 AR",
                ShortLabel = "フェアリー",
                Code = "FAR",
                Category = "中距離バランス",
                VisionClass = "中",
                Cost = 350,
                MagazineAmmo = 30,
                ReserveAmmo = 60,
                VisionRange = 330f,
                HearingMultiplier = 1f,
                FireCooldown = 0.17f,
                Damage = 13f,
                MoveSpeed = 224f,
                ProjectileRange = 315f,
                ScopedFov = false,
            },
            [WeaponType.Giant] = new()
            {
                Type = WeaponType.Giant,
                Label = "ジャイアント / 安定重視 AR",
                ShortLabel = "ジャイアント",
                Code = "GNT",
                Category = "中距離バランス",
                VisionClass = "中",
                Cost = 400,
                MagazineAmmo = 30,
                ReserveAmmo = 90,
                VisionRange = 340f,
                HearingMultiplier = 0.95f,
                FireCooldown = 0.21f,
                Damage = 16f,
                MoveSpeed = 210f,
                ProjectileRange = 332f,
                ScopedFov = false,
            },
            [WeaponType.Juggernaut] = new()
            {
                Type = WeaponType.Juggernaut,
                Label = "ジャガーノート / 制圧型 LMG",
                ShortLabel = "ジャガーノート",
                Code = "JGN",
                Category = "中距離バランス",
                VisionClass = "中",
                Cost = 560,
                MagazineAmmo = 100,
                ReserveAmmo = 100,
                VisionRange = 320f,
                HearingMultiplier = 0.88f,
                FireCooldown = 0.12f,
                Damage = 11f,
                MoveSpeed = 188f,
                ProjectileRange = 336f,
                ScopedFov = false,
            },
            [WeaponType.Violet] = new()
            {
                Type = WeaponType.Violet,
                Label = "ヴァイオレット / 軽量 SR",
                ShortLabel = "ヴァイオレット",
                Code = "VLT",
                Category = "遠距離特化",
                VisionClass = "極大",
                Cost = 470,
                MagazineAmmo = 5,
                ReserveAmmo = 10,
                VisionRange = 470f,
                HearingMultiplier = 0.75f,
                FireCooldown = 0.62f,
                Damage = 34f,
                MoveSpeed = 192f,
                ProjectileRange = 420f,
                ScopedFov = true,
            },
            [WeaponType.Changer] = new()
            {
                Type = WeaponType.Changer,
                Label = "チェンジャー / 一撃重視 SR",
                ShortLabel = "チェンジャー",
                Code = "CHG",
                Category = "遠距離特化",
                VisionClass = "極大",
                Cost = 620,
                MagazineAmmo = 5,
                ReserveAmmo = 5,
                VisionRange = 500f,
                HearingMultiplier = 0.7f,
                FireCooldown = 0.85f,
                Damage = 52f,
                MoveSpeed = 176f,
                ProjectileRange = 460f,
                ScopedFov = true,
            },
            [WeaponType.Howl] = new()
            {
                Type = WeaponType.Howl,
                Label = "ハウル / セミオート DMR",
                ShortLabel = "ハウル",
                Code = "HWL",
                Category = "遠距離特化",
                VisionClass = "大",
                Cost = 430,
                MagazineAmmo = 15,
                ReserveAmmo = 30,
                VisionRange = 390f,
                HearingMultiplier = 0.82f,
                FireCooldown = 0.24f,
                Damage = 19f,
                MoveSpeed = 205f,
                ProjectileRange = 375f,
                ScopedFov = false,
            },
            [WeaponType.Pulse] = new()
            {
                Type = WeaponType.Pulse,
                Label = "パルス / 安定型 HG",
                ShortLabel = "パルス",
                Code = "PLS",
                Category = "サブウェポン",
                VisionClass = "中",
                Cost = 90,
                MagazineAmmo = 12,
                ReserveAmmo = 36,
                VisionRange = 260f,
                HearingMultiplier = 1.05f,
                FireCooldown = 0.24f,
                Damage = 12f,
                MoveSpeed = 238f,
                ProjectileRange = 230f,
                ScopedFov = false,
            },
            [WeaponType.Shard] = new()
            {
                Type = WeaponType.Shard,
                Label = "シャード / 速射型 HG",
                ShortLabel = "シャード",
                Code = "SHD",
                Category = "サブウェポン",
                VisionClass = "中",
                Cost = 130,
                MagazineAmmo = 18,
                ReserveAmmo = 54,
                VisionRange = 250f,
                HearingMultiplier = 1.1f,
                FireCooldown = 0.18f,
                Damage = 9f,
                MoveSpeed = 244f,
                ProjectileRange = 215f,
                ScopedFov = false,
            },
        };
    }

    private void EmitRipple(PointF position, float strength, RippleKind kind, Color color)
    {
        _ripples.Add(new Ripple
        {
            Position = position,
            Strength = strength,
            Lifetime = SoundCueLifetimeSeconds,
            Kind = kind,
            Color = color,
        });
    }

    private Point ScreenToCell(Point location)
    {
        var worldPoint = ScreenToWorldPoint(location);
        return new Point(
            (int)MathF.Floor((worldPoint.X - WorldBounds.Left) / CellSize),
            (int)MathF.Floor((worldPoint.Y - WorldBounds.Top) / CellSize));
    }

    private bool TryGetWorldPointFromScreen(Point screenPoint, out PointF worldPoint)
    {
        worldPoint = ScreenToWorldPoint(screenPoint);
        return worldPoint.X >= WorldBounds.Left &&
               worldPoint.X < WorldBounds.Right &&
               worldPoint.Y >= WorldBounds.Top &&
               worldPoint.Y < WorldBounds.Bottom;
    }

    private PointF ScreenToWorldPoint(Point screenPoint)
    {
        var points = new[] { new PointF(screenPoint.X, screenPoint.Y) };
        using var projection = CreateActiveWorldMatrix();
        projection.Invert();
        projection.TransformPoints(points);
        return points[0];
    }

    private Point WorldToCell(PointF point)
    {
        var x = (int)Math.Clamp((point.X - WorldBounds.Left) / CellSize, 0f, GridColumns - 1);
        var y = (int)Math.Clamp((point.Y - WorldBounds.Top) / CellSize, 0f, GridRows - 1);
        return new Point(x, y);
    }

    private Rectangle CellRectangle(Point cell)
    {
        return new Rectangle(WorldBounds.Left + (cell.X * CellSize), WorldBounds.Top + (cell.Y * CellSize), CellSize, CellSize);
    }

    private PointF CellCenter(Point cell)
    {
        return new PointF(
            WorldBounds.Left + (cell.X * CellSize) + (CellSize / 2f),
            WorldBounds.Top + (cell.Y * CellSize) + (CellSize / 2f));
    }

    private PointF CorePosition()
    {
        return CellCenter(new Point(14, 6));
    }

    private Point GetBombSiteCell()
    {
        return GetBombSite(CurrentObjectiveSiteId()).Cell;
    }

    private PointF BombSitePosition()
    {
        return BombSitePosition(CurrentObjectiveSiteId());
    }

    private PointF BombSitePosition(ObjectiveSiteId siteId)
    {
        return CellCenter(GetBombSite(siteId).Cell);
    }

    private static Point MirrorCellHorizontally(Point cell)
    {
        return new Point((GridColumns - 1) - cell.X, cell.Y);
    }

    private Matrix CreateActiveWorldMatrix()
    {
        var matrix = CreateWorldProjectionMatrix();
        if (_phase != GamePhase.Hunt || !_player.IsAlive)
        {
            return matrix;
        }

        var focusPoints = new[] { new PointF(_player.Position.X, _player.Position.Y) };
        matrix.TransformPoints(focusPoints);
        var focusScreen = focusPoints[0];
        var targetScreen = new PointF(
            WorldVisualBounds.Left + (WorldVisualBounds.Width * HuntCameraTargetX),
            WorldVisualBounds.Top + (WorldVisualBounds.Height * HuntCameraTargetY));

        matrix.Translate(-focusScreen.X, -focusScreen.Y, MatrixOrder.Append);
        matrix.Scale(HuntCameraZoom, HuntCameraZoom, MatrixOrder.Append);
        matrix.Translate(focusScreen.X, focusScreen.Y, MatrixOrder.Append);
        matrix.Translate(targetScreen.X - focusScreen.X, targetScreen.Y - focusScreen.Y, MatrixOrder.Append);
        return matrix;
    }

    private Matrix CreateWorldProjectionMatrix()
    {
        return new Matrix(
            WorldPerspectiveScaleX,
            0f,
            WorldPerspectiveShearX,
            WorldPerspectiveScaleY,
            WorldVisualBounds.Left - (WorldPerspectiveScaleX * WorldBounds.Left) - (WorldPerspectiveShearX * WorldBounds.Top),
            WorldVisualBounds.Top - (WorldPerspectiveScaleY * WorldBounds.Top));
    }

    private PointF[] GetProjectedWorldCorners()
    {
        var points = new[]
        {
            new PointF(WorldBounds.Left, WorldBounds.Top),
            new PointF(WorldBounds.Right, WorldBounds.Top),
            new PointF(WorldBounds.Right, WorldBounds.Bottom),
            new PointF(WorldBounds.Left, WorldBounds.Bottom),
        };

        using var projection = CreateActiveWorldMatrix();
        projection.TransformPoints(points);
        return points;
    }

    private string BuildToolLabel(BuildToolKind tool)
    {
        return tool switch
        {
            BuildToolKind.BlastDoor => "防壁ドア / 2AP",
            BuildToolKind.HoneyTrap => "ハチミツトラップ / 3AP",
            BuildToolKind.StaticNest => "スタティックネスト / 4AP",
            BuildToolKind.ReconBeacon => "リコンビーコン / 4AP",
            _ => "シールドリレー / 5AP",
        };
    }

    private string PhaseLabel()
    {
        return _phase switch
        {
            GamePhase.Construct => "構築",
            GamePhase.Bet => "投資",
            GamePhase.Hunt => IsPlayerTeamAttacking() ? "攻撃" : "防衛",
            GamePhase.RoundResult => "精算",
            GamePhase.Victory => "勝利",
            _ => "敗北",
        };
    }

    private Color PhaseColor()
    {
        return _phase switch
        {
            GamePhase.Construct => Color.FromArgb(255, 115, 225, 205),
            GamePhase.Bet => Color.FromArgb(255, 255, 225, 130),
            GamePhase.Hunt => Color.FromArgb(255, 255, 125, 105),
            GamePhase.Victory => Color.FromArgb(255, 120, 235, 165),
            GamePhase.Defeat => Color.FromArgb(255, 255, 105, 95),
            _ => Color.FromArgb(255, 205, 215, 225),
        };
    }

    private static float Distance(PointF left, PointF right)
    {
        var dx = left.X - right.X;
        var dy = left.Y - right.Y;
        return MathF.Sqrt((dx * dx) + (dy * dy));
    }

    private static float DegreesToRadians(float degrees) => degrees * (MathF.PI / 180f);

    private static float RadiansToDegrees(float radians) => radians * (180f / MathF.PI);

    private static float NormalizeAngle(float angle)
    {
        while (angle > MathF.PI)
        {
            angle -= MathF.PI * 2f;
        }

        while (angle < -MathF.PI)
        {
            angle += MathF.PI * 2f;
        }

        return angle;
    }
}
