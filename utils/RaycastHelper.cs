#if DEBUG
using System;
using UnityEngine;

namespace WankulCrazyPlugin.utils
{
    /// <summary>
    /// Helper de raycast partagé entre les outils de débogage (WankulDevTools &amp; WankulGameBridge).
    /// Filtre les bloqueurs invisibles et priorise les items / étiquettes de prix.
    /// </summary>
    public static class RaycastHelper
    {
        public static RaycastHit? GetBestRaycastHit(Ray ray, float maxDistance = 30f)
        {
            return GetBestRaycastHit(ray, maxDistance, out _);
        }

        public static RaycastHit? GetBestRaycastHit(Ray ray, float maxDistance, out bool hadRawHits)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);
            if (hits == null || hits.Length == 0)
            {
                hadRawHits = false;
                return null;
            }

            hadRawHits = true;
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            RaycastHit? candidateWithRenderer = null;

            foreach (var hit in hits)
            {
                var col = hit.collider;
                if (col == null || col.isTrigger) continue;

                string colName = col.gameObject.name;
                string layerName = LayerMask.LayerToName(col.gameObject.layer);

                // Ignorer les bloqueurs de navigation, obstacles invisibles, triggers et navmesh
                if (colName.IndexOf("NavBlocker", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    colName.IndexOf("NavMesh", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    colName.IndexOf("Pathfinding", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    colName.IndexOf("Obstacle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    colName.IndexOf("Door_Obstacle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    layerName.Equals("NavMeshBlocker", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Cible prioritaire absolue : objet qui contient un Item ou un UI_PriceTag
                if (col.GetComponentInParent<Item>() != null || col.GetComponentInParent<UI_PriceTag>() != null)
                {
                    return hit;
                }

                // Cible secondaire : objet avec un Renderer actif
                if (candidateWithRenderer == null)
                {
                    var rend = col.GetComponent<Renderer>() ?? col.GetComponentInParent<Renderer>();
                    if (rend != null && rend.enabled)
                    {
                        candidateWithRenderer = hit;
                    }
                }
            }

            return candidateWithRenderer;
        }
    }
}
#endif
