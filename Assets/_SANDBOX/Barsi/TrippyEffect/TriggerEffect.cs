using UnityEngine;

public class TriggerEffect : MonoBehaviour
{
    public Camera cam;
    public Material fullscreenMaterial;
    public GameObject player;

    public float wiggleAmount = 0.05f;
    public float wiggleSpeed = 2f;

    public float blurBase = 2f;
    public float blurAmplitude = 0.5f;

    private bool isActive = false;
    private Vector3 originalPos;

    void Start()
    {
        originalPos = cam.transform.localPosition;

        DisableEffects();
    }

    void Update()
    {
        if (fullscreenMaterial != null)
        {
            fullscreenMaterial.SetVector("_PlayerPos", player.transform.position);
        }

            if (Input.GetKeyDown(KeyCode.Space))
        {
            isActive = !isActive;
            if (fullscreenMaterial != null)
            {
                fullscreenMaterial.SetFloat("_isOn", isActive ? 1f : 0f);
                fullscreenMaterial.SetFloat("_BlurStrength", 0.01f);
            }
        }

        if (isActive)
        {
            float time = Time.time;

            float x = Mathf.Sin(time * wiggleSpeed) * wiggleAmount;
            float y = Mathf.Cos(time * wiggleSpeed * 1.3f) * wiggleAmount;
            cam.transform.localPosition = originalPos + new Vector3(x, y, 0);
        }
    }

    void DisableEffects()
    {
        cam.transform.localPosition = originalPos;
    }
}