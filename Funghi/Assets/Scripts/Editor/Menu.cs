﻿using UnityEngine;
using System.Collections;
using Tutorials;
using UnityEditor;

public static class Menu
{

    const string BaseMenu = "Fungus";

    [MenuItem(BaseMenu + "/Create Spawner")]
    static void CreateEnemySpawner()
    {
        var go = new GameObject("SpawnPoint");
        go.transform.position = GetPositionFromView();
        go.AddComponent<EntitySpawner>();
        Selection.activeGameObject = go;
    }

    static Vector3 GetPositionFromView()
    {
        Camera cam = EditorWindow.GetWindow<SceneView>().camera;
        if (!cam) { return Vector3.zero; }
        Vector3 p = cam.ViewportToWorldPoint(Vector2.one * 0.5f);
        p.y = 0;
        return p;
    }

    [MenuItem(BaseMenu + "/Create Path")]
    static void CreatePath()
    {
        var go = new GameObject("Path");
        go.transform.position = GetPositionFromView();
        go.AddComponent<PatrolPath>();
        Selection.activeGameObject = go;
    }

    [MenuItem(BaseMenu + "/Create Entity Trigger")]
    static void CreateEntityTrigger()
    {
        var go = new GameObject("EntityTrigger");
        go.transform.position = GetPositionFromView();
        go.AddComponent<EntityTrigger>();
        Selection.activeGameObject = go;
    }

    [MenuItem(BaseMenu + "/Create PoliceStation")]
    static void CreatePoliceStation()
    {
        var go = new GameObject("PoliceStation");
        go.transform.position = GetPositionFromView();
        go.AddComponent<PoliceStation>();
        Selection.activeGameObject = go;
    }

    [MenuItem(BaseMenu + "/Settings/Open")]
    static void OpenSettings()
    {
        if (StandardGameSettings.Get)
        {
            Selection.activeObject = StandardGameSettings.Get;
        }
        else
        {
            Debug.LogWarning("Not found!");
        }
    }

    [MenuItem(BaseMenu + "/Tutorial/Create")]
    static void CreateTutorial()
    {
        var t = GameObject.FindObjectOfType<Tutorial>();
        if (t)
        {
            Selection.activeGameObject = t.gameObject;
            return;
        }
        t = new GameObject("Tutorial").AddComponent<Tutorial>();
        var uiPanelObject = GameObject.Find("TutorialPanel");
        if (uiPanelObject)
        {
            t.UIAnchor = uiPanelObject.transform as RectTransform;
        }
        Selection.activeGameObject = t.gameObject;
    }

    [MenuItem(BaseMenu + "/Create SpawnerTriggerCollection")]
    static void CreateSPTC()
    {
        var sp = GameObject.FindObjectOfType<SpawnerTriggerCollection>();
        if (sp)
        {
            Selection.activeGameObject = sp.gameObject;
            return;
        }
        sp = new GameObject("SpawnerTriggerCollection").AddComponent<SpawnerTriggerCollection>();
        Selection.activeGameObject = sp.gameObject;
    }
}
