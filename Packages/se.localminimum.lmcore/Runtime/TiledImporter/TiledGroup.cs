﻿using LMCore.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace LMCore.TiledImporter
{
    [Serializable]
    public class TiledGroup
    {
        public string Name;
        public int Id;
        public List<TiledLayer> Layers;
        public List<TiledObjectLayer> ObjectLayers;

        // TODO: Probably use same strategy as with SerializableDictionary to support nesting groups
        // or if that doesn't work we can subclass to ahve some nesting levels should we really want them
        // public List<TiledGroup> Groups;
        public TiledCustomProperties CustomProperties;

        public string LayerNames() => string.Join(", ", Layers.Select(layer => layer.Name));

        public static Func<TiledGroup, bool> ShouldBeImported(bool filterLayerImport)
        {
            return (TiledGroup group) => !filterLayerImport || (group?.CustomProperties?.Bools?.GetValueOrDefault("Imported") ?? false);
        }

        public static Func<XElement, TiledGroup> FromFactory(TiledEnums enums, bool filterImport, Vector2Int scaling)
        {
            return (XElement group) => From(group, enums, filterImport, scaling);
        }

        public static TiledGroup From(XElement group, TiledEnums enums, bool filterImport, Vector2Int scaling)
        {
            if (group == null) return null;

            var customProps = TiledCustomProperties.From(group.Element("properties"), enums);

            return new TiledGroup()
            {
                Id = group.GetIntAttribute("id"),
                Name = group.GetAttribute("name"),
                Layers = group.HydrateElementsByName("layer", TiledLayer.FromFactory(enums), TiledLayer.ShouldBeImported(filterImport)).ToList(),
                ObjectLayers = group.HydrateElementsByName("objectgroup", TiledObjectLayer.FromFactory(enums, scaling), TiledObjectLayer.ShouldBeImported(filterImport)).ToList(),
                // Groups = group.HydrateElementsByName("group", FromFactory(enums, filterImport), ShouldBeImported(filterImport)).ToList(),
                CustomProperties = customProps,
            };
        }
    }
}
