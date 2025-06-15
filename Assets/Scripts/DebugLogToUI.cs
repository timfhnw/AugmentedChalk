using UnityEngine;
using UnityEngine.UI;

public class DebugLogToUI : MonoBehaviour
{
    [SerializeField] TMPro.TextMeshPro uiText;
    [SerializeField] int truncate = 2000;
    private string log = "";

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        log += logString + "\n";
        if (log.Length > truncate)
            log = log.Substring(log.Length - truncate);

        if (uiText != null)
            uiText.text = log;
    }
}
