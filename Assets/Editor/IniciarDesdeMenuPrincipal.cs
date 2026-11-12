using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
internal static class IniciarDesdeMenuPrincipal
{
    static IniciarDesdeMenuPrincipal()
    {
        if (!Application.isBatchMode)
            EditorApplication.delayCall += ConfigurarEscenaInicial;
    }

    private static void ConfigurarEscenaInicial()
    {
        var menu = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/_Project/Scenes/MainMenu.unity");
        if (menu != null) EditorSceneManager.playModeStartScene = menu;
    }
}
