using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RYZECHo.UI
{
    /// <summary>
    /// Assets/UI/Prefabs/*.prefab.json から UIScreen Prefab を動的に読み込んで生成する。
    /// UIScreenManager に Prefab を登録する仕組みを提供する。
    /// </summary>
    public class GameScenePrefabLoader : MonoBehaviour
    {
        [Header("Prefab Loader Settings")]
        [Tooltip("prefab.json が配置されているディレクトリパス（Assetsからの相対パス）")]
        [SerializeField] private string prefabJsonPath = "Assets/UI/Prefabs";

        [Header("Target Screens")]
        [Tooltip("読み込む対象の画面名リスト")]
        [SerializeField] private string[] targetScreenNames = new[]
        {
            "TitleScreen",
            "MainMenu",
            "DifficultySelect",
            "SetupScreen",
            "GameOverScreen"
        };

        private Dictionary<UIScreenManager.ScreenType, GameObject> _loadedScreens;

        public Dictionary<UIScreenManager.ScreenType, GameObject> LoadedScreens => _loadedScreens;

        private void Start()
        {
            LoadAndRegisterScreens();
        }

        /// <summary>
        /// 指定された prefab.json ファイルから UIScreen を読み込み、UIScreenManager に登録する。
        /// </summary>
        public void LoadAndRegisterScreens()
        {
            _loadedScreens = new Dictionary<UIScreenManager.ScreenType, GameObject>();

            // prefab.json ファイルを取得
            var jsonFiles = GetPrefabJsonFiles();

            foreach (var screenName in targetScreenNames)
            {
                var filePath = Path.Combine(Application.dataPath, prefabJsonPath, $"{screenName}.prefab.json");

                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[GameScenePrefabLoader] prefab.json not found: {filePath}");
                    continue;
                }

                var jsonContent = File.ReadAllText(filePath);
                var screenPrefab = ParseAndInstantiate(jsonContent, screenName);

                if (screenPrefab == null)
                {
                    Debug.LogWarning($"[GameScenePrefabLoader] Failed to instantiate screen: {screenName}");
                    continue;
                }

                screenPrefab.name = screenName;
                screenPrefab.transform.SetParent(transform);

                var screenType = GetScreenType(screenName);
                _loadedScreens[screenType] = screenPrefab;

                Debug.Log($"[GameScenePrefabLoader] Loaded screen: {screenName} ({screenType})");
            }

            // UIScreenManager に登録
            RegisterToUIScreenManager();
        }

        /// <summary>
        /// prefab.json ファイルのパスリストを取得する。
        /// </summary>
        private List<string> GetPrefabJsonFiles()
        {
            var fullPath = Path.Combine(Application.dataPath, prefabJsonPath);
            if (!Directory.Exists(fullPath))
            {
                Debug.LogWarning($"[GameScenePrefabLoader] Directory not found: {fullPath}");
                return new List<string>();
            }

            return Directory.GetFiles(fullPath, "*.prefab.json")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .ToList();
        }

        /// <summary>
        /// JSON文字列から GameObject をパースして Instantiate する。
        /// 2つのフォーマットに対応:
        ///   1. version: "1.0" フォーマット (prefab.name, prefab.children)
        ///   2. 旧フォーマット (prefabName, components)
        /// </summary>
        private GameObject ParseAndInstantiate(string jsonContent, string defaultName)
        {
            try
            {
                var root = JObject.Parse(jsonContent);

                // フォーマット判別
                if (root["version"] != null && root["prefab"] != null)
                {
                    return InstantiateFromV1Format(root, defaultName);
                }
                else
                {
                    return InstantiateFromLegacyFormat(root, defaultName);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameScenePrefabLoader] Failed to parse JSON: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// version: "1.0" フォーマットから GameObject を生成する。
        /// </summary>
        private GameObject InstantiateFromV1Format(JObject root, string defaultName)
        {
            var prefabData = root["prefab"] as JObject;
            var name = prefabData?["name"]?.ToString() ?? defaultName;
            var screenTypeStr = prefabData?["screenType"]?.ToString() ?? name;

            // ルート GameObject を作成
            var go = new GameObject(name);
            var screen = go.AddComponent<UIScreen>();

            // screenType に応じた具体的なコンポーネントを追加
            AddScreenControllerComponent(go, screenTypeStr);

            // ゲームオブジェクトのコンポーネントを適用
            var gameObjectData = prefabData?["gameObject"] as JObject;
            if (gameObjectData != null)
            {
                ApplyComponents(go, gameObjectData);
            }

            // 子オブジェクトを再帰的に構築
            var children = prefabData?["children"] as JArray;
            if (children != null)
            {
                foreach (var childData in children)
                {
                    var childGo = BuildGameObjectFromJson(childData as JObject);
                    if (childGo != null)
                    {
                        childGo.transform.SetParent(go.transform, false);
                    }
                }
            }

            return go;
        }

        /// <summary>
        /// 旧フォーマットから GameObject を生成する。
        /// </summary>
        private GameObject InstantiateFromLegacyFormat(JObject root, string defaultName)
        {
            var name = root["prefabName"]?.ToString() ?? root["name"]?.ToString() ?? defaultName;
            var components = root["components"] as JArray;

            var go = new GameObject(name);

            if (components != null)
            {
                foreach (var comp in components)
                {
                    var compData = comp as JObject;
                    if (compData == null) continue;

                    var compType = compData["type"]?.ToString();
                    if (compType == null) continue;

                    // Canvas 系は直接追加
                    if (compType == "Canvas")
                    {
                        var canvas = go.AddComponent<Canvas>();
                        var settings = compData["settings"] as JObject;
                        if (settings != null)
                        {
                            var renderMode = settings["renderMode"]?.ToString();
                            // ESC-0114: URenderMode → RenderMode (Unity 2022+)
                            if (Enum.TryParse(renderMode, true, out RenderMode mode))
                            {
                                canvas.renderMode = mode;
                            }
                        }
                        continue;
                    }

                    if (compType == "CanvasScaler")
                    {
                        go.AddComponent<CanvasScaler>();
                        continue;
                    }

                    if (compType == "GraphicRaycaster")
                    {
                        go.AddComponent<GraphicRaycaster>();
                        continue;
                    }

                    // Panel は子オブジェクトとして処理
                    if (compType == "Panel")
                    {
                        var panelGo = BuildPanelFromJson(compData);
                        if (panelGo != null)
                        {
                            panelGo.transform.SetParent(go.transform, false);
                        }
                        continue;
                    }

                    // UIScreen コントローラー
                    if (compType.EndsWith("UI") || compType.EndsWith("Controller"))
                    {
                        AddScreenControllerComponent(go, compType);
                    }
                }
            }

            return go;
        }

        /// <summary>
        /// JSONデータから GameObject を再帰的に構築する。
        /// </summary>
        private GameObject BuildGameObjectFromJson(JObject data)
        {
            if (data == null) return null;

            var name = data["name"]?.ToString() ?? "GameObject";
            var type = data["type"]?.ToString();

            GameObject go;

            // 基本プリミティブから作成
            go = CreateGameObjectFromType(type);
            if (go == null)
            {
                go = new GameObject(name);
            }
            else
            {
                go.name = name;
            }

            // コンポーネントを適用
            var components = data["components"] as JArray;
            if (components != null)
            {
                foreach (var comp in components)
                {
                    var compData = comp as JObject;
                    if (compData == null) continue;
                    ApplyComponentToGameObject(go, compData);
                }
            }

            // 子オブジェクトを再帰的に構築
            var children = data["children"] as JArray;
            if (children != null)
            {
                foreach (var childData in children)
                {
                    var childGo = BuildGameObjectFromJson(childData as JObject);
                    if (childGo != null)
                    {
                        childGo.transform.SetParent(go.transform, false);
                    }
                }
            }

            // 活性状態を設定
            var isActive = data["isActive"];
            if (isActive != null)
            {
                go.SetActive(isActive.Value<bool>());
            }

            return go;
        }

        /// <summary>
        /// 型名から基本的な GameObject を作成する（Image, TextMeshProUGUI, Button 等）。
        /// </summary>
        private GameObject CreateGameObjectFromType(string type)
        {
            if (string.IsNullOrEmpty(type)) return null;

            switch (type)
            {
                case "RectTransform":
                    // RectTransform は GameObject + RectTransform
                    return new GameObject();

                case "Image":
                    var imgGo = new GameObject();
                    imgGo.AddComponent<Image>();
                    return imgGo;

                case "TextMeshProUGUI":
                    var txtGo = new GameObject();
                    txtGo.AddComponent<TMPro.TMP_Text>();
                    return txtGo;

                case "Button":
                    var btnGo = new GameObject();
                    btnGo.AddComponent<Button>();
                    return btnGo;

                case "GameObject":
                    return new GameObject();

                default:
                    return null;
            }
        }

        /// <summary>
        /// Panel 用の GameObject を JSON から構築する。
        /// </summary>
        private GameObject BuildPanelFromJson(JObject data)
        {
            if (data == null) return null;

            var name = data["name"]?.ToString() ?? "Panel";
            var settings = data["settings"] as JObject;

            var go = new GameObject(name);
            var rectTransform = go.AddComponent<RectTransform>();
            var image = go.AddComponent<Image>();

            if (settings != null)
            {
                // カラー設定
                var color = settings["color"] as JObject;
                if (color != null)
                {
                    image.color = new UnityEngine.Color(
                        color["r"]?.Value<float>() ?? 1f,
                        color["g"]?.Value<float>() ?? 1f,
                        color["b"]?.Value<float>() ?? 1f,
                        color["a"]?.Value<float>() ?? 1f
                    );
                }

                // サイズ設定
                var sizeDelta = settings["sizeDelta"] as JObject;
                if (sizeDelta != null)
                {
                    rectTransform.sizeDelta = new UnityEngine.Vector2(
                        sizeDelta["x"]?.Value<float>() ?? 400f,
                        sizeDelta["y"]?.Value<float>() ?? 300f
                    );
                }

                // 位置設定
                var pos = settings["anchoredPosition"] as JObject;
                if (pos != null)
                {
                    rectTransform.anchoredPosition = new UnityEngine.Vector2(
                        pos["x"]?.Value<float>() ?? 0f,
                        pos["y"]?.Value<float>() ?? 0f
                    );
                }
            }

            // 子オブジェクトを処理
            var children = data["children"] as JArray;
            if (children != null)
            {
                foreach (var childData in children)
                {
                    var childGo = BuildChildFromLegacy(childData as JObject);
                    if (childGo != null)
                    {
                        childGo.transform.SetParent(go.transform, false);
                    }
                }
            }

            return go;
        }

        /// <summary>
        /// 旧フォーマットの子オブジェクトを構築する。
        /// </summary>
        private GameObject BuildChildFromLegacy(JObject data)
        {
            if (data == null) return null;

            var name = data["name"]?.ToString() ?? "Child";
            var type = data["type"]?.ToString();
            var settings = data["settings"] as JObject;

            var go = new GameObject(name);

            switch (type)
            {
                case "TextMeshProUGUI":
                    var text = go.AddComponent<TMP_Text>();
                    if (settings != null)
                    {
                        text.text = settings["text"]?.ToString() ?? "";
                        text.fontSize = settings["fontSize"]?.Value<float>() ?? 14f;
                        // ESC-0114: TMP_Text.alignment is TextAlignmentOptions, not TextAnchor
                        text.alignment = (TextAlignmentOptions)(object)ParseTextAlignment(settings["alignment"]?.ToString());
                        var color = settings["color"] as JObject;
                        if (color != null)
                        {
                            text.color = new UnityEngine.Color(
                                color["r"]?.Value<float>() ?? 1f,
                                color["g"]?.Value<float>() ?? 1f,
                                color["b"]?.Value<float>() ?? 1f,
                                color["a"]?.Value<float>() ?? 1f
                            );
                        }
                        var pos = settings["anchoredPosition"] as JObject;
                        if (pos != null)
                        {
                            go.GetComponent<RectTransform>()?.SetLocalPositionAndRotation(
                                new Vector3(pos["x"]?.Value<float>() ?? 0f, pos["y"]?.Value<float>() ?? 0f, 0f),
                                Quaternion.identity);
                        }
                    }
                    break;

                case "InputField":
                    // ESC-0114: InputField is in UnityEngine.UI
                    var input = go.AddComponent<UnityEngine.UI.InputField>();
                    if (settings != null)
                    {
                        var placeholder = settings["placeholder"]?.ToString();
                        if (placeholder != null)
                        {
                            var placeholderText = input.placeholder as TMP_Text;
                            if (placeholderText != null)
                                placeholderText.text = placeholder;
                        }
                        var sizeDelta = settings["sizeDelta"] as JObject;
                        if (sizeDelta != null)
                        {
                            var rt = go.GetComponent<RectTransform>();
                            if (rt != null) rt.sizeDelta = new Vector2(
                                sizeDelta["x"]?.Value<float>() ?? 200f,
                                sizeDelta["y"]?.Value<float>() ?? 40f);
                        }
                    }
                    break;

                case "Button":
                    var btn = go.AddComponent<Button>();
                    if (settings != null)
                    {
                        var btnText = settings["text"]?.ToString();
                        if (btnText != null)
                        {
                            var txtComp = go.AddComponent<TMP_Text>();
                            txtComp.text = btnText;
                        }
                        var sizeDelta = settings["sizeDelta"] as JObject;
                        if (sizeDelta != null)
                        {
                            var rt = go.GetComponent<RectTransform>();
                            if (rt != null) rt.sizeDelta = new Vector2(
                                sizeDelta["x"]?.Value<float>() ?? 200f,
                                sizeDelta["y"]?.Value<float>() ?? 40f);
                        }
                    }
                    break;

                default:
                    // Image デフォルト
                    go.AddComponent<Image>();
                    break;
            }

            return go;
        }

        // ESC-0114: TMP_Text.alignment uses TextAlignmentOptions, not TextAnchor
        private TextAnchor ParseTextAlignment(string alignment)
        {
            if (string.IsNullOrEmpty(alignment)) return TextAnchor.MiddleCenter;

            return alignment.ToLower() switch
            {
                "left" => TextAnchor.LowerLeft,
                "right" => TextAnchor.LowerRight,
                "center" => TextAnchor.MiddleCenter,
                "justify" => TextAnchor.UpperLeft,
                _ => TextAnchor.MiddleCenter
            };
        }

        /// <summary>
        /// JSON コンポーネントデータを GameObject に適用する。
        /// </summary>
        private void ApplyComponentToGameObject(GameObject go, JObject compData)
        {
            var type = compData["type"]?.ToString();
            var properties = compData["properties"] as JObject;

            if (type == null) return;

            switch (type)
            {
                case "RectTransform":
                    ApplyRectTransform(go, properties);
                    break;

                case "Image":
                    ApplyImageComponent(go, properties);
                    break;

                case "TextMeshProUGUI":
                    ApplyTextMeshPro(go, properties);
                    break;

                case "Button":
                    ApplyButtonComponent(go, properties);
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// RectTransform のプロパティを適用する。
        /// </summary>
        private void ApplyRectTransform(GameObject go, JObject properties)
        {
            if (properties == null) return;

            var rt = go.GetComponent<RectTransform>();
            if (rt == null)
            {
                rt = go.AddComponent<RectTransform>();
            }

            // anchoredPosition
            var anchoredPos = properties["anchoredPosition"] as JArray;
            if (anchoredPos != null && anchoredPos.Count >= 2)
            {
                rt.anchoredPosition = new Vector2(anchoredPos[0].Value<float>(), anchoredPos[1].Value<float>());
            }

            // sizeDelta
            var sizeDelta = properties["sizeDelta"] as JArray;
            if (sizeDelta != null && sizeDelta.Count >= 2)
            {
                rt.sizeDelta = new Vector2(sizeDelta[0].Value<float>(), sizeDelta[1].Value<float>());
            }

            // anchorMin
            var anchorMin = properties["anchorMin"] as JArray;
            if (anchorMin != null && anchorMin.Count >= 2)
            {
                rt.anchorMin = new Vector2(anchorMin[0].Value<float>(), anchorMin[1].Value<float>());
            }

            // anchorMax
            var anchorMax = properties["anchorMax"] as JArray;
            if (anchorMax != null && anchorMax.Count >= 2)
            {
                rt.anchorMax = new Vector2(anchorMax[0].Value<float>(), anchorMax[1].Value<float>());
            }

            // pivot
            var pivot = properties["pivot"] as JArray;
            if (pivot != null && pivot.Count >= 2)
            {
                rt.pivot = new Vector2(pivot[0].Value<float>(), pivot[1].Value<float>());
            }
        }

        /// <summary>
        /// Image コンポーネントのプロパティを適用する。
        /// </summary>
        private void ApplyImageComponent(GameObject go, JObject properties)
        {
            var image = go.GetComponent<Image>();
            if (image == null) image = go.AddComponent<Image>();

            if (properties == null) return;

            // color
            var color = properties["color"] as JArray;
            if (color != null && color.Count >= 3)
            {
                image.color = new UnityEngine.Color(
                    color[0].Value<float>(),
                    color[1].Value<float>(),
                    color[2].Value<float>(),
                    color.Count > 3 ? color[3].Value<float>() : 1f
                );
            }

            // raycastTarget
            var raycastTarget = properties["raycastTarget"];
            if (raycastTarget != null)
            {
                image.raycastTarget = raycastTarget.Value<bool>();
            }

            // fillMethod
            var fillMethod = properties["fillMethod"];
            if (fillMethod != null)
            {
                image.fillMethod = (Image.FillMethod)fillMethod.Value<int>();
            }
        }

        /// <summary>
        /// TextMeshProUGUI コンポーネントのプロパティを適用する。
        /// </summary>
        private void ApplyTextMeshPro(GameObject go, JObject properties)
        {
            var text = go.GetComponent<TMPro.TMP_Text>();
            if (text == null) text = go.AddComponent<TMPro.TMP_Text>();

            if (properties == null) return;

            // text
            var textStr = properties["text"]?.ToString();
            if (textStr != null) text.text = textStr;

            // fontSize
            var fontSize = properties["fontSize"];
            if (fontSize != null) text.fontSize = fontSize.Value<float>();

            // color
            var color = properties["color"] as JArray;
            if (color != null && color.Count >= 3)
            {
                text.color = new UnityEngine.Color(
                    color[0].Value<float>(),
                    color[1].Value<float>(),
                    color[2].Value<float>(),
                    color.Count > 3 ? color[3].Value<float>() : 1f
                );
            }

            // alignment
            var alignment = properties["alignment"]?.ToString();
            if (alignment != null)
            {
                // ESC-0114: TMP_Text.alignment is TextAlignmentOptions, not TextAnchor
                text.alignment = (TextAlignmentOptions)(object)ParseTextAlignment(alignment);
            }

            // fontWeight
            var fontWeight = properties["fontWeight"];
            // 修正: このTextMeshPro環境で利用できるFontWeight列挙体を使う。
            if (fontWeight != null) text.fontWeight = (FontWeight)fontWeight.Value<int>();
        }

        /// <summary>
        /// Button コンポーネントのプロパティを適用する。
        /// </summary>
        private void ApplyButtonComponent(GameObject go, JObject properties)
        {
            var button = go.GetComponent<Button>();
            if (button == null) button = go.AddComponent<Button>();

            if (properties == null) return;

            // transition
            var transition = properties["transition"];
            if (transition != null)
            {
                button.transition = (Button.Transition)transition.Value<int>();
            }

            // targetGraphic
            var targetGraphic = properties["targetGraphic"]?.ToString();
            if (targetGraphic != null)
            {
                // Find the image with this name and set as target
                var targetObj = go.transform.Find(targetGraphic);
                if (targetObj != null)
                {
                    var image = targetObj.GetComponent<Image>();
                    if (image != null) button.targetGraphic = image;
                }
            }
        }

        /// <summary>
        /// GameObject にUIScreenサブクラスのコントローラーコンポーネントを追加する。
        /// </summary>
        private void AddScreenControllerComponent(GameObject go, string screenTypeStr)
        {
            var controllerType = screenTypeStr switch
            {
                "TitleScreen" => typeof(TitleScreenController),
                "MainMenu" => typeof(MainMenuController),
                "DifficultySelect" => typeof(DifficultySelectController),
                "SetupScreen" => typeof(SetupScreenController),
                "GameOverScreen" => typeof(GameOverScreenController),
                _ => typeof(UIScreen)
            };

            go.AddComponent(controllerType);
        }

        /// <summary>
        /// 既存のコンポーネントを GameObject に適用する（V1 format の gameObject.components 用）。
        /// </summary>
        private void ApplyComponents(GameObject go, JObject gameObjectData)
        {
            var components = gameObjectData["components"] as JArray;
            if (components == null) return;

            foreach (var comp in components)
            {
                var compData = comp as JObject;
                if (compData == null) continue;

                var type = compData["type"]?.ToString();
                var properties = compData["properties"] as JObject;

                if (type == null) continue;

                // UIScreen 基底は既に追加済み
                if (type == "UIScreen") continue;

                // 具体的なコントローラータイプを適用
                switch (type)
                {
                    case "TitleScreenUI":
                        go.AddComponent<TitleScreenController>();
                        break;
                    case "MainMenuUI":
                        go.AddComponent<MainMenuController>();
                        break;
                    case "DifficultySelectUI":
                        go.AddComponent<DifficultySelectController>();
                        break;
                    case "GameOverScreenUI":
                        go.AddComponent<GameOverScreenController>();
                        break;
                    default:
                        Debug.LogWarning($"[GameScenePrefabLoader] Unknown component type: {type}");
                        break;
                }
            }
        }

        /// <summary>
        /// 画面名から ScreenType を取得する。
        /// </summary>
        private UIScreenManager.ScreenType GetScreenType(string screenName)
        {
            return screenName switch
            {
                "TitleScreen" => UIScreenManager.ScreenType.TitleScreen,
                "MainMenu" => UIScreenManager.ScreenType.MainMenu,
                "DifficultySelect" => UIScreenManager.ScreenType.DifficultySelect,
                "SetupScreen" => UIScreenManager.ScreenType.SetupScreen,
                "GameOverScreen" => UIScreenManager.ScreenType.GameOver,
                _ => UIScreenManager.ScreenType.TitleScreen
            };
        }

        /// <summary>
        /// 読み込んだスクリーンを UIScreenManager に登録する。
        /// </summary>
        private void RegisterToUIScreenManager()
        {
            if (UIScreenManager.Instance == null)
            {
                Debug.LogError("[GameScenePrefabLoader] UIScreenManager not found!");
                return;
            }

            // screenPrefabs を取得（SerializeField なので反射でアクセス）
            var managerField = typeof(UIScreenManager).GetField("screenPrefabs",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);

            if (managerField == null)
            {
                Debug.LogError("[GameScenePrefabLoader] Could not find screenPrefabs field!");
                return;
            }

            var currentPrefabs = managerField.GetValue(UIScreenManager.Instance) as GameObject[];
            if (currentPrefabs == null) currentPrefabs = new GameObject[0];

            // 既存 + 読み込んだ Prefab を結合
            var allPrefabs = new List<GameObject>(currentPrefabs);
            foreach (var kvp in _loadedScreens)
            {
                if (!allPrefabs.Contains(kvp.Value))
                {
                    allPrefabs.Add(kvp.Value);
                }
            }

            // 配列を再設定
            managerField.SetValue(UIScreenManager.Instance, allPrefabs.ToArray());

            Debug.Log($"[GameScenePrefabLoader] Registered {_loadedScreens.Count} screens to UIScreenManager.");
        }
    }
}
