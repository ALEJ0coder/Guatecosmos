using System.Collections.Generic;
using LunarOutpost.Core;
using UnityEngine;

namespace LunarOutpost.MiniGames.Pipes
{
    /// <summary>
    /// Prototipo jugable del minijuego Pipes.
    /// El jugador hace clic en los tubos para girarlos y conectar la fuente con el tanque de oxígeno.
    /// La interfaz se dibuja con OnGUI para que el prototipo funcione sin crear Canvas ni prefabs.
    /// </summary>
    public sealed class PipesMiniGame : MonoBehaviour
    {
        private const int GridSize = 5;
        private const int North = 1;
        private const int East = 2;
        private const int South = 4;
        private const int West = 8;
        private const int StartX = 0;
        private const int StartY = 2;
        private const int GoalX = 4;
        private const int GoalY = 2;
        private const float CellWidth = 76f;
        private const float CellHeight = 62f;

        private enum TileKind
        {
            Empty,
            Straight,
            Corner
        }

        private struct PipeCell
        {
            public TileKind kind;
            public int rotation;
        }

        private readonly PipeCell[,] cells = new PipeCell[GridSize, GridSize];

        private static readonly Vector2Int[] DirectionSteps =
        {
            new Vector2Int(0, -1), // Norte
            new Vector2Int(1, 0),  // Este
            new Vector2Int(0, 1),  // Sur
            new Vector2Int(-1, 0)  // Oeste
        };

        private static readonly int[] DirectionBits = { North, East, South, West };
        private static readonly int[] OppositeBits = { South, West, North, East };

        private bool completed;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle tileStyle;
        private GUIStyle endpointStyle;

        private void Start()
        {
            EnsureCoreServices();
            ResetPuzzle();
        }

        /// <summary>En una escena nueva, crea los servicios Core si todavía no existen.</summary>
        private void EnsureCoreServices()
        {
            if (ResourceManager.Instance == null)
            {
                new GameObject("ResourceManager").AddComponent<ResourceManager>();
            }

            if (GameManager.Instance == null)
            {
                new GameObject("GameManager").AddComponent<GameManager>();
            }
        }

        /// <summary>Deja cinco tubos en su orientación inicial. Hay que girarlos para formar el camino.</summary>
        private void ResetPuzzle()
        {
            completed = false;

            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    cells[x, y] = new PipeCell { kind = TileKind.Empty, rotation = 0 };
                }
            }

