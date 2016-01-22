﻿using UnityEngine;
using Pathfinding;

public class SlimeRenderer : MonoBehaviour
{

    Material whiteMaterial;
    RenderTexture slimeTex;
    public Material groundMaterial;
    StandardGameSettings sgs;

    int framesSkipped = 0;

    [Range(1, 4)]
    [Header("multiples of 128")]
    public int textureSize = 2;


    [SerializeField]
    Camera projectorCam;

    void Start()
    {
        sgs = StandardGameSettings.Get;
        whiteMaterial = new Material(Shader.Find("Unlit/Color"));
        whiteMaterial.color = Color.green;
        CreateRenderTex();
    }

    void CreateRenderTex()
    {
        if (slimeTex != null)
        {
            slimeTex.DiscardContents();
            slimeTex.Release();
        }
        int sampleSize = 128 * textureSize;
        slimeTex = new RenderTexture(sampleSize, sampleSize, 0);
    }

    void OnApplicationQuit()
    {
        groundMaterial.mainTexture = null;
        if (slimeTex)
        {
            slimeTex.DiscardContents();
            slimeTex.Release();
        }
    }

#if UNITY_EDITOR
    int prevSampleSize = 2;
#endif
    void OnPreRender()
    {
#if UNITY_EDITOR
        if (prevSampleSize != textureSize)
        {
            CreateRenderTex();
            prevSampleSize = textureSize;
        }
#endif
        if (framesSkipped < sgs.renderFrameSkip)
        {
            framesSkipped++;
            return;
        }
        if (slimeTex == null) { CreateRenderTex(); }
        framesSkipped = 0;
        slimeTex.DiscardContents(true, true);
        RenderTexture previousRT = RenderTexture.active;
        RenderTexture.active = slimeTex;
        GridGraph gg = AstarPath.active.astarData.gridGraph;
        whiteMaterial.SetPass(0);
        GL.Clear(true, true, Color.red);
        GL.PushMatrix();
        GL.LoadIdentity();
        GL.LoadProjectionMatrix(projectorCam.projectionMatrix);
        GL.MultMatrix(projectorCam.worldToCameraMatrix);
        GL.Begin(GL.QUADS);
        GL.Color(Color.green);
        for (int i = 0; i < gg.nodes.Length; i++)
        {
            if (gg.nodes[i].Tag != SlimeHandler.slimeTag || !gg.nodes[i].Walkable)
            {
                continue;
            }
            Vector3 bottomLeft = (Vector3)gg.nodes[i].position;
            bottomLeft.x -= (gg.nodeSize * 0.5f);
            bottomLeft.z -= (gg.nodeSize * 0.5f);
            GL.Vertex(bottomLeft);
            GL.Vertex3(bottomLeft.x, 0, bottomLeft.z + gg.nodeSize);
            GL.Vertex3(bottomLeft.x + gg.nodeSize, 0, bottomLeft.z + gg.nodeSize);
            GL.Vertex3(bottomLeft.x + gg.nodeSize, 0, bottomLeft.z);
        }
        GL.End();
        GL.PopMatrix();
        RenderTexture.active = previousRT;
        groundMaterial.mainTexture = slimeTex;
        groundMaterial.SetMatrix("_projMatrix", (projectorCam.projectionMatrix * projectorCam.worldToCameraMatrix));
    }
}