using UnityEngine;

namespace Benjathemaker
{
    public class SimpleGemsAnim : MonoBehaviour
    {
        public bool isRotating = false;
        public bool rotateX = false;
        public bool rotateY = false;
        public bool rotateZ = false;
        public float rotationSpeed = 90f;

        public bool isFloating = false;
        public bool useEasingForFloating = false;
        public float floatHeight = 1f;
        public float floatSpeed = 1f;

        public Vector3 startScale = Vector3.one;
        public Vector3 endScale = Vector3.one;
        public bool isScaling = false;
        public bool useEasingForScaling = false;
        public float scaleLerpSpeed = 1f;

        private Vector3 initialPosition;
        private float floatTimer;
        private float scaleTimer;

        // Rotation vektörünü her frame new Vector3 ile yaratmak yerine bir kere sakla
        private Vector3 rotationVector;

        void Awake()
        {
            rotationVector = new Vector3(
                rotateX ? 1 : 0,
                rotateY ? 1 : 0,
                rotateZ ? 1 : 0);
        }

        // OnEnable: SetActive(true) her çaðrýldýðýnda çalýþýr.
        // Start sadece ilk seferde çalýþýr — pooling ile kullanýlan objelerde
        // pozisyon/timer sýfýrlama için OnEnable þart.
        void OnEnable()
        {
            initialPosition = transform.position;
            floatTimer = 0f;
            scaleTimer = 0f;
        }

        void Update()
        {
            if (isRotating)
                transform.Rotate(rotationVector * (rotationSpeed * Time.deltaTime));

            if (isFloating)
            {
                floatTimer += Time.deltaTime * floatSpeed;
                float t = Mathf.PingPong(floatTimer, 1f);
                if (useEasingForFloating) t = EaseInOutQuad(t);

                // new Vector3 yerine direkt set — heap allocation yok
                Vector3 pos = initialPosition;
                pos.y += t * floatHeight;
                transform.position = pos;
            }

            if (isScaling)
            {
                scaleTimer += Time.deltaTime * scaleLerpSpeed;
                float t = Mathf.PingPong(scaleTimer, 1f);
                if (useEasingForScaling) t = EaseInOutQuad(t);
                transform.localScale = Vector3.Lerp(startScale, endScale, t);
            }
        }

        float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        }
    }
}