            // Camino correcto: fuente → (1,2) → (2,2) → (2,1) → (3,1) → (3,2) → tanque.
            cells[1, 2] = new PipeCell { kind = TileKind.Straight, rotation = 0 };
            cells[2, 2] = new PipeCell { kind = TileKind.Corner, rotation = 0 };
            cells[2, 1] = new PipeCell { kind = TileKind.Corner, rotation = 0 };
            cells[3, 1] = new PipeCell { kind = TileKind.Corner, rotation = 0 };
            cells[3, 2] = new PipeCell { kind = TileKind.Corner, rotation = 0 };
        }

        private void RotatePipe(int x, int y)
        {
            if (completed || cells[x, y].kind == TileKind.Empty)
            {
                return;
            }

            PipeCell cell = cells[x, y];
            cell.rotation = (cell.rotation + 1) % 4;
            cells[x, y] = cell;

            if (HasCompletePath())
            {
                CompleteMiniGame();
            }
        }

        /// <summary>Convierte el tipo y la rotación de un tubo en sus conexiones actuales.</summary>
        private int GetPorts(int x, int y)
        {
            if (x == StartX && y == StartY)
            {
                return East;
            }

            if (x == GoalX && y == GoalY)
            {
                return West;
            }

            PipeCell cell = cells[x, y];
            if (cell.kind == TileKind.Empty)
            {
                return 0;
            }

            // Tubo recto base: norte-sur. Codo base: norte-este.
            int ports = cell.kind == TileKind.Straight ? North | South : North | East;

            // Cada giro horario mueve N→E→S→W→N.
            for (int i = 0; i < cell.rotation; i++)
            {
                ports = ((ports << 1) & 15) | ((ports >> 3) & 1);
            }

            return ports;
        }

        /// <summary>Busca un camino continuo desde la fuente hasta el tanque.</summary>
        private bool HasCompletePath()
        {
            Queue<Vector2Int> pending = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            Vector2Int start = new Vector2Int(StartX, StartY);

            pending.Enqueue(start);
            visited.Add(start);

            while (pending.Count > 0)
            {
                Vector2Int current = pending.Dequeue();
                int currentPorts = GetPorts(current.x, current.y);

                for (int direction = 0; direction < 4; direction++)
                {
                    if ((currentPorts & DirectionBits[direction]) == 0)
                    {
                        continue;
                    }

                    Vector2Int next = current + DirectionSteps[direction];
                    if (next.x < 0 || next.x >= GridSize || next.y < 0 || next.y >= GridSize)
                    {
                        continue;
                    }

                    int nextPorts = GetPorts(next.x, next.y);
                    if ((nextPorts & OppositeBits[direction]) == 0)
                    {
                        continue;
                    }

                    if (next.x == GoalX && next.y == GoalY)
                    {
                        return true;
                    }

                    if (!visited.Contains(next))
                    {
                        visited.Add(next);
                        pending.Enqueue(next);
                    }
                }
            }

            return false;
        }

        private void CompleteMiniGame()
        {
            if (completed)
            {
                return;
            }

            completed = true;

            if (ResourceManager.Instance != null)
            {
                // Resolver el circuito recupera 20 puntos de comida y oxígeno.
                ResourceManager.Instance.Change(ResourceType.FoodOxygen, 20f);
            }

            GameEvents.RaiseMiniGameFinished("Pipes", true);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 23,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = new Color(0.10f, 0.18f, 0.28f);

            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            subtitleStyle.normal.textColor = new Color(0.18f, 0.22f, 0.27f);

            tileStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            endpointStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
        }

        private void OnGUI()
        {
            EnsureStyles();

            float panelWidth = Mathf.Min(520f, Screen.width - 20f);
            float panelHeight = Mathf.Min(620f, Screen.height - 20f);
            Rect panel = new Rect((Screen.width - panelWidth) / 2f, (Screen.height - panelHeight) / 2f,
                                  panelWidth, panelHeight);

            GUILayout.BeginArea(panel, GUI.skin.window);
            GUILayout.BeginVertical();

            GUILayout.Space(8f);
            GUILayout.Label("GUATECOSMOS", titleStyle);
            GUILayout.Label("PIPES — RESTABLECE EL SUMINISTRO DE OXÍGENO", subtitleStyle);
            GUILayout.Space(6f);
            GUILayout.Label("Haz clic en cada tubo para girarlo 90°. Conecta la FUENTE con el TANQUE.", subtitleStyle);
            GUILayout.Space(12f);

            DrawBoard();

            GUILayout.Space(12f);
            float oxygen = ResourceManager.Instance != null
                ? ResourceManager.Instance.Get(ResourceType.FoodOxygen)
                : 0f;
            GUILayout.Label("COMIDA Y OXÍGENO: " + oxygen.ToString("0") + " / 100", subtitleStyle);

            if (completed)
            {
                GUILayout.Label("¡CIRCUITO CONECTADO! +20 DE COMIDA Y OXÍGENO", titleStyle);
            }
            else
            {
                GUILayout.Label("Pista: la tubería debe formar un camino continuo, sin huecos.", subtitleStyle);
            }

            GUILayout.Space(8f);
            if (GUILayout.Button(completed ? "JUGAR OTRA VEZ" : "REINICIAR TABLERO", GUILayout.Height(34f)))
            {
                ResetPuzzle();
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawBoard()
        {
            for (int y = 0; y < GridSize; y++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                for (int x = 0; x < GridSize; x++)
                {
                    if (x == StartX && y == StartY)
                    {
                        GUILayout.Box("FUENTE\nO₂", endpointStyle, GUILayout.Width(CellWidth), GUILayout.Height(CellHeight));
                    }
                    else if (x == GoalX && y == GoalY)
                    {
                        GUILayout.Box("TANQUE\nO₂", endpointStyle, GUILayout.Width(CellWidth), GUILayout.Height(CellHeight));
                    }
                    else if (cells[x, y].kind != TileKind.Empty)
                    {
                        GUI.backgroundColor = completed
                            ? new Color(0.55f, 0.85f, 0.62f)
                            : new Color(0.60f, 0.80f, 0.92f);

                        if (GUILayout.Button(GetPipeGlyph(GetPorts(x, y)), tileStyle,
                                             GUILayout.Width(CellWidth), GUILayout.Height(CellHeight)))
                        {
                            RotatePipe(x, y);
                        }

                        GUI.backgroundColor = Color.white;
                    }
                    else
                    {
                        GUILayout.Box("", GUILayout.Width(CellWidth), GUILayout.Height(CellHeight));
                    }
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>Devuelve un símbolo visual según las dos salidas del tubo.</summary>
        private string GetPipeGlyph(int ports)
        {
            if (ports == (North | South)) return "│";
            if (ports == (East | West)) return "─";
            if (ports == (North | East)) return "└";
            if (ports == (East | South)) return "┌";
            if (ports == (South | West)) return "┐";
            if (ports == (West | North)) return "┘";
            return "?";
        }
    }
}
