# Roadmap & Future Features - Horizon-Faser

Este documento contiene propuestas de arquitectura y diseño para llevar la aplicación de Realidad Mixta de Horizon-Faser al siguiente nivel (Enterprise / Industria 4.0), mejorando la eficiencia de los técnicos en campo y la integración con el ecosistema Quarkbau.

## 1. Interacciones XR y Realidad Aumentada Avanzada

*   **Visión de "Rayos X" Subterránea:** 
    Aprovechando el *Passthrough* de Meta Quest 3 y las coordenadas de Cesium, implementar un *shader* especial en el suelo virtual. Esto permitiría al técnico mirar al asfalto de la calle y "ver debajo de la tierra" por dónde pasan exactamente los conductos, las *Muffes* (cajas de empalme) y las conexiones hacia el *HÜP* (Punto de entrega).
*   **Interacción Directa con Manos (Hand Tracking):** 
    En lugar de depender únicamente de los controladores y punteros (`XRPointerMagnet`), implementar interacciones físicas usando Meta XR y XR Interaction Toolkit: agarrar y mover un holograma de un *Netzverteiler* o hacer gestos de *Pinch-to-zoom* sobre el mapa de la ciudad.
*   **Menús Diegéticos (Wrist UI):** 
    Eliminar interfaces flotantes clásicas (paneles de Dashboard que bloquean la vista) y cambiarlas por menús holográficos que se desplieguen directamente en la muñeca o en la palma de la mano del técnico.
*   **Herramienta de Medición Espacial (AR Ruler):** 
    Función para marcar dos puntos físicos en el mundo real (ej. desde el HÜP hasta la acera) para que la app calcule instantáneamente los metros de cable o zanja que se necesitan.

## 2. Flujos de Trabajo de Campo y Negocio (Fieldwork)

*   **Soporte Offline Robusto (Sincronización Local):** 
    Implementar SQLite o Realm local. El técnico descarga los datos del *Cluster* asignado en la oficina, va al campo (sótanos o zanjas sin internet), trabaja en modo offline, y la app sincroniza automáticamente los cambios (`QuarkbauProject`, incidentes, etc.) al recuperar la conexión.
*   **Reporte de Incidentes con Anclajes Espaciales (Spatial Anchors):** 
    Permitir anclar un "holograma de alerta" permanentemente a las coordenadas exactas de un problema en la calle (usando `SpatialAnchorManager`), adjuntando fotos o audios. El siguiente equipo verá un ícono flotando exactamente donde está el problema físico.
*   **Asistente Holográfico de Empalmes (Splicing Guide):** 
    Al acercarse a un *Netzverteiler* o *Muffe*, la gafa superpone guías de colores indicando al operario qué cable empalmar con cuál (ej. *"El cable azul va al puerto B"*), reduciendo errores humanos.

## 3. Colaboración e Integraciones

*   **Asistencia de Experto Remoto (See-What-I-See):** 
    Integrar un protocolo de baja latencia (WebRTC, Agora o Photon). Un técnico novato llama a soporte; el ingeniero ve la cámara de la Quest 3 desde el `web-dashboard` y puede dibujar marcas en la pantalla web que aparecen en el mundo AR del técnico.
*   **Capas GIS Multidisciplinares (WMS/WFS):** 
    Agregar capas geográficas para mostrar instalaciones públicas de agua, gas y electricidad superpuestas en Cesium, previniendo cortes accidentales al excavar.

## 4. Pulido Arquitectónico (Código y Rendimiento)

*   **Agrupación Geométrica (Clustering / LOD) Dinámica:** 
    Sistema para agrupar marcadores lejanos (cuando un *Cluster* tiene miles de nodos) en un solo punto, separándolos a medida que el usuario se acerca, para mantener un rendimiento óptimo de FPS en las Quest 3.
*   **Sistema de Inyección de Dependencias (DI):** 
    Usar frameworks como *Zenject / Extenject* o *VContainer* para el manejo de repositorios (como `QuarkBauRemoteRepository`), autenticación y controladores de UI, haciendo el proyecto altamente modular y testeable.
