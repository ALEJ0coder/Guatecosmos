using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LunarOutpost.MiniGames.Sokoban
{
    /// <summary>Acciones que la interfaz necesita poder disparar; las define SokobanMiniGame.</summary>
    public sealed class SokobanUICallbacks
    {
        public Action<Vector2Int> RequestDirection;
        public Action Undo;
        public Action Reset;
        public Action StartOrRetryMission;
        public Action Pause;
        public Action Resume;
        public Action ExitToMenu;
    }

    /// <summary>
    /// Construye toda la interfaz (Canvas, HUD, tablero, controles, panel de pausa y
    /// panel de victoria) usando solo UI de Unity: nada de sprites ni prefabs externos.
    /// SokobanMiniGame la crea una vez y luego solo llama a los métodos "Refresh*"/"Show*".
    /// </summary>
    public sealed class SokobanUIController : MonoBehaviour
    {
        [Header("Colores del tablero")]
        [SerializeField] private Color floorColor = new Color(0.16f, 0.22f, 0.30f);
        [SerializeField] private Color wallColor = new Color(0.07f, 0.11f, 0.17f);
        [SerializeField] private Color goalColor = new Color(0.75f, 0.52f, 0.18f);
        [SerializeField] private Color batteryColor = new Color(0.16f, 0.55f, 0.78f);
        [SerializeField] private Color playerColor = new Color(0.20f, 0.70f, 0.56f);
        [SerializeField] private Color connectedColor = new Color(0.20f, 0.62f, 0.38f);

        public static readonly string[] EducationalMessages =
        {
            "Las baterías guardan electricidad para cuando los paneles solares no generan suficiente.",
            "Una base espacial reparte su energía entre luces, comunicaciones, calefacción y soporte vital.",
            "La energía es limitada: recuperarla es un problema de almacenamiento y distribución, no magia.",
        };

        private int boardWidth;
        private int boardHeight;

        private Slider powerSlider;
        private Slider restSlider;
        private TMP_Text powerValueText;
        private TMP_Text restValueText;
        private TMP_Text boardInfoText;
        private TMP_Text statusText;
        private TMP_Text scienceTipText;
        private TMP_Text missionButtonText;
        private Image[,] tileImages;
        private TMP_Text[,] tileTexts;
        private Button undoButton;
        private Button resetButton;
        private Button missionButton;

        private GameObject pausePanel;
        private GameObject victoryPanel;
        private TMP_Text victoryRewardText;
        private TMP_Text victoryExplanationText;

        private int scienceTipIndex;

        public void BuildInterface(int width, int height, SokobanUICallbacks callbacks)
        {
            boardWidth = width;
            boardHeight = height;

            GameObject canvasObject = new GameObject("SokobanCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            CreateImage("Fondo", canvasObject.transform, new Color(0.025f, 0.045f, 0.08f), Vector2.zero, Vector2.zero, true);

            CreateText("Titulo", canvasObject.transform, "BATTERY GRID",
                new Vector2(0f, 480f), new Vector2(1120f, 56f), 36, TextAlignmentOptions.Center, Color.white);
            CreateText("Subtitulo", canvasObject.transform, "Restore the Outpost Power",
                new Vector2(0f, 438f), new Vector2(1160f, 36f), 20, TextAlignmentOptions.Center, new Color(0.45f, 0.85f, 1f));
            CreateText("Instrucciones", canvasObject.transform,
                "Empuja las dos baterías hasta las ranuras doradas para recuperar la energía eléctrica.",
                new Vector2(0f, 402f), new Vector2(1160f, 32f), 17, TextAlignmentOptions.Center, new Color(0.76f, 0.85f, 0.94f));

            CreateText("PowerLabel", canvasObject.transform, "POWER",
                new Vector2(-360f, 364f), new Vector2(340f, 26f), 16, TextAlignmentOptions.Center, new Color(0.45f, 0.85f, 1f));
            powerSlider = CreateSlider(canvasObject.transform, "PowerSlider", new Vector2(-360f, 334f), new Color(0.20f, 0.72f, 0.94f));
            powerValueText = CreateText("PowerValue", canvasObject.transform, "0 / 100",
                new Vector2(-360f, 308f), new Vector2(340f, 22f), 14, TextAlignmentOptions.Center, Color.white);

            CreateText("RestLabel", canvasObject.transform, "ASTRONAUT ENERGY",
                new Vector2(360f, 364f), new Vector2(340f, 26f), 16, TextAlignmentOptions.Center, new Color(0.55f, 0.92f, 0.69f));
            restSlider = CreateSlider(canvasObject.transform, "RestSlider", new Vector2(360f, 334f), new Color(0.25f, 0.78f, 0.48f));
            restValueText = CreateText("RestValue", canvasObject.transform, "0 / 100",
                new Vector2(360f, 308f), new Vector2(340f, 22f), 14, TextAlignmentOptions.Center, Color.white);

            BuildBoard(canvasObject.transform);

            boardInfoText = CreateText("BoardInfo", canvasObject.transform, "BATERÍAS CONECTADAS: 0 / 2   MOVIMIENTOS: 0",
                new Vector2(0f, -300f), new Vector2(900f, 30f), 18, TextAlignmentOptions.Center, new Color(0.95f, 0.82f, 0.48f));
            statusText = CreateText("Status", canvasObject.transform, "Preparando la misión...",
                new Vector2(0f, -334f), new Vector2(1160f, 34f), 18, TextAlignmentOptions.Center, Color.white);

            CreateButton(canvasObject.transform, "↑", new Vector2(-132f, -392f), new Vector2(76f, 54f),
                () => callbacks.RequestDirection?.Invoke(Vector2Int.up), new Color(0.18f, 0.35f, 0.50f));
            CreateButton(canvasObject.transform, "←", new Vector2(-44f, -392f), new Vector2(76f, 54f),
                () => callbacks.RequestDirection?.Invoke(Vector2Int.left), new Color(0.18f, 0.35f, 0.50f));
            CreateButton(canvasObject.transform, "↓", new Vector2(44f, -392f), new Vector2(76f, 54f),
                () => callbacks.RequestDirection?.Invoke(Vector2Int.down), new Color(0.18f, 0.35f, 0.50f));
            CreateButton(canvasObject.transform, "→", new Vector2(132f, -392f), new Vector2(76f, 54f),
                () => callbacks.RequestDirection?.Invoke(Vector2Int.right), new Color(0.18f, 0.35f, 0.50f));

            undoButton = CreateButton(canvasObject.transform, "UNDO", new Vector2(-320f, -392f), new Vector2(160f, 52f),
                () => callbacks.Undo?.Invoke(), new Color(0.28f, 0.36f, 0.48f));
            resetButton = CreateButton(canvasObject.transform, "RESTART", new Vector2(-150f, -460f), new Vector2(170f, 50f),
                () => callbacks.Reset?.Invoke(), new Color(0.48f, 0.34f, 0.20f));
            missionButton = CreateButton(canvasObject.transform, "START MISSION", new Vector2(30f, -460f), new Vector2(210f, 50f),
                () => callbacks.StartOrRetryMission?.Invoke(), new Color(0.12f, 0.48f, 0.36f));
            missionButtonText = missionButton.GetComponentInChildren<TMP_Text>();

            CreateButton(canvasObject.transform, "PAUSE", new Vector2(270f, -460f), new Vector2(140f, 50f),
                () => callbacks.Pause?.Invoke(), new Color(0.30f, 0.30f, 0.34f));
            CreateButton(canvasObject.transform, "EXIT", new Vector2(430f, -460f), new Vector2(130f, 50f),
                () => callbacks.ExitToMenu?.Invoke(), new Color(0.55f, 0.20f, 0.20f));

            scienceTipText = CreateText("ScienceNote", canvasObject.transform, EducationalMessages[0],
                new Vector2(0f, -515f), new Vector2(1220f, 30f), 14, TextAlignmentOptions.Center, new Color(0.65f, 0.76f, 0.86f));

            BuildPausePanel(canvasObject.transform, callbacks);
            BuildVictoryPanel(canvasObject.transform, callbacks);

            if (TMP_Settings.defaultFontAsset == null)
                Debug.LogWarning("Sokoban: importa TMP Essential Resources (Window > TextMeshPro) para ver las fuentes correctamente.");
        }

        // ---------------------------------------------------------------
        // Tablero
        // ---------------------------------------------------------------

        private void BuildBoard(Transform parent)
        {
            GameObject boardObject = CreateUIObject("Board", parent, typeof(Image), typeof(GridLayoutGroup));
            RectTransform boardRect = boardObject.GetComponent<RectTransform>();
            SetRect(boardRect, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(548f, 548f));

            Image background = boardObject.GetComponent<Image>();
            background.color = new Color(0.07f, 0.12f, 0.19f);
            background.raycastTarget = false;

            GridLayoutGroup grid = boardObject.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(62f, 62f);
            grid.spacing = new Vector2(4f, 4f);
            grid.padding = new RectOffset(5, 5, 5, 5);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = boardWidth;

            tileImages = new Image[boardWidth, boardHeight];
            tileTexts = new TMP_Text[boardWidth, boardHeight];

            for (int row = 0; row < boardHeight; row++)
            {
                int y = boardHeight - 1 - row;
                for (int x = 0; x < boardWidth; x++)
                {
                    GameObject tile = CreateUIObject($"Tile_{x}_{y}", boardObject.transform, typeof(Image));
                    Image tileImage = tile.GetComponent<Image>();
                    tileImage.color = floorColor;
                    tileImage.raycastTarget = false;
                    tileImages[x, y] = tileImage;

                    TMP_Text tileText = CreateText($"TileLabel_{x}_{y}", tile.transform, "",
                        Vector2.zero, Vector2.zero, 13, TextAlignmentOptions.Center, Color.white);
                    tileText.rectTransform.anchorMin = Vector2.zero;
                    tileText.rectTransform.anchorMax = Vector2.one;
                    tileText.rectTransform.offsetMin = Vector2.zero;
                    tileText.rectTransform.offsetMax = Vector2.zero;
                    tileTexts[x, y] = tileText;
                }
            }
        }

        public void RefreshBoard(SokobanBoard board)
        {
            if (tileImages == null) return;

            for (int y = 0; y < boardHeight; y++)
            {
                for (int x = 0; x < boardWidth; x++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    Image image = tileImages[x, y];
                    TMP_Text label = tileTexts[x, y];

                    if (board.IsWall(position))
                    {
                        image.color = wallColor;
                        label.text = "";
                        continue;
                    }

                    int batteryIndex = board.BatteryIndexAt(position);
                    if (batteryIndex >= 0)
                    {
                        bool connected = board.Batteries[batteryIndex].IsConnected;
                        image.color = connected ? connectedColor : batteryColor;
                        label.text = connected ? "BATTERY\n✓" : "BATTERY";
                        label.color = Color.white;
                    }
                    else if (board.PlayerPosition == position)
                    {
                        image.color = playerColor;
                        label.text = board.IsGoal(position) ? "ASTRO\n✓" : "ASTRO";
                        label.color = Color.white;
                    }
                    else if (board.IsGoal(position))
                    {
                        image.color = goalColor;
                        label.text = "SLOT";
                        label.color = Color.white;
                    }
                    else
                    {
                        image.color = floorColor;
                        label.text = "";
                    }
                }
            }
        }

        // ---------------------------------------------------------------
        // HUD
        // ---------------------------------------------------------------

        public void RefreshHud(float power, float rest, float restCost, int connected, int required, int moveCount)
        {
            if (powerSlider != null) powerSlider.SetValueWithoutNotify(power);
            if (restSlider != null) restSlider.SetValueWithoutNotify(rest);
            if (powerValueText != null) powerValueText.text = $"{power:0} / 100";
            if (restValueText != null) restValueText.text = $"{rest:0} / 100  (-{restCost:0} al iniciar)";
            if (boardInfoText != null)
                boardInfoText.text = $"BATERÍAS CONECTADAS: {connected} / {required}   MOVIMIENTOS: {moveCount}";
        }

        public void SetStatus(string message, bool positive)
        {
            if (statusText == null) return;
            statusText.text = message;
            statusText.color = positive ? new Color(0.45f, 1f, 0.67f) : Color.white;
        }

        public void SetControlsInteractable(bool isPlaying, bool canUndo, bool missionButtonEnabled, bool hasWon)
        {
            if (undoButton != null) undoButton.interactable = isPlaying && canUndo;
            if (resetButton != null) resetButton.interactable = isPlaying;
            if (missionButton != null)
            {
                missionButton.interactable = missionButtonEnabled;
                if (missionButtonText != null)
                    missionButtonText.text = hasWon ? "RETRY (-10 REST)" : "START MISSION";
            }
        }

        /// <summary>Cambia el mensaje científico visible bajo el tablero; se llama al empezar cada misión.</summary>
        public void ShowNextScienceTip()
        {
            scienceTipIndex = (scienceTipIndex + 1) % EducationalMessages.Length;
            if (scienceTipText != null)
                scienceTipText.text = EducationalMessages[scienceTipIndex];
        }

        // ---------------------------------------------------------------
        // Panel de pausa
        // ---------------------------------------------------------------

        private void BuildPausePanel(Transform parent, SokobanUICallbacks callbacks)
        {
            pausePanel = CreateUIObject("PausePanel", parent, typeof(Image));
            RectTransform rect = pausePanel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            pausePanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

            CreateText("PauseTitle", pausePanel.transform, "PAUSA",
                new Vector2(0f, 60f), new Vector2(500f, 60f), 34, TextAlignmentOptions.Center, Color.white);
            CreateButton(pausePanel.transform, "CONTINUAR", new Vector2(0f, -20f), new Vector2(260f, 56f),
                () => callbacks.Resume?.Invoke(), new Color(0.12f, 0.48f, 0.36f));
            CreateButton(pausePanel.transform, "SALIR", new Vector2(0f, -90f), new Vector2(260f, 56f),
                () => callbacks.ExitToMenu?.Invoke(), new Color(0.55f, 0.20f, 0.20f));

            pausePanel.SetActive(false);
        }

        public void ShowPausePanel() => pausePanel?.SetActive(true);
        public void HidePausePanel() => pausePanel?.SetActive(false);

        // ---------------------------------------------------------------
        // Panel de victoria
        // ---------------------------------------------------------------

        private void BuildVictoryPanel(Transform parent, SokobanUICallbacks callbacks)
        {
            victoryPanel = CreateUIObject("VictoryPanel", parent, typeof(Image));
            RectTransform rect = victoryPanel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            victoryPanel.GetComponent<Image>().color = new Color(0.02f, 0.08f, 0.06f, 0.92f);

            CreateText("VictoryTitle", victoryPanel.transform, "¡ENERGÍA RESTAURADA!",
                new Vector2(0f, 160f), new Vector2(900f, 60f), 32, TextAlignmentOptions.Center, new Color(0.45f, 1f, 0.67f));

            victoryExplanationText = CreateText("VictoryExplanation", victoryPanel.transform, "",
                new Vector2(0f, 90f), new Vector2(900f, 70f), 18, TextAlignmentOptions.Center, new Color(0.85f, 0.92f, 0.96f));

            victoryRewardText = CreateText("VictoryReward", victoryPanel.transform, "",
                new Vector2(0f, 30f), new Vector2(700f, 36f), 20, TextAlignmentOptions.Center, new Color(0.95f, 0.82f, 0.48f));

            CreateButton(victoryPanel.transform, "INTENTAR DE NUEVO", new Vector2(-140f, -60f), new Vector2(260f, 56f),
                () => callbacks.StartOrRetryMission?.Invoke(), new Color(0.12f, 0.48f, 0.36f));
            CreateButton(victoryPanel.transform, "VOLVER AL MENÚ", new Vector2(140f, -60f), new Vector2(260f, 56f),
                () => callbacks.ExitToMenu?.Invoke(), new Color(0.28f, 0.36f, 0.48f));

            victoryPanel.SetActive(false);
        }

        public void ShowVictoryPanel(float rewardGained, string educationalMessage)
        {
            if (victoryExplanationText != null) victoryExplanationText.text = educationalMessage;
            if (victoryRewardText != null) victoryRewardText.text = $"Recompensa obtenida: +{rewardGained:0} de Power";
            victoryPanel?.SetActive(true);
        }

        public void HideVictoryPanel() => victoryPanel?.SetActive(false);

        // ---------------------------------------------------------------
        // Utilidades de construcción de UI
        // ---------------------------------------------------------------

        private Slider CreateSlider(Transform parent, string name, Vector2 position, Color fillColor)
        {
            GameObject sliderObject = CreateUIObject(name, parent, typeof(Image), typeof(Slider));
            SetRect(sliderObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), position, new Vector2(360f, 18f));

            Image background = sliderObject.GetComponent<Image>();
            background.color = new Color(0.12f, 0.18f, 0.24f);
            background.raycastTarget = false;

            GameObject fillObject = CreateUIObject("Fill", sliderObject.transform, typeof(Image));
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            fillObject.GetComponent<Image>().color = fillColor;
            fillObject.GetComponent<Image>().raycastTarget = false;

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = true;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.fillRect = fillRect;
            slider.targetGraphic = background;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private Button CreateButton(Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, Color color)
        {
            GameObject buttonObject = CreateUIObject("Button_" + label, parent, typeof(Image), typeof(Button));
            SetRect(buttonObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), position, size);

            Image image = buttonObject.GetComponent<Image>();
            image.color = color;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(action);

            TMP_Text buttonText = CreateText("Label", buttonObject.transform, label, Vector2.zero, size, 18, TextAlignmentOptions.Center, Color.white);
            buttonText.rectTransform.anchorMin = Vector2.zero;
            buttonText.rectTransform.anchorMax = Vector2.one;
            buttonText.rectTransform.offsetMin = Vector2.zero;
            buttonText.rectTransform.offsetMax = Vector2.zero;
            buttonText.raycastTarget = false;
            return button;
        }

        private TMP_Text CreateText(string name, Transform parent, string value, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            GameObject textObject = CreateUIObject(name, parent, typeof(TextMeshProUGUI));
            TMP_Text text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            text.font = TMP_Settings.defaultFontAsset;
            SetRect(text.rectTransform, new Vector2(0.5f, 0.5f), position, size);
            return text;
        }

        private Image CreateImage(string name, Transform parent, Color color, Vector2 position, Vector2 size, bool stretch)
        {
            GameObject imageObject = CreateUIObject(name, parent, typeof(Image));
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;

            if (stretch)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                SetRect(rect, new Vector2(0.5f, 0.5f), position, size);
            }
            return image;
        }

        private GameObject CreateUIObject(string name, Transform parent, params Type[] components)
        {
            GameObject result = new GameObject(name, components);
            result.transform.SetParent(parent, false);
            return result;
        }

        private void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
