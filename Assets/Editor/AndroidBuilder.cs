using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlackAlert.Editor
{
    public static class AndroidBuilder
    {
        public static void BuildAndroid()
        {
            var scenePath="Assets/Scenes/Entry.unity";
            Directory.CreateDirectory("Assets/Scenes");
            if (!File.Exists(scenePath))
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene,scenePath);
            }
            AssetDatabase.Refresh();
            PlayerSettings.companyName="Black Alert Studio";
            PlayerSettings.productName="Black Alert";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.blackalert.shadowprotocol");
            PlayerSettings.bundleVersion="0.1.0";
            PlayerSettings.Android.bundleVersionCode=1;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle=false;
            var path="build/Android/BlackAlert.apk";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var opts=new BuildPlayerOptions
            {
                scenes=new[]{scenePath},
                locationPathName=path,
                target=BuildTarget.Android,
                options=BuildOptions.None
            };
            var report=BuildPipeline.BuildPlayer(opts);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception("Unity Android export failed: "+report.summary.result);
            Debug.Log("Built "+path+" ("+new FileInfo(path).Length+" bytes)");
        }
    }
}
