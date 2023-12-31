using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasS_Title : MonoBehaviour
{
    [SerializeField] Button playButton;
    [SerializeField] TMP_InputField nameInputField;

    void Start()
    {
        SelectDefaultTitleButton();
    }

    void SelectDefaultTitleButton()
    {
        playButton.Select();
    }

    public void Button_OnPlay()
    {
        CameraManager.Instance.SwitchCamera(CameraManager.Instance.playerCamera);

        if(nameInputField.text != "")
        {
            UI_Manager.Instance.userName = nameInputField.text;
        }

        UI_Manager.Instance.currentCanvasMenu = UI_Manager.GameUIs.Gameplay;

        Destroy(gameObject);
    }
}
