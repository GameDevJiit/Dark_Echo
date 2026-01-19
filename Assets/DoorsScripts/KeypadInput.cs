using UnityEngine;
using TMPro;

public class KeypadInput : MonoBehaviour
{
    [Header("UI")]
    public GameObject keypadPanel;
    public TMP_Text inputText;
    public TMP_Text messageText;

    [Header("Door")]
    public PasswordDoor connectedDoor;

    [Header("Settings")]
    public int passwordLength = 3;

    private string currentInput = "";
    private bool playerInside = false;
    private bool keypadActive = false;

    void Start()
    {
        keypadPanel.SetActive(false);
        inputText.text = "";
        messageText.text = "";
    }

    void Update()
    {
        if (playerInside && !keypadActive && Input.GetKeyDown(KeyCode.F))
        {
            OpenKeypad();
        }

        if (!keypadActive) return;

        foreach (char c in Input.inputString)
        {
            if (char.IsLetterOrDigit(c))
            {
                currentInput += char.ToUpper(c);
                inputText.text = currentInput;

                if (currentInput.Length == passwordLength)
                {
                    bool success = connectedDoor.TryPassword(currentInput);

                    if (success)
                    {
                        Invoke(nameof(CloseKeypad), 0.5f);
                    }
                    else
                    {
                        messageText.text = "Try Again";
                    }

                    currentInput = "";
                    inputText.text = "";
                }
            }
            else if (c == '\b' && currentInput.Length > 0)
            {
                currentInput = currentInput.Substring(0, currentInput.Length - 1);
                inputText.text = currentInput;
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseKeypad();
        }
    }

    void OpenKeypad()
    {
        keypadActive = true;
        keypadPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PlayerFreeze.Instance.Freeze();
    }

    void CloseKeypad()
    {
        keypadActive = false;
        keypadPanel.SetActive(false);

        currentInput = "";
        inputText.text = "";
        messageText.text = "";

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        PlayerFreeze.Instance.Unfreeze();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            connectedDoor.promptText.gameObject.SetActive(true);
            connectedDoor.promptText.text = "Press F to Enter Password";
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            CloseKeypad();
            connectedDoor.promptText.gameObject.SetActive(false);
        }
    }
}