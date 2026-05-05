using UnityEngine;
using Unity.Cinemachine; 

public class CameraController : MonoBehaviour
{
    [Header("References")]
    public CinemachineCamera vcam;
    private CinemachinePositionComposer composer;

    [Header("Hollow Knight Look Settings")]
    public float lookOffsetAmount = 0.2f;
    public float lookLerpSpeed = 4f;

    private float originalScreenY;
    private float targetScreenY;

    void Start()
    {
        composer = vcam.GetComponent<CinemachinePositionComposer>();
        originalScreenY = composer.Composition.ScreenPosition.y;
    }

    void Update()
    {
        float vInput = Input.GetAxisRaw("Vertical");

        if (vInput > 0) 
            targetScreenY = originalScreenY + lookOffsetAmount;
        else if (vInput < 0) 
            targetScreenY = originalScreenY - lookOffsetAmount;
        else
            targetScreenY = originalScreenY;

        Vector2 currentPos = composer.Composition.ScreenPosition;
        currentPos.y = Mathf.Lerp(currentPos.y, targetScreenY, Time.deltaTime * lookLerpSpeed);
        composer.Composition.ScreenPosition = currentPos;
    }
}
