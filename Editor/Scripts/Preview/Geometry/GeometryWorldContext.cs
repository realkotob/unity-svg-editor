using System.Collections.Generic;
using Unity.VectorGraphics;
using Core.UI.Extensions;

namespace SvgEditor.Preview.Geometry
{
    internal readonly struct GeometryWorldContext
    {
        public GeometryWorldContext(
            IReadOnlyDictionary<SceneNode, int> drawOrderByNode,
            IReadOnlyDictionary<SceneNode, Matrix2D> worldTransformByNode,
            IReadOnlyDictionary<SceneNode, TessellatedNodeGeometry> worldGeometryByNode)
        {
            DrawOrderByNode = drawOrderByNode;
            WorldTransformByNode = worldTransformByNode;
            WorldGeometryByNode = worldGeometryByNode;
        }

        public IReadOnlyDictionary<SceneNode, int> DrawOrderByNode { get; }
        public IReadOnlyDictionary<SceneNode, Matrix2D> WorldTransformByNode { get; }
        public IReadOnlyDictionary<SceneNode, TessellatedNodeGeometry> WorldGeometryByNode { get; }
    }
}
