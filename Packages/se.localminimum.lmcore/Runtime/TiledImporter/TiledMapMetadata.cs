﻿using LMCore.Extensions;
using System;
using System.IO;
using System.Xml.Linq;
using UnityEngine;

namespace LMCore.TiledImporter
{
    [Serializable]
    public class TiledMapMetadata
    {
        public Vector2Int MapSize;
        public Vector2Int TileSize;
        public string Class;
        public bool Infinite;
        public TiledCustomProperties CustomProperties;
        public string Source;

        public string Name => $"TiledMap - {Path.GetFileNameWithoutExtension(Source)}";

        public override string ToString() => $"<MapMetadata size={MapSize} infinite={Infinite} />";

        public static TiledMapMetadata From(XElement map, TiledEnums enums, string source) => map == null ? null :
            new TiledMapMetadata()
            {
                MapSize = map.GetVector2IntAttribute("width", "height"),
                TileSize = map.GetVector2IntAttribute("tilewidth", "tileheight"),
                Class = map.GetAttribute("class"),
                Infinite = map.GetBoolAttribute("infinite"),
                CustomProperties = TiledCustomProperties.From(map.Element("properties"), enums),
                Source = source
            };
    }
}
