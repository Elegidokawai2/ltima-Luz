using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    static string[] Escenas => EditorBuildSettings.scenes
        .Where(s => s.enabled && File.Exists(s.path))
        .Select(s => s.path)
        .ToArray();

    [MenuItem("Build/macOS")]
    public static void BuildMac() => Construir(BuildTarget.StandaloneOSX, "Builds/macOS/LaUltimaLuz.app");

    [MenuItem("Build/Windows")]
    public static void BuildWindows() => Construir(BuildTarget.StandaloneWindows64, "Builds/Windows/LaUltimaLuz.exe");

    [MenuItem("Build/WebGL")]
    public static void BuildWebGL()
    {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        Construir(BuildTarget.WebGL, "Builds/WebGL");
    }

    public static void BuildAll()
    {
        BuildMac();
        BuildWindows();
        BuildWebGL();
    }

    static void Construir(BuildTarget target, string ruta)
    {
        BuildPlayerOptions opciones = new BuildPlayerOptions
        {
            scenes = Escenas,
            locationPathName = ruta,
            target = target,
            options = BuildOptions.None
        };
        BuildReport reporte = BuildPipeline.BuildPlayer(opciones);
        Debug.Log($"[Build] {target}: {reporte.summary.result} ({reporte.summary.totalErrors} errores) -> {ruta}");
        if (reporte.summary.result != BuildResult.Succeeded && Application.isBatchMode)
            EditorApplication.Exit(1);
    }
}
