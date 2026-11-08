using UnityEngine;
using UnityEngine.UI;

namespace TacticaUniversalis.Presentation
{
    /// <summary>Triangle-fan view cone whose rim can be clipped by map collision.</summary>
    public sealed class ViewConeMeshGraphic : MaskableGraphic
    {
        private Vector2[] _rim = new Vector2[24];
        private int _count;

        public void SetFan(Color tint, Vector2[] rim, int count)
        {
            color = tint;
            if (rim == null || count < 2)
            {
                _count = 0;
                SetVerticesDirty();
                return;
            }

            if (_rim == null || _rim.Length < count)
                _rim = new Vector2[count];
            for (int i = 0; i < count; i++)
                _rim[i] = rim[i];
            _count = count;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_count < 2)
                return;

            Color32 tint = color;
            var v = UIVertex.simpleVert;
            v.color = tint;
            v.position = Vector3.zero;
            vh.AddVert(v);
            for (int i = 0; i < _count; i++)
            {
                v.position = new Vector3(_rim[i].x, _rim[i].y, 0f);
                vh.AddVert(v);
            }

            for (int i = 0; i < _count - 1; i++)
                vh.AddTriangle(0, i + 1, i + 2);
        }
    }
}
