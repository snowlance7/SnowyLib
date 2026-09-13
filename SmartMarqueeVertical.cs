using System.Collections;
using UnityEngine;

namespace SnowyLib
{
    public class VerticalSmartMarquee : MonoBehaviour
    {
        RectTransform _transform = null!, _parent = null!;

        [SerializeField] float _stoppedTime = 1f, _movingTime = 2f;

        [SerializeField] bool snapBottom;

        Coroutine? _currentAnim;

        void Awake()
        {
            _transform = GetComponent<RectTransform>();
            _parent = transform.parent.GetComponent<RectTransform>();
        }

        void Update()
        {
            if (_parent.rect.height < _transform.rect.height)
            {
                if (_currentAnim != null)
                    return;

                _transform.anchoredPosition = new Vector2(_transform.anchoredPosition.x, (_transform.rect.height - _parent.rect.height) / 2f);
                _currentAnim = StartCoroutine(DoAnimation());
            }
            else if (_currentAnim != null)
            {
                StopCoroutine(_currentAnim);
                _currentAnim = null;
                _transform.localPosition = Vector3.zero;
            }
        }

        IEnumerator DoAnimation()
        {
            // Wait X seconds
            yield return new WaitForSeconds(_stoppedTime);

            float t = 0;

            float distance = _transform.rect.height - _parent.rect.height;

            Vector3 step = Vector3.down * distance / _movingTime;

            // Move up for Y seconds
            while (t < _movingTime)
            {
                t += Time.deltaTime;

                _transform.localPosition += step * Time.deltaTime;

                yield return null;
            }

            // Wait for X seconds
            yield return new WaitForSeconds(_stoppedTime);

            if (!snapBottom)
            {
                // Move back down for Y seconds
                while (t > 0)
                {
                    t -= Time.deltaTime;

                    _transform.localPosition -= step * Time.deltaTime;

                    yield return null;
                }
            }

            _currentAnim = null;
        }
    }
}