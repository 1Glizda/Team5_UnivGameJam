using UnityEngine;
using Unity.Cinemachine;
using RW.MonumentValley;

[RequireComponent(typeof(CinemachineCamera))]
public class CameraFollowPlayer : MonoBehaviour
{
    [Tooltip("Leave empty to auto-find via PlayerController component.")]
    [SerializeField] private Transform target;

    [Tooltip("How quickly the camera catches up to the player (higher = snappier).")]
    [SerializeField] private float damping = 1f;

    private CinemachineCamera _vcam;

    private void Awake()
    {
        _vcam = GetComponent<CinemachineCamera>();
    }

    private void Start()
    {
        if (target == null)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
                target = player.transform;
        }

        if (target == null)
        {
            Debug.LogWarning("[CameraFollowPlayer] No player found in scene.", this);
            return;
        }

        _vcam.Follow = target;

        var composer = GetComponent<CinemachinePositionComposer>();
        if (composer == null)
            composer = gameObject.AddComponent<CinemachinePositionComposer>();

        // ScreenPosition is an offset from screen center, so (0,0) = centered
        var composition = composer.Composition;
        composition.ScreenPosition = Vector2.zero;
        composer.Composition = composition;

        composer.Damping = new Vector3(damping, damping, damping);
    }
}
