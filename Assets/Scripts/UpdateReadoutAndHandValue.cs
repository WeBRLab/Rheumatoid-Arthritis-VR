using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpdateReadoutAndHandValue : MonoBehaviour
{
    
    public ArthritisHandVisualizer visualizer;
    public Slider slider;
    public TextMeshProUGUI tmp;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnNumberChanged()
    {
        tmp.text = slider.value.ToString();
        visualizer.fingerRotationSpeed = slider.value;
    }
}
