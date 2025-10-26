using UnityEngine;
using UnityEngine.UI;

public class SpinnerController : MonoBehaviour
{
    private bool spinnerEnabled;

    private Image spinnerImg;
    private RectTransform spinnerRect;

    void Start()
    {
        spinnerEnabled = gameObject.activeInHierarchy;

        spinnerImg = GetComponent<Image>();
        spinnerRect = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (spinnerEnabled)
        {
            spinnerImg.fillAmount = (spinnerImg.fillAmount + Time.deltaTime) % 1;
            spinnerRect.eulerAngles += Vector3.back * (Time.deltaTime * 180f);
        }
    }

    void OnEnable()
    {
        spinnerEnabled = true;
    }

    void OnDisable()
    {
        spinnerEnabled = false;
    }
}