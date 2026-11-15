using System.Collections.Generic;
using LunarOutpost.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LunarOutpost.MiniGames.Sokoban
{
    /// <summary>
    /// Sokoban de baterías para Guatecosmos.
    /// El astronauta mueve cajas de baterías hasta las ranuras de energía.
    /// El tablero y la interfaz se crean automáticamente; no hacen falta prefabs.
    /// </summary>
    public sealed class SokobanMiniGame : MonoBehaviour
    {
        private const int BoardWidth = 8;
        private const int BoardHeight = 8;
        private const int RequiredBatteries = 2;

        [Header("Costos y recompensas (ajustables en el Inspector)")]
        [SerializeField, Range(0f, 100f)] private float restCost = 10f;
        [SerializeField, Range(0f, 100f)] private float powerReward = 25f;

        [Header("Colores del tablero")]
        [SerializeField] private Color floorColor = new Color(0.16f, 0.22f, 0.30f);
        [SerializeField] private Color wallColor = new Color(0.07f, 0.11f, 0.17f);
        [SerializeField] private Color goalColor = new Color(0.75f, 0.52f, 0.18f);
        [SerializeField] private Color crateColor = new Color(0.16f, 0.55f, 0.78f);
        [SerializeField] private Color playerColor = new Color(0.20f, 0.70f, 0.56f);
        [SerializeField] private Color solvedColor = new Color(0.20f, 0.62f, 0.38f);

        // El tablero usa coordenadas (x,y), con (0,0) en la esquina inferior izquierda.
        private readonly HashSet<Vector2Int> walls = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> goals = new HashSet<Vector2Int>();
        private readonly List<Vector2Int> crates = new List<Vector2Int>();
        private readonly Stack<PuzzleSnapshot> history = new Stack<PuzzleSnapshot>();

        private Vector2Int playerPosition;
        private int moveCount;
        private bool isPlaying;
        private bool hasWon;
        private bool statusIsPositive;
        private string statusMessage = "Preparando la misión...";

        // Elementos visuales creados desde el código.
        private Slider powerSlider;
        private Slider restSlider;
        private TMP_Text powerValueText;
        private TMP_Text restValueText;
        private TMP_Text boardInfoText;
        private TMP_Text statusText;
        private TMP_Text missionButtonText;
        private Image[,] tileImages;
        private TMP_Text[,] tileTexts;
        private Button undoButton;
        private Button resetButton;
        private Button missionButton;

        private struct PuzzleSnapshot
        {
            public Vector2Int player;
            public List<Vector2Int> crates;
            public int moves;
        }

        private void Start()
        {
            EnsureCoreServices();
            EnsureEventSystem();
            BuildUserInterface();
            CreateLevel();
            BeginNewAttempt();
            RefreshInterface();
        }

        private void Update()
        {
            if (!isPlaying || Keyboard.current == null)
            {
                return;
            }

            // El proyecto usa el Input System nuevo, por eso leemos Keyboard.current.
            if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
                MovePlayer(Vector2Int.up);
            else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
                MovePlayer(Vector2Int.right);
            else if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
                MovePlayer(Vector2Int.down);
            else if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
                MovePlayer(Vector2Int.left);
        }

        /// <summary>Permite abrir la escena directamente, incluso si Core aún no está en ella.</summary>
        private void EnsureCoreServices()
        {
            if (ResourceManager.Instance == null)
                new GameObject("ResourceManager").AddComponent<ResourceManager>();

            if (GameManager.Instance == null)
                new GameObject("GameManager").AddComponent<GameManager>();
        }

        /// <summary>El proyecto usa Input System; la UI necesita su módulo de eventos.</summary>
        private void EnsureEventSystem()
        {
            EventSystem eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem == null)
                eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();

            BaseInputModule currentModule = eventSystem.GetComponent<BaseInputModule>();
            if (!(currentModule is InputSystemUIInputModule))
            {
                if (currentModule != null)
                    Destroy(currentModule);

                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        /// <summary>Construye un Canvas con textos TMP, barras Slider y botones.</summary>
        private void BuildUserInterface()
        {
            GameObject canvasObject = new GameObject("SokobanCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            CreateImage("Fondo", canvasObject.transform, ColorValue(0.025f, 0.045f, 0.08f),
                Vector2.zero, Vector2.zero, true);

            CreateText("Titulo", canvasObject.transform, "GUATECOSMOS | MISIÓN DE BATERÍAS",
                new Vector2(0f, -48f), new Vector2(1120f, 58f), 34,
                TextAlignmentOptions.Center, Color.white, true);

            CreateText("Subtitulo", canvasObject.transform,
                "Lleva las dos baterías hasta sus ranuras para recuperar energía eléctrica.",
                new Vector2(0f, -105f), new Vector2(1160f, 48f), 22,
                TextAlignmentOptions.Center, new Color(0.76f, 0.85f, 0.94f), true);

            CreateText("PowerLabel", canvasObject.transform, "ENERGÍA ELÉCTRICA",
                new Vector2(-250f, -155f), new Vector2(420f, 30f), 18,
                TextAlignmentOptions.Center, new Color(0.45f, 0.85f, 1f), true);
            powerSlider = CreateSlider(canvasObject.transform, "PowerSlider",
                new Vector2(-250f, -190f), new Color(0.20f, 0.72f, 0.94f));
            powerValueText = CreateText("PowerValue", canvasObject.transform, "60 / 100",
                new Vector2(-250f, -218f), new Vector2(420f, 24f), 16,
                TextAlignmentOptions.Center, Color.white, true);

            CreateText("RestLabel", canvasObject.transform, "DESCANSO DEL ASTRONAUTA",
                new Vector2(250f, -155f), new Vector2(420f, 30f), 18,
                TextAlignmentOptions.Center, new Color(0.55f, 0.92f, 0.69f), true);
            restSlider = CreateSlider(canvasObject.transform, "RestSlider",
                new Vector2(250f, -190f), new Color(0.25f, 0.78f, 0.48f));
            restValueText = CreateText("RestValue", canvasObject.transform, "60 / 100",
                new Vector2(250f, -218f), new Vector2(420f, 24f), 16,
                TextAlignmentOptions.Center, Color.white, true);

            BuildBoard(canvasObject.transform);

            boardInfoText = CreateText("BoardInfo", canvasObject.transform, "BATERÍAS EN RANURA: 0 / 2     MOVIMIENTOS: 0",
                new Vector2(0f, -316f), new Vector2(900f, 34f), 19,
                TextAlignmentOptions.Center, new Color(0.95f, 0.82f, 0.48f), true);

            statusText = CreateText("Status", canvasObject.transform, statusMessage,
                new Vector2(0f, -352f), new Vector2(1160f, 38f), 19,
                TextAlignmentOptions.Center, Color.white, true);

            // Controles visibles para quien no conozca las teclas.
            CreateButton(canvasObject.transform, "↑", new Vector2(-132f, -405f),
                new Vector2(76f, 54f), () => MovePlayer(Vector2Int.up), new Color(0.18f, 0.35f, 0.50f));
            CreateButton(canvasObject.transform, "←", new Vector2(-44f, -405f),
                new Vector2(76f, 54f), () => MovePlayer(Vector2Int.left), new Color(0.18f, 0.35f, 0.50f));
            CreateButton(canvasObject.transform, "↓", new Vector2(44f, -405f),
                new Vector2(76f, 54f), () => MovePlayer(Vector2Int.down), new Color(0.18f, 0.35f, 0.50f));
            CreateButton(canvasObject.transform, "→", new Vector2(132f, -405f),
                new Vector2(76f, 54f), () => MovePlayer(Vector2Int.right), new Color(0.18f, 0.35f, 0.50f));

            undoButton = CreateButton(canvasObject.transform, "DESHACER",
                new Vector2(-230f, -470f), new Vector2(190f, 52f), UndoMove,
                new Color(0.28f, 0.36f, 0.48f));
            resetButton = CreateButton(canvasObject.transform, "REINICIAR TABLERO",
                new Vector2(0f, -470f), new Vector2(220f, 52f), ResetPuzzle,
                new Color(0.48f, 0.34f, 0.20f));
            missionButton = CreateButton(canvasObject.transform, "NUEVA MISIÓN",
                new Vector2(250f, -470f), new Vector2(220f, 52f), BeginNewAttempt,
                new Color(0.12f, 0.48f, 0.36f));
            missionButtonText = missionButton.GetComponentInChildren<TMP_Text>();
            missionButton.interactable = false;

            CreateText("ScienceNote", canvasObject.transform,
                "CIENCIA: en microgravedad se usan pasamanos y anclajes para estabilizarse. Aquí las baterías viajan por guías de carga.",
                new Vector2(0f, -515f), new Vector2(1220f, 30f), 14,
                TextAlignmentOptions.Center, new Color(0.65f, 0.76f, 0.86f), true);

            if (TMP_Settings.defaultFontAsset == null)
            {
                Debug.LogWarning("Sokoban: importa TMP Essential Resources desde Window > TextMeshPro para mostrar las fuentes correctamente.");
            }
        }

        private void BuildBoard(Transform parent)
        {
            GameObject boardObject = CreateUIObject("Board", parent, typeof(Image), typeof(GridLayoutGroup));
            RectTransform boardRect = boardObject.GetComponent<RectTransform>();
            SetRect(boardRect, new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(548f, 548f));

            Image boardBackground = boardObject.GetComponent<Image>();
            boardBackground.color = new Color(0.07f, 0.12f, 0.19f, 1f);
            boardBackground.raycastTarget = false;

            GridLayoutGroup grid = boardObject.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(62f, 62f);
            grid.spacing = new Vector2(4f, 4f);
            grid.padding = new RectOffset(5, 5, 5, 5);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = BoardWidth;

            tileImages = new Image[BoardWidth, BoardHeight];
            tileTexts = new TMP_Text[BoardWidth, BoardHeight];

            // La cuadrícula visual se crea de arriba hacia abajo, pero el tablero usa y=0 abajo.
            for (int row = 0; row < BoardHeight; row++)
            {
                int y = BoardHeight - 1 - row;
                for (int x = 0; x < BoardWidth; x++)
                {
                    GameObject tile = CreateUIObject("Tile_" + x + "_" + y,
                        boardObject.transform, typeof(Image));
                    Image tileImage = tile.GetComponent<Image>();
                    tileImage.color = floorColor;
                    tileImage.raycastTarget = false;
                    tileImages[x, y] = tileImage;

                    TMP_Text tileText = CreateText("TileLabel_" + x + "_" + y,
                        tile.transform, "", Vector2.zero, Vector2.zero, 13,
                        TextAlignmentOptions.Center, Color.white, false);
                    RectTransform textRect = tileText.rectTransform;
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = Vector2.zero;
                    textRect.offsetMax = Vector2.zero;
                    tileTexts[x, y] = tileText;
                }
            }
        }

        private Slider CreateSlider(Transform parent, string name, Vector2 position, Color fillColor)
        {
            GameObject sliderObject = CreateUIObject(name, parent, typeof(Image), typeof(Slider));
            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            SetRect(sliderRect, new Vector2(0.5f, 0.5f), position, new Vector2(360f, 18f));

            Image background = sliderObject.GetComponent<Image>();
            background.color = new Color(0.12f, 0.18f, 0.24f);
            background.raycastTarget = false;

            GameObject fillObject = CreateUIObject("Fill", sliderObject.transform, typeof(Image));
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            Image fillImage = fillObject.GetComponent<Image>();
            fillImage.color = fillColor;
            fillImage.raycastTarget = false;

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

        private Button CreateButton(Transform parent, string label, Vector2 position,
            Vector2 size, UnityEngine.Events.UnityAction action, Color color)
        {
            GameObject buttonObject = CreateUIObject("Button_" + label,
                parent, typeof(Image), typeof(Button));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.5f), position, size);

            Image image = buttonObject.GetComponent<Image>();
            image.color = color;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(action);

            TMP_Text buttonText = CreateText("Label", buttonObject.transform, label,
                Vector2.zero, size, 18, TextAlignmentOptions.Center, Color.white, false);
            buttonText.rectTransform.anchorMin = Vector2.zero;
            buttonText.rectTransform.anchorMax = Vector2.one;
            buttonText.rectTransform.offsetMin = Vector2.zero;
            buttonText.rectTransform.offsetMax = Vector2.zero;
            buttonText.raycastTarget = false;
            return button;
        }

        private TMP_Text CreateText(string name, Transform parent, string value,
            Vector2 position, Vector2 size, float fontSize,
            TextAlignmentOptions alignment, Color color, bool centered)
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

            RectTransform rect = text.rectTransform;
            if (centered)
                SetRect(rect, new Vector2(0.5f, 0.5f), position, size);
            else
                SetRect(rect, new Vector2(0.5f, 0.5f), position, size);

            return text;
        }

        private Image CreateImage(string name, Transform parent, Color color,
            Vector2 position, Vector2 size, bool stretch)
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

        private GameObject CreateUIObject(string name, Transform parent, params System.Type[] components)
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

        private Color ColorValue(float red, float green, float blue)
        {
            return new Color(red, green, blue, 1f);
        }

        /// <summary>Prepara los muros, las dos baterías y las dos ranuras de energía.</summary>
        private void CreateLevel()
        {
            walls.Clear();
            goals.Clear();
            crates.Clear();

            for (int x = 0; x < BoardWidth; x++)
            {
                walls.Add(new Vector2Int(x, 0));
                walls.Add(new Vector2Int(x, BoardHeight - 1));
            }
            for (int y = 0; y < BoardHeight; y++)
            {
                walls.Add(new Vector2Int(0, y));
                walls.Add(new Vector2Int(BoardWidth - 1, y));
            }

            // Un bloque lateral crea un pequeño reto de navegación sin bloquear la solución.
            walls.Add(new Vector2Int(2, 6));

            playerPosition = new Vector2Int(1, 1);
            crates.Add(new Vector2Int(2, 4));
            crates.Add(new Vector2Int(3, 4));
            goals.Add(new Vector2Int(2, 2));
            goals.Add(new Vector2Int(3, 2));
            // Solución de prueba: ↑↑↑↑→↓↓↑↑→↓↓
            moveCount = 0;
            history.Clear();
        }

        /// <summary>Comenzar una misión cuesta descanso; repetir una misión también tiene costo.</summary>
        private void BeginNewAttempt()
        {
            if (isPlaying)
                return;

            EnsureCoreServices();
            float currentRest = ResourceManager.Instance.Get(ResourceType.Rest);
            if (currentRest < restCost)
            {
                statusIsPositive = false;
                statusMessage = "Necesitas " + restCost.ToString("0") + " de descanso. Tienes " + currentRest.ToString("0") + ". Vuelve a descansar y prueba otra vez.";
                RefreshInterface();
                return;
            }

            ResourceManager.Instance.Change(ResourceType.Rest, -restCost);
            isPlaying = true;
            hasWon = false;
            ResetPuzzleState();
            statusIsPositive = false;
            statusMessage = "¡Misión iniciada! Mueve al astronauta y coloca las dos baterías en las ranuras.";
            RefreshInterface();
        }

        /// <summary>Reiniciar el tablero no vuelve a cobrar descanso porque sigue siendo la misma misión.</summary>
        private void ResetPuzzle()
        {
            if (!isPlaying)
                return;

            ResetPuzzleState();
            statusIsPositive = false;
            statusMessage = "Tablero reiniciado. No se cobra descanso extra en esta misma misión.";
            RefreshInterface();
        }

        private void ResetPuzzleState()
        {
            playerPosition = new Vector2Int(1, 1);
            crates.Clear();
            crates.Add(new Vector2Int(2, 4));
            crates.Add(new Vector2Int(3, 4));
            moveCount = 0;
            history.Clear();
        }

        private void MovePlayer(Vector2Int direction)
        {
            if (!isPlaying)
                return;

            Vector2Int nextPosition = playerPosition + direction;
            if (!IsInsideBoard(nextPosition) || walls.Contains(nextPosition))
                return;

            int crateIndex = crates.IndexOf(nextPosition);
            if (crateIndex >= 0)
            {
                // Solo se empuja una batería si la casilla siguiente está libre.
                Vector2Int crateDestination = nextPosition + direction;
                if (!IsInsideBoard(crateDestination) || walls.Contains(crateDestination) || crates.Contains(crateDestination))
                    return;

                SaveSnapshot();
                crates[crateIndex] = crateDestination;
            }
            else
            {
                SaveSnapshot();
            }

            playerPosition = nextPosition;
            moveCount++;

            if (IsPuzzleSolved())
                CompleteMission();

            RefreshInterface();
        }

        private bool IsInsideBoard(Vector2Int position)
        {
            return position.x >= 0 && position.x < BoardWidth &&
                   position.y >= 0 && position.y < BoardHeight;
        }

        private void SaveSnapshot()
        {
            PuzzleSnapshot snapshot = new PuzzleSnapshot
            {
                player = playerPosition,
                crates = new List<Vector2Int>(crates),
                moves = moveCount
            };
            history.Push(snapshot);
        }

        private void UndoMove()
        {
            if (!isPlaying || history.Count == 0)
                return;

            PuzzleSnapshot snapshot = history.Pop();
            playerPosition = snapshot.player;
            crates.Clear();
            crates.AddRange(snapshot.crates);
            moveCount = snapshot.moves;
            statusIsPositive = false;
            statusMessage = "Movimiento deshecho. ¡Puedes volver a intentarlo!";
            RefreshInterface();
        }

        private bool IsPuzzleSolved()
        {
            if (crates.Count != RequiredBatteries)
                return false;

            foreach (Vector2Int goal in goals)
            {
                if (!crates.Contains(goal))
                    return false;
            }
            return true;
        }

        private void CompleteMission()
        {
            if (hasWon)
                return;

            hasWon = true;
            isPlaying = false;
            float powerBefore = ResourceManager.Instance.Get(ResourceType.Power);
            float powerAfter = ResourceManager.Instance.Change(ResourceType.Power, powerReward);
            float actualPowerGained = powerAfter - powerBefore;
            GameEvents.RaiseMiniGameFinished("Sokoban", true);
            statusIsPositive = true;
            statusMessage = "¡EXCELENTE! Las dos baterías están conectadas. Recuperaste +" + actualPowerGained.ToString("0") + " de energía eléctrica.";
        }

        private void RefreshInterface()
        {
            if (ResourceManager.Instance != null)
            {
                float power = ResourceManager.Instance.Get(ResourceType.Power);
                float rest = ResourceManager.Instance.Get(ResourceType.Rest);
                if (powerSlider != null) powerSlider.SetValueWithoutNotify(power);
                if (restSlider != null) restSlider.SetValueWithoutNotify(rest);
                if (powerValueText != null) powerValueText.text = power.ToString("0") + " / 100";
                if (restValueText != null) restValueText.text = rest.ToString("0") + " / 100  (−" + restCost.ToString("0") + " al empezar)";
            }

            int batteriesInSlots = 0;
            foreach (Vector2Int crate in crates)
                if (goals.Contains(crate)) batteriesInSlots++;

            if (boardInfoText != null)
                boardInfoText.text = "BATERÍAS EN RANURA: " + batteriesInSlots + " / " + RequiredBatteries +
                                     "       MOVIMIENTOS: " + moveCount;

            if (statusText != null)
            {
                statusText.text = statusMessage;
                statusText.color = statusIsPositive ? new Color(0.45f, 1f, 0.67f) : Color.white;
            }

            if (undoButton != null) undoButton.interactable = isPlaying && history.Count > 0;
            if (resetButton != null) resetButton.interactable = isPlaying;
            if (missionButton != null)
            {
                missionButton.interactable = !isPlaying;
                if (missionButtonText != null)
                    missionButtonText.text = hasWon ? "OTRA MISIÓN (−10 DESCANSO)" : "INTENTAR MISIÓN";
            }

            RefreshBoardVisuals();
        }

        private void RefreshBoardVisuals()
        {
            if (tileImages == null || tileTexts == null)
                return;

            for (int y = 0; y < BoardHeight; y++)
            {
                for (int x = 0; x < BoardWidth; x++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    Image image = tileImages[x, y];
                    TMP_Text label = tileTexts[x, y];

                    if (walls.Contains(position))
                    {
                        image.color = wallColor;
                        label.text = "";
                    }
                    else if (crates.Contains(position))
                    {
                        bool onGoal = goals.Contains(position);
                        image.color = onGoal ? solvedColor : crateColor;
                        label.text = onGoal ? "BATERÍA\n✓" : "BATERÍA";
                        label.color = Color.white;
                    }
                    else if (playerPosition == position)
                    {
                        image.color = playerColor;
                        label.text = goals.Contains(position) ? "ASTRO\n✓" : "ASTRO";
                        label.color = Color.white;
                    }
                    else if (goals.Contains(position))
                    {
                        image.color = goalColor;
                        label.text = "RANURA";
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

        private void OnDestroy()
        {
            // La interfaz pertenece a esta escena y se destruye junto con ella.
        }
    }
}
