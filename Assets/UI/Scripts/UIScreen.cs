using UnityEngine;
using Color = UnityEngine.Color;
using TMPro;
using UnityEngine.UI;

namespace RYZECHo.UI
{
    /// <summary>
    /// UI画面の基底クラス。各画面はこのクラスを継承して実装する。
    /// </summary>
    public abstract class UIScreen : MonoBehaviour
    {
        [Header("Screen Settings")]
        [Tooltip("画面の表示/非表示を切り替えるルートオブジェクト")]
        [SerializeField] private GameObject screenRoot;

        [Header("Font Settings")]
        [Tooltip("デフォルトフォント")]
        [SerializeField] protected TMP_FontAsset defaultFont;

        protected bool _isActive;

        public bool IsActive => _isActive;
        public GameObject ScreenRoot => screenRoot;

        /// <summary>
        /// 画面を初期化（Prefab読み込み時などに呼ばれる）
        /// </summary>
        public virtual void Initialize()
        {
            if (screenRoot != null)
                screenRoot.SetActive(false);
        }

        /// <summary>
        /// 画面を表示する
        /// </summary>
        public virtual void Show()
        {
            if (screenRoot != null)
                screenRoot.SetActive(true);
            _isActive = true;
            OnShow();
        }

        /// <summary>
        /// 画面を非表示にする
        /// </summary>
        public virtual void Hide()
        {
            if (screenRoot != null)
                screenRoot.SetActive(false);
            _isActive = false;
            OnHide();
        }

        /// <summary>
        /// 画面が初めて表示されるときに呼ばれる（オーバーライド用）
        /// </summary>
        public virtual void OnShow() { }

        /// <summary>
        /// 画面が非表示になるときに呼ばれる（オーバーライド用）
        /// </summary>
        public virtual void OnHide() { }

        /// <summary>
        /// ゲーム開始を要求
        /// </summary>
        public abstract void OnStartGame();

        /// <summary>
        /// 設定を開く
        /// </summary>
        public abstract void OnOpenSettings();

        /// <summary>
        /// アプリを終了
        /// </summary>
        public abstract void OnExitGame();

        /// <summary>
        /// リトライ（ゲーム再開）
        /// </summary>
        public abstract void OnRetry();

        /// <summary>
        /// TextMeshProのTextコンポーネントを取得するヘルパー
        /// </summary>
        protected TMP_Text GetTextComponent<T>(string name) where T : Component
        {
            var obj = screenRoot?.transform?.Find(name);
            if (obj == null) return null;
            return obj.GetComponentInChildren<TMP_Text>();
        }

        /// <summary>
        /// Buttonを取得するヘルパー
        /// </summary>
        protected Button GetButton(string name)
        {
            var obj = screenRoot?.transform?.Find(name);
            return obj?.GetComponent<Button>();
        }

        /// <summary>
        /// Buttonにコールバックを登録するヘルパー
        /// </summary>
        protected void AddButtonListener(Button button, System.Action callback)
        {
            if (button != null && callback != null)
                button.onClick.AddListener(() => callback());
        }

        /// <summary>
        /// RectTransformのサイズをセット
        /// </summary>
        protected void SetRectTransformSize(RectTransform rt, float width, float height)
        {
            if (rt != null)
                rt.sizeDelta = new Vector2(width, height);
        }

        /// <summary>
        /// RectTransformの位置をセット
        /// </summary>
        protected void SetRectTransformAnchors(RectTransform rt, Vector2 min, Vector2 max)
        {
            if (rt != null)
            {
                rt.anchorMin = min;
                rt.anchorMax = max;
                rt.pivot = new Vector2(0.5f, 0.5f);
            }
        }
    }
}
