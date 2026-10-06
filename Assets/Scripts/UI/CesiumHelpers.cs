using UnityEngine;
using CesiumForUnity;

namespace HorizonFaser.UI
{
    public class SegmentConnector : MonoBehaviour
    {
        public Transform PointA;
        public Transform PointB;
        public float Thickness = 0.5f;

        /// <summary>
        /// Grosor angular mínimo (radianes) que debe ocupar la tubería en pantalla.
        /// 0.004 rad ≈ 0.23° ≈ 4-6 px en Quest 2/3. Evita que la tubería quede por
        /// debajo de 1 píxel a distancia y "parpadee" (shimmering). De cerca se usa
        /// el grosor real (Thickness). Pon 0 para desactivarlo.
        /// </summary>
        public float MinAngularThickness = 0.004f;

        private Transform _cam;

        void Update()
        {
            if (PointA == null || PointB == null) return;
            
            Vector3 posA = PointA.position;
            Vector3 posB = PointB.position;
            float distance = Vector3.Distance(posA, posB);

            float effectiveThickness = Thickness;
            if (MinAngularThickness > 0f)
            {
                if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
                if (_cam != null && distance >= 0.01f)
                {
                    // Distancia de la cámara al punto más cercano del tramo A-B
                    Vector3 ab = posB - posA;
                    float t = Mathf.Clamp01(Vector3.Dot(_cam.position - posA, ab) / ab.sqrMagnitude);
                    float camDist = Vector3.Distance(_cam.position, posA + ab * t);
                    effectiveThickness = Mathf.Max(Thickness, camDist * MinAngularThickness);
                }
            }
            
            if (distance < 0.01f) 
            {
                // Si Cesium aún no ha calculado la distancia real, lo escondemos
                transform.localScale = Vector3.zero;
                return;
            }

            Vector3 centerPos = (posA + posB) / 2f;
            transform.position = centerPos;

            Vector3 direction = (posB - posA).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
                // Si la malla original tenía rotación previa (ej: Z=90 o X=90) se ajustó en su padre/hijo.
                // Aquí rotamos el contenedor global para apuntar de A a B.
            }

            // Dado que nuestro collider y el visualizador asumen 2 unidades de alto, dividimos la distancia real entre 2
            transform.localScale = new Vector3(effectiveThickness, distance / 2f, effectiveThickness);
            // IMPORTANTE: Como usamos el eje Y para alargar (Cylinder nativo o el FBX corregido), 
            // LookRotation nos apunta el eje Z hacia adelante. Para que el cilindro (Y-up) se acueste sobre Z:
            transform.Rotate(90f, 0f, 0f, Space.Self);
        }
    }

    public class NodePositioner : MonoBehaviour
    {
        public CesiumForUnity.CesiumGlobeAnchor Anchor;
        public Vector3 NodeScale = new Vector3(5f, 5f, 5f);

        private bool _positioned = false;

        void Update()
        {
            if (_positioned) return;
            if (Anchor == null) return;

            Vector3 pos = transform.position;
            float distFromOrigin = pos.magnitude;

            // Cuando Cesium calcule la posición GPS real (ya no estará en 0,0,0)
            if (distFromOrigin > 0.5f)
            {
                transform.localScale = NodeScale;
                _positioned = true;
                Debug.Log($"[Cesium] Nodo '{gameObject.name}' posicionado correctamente.");
            }
        }
    }

    public class IconRotator : MonoBehaviour
    {
        public float rotationSpeed = 30f;
        
        void Update()
        {
            // Rota sobre el eje Y global/local suavemente
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
        }
    }

    public class GazeScaler : MonoBehaviour
    {
        public Vector3 baseScale = new Vector3(300f, 300f, 300f);
        public Vector3 hoverScale = new Vector3(1000f, 1000f, 1000f);
        public float scaleSpeed = 8f;
        public float lookThreshold = 0.96f; // Aprox 15 grados de cono de visión

        private Transform _cameraTransform;

        void Start()
        {
            if (Camera.main != null) _cameraTransform = Camera.main.transform;
            transform.localScale = baseScale;
        }

        void Update()
        {
            if (_cameraTransform == null) return;

            Vector3 toIcon = (transform.position - _cameraTransform.position).normalized;
            float dot = Vector3.Dot(_cameraTransform.forward, toIcon);

            Vector3 targetScale = (dot > lookThreshold) ? hoverScale : baseScale;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
        }
    }
}
