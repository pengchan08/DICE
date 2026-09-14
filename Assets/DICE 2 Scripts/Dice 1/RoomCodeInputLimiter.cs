using UnityEngine;
using UnityEngine.UI;

public class RoomCodeInputLimiter : MonoBehaviour
{
    private InputField inputField;
    private const int MaxLength = 4;

    void Awake()
    {
        inputField = GetComponent<InputField>();
        inputField.onValidateInput += ValidateChar;
    }

    char ValidateChar(string text, int charIndex, char addedChar)
    {
        // 숫자가 아니면 입력 거부
        if (!char.IsDigit(addedChar))
        {
            return '\0';
        }

        // 이미 4자리면 더 이상 입력 거부
        if (text.Length >= MaxLength)
        {
            return '\0';
        }

        return addedChar;
    }

    void OnDestroy()
    {
        if (inputField != null)
        {
            inputField.onValidateInput -= ValidateChar;
        }
    }
}