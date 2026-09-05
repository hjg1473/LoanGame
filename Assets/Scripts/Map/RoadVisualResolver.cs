using System.Collections.Generic;
using UnityEngine;

namespace LoadGame.Map
{
    public sealed class RoadVisualResolver
    {
        private readonly Dictionary<int, RoadVisualDefinition> definitionsByMask = new Dictionary<int, RoadVisualDefinition>();

        public RoadVisualResolver(RoadVisualDefinition[] definitions)
        {
            if (definitions == null)
            {
                return;
            }

            foreach (RoadVisualDefinition definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                definitionsByMask[definition.connectionMask & 15] = definition;
            }
        }

        public bool TryResolve(int connectionMask, out RoadVisualDefinition definition)
        {
            if (definitionsByMask.TryGetValue(connectionMask & 15, out definition))
            {
                return true;
            }

            int normalizedMask = NormalizeSparseMask(connectionMask);
            return definitionsByMask.TryGetValue(normalizedMask, out definition);
        }

        private static int NormalizeSparseMask(int connectionMask)
        {
            connectionMask &= 15;

            if (connectionMask == 1 || connectionMask == 4)
            {
                return 5;
            }

            if (connectionMask == 2 || connectionMask == 8)
            {
                return 10;
            }

            return connectionMask;
        }
    }
}
