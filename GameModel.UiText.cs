namespace RYZECHo.Prototype;

internal sealed partial class GameModel
{
    private string CurrentModeTitle()
    {
        return _phase switch
        {
            GamePhase.Construct => "初期構築フェーズ",
            GamePhase.Bet => "ボス選出 & ロードアウト",
            GamePhase.Hunt => IsPlayerTeamAttacking() ? "攻撃ラウンド進行中" : "防衛ラウンド進行中",
            GamePhase.RoundResult => "ラウンド精算",
            GamePhase.Victory => "マッチ勝利",
            _ => "作戦失敗",
        };
    }

    private string CurrentModeBody()
    {
        return _phase switch
        {
            GamePhase.Construct => _sideSwapConstructPending
                ? $"{BuildToolLabel(_selectedBuildTool)} を選択中。攻守交代に向けた再エディット中です。エージェント {SelectedAgentProfile().Name} / スキン {SelectedStructureSkinName()} / AD {SelectedAdThemeName()}。"
                : $"{BuildToolLabel(_selectedBuildTool)} を選択中。初期配置は前半ラウンドを支えます。エージェント {SelectedAgentProfile().Name} / スキン {SelectedStructureSkinName()} / AD {SelectedAdThemeName()}。",
            GamePhase.Bet => $"{PlayerRoleLabel()}。ボス {_selectedBossName} は残り {BossSelectionsRemaining(_selectedBossName)} 回。個別投資 {SelectedBossInvestment()}c / 総投資 {_selectedBet}c、メイン {_weaponStats[_selectedWeapon].Label}、サブ {_weaponStats[_selectedSidearmWeapon].Label}。エージェント {SelectedAgentProfile().Name} スキル {(_agentSkillPurchased ? "購入済み" : $"{AgentSkillPurchaseCost}c")}",
            GamePhase.Hunt => HuntStatusSummary(),
            GamePhase.RoundResult => _resultMessage,
            GamePhase.Victory => "7 ラウンド先取、または OT 2 本差でマッチに勝利しました。",
            _ => "サイト喪失、またはチーム壊滅。再編成して再挑戦。",
        };
    }

    private string CurrentObjectiveTitle()
    {
        return _phase switch
        {
            GamePhase.Construct => _sideSwapConstructPending ? "再構築中" : "構築中",
            GamePhase.Bet => "作戦準備",
            GamePhase.Hunt => CurrentHuntObjectiveTitle(),
            GamePhase.RoundResult => "ラウンド精算",
            GamePhase.Victory => "作戦成功",
            _ => "ラウンド敗北",
        };
    }

    private string CurrentObjectiveBody()
    {
        return _phase switch
        {
            GamePhase.Construct => _sideSwapConstructPending
                ? $"{BuildToolLabel(_selectedBuildTool)}\n後半戦 AP {_buildPoints}\n{SelectedAgentProfile().Name} / {SelectedStructureSkinName()}"
                : $"{BuildToolLabel(_selectedBuildTool)}\n残り AP {_buildPoints}\n{SelectedAgentProfile().Name} / {SelectedStructureSkinName()}",
            GamePhase.Bet => $"{PlayerRoleLabel()}\nボス {_selectedBossName} / 個別 {SelectedBossInvestment()}c / 総額 {_selectedBet}c\n{WeaponLoadoutLabel(_selectedWeapon)} + {WeaponLoadoutLabel(_selectedSidearmWeapon)} / ULT {SelectedBossUltPoints()}/{MaxUltPoints}",
            GamePhase.Hunt => HuntObjectiveBody(),
            GamePhase.RoundResult => $"{_resultMessage}\nSCORE {_playerRoundWins}-{_enemyRoundWins}\n{ProfileSummaryLine()}",
            GamePhase.Victory => $"最終スコア {_playerRoundWins}-{_enemyRoundWins}\n最終資産 {_credits} クレジット",
            _ => $"最終スコア {_playerRoundWins}-{_enemyRoundWins}\n再編成して再挑戦。",
        };
    }

    private string CurrentControlsHint()
    {
        return _phase switch
        {
            GamePhase.Construct => _sideSwapConstructPending ? "1-5基本設置  Tab全設置物  6エージェント  Q/Eスキン  R広告  Tストア  Enter後半戦へ" : "1-5基本設置  Tab全設置物  6エージェント  Q/Eスキン  R広告  Tストア  Enter確定",
            GamePhase.Bet => "1-4 ボス兼投資先  5スキル購入  6エージェント  Q/E選択  Rメイン/サブ切替  A/D個別投資  Tストア  Enter出撃",
            GamePhase.Hunt => HuntControlsHint(),
            _ => "Enter進行  R再挑戦",
        };
    }

