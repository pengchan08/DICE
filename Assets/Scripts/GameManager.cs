using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    public Text[] slotTexts;
    public Button[] slotButtons;
    public int[] diceNumbers = new int[5];
    public int diceNumber;
    public bool isValueStored = false;
    public bool hasRolled = false;
    
    public int maxRolls = 8;
    public int currentRolls = 0;
    public Text rollCountText;
    
    public Text[] ScoreText;

    public int currentRound = 1;
    public int maxRounds = 3;
    public Text roundText;
    public GameObject restartPanel;
    
    public Button[] scoreButtons;
    public Image[] buttonImages;
    public Color selectedColor;
    private Color[] originalColors;
    
    private bool[] scoreUsed = new bool[12];
    public int totalScore = 0;
    public Text totalScoreText;

    public bool isRolling = false;
    
    public GameObject fullSlotText;

    public bool bonusGiven = false;
    public GameObject bonusGivenText;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int index = i;
        }
    }
    
    void Start()
    {
        UpdateTexts();
        UpdateRollCountUI();
        UpdateRoundUI();
        restartPanel.SetActive(false);
        
        originalColors = new Color[buttonImages.Length];
        for (int i = 0; i < 12; i++)
        {
            int capturedIndex = i;
            scoreButtons[i].onClick.AddListener(() => {
                ApplyScore(capturedIndex);
            });
        }
        
        for (int i = 0; i < buttonImages.Length; i++)
        {
            originalColors[i] = buttonImages[i].color;
        }
        
        for (int i = 0; i < scoreButtons.Length; i++)
        {
            int index = i;
            scoreButtons[i].onClick.AddListener(() => ChangeImageColor(index));
        }
    }
    
    void Update()
    {
        if (diceNumber != 0 && !isValueStored)
        {
            SaveToSlot(diceNumber);
            isValueStored = true;
        }
        
        CalculateScore();
        
        ScoreText[0].text = "Ones : " + CalculateNumberScore(1) + "점";
        ScoreText[1].text = "Twos : " + CalculateNumberScore(2) + "점";
        ScoreText[2].text = "Threes : " + CalculateNumberScore(3) + "점";
        ScoreText[3].text = "Fours : " + CalculateNumberScore(4) + "점";
        ScoreText[4].text = "Fives : " + CalculateNumberScore(5) + "점";
        ScoreText[5].text = "Sixes : " + CalculateNumberScore(6) + "점";
        
        ScoreText[6].text = "Choice : " + GetChoiceScore() + "점"; // choice
        ScoreText[7].text = "Four of a Kind : " + GetFourOfAKindScore() + "점"; // four of a kind
        ScoreText[8].text = "Full House : " + GetFullHouseScore() + "점"; // full house
        ScoreText[9].text = "Little Straight : " + GetLittleStraightScore() + "점"; // little straight
        ScoreText[10].text = "Big Straight : " + GetBigStraightScore() + "점"; // big straight
        ScoreText[11].text = "Yacht : " + GetYachtScore() + "점"; // yacht
    }
    
    public void ShowFullSlotMessage()
    {
        StartCoroutine(ShowFullSlotMessageCoroutine());
    }

    private IEnumerator ShowFullSlotMessageCoroutine()
    {
        fullSlotText.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        fullSlotText.SetActive(false);
    }
    
    private void CalculateScore()
    {
        // 63점 이상이면 35점 추가
        if (totalScore >= 63 && !bonusGiven)
        {
            totalScore += 40;
            bonusGiven = true;
            bonusGivenText.SetActive(true);
            UpdateTotalScoreUI();
            StartCoroutine(HideBonusTextAfterDelay());
        }
        UpdateTotalScoreUI();
    }

    private IEnumerator HideBonusTextAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        bonusGivenText.SetActive(false);
    }
    
    void UpdateTotalScoreUI()
    {
        totalScoreText.text = "Score : " + totalScore;
    }
    
    public void ApplyScore(int index)
    {
        if (index < 0 || index >= scoreUsed.Length) return;

        int score = 0;

        switch (index)
        {
            case 0: score = CalculateNumberScore(1); break;
            case 1: score = CalculateNumberScore(2); break;
            case 2: score = CalculateNumberScore(3); break;
            case 3: score = CalculateNumberScore(4); break;
            case 4: score = CalculateNumberScore(5); break;
            case 5: score = CalculateNumberScore(6); break;
            case 6: score = GetChoiceScore(); break;
            case 7: score = GetFourOfAKindScore(); break;
            case 8: score = GetFullHouseScore(); break;
            case 9: score = GetLittleStraightScore(); break;
            case 10: score = GetBigStraightScore(); break;
            case 11: score = GetYachtScore(); break;
        }

        totalScore += score;
        scoreUsed[index] = true;

        ResetDiceAndSlots();
        UpdateTotalScoreUI();
        NextRound();
        ChangeImageColor(index);
    }
    
    public void ChangeImageColor(int index)
    {
        if (index < 12)
        {
            scoreButtons[index].interactable = false;
        }
        
        if (buttonImages != null && buttonImages.Length > index)
        {
            buttonImages[index].color = selectedColor;
        }
    }
    
    public void ResetImageColors()
    {
        for (int i = 0; i < buttonImages.Length; i++)
        {
            if (buttonImages != null && buttonImages.Length > i)
            {
                buttonImages[i].color = originalColors[i];
            }
            scoreButtons[i].interactable = true;
        }
    }
    
    public void RestartGame()
    {
        currentRound = 1;
        currentRolls = 0;
        totalScore = 0;
        totalScoreText.text = "Score : 0";
        
        for (int i = 0; i < scoreUsed.Length; i++)
        {
            scoreUsed[i] = false;
            
            switch (i)
            {
                case 0: ScoreText[i].text = "Ones : " + CalculateNumberScore(1) + "점"; break;
                case 1: ScoreText[i].text = "Twos : " + CalculateNumberScore(2) + "점"; break;
                case 2: ScoreText[i].text = "Threes : " + CalculateNumberScore(3) + "점"; break;
                case 3: ScoreText[i].text = "Fours : " + CalculateNumberScore(4) + "점"; break;
                case 4: ScoreText[i].text = "Fives : " + CalculateNumberScore(5) + "점"; break;
                case 5: ScoreText[i].text = "Sixes : " + CalculateNumberScore(6) + "점"; break;
                case 6: ScoreText[i].text = "Choice : " + GetChoiceScore() + "점"; break;
                case 7: ScoreText[i].text = "Four of a Kind : " + GetFourOfAKindScore() + "점"; break;
                case 8: ScoreText[i].text = "Full House : " + GetFullHouseScore() + "점"; break;
                case 9: ScoreText[i].text = "Little Straight : " + GetLittleStraightScore() + "점"; break;
                case 10: ScoreText[i].text = "Big Straight : " + GetBigStraightScore() + "점"; break;
                case 11: ScoreText[i].text = "Yacht : " + GetYachtScore() + "점"; break;
            }
        }
        
        ResetDiceAndSlots();
        UpdateRoundUI();
        restartPanel.SetActive(false);
        ResetImageColors();
    }
    
    public void ResetDiceAndSlots()
    {
        for (int i = 0; i < diceNumbers.Length; i++)
        {
            diceNumbers[i] = 0;
        }
        isValueStored = false;
        hasRolled = false;
        diceNumber = 0;
        currentRolls = 0;

        UpdateTexts();
        UpdateRollCountUI();
    }
    
    public void SaveToSlot(int number)
    {
        for (int i = 0; i < diceNumbers.Length; i++)
        {
            if (diceNumbers[i] == 0)
            {
                diceNumbers[i] = number;
                UpdateTexts();
                break;
            }
        }
    }
    
    public void ResetSlot(int index)
    {
        if (currentRolls < maxRolls && index >= 0 && index < diceNumbers.Length)
        {
            diceNumbers[index] = 0;
            UpdateTexts();
            
            diceNumber = 0;
            isValueStored = false;
            hasRolled = false;
        }
    }

    public bool AreAllslotsFilled()
    {
        foreach (int num in diceNumbers)
        {
            if (num == 0) return false;
        }
        return true;
    }
    
    public void NextRound()
    {
        if (currentRound < maxRounds)
        {
            currentRound++;
            ResetDiceAndSlots();
            UpdateRoundUI();
        }
        else
        {
            ShowFinalScore();
        }
    }
    
    void UpdateRoundUI()
    {
        roundText.text = "라운드 : " + currentRound;
    }
    
    void ShowFinalScore()
    {
        CalculateScore();
        restartPanel.SetActive(true);
        restartPanel.GetComponentInChildren<Text>().text = "최종 점수 : " + totalScore + "점";
    }
    
    public void UpdateTexts()
    {
        for (int i = 0; i < slotTexts.Length; i++)
        {
            slotTexts[i].text = diceNumbers[i].ToString();
        }
        
        isValueStored = false;
    }
    
    
    public bool CanRollDice()
    {
        return currentRolls < maxRolls;
    }

    public void IncrementRollCount()
    {
        currentRolls++;
        UpdateRollCountUI();
    }

    public void UpdateRollCountUI()
    {
        int remaining = maxRolls - currentRolls;
        rollCountText.text = "남은 횟수 : " + remaining;
    }

    public int CalculateNumberScore(int targetNumber)
    {
        int score = 0;
        foreach (int number in diceNumbers)
        {
            if (number == targetNumber)
            {
                score += targetNumber;
            }
        }
        return score;
    }
    
    // Choice: 주사위 눈 총합
    public int GetChoiceScore()
    {
        int sum = 0;
        foreach (int number in diceNumbers)
        {
            sum += number;
        }
        return sum;
    }
    
    // Four of a Kind
    public int GetFourOfAKindScore()
    {
        var count = new int[7]; // 1~6 사용
        foreach (int num in diceNumbers)
        {
            count[num]++;
        }

        for (int i = 1; i <= 6; i++)
        {
            if (count[i] >= 4)
            {
                return i * 4;
            }
        }
        return 0;
    }
    
    // Full House
    public int GetFullHouseScore()
    {
        var count = new int[7];
        foreach (int num in diceNumbers)
        {
            count[num]++;
        }

        bool hasThree = false;
        bool hasTwo = false;

        foreach (int c in count)
        {
            if (c == 3) hasThree = true;
            if (c == 2) hasTwo = true;
        }

        if (hasThree && hasTwo)
        {
            int sum = 0;
            foreach (int n in diceNumbers)
                sum += n;
            return sum;
        }

        return 0;
    }
    
    // Little Straight: 1,2,3,4,5
    public int GetLittleStraightScore()
    {
        var required = new HashSet<int> { 1, 2, 3, 4, 5 };
        return new HashSet<int>(diceNumbers).SetEquals(required) ? 30 : 0;
    }
    
    // Big Straight: 2,3,4,5,6
    public int GetBigStraightScore()
    {
        var required = new HashSet<int> { 2, 3, 4, 5, 6 };
        return new HashSet<int>(diceNumbers).SetEquals(required) ? 30 : 0;
    }
    
    
    // Yacht: 같은 눈 5개
    public int GetYachtScore()
    {
        int[] counts = new int[7];

        foreach (int num in diceNumbers)
        {
            counts[num]++;
        }

        for (int i = 1; i <= 6; i++)
        {
            if (counts[i] == 5)
            {
                return 50;
            }
        }

        return 0;
    }
}