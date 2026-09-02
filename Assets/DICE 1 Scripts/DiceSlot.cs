using UnityEngine;
using UnityEngine.UI;

public class DiceSlot : MonoBehaviour
{
    public int index;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            GameManager.Instance.ResetSlot(index);
        });
    }

    void ResetSlot()
    {
        GameManager.Instance.ResetSlot(index);
    }
}
