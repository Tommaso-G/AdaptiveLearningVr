using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class SliderValueVisual : MonoBehaviour
{
    private TMP_Text valueText;
    [SerializeField]
    Slider slider;
    private bool firstTime = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        valueText = GetComponent<TMP_Text>();
    }

    public void UpdateValueText()
    {
        if (firstTime)
        {
            firstTime = false;
            valueText.text = " ";
            valueText.fontSize = 20f;
            valueText.color = Color.white;
            valueText.alpha = 1f;
            valueText.fontStyle = FontStyles.Bold;
        }

        float valueInt = slider.value;
        valueText.text = ((int)valueInt).ToString();
    }
}
