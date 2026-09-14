using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class NameInputLimiter : MonoBehaviour
{
    private InputField inputField;

    private const int KoreanMaxLength = 4;
    private const int EnglishMaxLength = 8;

    void Awake()
    {
        inputField = GetComponent<InputField>();
        inputField.onValidateInput += ValidateChar;
        inputField.onValueChanged.AddListener(OnValueChanged);
    }

    // 문자 단위 필터: 한글 또는 영어 알파벳만 허용, 그 외는 입력 자체를 차단
    char ValidateChar(string text, int charIndex, char addedChar)
    {
        bool isEnglish = (addedChar >= 'a' && addedChar <= 'z') || (addedChar >= 'A' && addedChar <= 'Z');
        bool isKorean = IsKoreanChar(addedChar);

        if (!isEnglish && !isKorean)
        {
            return '\0'; // 한글/영어가 아니면 입력 거부 (숫자, 특수문자, 다른 언어 등)
        }

        return addedChar;
    }

    bool IsKoreanChar(char c)
    {
        // 완성형 한글 음절(가~힣) + 자모(ㄱ~ㅎ, ㅏ~ㅣ) 범위
        return (c >= 0xAC00 && c <= 0xD7A3) || (c >= 0x3131 && c <= 0x318E);
    }

    // 길이 제한: 입력된 내용이 한글인지 영어인지 판별해서 그에 맞는 최대 길이로 자름
    void OnValueChanged(string currentText)
    {
        if (string.IsNullOrEmpty(currentText)) return;

        bool containsKorean = Regex.IsMatch(currentText, @"[\uAC00-\uD7A3\u3131-\u318E]");
        int maxLength = containsKorean ? KoreanMaxLength : EnglishMaxLength;

        if (currentText.Length > maxLength)
        {
            string trimmed = currentText.Substring(0, maxLength);
            inputField.text = trimmed;
            inputField.caretPosition = trimmed.Length; // 커서를 끝으로 유지
        }
    }

    void OnDestroy()
    {
        if (inputField != null)
        {
            inputField.onValidateInput -= ValidateChar;
            inputField.onValueChanged.RemoveListener(OnValueChanged);
        }
    }
}