    private string TimerLabel()
    {
        return _phase switch
        {
            GamePhase.Hunt when _bombPlanted => "爆破まで",
            GamePhase.Hunt => "設置猶予",
            _ => "残り時間",
        };
    }

    private string HuntStatusSummary()
    {
        if (_bombPlanted)
        {
            return IsPlayerTeamAttacking()
                ? $"サイト {CurrentObjectiveSiteLabel()} にボム設置済み。爆破まで {Math.Max(0f, _roundTimer):0.0} 秒、敵解除進行 {_bombDefuseProgress:0.0}/{BombDefuseSeconds:0}s。"
                : $"サイト {CurrentObjectiveSiteLabel()} にボム設置済み。爆破まで {Math.Max(0f, _roundTimer):0.0} 秒、解除進行 {_bombDefuseProgress:0.0}/{BombDefuseSeconds:0}s。";
        }

        if (_bombPlantProgress > 0f && _activePlanter is not null)
        {
            return IsPlayerTeamAttacking()
                ? $"{_activePlanter.Name} がサイト {CurrentObjectiveSiteLabel()} で設置中。{BombPlantSeconds - Math.Clamp(_bombPlantProgress, 0f, BombPlantSeconds):0.0} 秒で設置完了。"
                : $"{_activePlanter.Name} がサイト {CurrentObjectiveSiteLabel()} で設置中。{BombPlantSeconds - Math.Clamp(_bombPlantProgress, 0f, BombPlantSeconds):0.0} 秒で起爆準備完了。";
        }

        return $"{GetFovDegrees(_player.Weapon):0} 度視界で{PlayerRoleShortLabel()}中。{AgentRuntimeSummary()}。注力サイト {GetBombSite(_attackFocusSite).Label}、設置猶予 {Math.Max(0f, _roundTimer):0.0} 秒、敵編成 {LiveEnemyCount()}/{TeamSize}。{(IsPlayerBreathingExposed() ? " 呼吸音が漏れています。" : string.Empty)}";
    }

    private string HuntObjectiveBody()
    {
        if (_bombPlanted)
        {
            return IsPlayerTeamAttacking()
                ? $"サイト {CurrentObjectiveSiteLabel()}\n敵解除 {_bombDefuseProgress:0.0}/{BombDefuseSeconds:0}"
                : $"サイト {CurrentObjectiveSiteLabel()}\n解除 {_bombDefuseProgress:0.0}/{BombDefuseSeconds:0}";
        }

        if (_bombPlantProgress > 0f && _activePlanter is not null)
        {
            return $"設置中 {_activePlanter.Name}\nサイト {CurrentObjectiveSiteLabel()} / 進行 {_bombPlantProgress:0.0}/{BombPlantSeconds:0}";
        }

        return IsPlayerTeamAttacking()
            ? $"A/B サイト侵攻中\n注力 {GetBombSite(_attackFocusSite).Label} / 敵残数 {LiveEnemyCount()}/{TeamSize}"
            : $"A/B サイト防衛中\n敵残数 {LiveEnemyCount()}/{TeamSize}";
    }

    private string CurrentHuntObjectiveTitle()
    {
        if (_bombPlanted)
        {
            return IsPlayerTeamAttacking() ? "爆破維持" : "解除中";
        }

        if (_bombPlantProgress > 0f)
        {
            return IsPlayerTeamAttacking() ? $"設置中 {CurrentObjectiveSiteLabel()}" : $"設置阻止 {CurrentObjectiveSiteLabel()}";
        }

        return IsPlayerTeamAttacking() ? "攻撃中 A/B" : "防衛中 A/B";
    }

    private string HuntControlsHint()
    {
        if (_bombPlanted)
        {
            return IsPlayerTeamAttacking() ? "WASD移動  Q/E武器切替  1/2/3スキル  左クリック射撃  サイト防衛" : "WASD移動  Q/E武器切替  1/2/3スキル  左クリック射撃  F長押し解除";
        }

        return IsPlayerTeamAttacking() ? "WASD移動  Q/E武器切替  1/2/3スキル  左クリック射撃  F長押し設置" : "WASD移動  Q/E武器切替  1/2/3スキル  マウス照準  左クリック射撃";
    }

    private string CurrentSiteActionLabel()
    {
        if (_bombPlanted)
        {
            return IsPlayerTeamAttacking() ? $"維持 {CurrentObjectiveSiteLabel()}" : $"解除 {CurrentObjectiveSiteLabel()}";
        }

        return IsPlayerTeamAttacking() ? "設置 A/B" : "監視 A/B";
    }

}
