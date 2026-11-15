using System.Collections.Generic;
using System.IO;
using LunarOutpost.MiniGames.Sokoban;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LunarOutpost.EditorTools
{
    /// <summary>
    /// Crea la escena de Sokoban automáticamente para que no haya que añadir objetos a mano.
    /// Se ejecuta desde el menú Guatecosmos > Sokoban > Crear escena de demostración.
    /// </summary>
    public static class SokobanSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/MiniGames/SokobanScene.unity";

        [MenuItem("Guatecosmos/Sokoban/Crear escena de demostración")]
        public static void CreateSokobanScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Sal del modo Play",
                    "Detén el modo Play antes de generar la escena de Sokoban.", "Entendido");
                return;
            }

            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog(
                    "La escena ya existe",
                    "¿Quieres reemplazar SokobanScene? La escena actual se perderá si no guardaste una copia.",
                    "Reemplazar", "Cancelar"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWants())
                return;

            string absoluteDirectory = Path.Combine(Application.dataPath, "_Project/Scenes/MiniGames");
            Directory.CreateDirectory(absoluteDirectory);
            AssetDatabase.Refresh();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.045f, 0.08f);
            cameraObject.AddComponent<AudioListener>(); // evita el warning de "no hay AudioListener" si luego se asignan clips.

            // SokobanMiniGame.Start() crea el resto de los componentes (UI, input, audio,
            // tablero, controlador de recursos) por código, así que no hace falta cablear
            // nada más aquí ni usar prefabs.
            GameObject gameObject = new GameObject("SokobanMiniGame");
            gameObject.AddComponent<SokobanMiniGame>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));

            EditorUtility.DisplayDialog("Sokoban listo",
                "Se creó SokobanScene y se añadió a Build Settings.\n\nPulsa Play para probar el minijuego.",
                "Entendido");
        }

        /// <summary>Alias en inglés del mismo comando, tal como se describe en el encargo original (sección 10).</summary>
        [MenuItem("Guatecosmos/Sokoban/Create Demo Scene")]
        public static void CreateSokobanSceneEnglishAlias() => CreateSokobanScene();

        private static void AddSceneToBuildSettings(string path)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (EditorBuildSettingsScene scene in scenes)
            {
                if (scene.path == path)
                    return;
            }

            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
