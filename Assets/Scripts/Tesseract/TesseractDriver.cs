using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class TesseractDriver
{
    private TesseractWrapper _tesseract;
    private static readonly List<string> fileNames = new List<string> { "tessdata.tgz" };

    public string CheckTessVersion()
    {
        _tesseract = new TesseractWrapper();

        try
        {
            string version = "Tesseract version: " + _tesseract.Version();
            UnityEngine.Debug.Log(version);
            return version;
        }
        catch (Exception e)
        {
            string errorMessage = e.GetType() + " - " + e.Message;
            UnityEngine.Debug.LogError("Tesseract version: " + errorMessage);
            return errorMessage;
        }
    }

    public void Setup(UnityAction onSetupComplete)
    {
#if UNITY_EDITOR
        OcrSetup(onSetupComplete);
#elif UNITY_ANDROID
        CopyAllFilesToPersistentData(fileNames, onSetupComplete);
#else
        OcrSetup(onSetupComplete);
#endif
    }

    public void OcrSetup(UnityAction onSetupComplete)
    {
        _tesseract = new TesseractWrapper();

#if UNITY_EDITOR
        string datapath = Path.Combine(Application.streamingAssetsPath, "tessdata");
#elif UNITY_ANDROID
        string datapath = Application.persistentDataPath + "/tessdata/";
#else
        string datapath = Path.Combine(Application.streamingAssetsPath, "tessdata");
#endif

        if (_tesseract.Init("eng", datapath))
        {
            UnityEngine.Debug.Log("Init Successful");
            onSetupComplete?.Invoke();
        }
        else
        {
            UnityEngine.Debug.LogError(_tesseract.GetErrorMessage());
        }
    }

    private async void CopyAllFilesToPersistentData(List<string> fileNames, UnityAction onSetupComplete)
    {
        String fromPath = "jar:file://" + Application.dataPath + "!/assets/";
        String toPath = Application.persistentDataPath + "/";

        foreach (String fileName in fileNames)
        {
            if (!File.Exists(toPath + fileName))
            {
                UnityEngine.Debug.Log("Copying from " + fromPath + fileName + " to " + toPath);
                UnityWebRequest www = UnityWebRequest.Get(fromPath + fileName);
                var operation = www.SendWebRequest();

                while (!operation.isDone)
                {
                    await Task.Yield(); // Avoids blocking the main thread
                }

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Failed to load: " + fileName + " - " + www.error);
                    return;
                }

                File.WriteAllBytes(toPath + fileName, www.downloadHandler.data);
                UnityEngine.Debug.Log("File copy done");
                www.Dispose();
                www = null;
            }
            else
            {
                UnityEngine.Debug.Log("File exists! " + toPath + fileName);
            }

            UnZipData(fileName);
        }

        OcrSetup(onSetupComplete);
    }

    public string GetErrorMessage()
    {
        return _tesseract?.GetErrorMessage();
    }

    public string Recognize(Texture2D imageToRecognize)
    {
        return _tesseract.Recognize(imageToRecognize);
    }

    public Texture2D GetHighlightedTexture()
    {
        return _tesseract.GetHighlightedTexture();
    }

    private void UnZipData(string fileName)
    {
        if (File.Exists(Application.persistentDataPath + "/" + fileName))
        {
            UnZipUtil.ExtractTGZ(Application.persistentDataPath + "/" + fileName, Application.persistentDataPath);
            UnityEngine.Debug.Log("UnZipping Done");
        }
        else
        {
            UnityEngine.Debug.LogError(fileName + " not found!");
        }
    }
}
