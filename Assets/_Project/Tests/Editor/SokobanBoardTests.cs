using LunarOutpost.MiniGames.Sokoban;
using NUnit.Framework;
using UnityEngine;

namespace LunarOutpost.Tests.Sokoban
{
    /// <summary>
    /// Pruebas EditMode sobre SokobanBoard y SokobanPlayerController. Al ser clases de C#
    /// puro (sin MonoBehaviour), no requieren levantar una escena ni entrar en Play Mode.
    /// </summary>
    public class SokobanBoardTests
    {
        private SokobanLevelData level;

        [SetUp]
        public void SetUp()
        {
            level = SokobanLevelData.CreateDefaultLevel();
        }

        [Test]
        public void ElNivelPorDefectoEsEstructuralmenteValido()
        {
            var errors = SokobanLevelValidator.ValidateStructure(level);
            Assert.IsEmpty(errors, string.Join(" | ", errors));
        }

        [Test]
        public void ElNivelPorDefectoEsResolublePorBusqueda()
        {
            Assert.IsTrue(SokobanLevelValidator.IsSolvable(level));
        }

        [Test]
        public void LaSolucionDeReferenciaResuelveElNivel()
        {
            SokobanBoard board = new SokobanBoard();
            board.LoadLevel(level);

            foreach (Vector2Int direction in SokobanLevelData.GetReferenceSolution())
                board.TryMove(direction);

            Assert.IsTrue(board.IsSolved());
        }

        [Test]
        public void NoSePuedeAtravesarUnaPared()
        {
            SokobanBoard board = new SokobanBoard();
            board.LoadLevel(level);
            Vector2Int before = board.PlayerPosition;

            // (0, y) es pared perimetral: el astronauta empieza en (1,1), moverse a la izquierda choca con el muro.
            bool moved = board.TryMove(Vector2Int.left);

            Assert.IsFalse(moved);
            Assert.AreEqual(before, board.PlayerPosition);
        }

        [Test]
        public void NoSePuedenEmpujarDosBateriasALaVez()
        {
            // Construimos un nivel mínimo: astronauta - batería - batería - pared, en línea recta.
            var walls = new System.Collections.Generic.HashSet<Vector2Int>();
            for (int x = 0; x < 6; x++)
            {
                walls.Add(new Vector2Int(x, 0));
                walls.Add(new Vector2Int(x, 3));
            }
            for (int y = 0; y < 3; y++)
            {
                walls.Add(new Vector2Int(0, y));
                walls.Add(new Vector2Int(5, y));
            }

            var customLevel = new SokobanLevelData(
                width: 6, height: 3,
                playerStart: new Vector2Int(1, 1),
                batteryStarts: new System.Collections.Generic.List<Vector2Int> { new Vector2Int(2, 1), new Vector2Int(3, 1) },
                stationPositions: new System.Collections.Generic.List<Vector2Int> { new Vector2Int(2, 2), new Vector2Int(3, 2) },
                walls: walls);

            SokobanBoard board = new SokobanBoard();
            board.LoadLevel(customLevel);

            bool moved = board.TryMove(Vector2Int.right); // empujaría la batería 1 contra la batería 2

            Assert.IsFalse(moved);
        }

        [Test]
        public void DeshacerRestauraElEstadoAnterior()
        {
            SokobanPlayerController controller = new SokobanPlayerController(new SokobanBoard());
            controller.StartLevel(level);
            Vector2Int initialPlayerPosition = controller.Board.PlayerPosition;

            controller.Move(Vector2Int.up);
            Assert.AreNotEqual(initialPlayerPosition, controller.Board.PlayerPosition);

            bool undone = controller.Undo();

            Assert.IsTrue(undone);
            Assert.AreEqual(initialPlayerPosition, controller.Board.PlayerPosition);
        }

        [Test]
        public void ReiniciarNivelNoDejaMovimientosEnElHistorial()
        {
            SokobanPlayerController controller = new SokobanPlayerController(new SokobanBoard());
            controller.StartLevel(level);
            controller.Move(Vector2Int.up);

            controller.ResetLevel(level);

            Assert.IsFalse(controller.CanUndo);
            Assert.AreEqual(0, controller.Board.MoveCount);
        }

        [Test]
        public void ResolverElNivelDisparaElEventoDeVictoriaUnaSolaVez()
        {
            SokobanPlayerController controller = new SokobanPlayerController(new SokobanBoard());
            controller.StartLevel(level);

            int solvedCount = 0;
            controller.OnLevelSolved += () => solvedCount++;

            foreach (Vector2Int direction in SokobanLevelData.GetReferenceSolution())
                controller.Move(direction);

            Assert.AreEqual(1, solvedCount);
            Assert.IsFalse(controller.IsActive); // tras ganar, el controlador deja de aceptar movimientos.
        }
    }
}
