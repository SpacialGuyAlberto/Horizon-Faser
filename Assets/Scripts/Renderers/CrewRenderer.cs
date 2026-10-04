using UnityEngine;
using HorizonFaser.Models;
using CesiumForUnity;
using HorizonFaser.UI;

namespace HorizonFaser.Renderers
{
    public static class CrewRenderer
    {
        public static void RenderCrewOnSegment(QuarkBauSegment segment, GameObject parent)
        {
            // Verificamos si el segmento está en progreso o tiene una cuadrilla asignada
            bool isActive = segment.CurrentState == "IN_PROGRESS" || segment.AssignedCrewId != null;
            if (!isActive) return;

            if (segment.Geometry == null || segment.Geometry.Count < 2) return;

            // Buscar el punto medio del segmento para posicionar al crew
            int midIndex = segment.Geometry.Count / 2;
            var midPoint = segment.Geometry[midIndex];

            GameObject crewWrapper = new GameObject($"Crew_Segment_{segment.Id}");
            crewWrapper.transform.SetParent(parent.transform, false);
            
            var crewAnchor = crewWrapper.AddComponent<CesiumGlobeAnchor>();
            
            // Lo elevamos ligeramente del suelo
            double elevation = midPoint.AltitudeZ ?? 0;
            crewAnchor.longitudeLatitudeHeight = new Unity.Mathematics.double3(midPoint.Longitude, midPoint.Latitude, elevation + 2.0);

            // Intentar cargar el modelo estilizado desde Resources (ej. obrero o camioneta)
            // Ruta recomendada para cuando el usuario importe el modelo
            string modelPath = "Models/Crew/Meshy_Crew_Model"; 
            GameObject crewPrefab = Resources.Load<GameObject>(modelPath);
            GameObject crewVis;

            if (crewPrefab != null)
            {
                crewVis = UnityEngine.Object.Instantiate(crewPrefab);
                crewVis.transform.SetParent(crewWrapper.transform, false);
                crewVis.transform.localScale = new Vector3(300f, 300f, 300f);

                // Asignar el shader URP lit para que se vea integrado en el entorno
                Material urpMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                Texture2D tex = Resources.Load<Texture2D>(modelPath);
                if (tex != null) urpMat.mainTexture = tex;
                
                urpMat.SetFloat("_Metallic", 0.0f);
                urpMat.SetFloat("_Smoothness", 0.3f); // Estilo suave/mate (Apple-like)

                foreach (var r in crewVis.GetComponentsInChildren<Renderer>())
                {
                    r.material = urpMat;
                }
            }
            else
            {
                // FALLBACK: Si no existe el modelo aún, dibujamos una cápsula estilizada que representa al obrero
                crewVis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                crewVis.transform.SetParent(crewWrapper.transform, false);
                // Escala masiva para que sea visible desde el cielo
                crewVis.transform.localScale = new Vector3(8f, 12f, 8f);
                
                // Color corporativo brillante (Naranja/Amarillo de alta visibilidad)
                Material fallbackMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                fallbackMat.color = new Color(1f, 0.6f, 0.0f); // Naranja seguridad
                fallbackMat.SetFloat("_Smoothness", 0.5f); // Toque plástico suave
                crewVis.GetComponent<MeshRenderer>().material = fallbackMat;
                
                // Quitamos el collider físico del fallback para que no moleste al láser XR
                UnityEngine.Object.Destroy(crewVis.GetComponent<Collider>());
            }

            // Añadir scripts de UI XR (Rotación suave y agrandamiento al mirar)
            var rotator = crewWrapper.AddComponent<IconRotator>();
            rotator.rotationSpeed = 20f; // Rotación tranquila

            var gaze = crewVis.AddComponent<GazeScaler>();
            gaze.baseScale = crewVis.transform.localScale;
            gaze.hoverScale = crewVis.transform.localScale * 1.8f; // Crece al mirarlo
        }
    }
}
