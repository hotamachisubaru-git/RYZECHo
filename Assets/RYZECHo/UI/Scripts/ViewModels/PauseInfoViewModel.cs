using UnityEngine;
using Color = UnityEngine.Color;

namespace RYZECHo
{
    /// <summary>
    /// ポーズ画面に表示する情報データ（ViewModel）。\n    /// GameModelからポーズ情報を読み取り、UIに渡す。
    /// </summary>
    public struct PauseInfoViewModel
    {
        public string PhaseLabel;
        public int PlayerScore;
        public int EnemyScore;
        public int CurrentRound;
        public int Credits;

        /// <summary>
        /// GameModelからポーズ情報を構築して返す。
        /// </summary>
        public static PauseInfoViewModel FromGameModel(GameModel model)
        {
            if (model == null) return new PauseInfoViewModel();

            return new PauseInfoViewModel
            {
                PhaseLabel = GetPhaseLabel(model.GetPhase()),
                PlayerScore = model.GetPlayerRoundWins(),
                EnemyScore = model.GetEnemyRoundWins(),
                CurrentRound = model.GetCurrentRound(),
                Credits = model.GetCredits(),
            };
        }

        private static string GetPhaseLabel(GamePhase phase)
        {
            return phase switch
            {
                GamePhase.Construct => "構築フェーズ",
                GamePhase.Bet => "ベットフェーズ",
                GamePhase.Hunt => "ハンティングフェーズ",
                GamePhase.RoundResult => "ラウンド結果",
                GamePhase.Victory => "勝利",
                GamePhase.Defeat => "敗北",
                _ => "不明",
            };
        }
    }
}
