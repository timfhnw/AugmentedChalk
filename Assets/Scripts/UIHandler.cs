using UnityEngine;
using UnityEngine.UI;

public class UIHandler : MonoBehaviour
{
    public Button ok;
    public GameObject popupCanvas;
    public GameObject tapText;

    public Button start;
    public GameObject startCanvas;
    public GameObject startText;

    public Button deleteButton;
    void Start()
    {
        if (popupCanvas != null) popupCanvas.SetActive(false);
        if (tapText != null) tapText.SetActive(false);

        if (ok != null)
            ok.onClick.AddListener(OkButton);
        else
            Debug.LogError("Button not assigned");

        if (start != null)
            start.onClick.AddListener(LeaveStart);
        else
            Debug.LogError("Button not assigned");
    }
    public void OkButton()
    {
        if (popupCanvas != null) popupCanvas.SetActive(false);
        if (tapText != null) tapText.SetActive(false);
    }

    public void ShowOverlay()
    {
        if (popupCanvas != null) popupCanvas.SetActive(true);
        if (tapText != null) tapText.SetActive(true);
    }

    public bool isActive()
    {
        return (startCanvas != null && startCanvas.activeSelf) || (popupCanvas != null && popupCanvas.activeSelf);
    }

    public void LeaveStart()
    {
        if (startCanvas != null) startCanvas.SetActive(false);
        if (startText != null) startText.SetActive(false);
    }

    public void setDelete(bool toggle)
    {
        deleteButton.gameObject.SetActive(toggle);
    }

    public bool isDeleteActive()
    {
        return deleteButton.gameObject.activeSelf;
    }
}
