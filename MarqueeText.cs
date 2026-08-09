using TMPro;
using UnityEngine;

public class MarqueeText : MonoBehaviour
{
    [SerializeField] private TMP_Text text = null!;
    [SerializeField] private float speed = 1f;
    [SerializeField] private float gap = 1f;

    private RectTransform textRect;
    private RectTransform viewport;

    public void Awake()
    {
        textRect = text.rectTransform;
        viewport = GetComponent<RectTransform>();
    }

    public void Start()
    {
        textRect.anchoredPosition = new Vector2(
            viewport.rect.width,
            textRect.anchoredPosition.y
        );
    }

    public void Update()
    {
        textRect.anchoredPosition += Vector2.left * speed * Time.deltaTime;

        float textRight = textRect.anchoredPosition.x + textRect.rect.width;

        if (textRight < 0f)
        {
            textRect.anchoredPosition = new Vector2(
                viewport.rect.width + gap,
                textRect.anchoredPosition.y
            );
        }
    }
}