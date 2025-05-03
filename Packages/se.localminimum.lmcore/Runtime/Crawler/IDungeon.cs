using System.Collections.Generic;
using UnityEngine;

namespace LMCore.Crawler
{
    public interface IDungeon  : IGridSizeProvider
    {
        public string MapName { get; }

        public IDungeonNode this[Vector3Int coordinates] { get; }
        public bool HasNodeAt(Vector3Int coordinates);
        public List<IDungeonNode> FindTeleportersById(int id);

        public Vector3 Position(GridEntity entity);
        public Vector3 Position(Vector3Int coordinates, Direction anchor, bool rotationRespectsAnchorDirection);

        public Vector3Int PlayerStartPosition { get; }
        public Direction PlayerStartDirection { get; }

        public GridEntity GetEntity(string identifier, bool includeDisabled = false);

        /// <summary>
        /// The registered player entity
        /// </summary>
        public GridEntity Player { get; }
    }
}
