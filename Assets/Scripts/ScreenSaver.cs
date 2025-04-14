using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScreenSaver : MonoBehaviour
{
    [SerializeField]
    Color[] colors;

    [SerializeField]
    Image logo;

    [SerializeField]
    TextMeshProUGUI brand;

    [SerializeField]
    float speed = 3f;

    [SerializeField]
    Vector2 padding;

    [SerializeField]
    float colorStayTime = 1f;

    [SerializeField]
    float colorLerpTime = 2f;

    Vector2 motionVector;
    int colorIdx;
    int nextColorIdx;
    bool nextColorIsLerp;
    float colorStepStart;

    private void Start()
    {
        var angle = Random.value * Mathf.PI * 2f;
        motionVector = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));

        logo.color = colors[0];
        brand.color = colors[0];

        nextColorIsLerp = true;
        colorStepStart = Time.timeSinceLevelLoad;
    }

    private void Update()
    {
        if (nextColorIsLerp && Time.timeSinceLevelLoad >= colorStepStart + colorStayTime)
        {
            nextColorIsLerp = false;
            colorStepStart = Time.timeSinceLevelLoad;
            nextColorIdx = (colorIdx + 1) % colors.Length;
        } else if (!nextColorIsLerp)
        {
            var progress = Mathf.Clamp01((Time.timeSinceLevelLoad - colorStepStart) / colorLerpTime);
            var color = Color.Lerp(colors[colorIdx], colors[nextColorIdx], progress);

            logo.color = color;
            brand.color = color;

            if (progress == 1f)
            {
                nextColorIsLerp = true;
                colorStepStart = Time.timeSinceLevelLoad;
            }
        }

        var rt = transform as RectTransform;

        var delta = speed * Time.deltaTime * motionVector;
        rt.anchoredPosition += delta;

        var canvasRt = GetComponentInParent<Canvas>().transform as RectTransform;
        var xBounds = canvasRt.rect.width / 2 - padding.x;
        var yBounds = canvasRt.rect.height / 2 - padding.y;
        var pos = rt.anchoredPosition;

        if (pos.x < -xBounds)
        {
            motionVector.x = Mathf.Abs(motionVector.x);
        }
        else if (pos.x > xBounds)
        {
            motionVector.x = -Mathf.Abs(motionVector.x);
        }

        if (pos.y < -yBounds)
        {
            motionVector.y = Mathf.Abs(motionVector.y);
        }
        else if (pos.y > yBounds)
        {
            motionVector.y = -Mathf.Abs(motionVector.y);
        }
        
    }
}
