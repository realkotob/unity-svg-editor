using System.Collections.Generic;
using Unity.VectorGraphics;

namespace SvgEditor.Preview.Geometry
{
    internal readonly struct PreviewGeometryWorldContext
    {
        public PreviewGeometryWorldContext(
            IReadOnlyDictionary<SceneNode, int> drawOrderByNode,
            IReadOnlyDictionary<SceneNode, Matrix2D> worldTransformByNode,
            IReadOnlyDictionary<SceneNode, PreviewTessellatedNodeGeometry> worldGeometryByNode)
        {
            DrawOrderByNode = drawOrderByNode;
            WorldTransformByNode = worldTransformByNode;
            WorldGeometryByNode = worldGeometryByNode;
        }

        public IReadOnlyDictionary<SceneNode, int> DrawOrderByNode { get; }
        public IReadOnlyDictionary<SceneNode, Matrix2D> WorldTransformByNode { get; }
        public IReadOnlyDictionary<SceneNode, PreviewTessellatedNodeGeometry> WorldGeometryByNode { get; }
    }
}
