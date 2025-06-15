using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class GradientBoundsSetter : MonoBehaviour
{
    public MaterialPropertyBlock block;
    public Renderer rend;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
    }

    private void LateUpdate()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf && mf.sharedMesh != null)
        {
            mf.sharedMesh.RecalculateBounds();
            Bounds objectBounds = mf.sharedMesh.bounds;
            float minY = objectBounds.min.y;
            float maxY = objectBounds.max.y;

            if (Mathf.Approximately(minY, maxY)) maxY = minY + 0.001f;// avoid 0

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            Renderer rend = GetComponent<Renderer>();
            rend.GetPropertyBlock(block);
            block.SetFloat("_GradientMin", minY);
            block.SetFloat("_GradientMax", maxY);
            rend.SetPropertyBlock(block);
        }
    }
}