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
