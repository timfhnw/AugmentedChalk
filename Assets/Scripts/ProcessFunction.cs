using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Windows;
using AngouriMath;
using Unity.VisualScripting;
using System.Linq;

public class ProcessFunction
{
    private const float scale = 0.025f;

    public static string ExtractFunction(string text)
    {
        var match = Regex.Match(text, @"f\([^)]*\)\s*=\s*([^\r\n]+)");
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    public static Entity ConvertTextToExpression(string text)
    {
        return MathS.FromString(text);
    }

    public static GameObject Plot3D(Entity expr, int start, int end, Material material, Material selected, GameObject frame)
    {
        var plotValues = EvaluateFunction(expr, start, end);
        return CreateARFunctionPlot(plotValues, end - start + 1, material, selected, frame);
    }

    static Vector3[] EvaluateFunction(Entity expr, int start, int end)
    {
        int size = end - start + 1;
        var result = new Vector3[size * size];

        int i = 0;
        for (int y = start; y <= end; y++)
        {
            for (int x = start; x <= end; x++)
            {
                int v = 0;
                var exprLocal = expr;
                foreach (var item in expr.Vars)
                {
                    if (v % 2 == 0)
                    {
                        exprLocal = exprLocal.Substitute(item, x);
                    }
                    else
                    {
                        exprLocal = exprLocal.Substitute(item, y);
                    }
                    v++;
                }
                var z = exprLocal.EvalNumerical();
                result[i++] = new Vector3(x, (float)z, y);
            }
        }

        return result;
    }

    static GameObject CreateARFunctionPlot(Vector3[] vertices, int sideLength, Material material = null, Material selected = null, GameObject frame = null)
    {
        var parent = new GameObject("FunctionPlot_AR");
        parent.transform.localScale *= scale;

        var box = parent.AddComponent<BoxCollider>();
        float plotSize = (sideLength - 1);
        float half = plotSize * 0.5f;
        box.size = new Vector3(half * 2, 0.1f, half * 2); // fixed height

        var plot = SetupPlotFromData(vertices, sideLength, material);
        plot.transform.SetParent(parent.transform, false);
        plot.AddComponent<GradientBoundsSetter>();
        var xrLayer = LayerMask.NameToLayer("XR Simulation");
        parent.layer = xrLayer;
        plot.layer = xrLayer;
        return parent;
    }

    static GameObject SetupPlotFromData(Vector3[] vertices, int sideLength, Material material = null)
    {
        var obj = new GameObject("FunctionPlot");
        // Mesh components
        var mesh = new Mesh();
        var filter = obj.AddComponent<MeshFilter>();
        var renderer = obj.AddComponent<MeshRenderer>();
        if (material != null)
            renderer.material = material;

        mesh.vertices = vertices;
        var triangles = GenerateGridTriangles(sideLength);

        // Flip winding
        //for (int i = 0; i < triangles.Length; i += 3)
        //{
        //    int tmp = triangles[i + 1];
        //    triangles[i + 1] = triangles[i + 2];
        //    triangles[i + 2] = tmp;
        //}

        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        filter.mesh = mesh;
        return obj;
    }

    static int[] GenerateGridTriangles(int sideLength)
    {
        int quads = (sideLength - 1) * (sideLength - 1);
        int[] triangles = new int[quads * 6];

        int tris = 0;
        for (int y = 0; y < sideLength - 1; y++)
        {
            for (int x = 0; x < sideLength - 1; x++)
            {
                int i = y * sideLength + x;
                triangles[tris++] = i;
                triangles[tris++] = i + sideLength;
                triangles[tris++] = i + 1;
                triangles[tris++] = i + 1;
                triangles[tris++] = i + sideLength;
                triangles[tris++] = i + sideLength + 1;
            }
        }

        return triangles;
    }

    public static Texture2D ApplyGaussianBlur(Texture2D source, Material blurMat, int iterations, int kernelSize)
    {
        if (kernelSize % 2 == 0 || kernelSize > 15)
            throw new ArgumentException("kernelSize must be odd and ? 15");

        int width = source.width;
        int height = source.height;

        RenderTexture rt1 = RenderTexture.GetTemporary(width, height, 0);
        RenderTexture rt2 = RenderTexture.GetTemporary(width, height, 0);
        Graphics.Blit(source, rt1);

        blurMat.SetVector("_TexelSize", new Vector4(1f / width, 1f / height, 0, 0));
        blurMat.SetInt("_KernelSize", kernelSize);

        int half = kernelSize / 2;
        float sigma = 0.3f * ((kernelSize - 1) * 0.5f - 1f) + 0.8f;
        float[] weights = new float[kernelSize];
        float sum = 0;

        for (int i = 0; i <= half; i++)
        {
            float w = Mathf.Exp(-0.5f * (i * i) / (sigma * sigma));
            weights[half + i] = weights[half - i] = w;
            sum += i == 0 ? w : 2 * w;
        }
        for (int i = 0; i < kernelSize; i++)
            weights[i] /= sum;

        blurMat.SetFloatArray("_Weights", weights);

        for (int i = 0; i < iterations; i++)
        {
            blurMat.SetVector("_BlurDir", new Vector4(1, 0, 0, 0));
            Graphics.Blit(rt1, rt2, blurMat);

            blurMat.SetVector("_BlurDir", new Vector4(0, 1, 0, 0));
            Graphics.Blit(rt2, rt1, blurMat);
        }

        RenderTexture.active = rt1;
        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.ReleaseTemporary(rt1);
        RenderTexture.ReleaseTemporary(rt2);
        RenderTexture.active = null;

        return result;
    }
}
