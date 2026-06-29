using System;

namespace RYZECHo.TacticalProto
{
    /// <summary>
    /// ゲーム状態管理（フェーズ、スコア、ターン）。
    /// 既存のGameModel.State.csから状態管理の概念を抽出。
    /// </summary>
    public sealed class GameState
    {
        // --- フェーズ ---
        public GamePhase Phase { get; internal set; }
        public float PhaseTimer { get; set; }

        // --- スコア ---
        public int PlayerRoundWins { get; set; }
        public int EnemyRoundWins { get; set; }
        public int CurrentRound { get; set; }
        public bool IsOvertime { get; set; }

        // --- ターン ---
        public int TurnIndex { get; set; }
        public TeamRole PlayerTeamRole { get; set; }
        public TeamRole EnemyTeamRole { get; set; }

        // --- クレジット ---
        public int Credits { get; set; }
        public int BuildPoints { get; set; }

        // --- ボム ---
        public bool BombPlanted { get; set; }
        public ObjectiveSiteId? ArmedBombSite { get; set; }
        public float BombFuseTimer { get; set; }

        // --- 進行 ---
        public float RoundTimer { get; set; }
        public float RoundDuration { get; set; }

        // --- イベント ---
        public string ResultMessage { get; set; } = "";
        public bool ShowBriefing { get; set; }

        public GameState()
        {
            Phase = GamePhase.Construct;
            PhaseTimer = 0f;
            CurrentRound = 1;
            PlayerTeamRole = TeamRole.Defense;
            EnemyTeamRole = TeamRole.Attack;
            Credits = GameConstants.StartingCredits;
            BuildPoints = GameConstants.InitialBuildPoints;
            RoundTimer = GameConstants.RoundDurationSeconds;
            RoundDuration = GameConstants.RoundDurationSeconds;
            ShowBriefing = true;
        }

        /// <summary>ラウンド開始時に状態をリセット。</summary>
        public void ResetRound()
        {
            BombPlanted = false;
            ArmedBombSite = null;
            BombFuseTimer = 0f;
            RoundTimer = RoundDuration;
            PhaseTimer = 0f;
            ShowBriefing = false;
        }

        /// <summary>フェーズ遷移。</summary>
        public void SetPhase(GamePhase newPhase)
        {
            Phase = newPhase;
            PhaseTimer = 0f;
        }

        /// <summary>チーム交代。ビルドポイントを回復。</summary>
        public void SwapSide()
        {
            PlayerTeamRole = PlayerTeamRole == TeamRole.Attack
                ? TeamRole.Defense
                : TeamRole.Attack;
            EnemyTeamRole = PlayerTeamRole == TeamRole.Attack
                ? TeamRole.Defense
                : TeamRole.Attack;
            BuildPoints = GameConstants.SideSwapBuildPointRefill;
        }

        /// <summary>ラウンド勝敗を記録。</summary>
        public void RecordRoundWin(bool playerWon)
        {
            if (playerWon)
                PlayerRoundWins++;
            else
                EnemyRoundWins++;

            // オーバータイム判定
            if (PlayerRoundWins >= GameConstants.OvertimeTriggerScore &&
                EnemyRoundWins >= GameConstants.OvertimeTriggerScore)
            {
                IsOvertime = true;
            }
        }

        /// <summary>マッチ終了判定。</summary>
        public bool IsMatchOver()
        {
            if (IsOvertime)
                return false;
            return PlayerRoundWins >= GameConstants.RoundsToWin ||
                   EnemyRoundWins >= GameConstants.RoundsToWin;
        }

        /// <summary>勝利側の判定。</summary>
        public bool PlayerWonMatch() =>
            PlayerRoundWins >= GameConstants.RoundsToWin;

        /// <summary>現在の勝敗ステータスを文字列化。</summary>
        public string ScoreString() =>
            $"R{CurrentRound} {PlayerRoundWins}-{EnemyRoundWins} {PlayerTeamRole}";
    }
}
