using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace HorizonFaser.UI
{
    [AddComponentMenu("UI/Effects/Gradient")]
    public class UIGradient : BaseMeshEffect
    {
        public Color32 topColor = Color.white;
        public Color32 bottomColor = Color.black;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;

            var vertexList = new List<UIVertex>();
            vh.GetUIVertexStream(vertexList);
            int count = vertexList.Count;
            if (count == 0) return;

            float bottomY = vertexList[0].position.y;
            float topY = vertexList[0].position.y;

            for (int i = 1; i < count; i++)
            {
                float y = vertexList[i].position.y;
                if (y > topY) topY = y;
                else if (y < bottomY) bottomY = y;
            }

            float uiElementHeight = topY - bottomY;
            if (uiElementHeight <= 0) return;

            for (int i = 0; i < count; i++)
            {
                UIVertex uiVertex = vertexList[i];
                byte colorAlpha = uiVertex.color.a;
                Color32 color = Color32.Lerp(bottomColor, topColor, (uiVertex.position.y - bottomY) / uiElementHeight);
                color.a = (byte)((color.a * colorAlpha) / 255);
                uiVertex.color = color;
                vertexList[i] = uiVertex;
            }
            vh.Clear();
            vh.AddUIVertexTriangleStream(vertexList);
        }
    }
}
