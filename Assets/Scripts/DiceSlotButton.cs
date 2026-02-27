using UnityEngine;
using UnityEngine.UI;

public class DiceSlotButton : MonoBehaviour
{
    public int index;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => {
            GameManager.Instance.ResetSlot(index);
        });
    }
}