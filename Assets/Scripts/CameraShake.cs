using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    Vector3 originalLocalPos;
    Coroutine shakeRoutine;

    void Awake()
    {
        originalLocalPos = transform.localPosition;
    }

    public void Shake(float duration, float strength)
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine(duration, strength));
    }

    IEnumerator ShakeRoutine(float duration, float strength)
    {
        float t = 0f;

        while (t < duration)
        {
            Vector3 offset = Random.insideUnitSphere * strength;
            offset.z = 0f; // keep zoom stable

            transform.localPosition = originalLocalPos + offset;

            t += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalLocalPos;
        shakeRoutine = null;
    }
}
