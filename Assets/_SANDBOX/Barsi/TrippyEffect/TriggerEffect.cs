using UnityEngine;
using RW.MonumentValley;
using Unity.Cinemachine;

public class TriggerEffect : MonoBehaviour
{
    public CinemachineCamera cam;
    public Material fullscreenMaterial;
    public GameObject player;
    public PlayerController playerController;

    public float wiggleAmount = 0.05f;
    public float wiggleSpeed = 2f;

    public float blurBase = 2f;
    public float blurAmplitude = 0.5f;

    private bool isActive = false;
    private Vector3 originalPos;

    void Start()
    {
        if (cam != null)
            originalPos = cam.transform.localPosition;

        DisableEffects();

        if (playerController != null)
        {
            playerController.onSpecialStateToggled.AddListener(OnSpecialStateChanged);
        }
        else
        {
            Debug.LogWarning("[TriggerEffect] PlayerController is not assigned!");
        }
    }

    void OnDestroy()
    {
        if (playerController != null)
        {
            playerController.onSpecialStateToggled.RemoveListener(OnSpecialStateChanged);
        }
    }

    void Update()
    {
        if (fullscreenMaterial != null && player != null && cam != null)
        {
            fullscreenMaterial.SetVector("_PlayerPos", player.transform.position);

            float dist = Vector3.Distance(cam.transform.position, player.transform.position);
            fullscreenMaterial.SetFloat("_Distance", dist);

            fullscreenMaterial.SetFloat("_FOV", cam.Lens.FieldOfView);
        }

        if (isActive && cam != null)
        {
            float time = Time.time;

            float x = Mathf.Sin(time * wiggleSpeed) * wiggleAmount;
            float y = Mathf.Cos(time * wiggleSpeed * 1.3f) * wiggleAmount;

            cam.transform.localPosition = originalPos + new Vector3(x, y, 0);

            if (fullscreenMaterial != null)
            {
                float blur = blurBase + Mathf.Sin(time * 2f) * blurAmplitude;
                fullscreenMaterial.SetFloat("_BlurStrength", blur);
            }
        }
    }

    void OnSpecialStateChanged(bool state)
    {
        isActive = state;

        if (fullscreenMaterial != null)
        {
            fullscreenMaterial.SetFloat("_isOn", state ? 1f : 0f);
        }

        if (!state)
        {
            DisableEffects();
        }
    }

    void DisableEffects()
    {
        if (cam != null)
        {
            cam.transform.localPosition = originalPos;
        }

        if (fullscreenMaterial != null)
        {
            fullscreenMaterial.SetFloat("_BlurStrength", 0f);
            fullscreenMaterial.SetFloat("_isOn", 0f);
        }
    }
}