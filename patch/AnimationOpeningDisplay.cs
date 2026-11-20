using UnityEngine;
using System.Collections;

namespace WankulCrazyPlugin.patch
{
    public class AnimationOpeningDisplay : MonoBehaviour
    {
        private IEnumerator AnimateChild(Transform child)
        {
            if (child == null)
            {
                yield break;
            }

            // Pivot sur l'arête supérieure arrière du display vertical (échelle 0.10, abaissé à y = -0.035) :
            // à angle = 0°, le couvercle repose à plat sur le dessus de la boîte (0.003, 0.0238, 0).
            Vector3 hingePivot = new Vector3(0.003f, 0.0238f, -0.046f);
            const float lidHalfDepth = 0.046f;
            const float startAngle = 0f;
            const float endAngle = 95f;
            const float duration = 1.0f;

            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                if (child == null)
                {
                    yield break;
                }

                float t = Mathf.SmoothStep(0f, 1f, elapsedTime / duration);
                float angle = Mathf.Lerp(startAngle, endAngle, t);
                float rad = angle * Mathf.Deg2Rad;

                child.localPosition = new Vector3(
                    hingePivot.x,
                    hingePivot.y + lidHalfDepth * Mathf.Sin(rad),
                    hingePivot.z + lidHalfDepth * Mathf.Cos(rad)
                );
                child.localRotation = Quaternion.Euler(angle, 180f, 0f);

                elapsedTime += Mathf.Min(Time.deltaTime, 0.05f);
                yield return null;
            }

            if (child == null)
            {
                yield break;
            }

            float endRad = endAngle * Mathf.Deg2Rad;
            child.localPosition = new Vector3(
                hingePivot.x,
                hingePivot.y + lidHalfDepth * Mathf.Sin(endRad),
                hingePivot.z + lidHalfDepth * Mathf.Cos(endRad)
            );
            child.localRotation = Quaternion.Euler(endAngle, 180f, 0f);
        }

        public void StartAnimation(Transform child)
        {
            StopAllCoroutines();
            StartCoroutine(AnimateChild(child));
        }
    }
}
