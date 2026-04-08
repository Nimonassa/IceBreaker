using System;
using System.Collections.Generic;
using UnityEngine;

public static class BreakableIceShardGenerator
{
    private const string ShardNamePrefix = "IceShard_";

    public struct GenerationSettings
    {
        public int minShardCount;
        public int maxShardCount;
        public float outerPadding;
        public float shardInset;
        public float shardGap;
        public float splitJitter;
        public float shardThickness;
        public float minimumShardArea;
        public float minimumShardFootprint;
    }

    public struct SelectionSettings
    {
        public float breakRadius;
        public float edgeNoise;
        public float edgeSpikiness;
        public int seed;
    }

    public static BreakableIceShardPiece[] CollectGeneratedShards(GameObject shardRoot)
    {
        if (shardRoot == null)
            return Array.Empty<BreakableIceShardPiece>();

        return shardRoot.GetComponentsInChildren<BreakableIceShardPiece>(true);
    }

    public static BreakableIceShardPiece[] SelectShardPiecesForBreak(
        BreakableIceShardPiece[] pieces,
        Vector2 impactLocalPoint,
        SelectionSettings settings)
    {
        if (pieces == null || pieces.Length == 0)
            return Array.Empty<BreakableIceShardPiece>();

        List<BreakableIceShardPiece> selectedPieces = new(pieces.Length);
        List<(BreakableIceShardPiece piece, float distanceSquared)> candidates = new(pieces.Length);
        BreakableIceShardPiece nearestPiece = null;
        float nearestDistanceSquared = float.MaxValue;

        foreach (BreakableIceShardPiece piece in pieces)
        {
            if (piece == null)
                continue;

            Vector2 offset = piece.LocalCenter - impactLocalPoint;
            float distanceSquared = offset.sqrMagnitude;
            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearestPiece = piece;
            }

            candidates.Add((piece, distanceSquared));

            float angle = Mathf.Atan2(offset.y, offset.x);
            float threshold = EvaluateBreakRadius(angle, settings);
            float effectiveRadius = threshold + piece.InfluenceRadius * 0.55f;
            if (distanceSquared <= effectiveRadius * effectiveRadius)
                selectedPieces.Add(piece);
        }

        if (selectedPieces.Count == 0 && nearestPiece != null)
            selectedPieces.Add(nearestPiece);

        int minimumShardCount = Mathf.Clamp(Mathf.RoundToInt(pieces.Length * 0.28f), 4, 7);
        if (selectedPieces.Count < minimumShardCount)
        {
            candidates.Sort((left, right) => left.distanceSquared.CompareTo(right.distanceSquared));
            foreach ((BreakableIceShardPiece piece, _) in candidates)
            {
                if (piece == null || selectedPieces.Contains(piece))
                    continue;

                selectedPieces.Add(piece);
                if (selectedPieces.Count >= minimumShardCount)
                    break;
            }
        }

        return selectedPieces.ToArray();
    }

    public static void RegenerateShards(
        GameObject shardRoot,
        Material shardMaterial,
        Vector3 surfaceCenter,
        float intactWidth,
        float intactDepth,
        int seed,
        GenerationSettings settings)
    {
        ClearGeneratedShards(shardRoot);
        if (shardRoot == null || shardMaterial == null)
            return;

        float usableWidth = Mathf.Max(
            settings.minimumShardFootprint * 2f,
            intactWidth - settings.outerPadding * 2f - settings.shardInset * 2f);
        float usableDepth = Mathf.Max(
            settings.minimumShardFootprint * 2f,
            intactDepth - settings.outerPadding * 2f - settings.shardInset * 2f);

        if (usableWidth <= settings.minimumShardFootprint || usableDepth <= settings.minimumShardFootprint)
            return;

        int minShardCount = Mathf.Max(4, settings.minShardCount);
        int maxShardCount = Mathf.Max(minShardCount, settings.maxShardCount);
        System.Random random = new(seed);
        int targetShardCount = random.Next(minShardCount, maxShardCount + 1);

        List<Vector2> shardSites = GenerateShardSites(random, targetShardCount, usableWidth, usableDepth, settings);
        List<List<Vector2>> shardPolygons = BuildVoronoiCells(shardSites, usableWidth, usableDepth);

        int shardIndex = 1;
        foreach (List<Vector2> polygon in shardPolygons)
        {
            if (polygon == null || polygon.Count < 3)
                continue;

            List<Vector2> insetPolygon = InsetConvexPolygon(polygon, settings.shardGap * 0.5f);
            if (insetPolygon.Count < 3)
                continue;

            float polygonArea = Mathf.Abs(ComputePolygonSignedArea(insetPolygon));
            if (polygonArea < settings.minimumShardArea)
                continue;

            float yOffset = Mathf.Lerp(-0.005f, 0.005f, (float)random.NextDouble());
            float thickness = settings.shardThickness * Mathf.Lerp(0.92f, 1.08f, (float)random.NextDouble());
            CreateShard(
                shardRoot,
                shardMaterial,
                $"{ShardNamePrefix}{shardIndex:00}",
                surfaceCenter,
                insetPolygon,
                yOffset,
                thickness);

            shardIndex++;
        }
    }

    public static void ClearGeneratedShards(GameObject shardRoot)
    {
        if (shardRoot == null)
            return;

        List<GameObject> shardObjects = new();
        foreach (Transform child in shardRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child == null || child == shardRoot.transform)
                continue;

            if (child.name.StartsWith(ShardNamePrefix, StringComparison.Ordinal))
                shardObjects.Add(child.gameObject);
        }

        foreach (GameObject shardObject in shardObjects)
        {
            if (shardObject == null)
                continue;

            MeshFilter meshFilter = shardObject.GetComponent<MeshFilter>();
            Mesh generatedMesh = meshFilter != null ? meshFilter.sharedMesh : null;
            if (generatedMesh != null)
            {
                if (meshFilter != null)
                    meshFilter.sharedMesh = null;

                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(generatedMesh);
                else
                    UnityEngine.Object.DestroyImmediate(generatedMesh);
            }

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(shardObject);
            else
                UnityEngine.Object.DestroyImmediate(shardObject);
        }
    }

    private static void CreateShard(
        GameObject shardRoot,
        Material shardMaterial,
        string shardName,
        Vector3 surfaceCenter,
        List<Vector2> polygon,
        float yOffset,
        float thickness)
    {
        Vector2 centroid = ComputePolygonCentroid(polygon);
        float influenceRadius = ComputeInfluenceRadius(polygon, centroid);
        Mesh shardMesh = BuildShardMesh($"{shardName}_Mesh", polygon, thickness);
        if (shardMesh == null)
            return;

        GameObject shard = new(shardName);
        shard.transform.SetParent(shardRoot.transform, false);
        shard.transform.localPosition = new Vector3(
            surfaceCenter.x + centroid.x,
            surfaceCenter.y + yOffset,
            surfaceCenter.z + centroid.y);
        shard.transform.localRotation = Quaternion.identity;
        shard.transform.localScale = Vector3.one;

        MeshFilter meshFilter = shard.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = shardMesh;

        MeshRenderer meshRenderer = shard.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = shardMaterial;

        BreakableIceShardPiece piece = shard.AddComponent<BreakableIceShardPiece>();
        piece.Configure(centroid, influenceRadius);
    }

    private static List<Vector2> GenerateShardSites(System.Random random, int targetShardCount, float width, float depth, GenerationSettings settings)
    {
        float halfWidth = width * 0.5f;
        float halfDepth = depth * 0.5f;
        float minDimension = Mathf.Min(width, depth);
        float padding = settings.minimumShardFootprint * 0.2f;
        float preferredSpacing = Mathf.Max(
            settings.minimumShardFootprint * 0.8f,
            minDimension / Mathf.Sqrt(targetShardCount) * 0.72f);

        List<Vector2> sites = new(targetShardCount);
        int attempts = 0;
        int maxAttempts = targetShardCount * 48;

        while (sites.Count < targetShardCount && attempts < maxAttempts)
        {
            attempts++;

            Vector2 candidate = new(
                Mathf.Lerp(-halfWidth, halfWidth, (float)random.NextDouble()),
                Mathf.Lerp(-halfDepth, halfDepth, (float)random.NextDouble()));

            if (!IsFarEnoughFromSites(candidate, sites, preferredSpacing))
                continue;

            sites.Add(candidate);
        }

        while (sites.Count < targetShardCount)
        {
            sites.Add(new(
                Mathf.Lerp(-halfWidth, halfWidth, (float)random.NextDouble()),
                Mathf.Lerp(-halfDepth, halfDepth, (float)random.NextDouble())));
        }

        for (int iteration = 0; iteration < 2; iteration++)
        {
            List<List<Vector2>> relaxedCells = BuildVoronoiCells(sites, width, depth);
            for (int index = 0; index < sites.Count; index++)
            {
                if (relaxedCells[index] == null || relaxedCells[index].Count < 3)
                    continue;

                Vector2 centroid = ComputePolygonCentroid(relaxedCells[index]);
                float jitterRadius = settings.splitJitter * minDimension * 0.08f;
                centroid += RandomPointInCircle(random, jitterRadius);
                sites[index] = ClampPointToRect(centroid, halfWidth, halfDepth, padding);
            }
        }

        return sites;
    }

    private static List<List<Vector2>> BuildVoronoiCells(List<Vector2> sites, float width, float depth)
    {
        float halfWidth = width * 0.5f;
        float halfDepth = depth * 0.5f;
        List<Vector2> bounds = new(4)
        {
            new(-halfWidth, -halfDepth),
            new(halfWidth, -halfDepth),
            new(halfWidth, halfDepth),
            new(-halfWidth, halfDepth),
        };

        List<List<Vector2>> cells = new(sites.Count);
        for (int siteIndex = 0; siteIndex < sites.Count; siteIndex++)
        {
            List<Vector2> polygon = new(bounds);
            Vector2 site = sites[siteIndex];

            for (int compareIndex = 0; compareIndex < sites.Count; compareIndex++)
            {
                if (siteIndex == compareIndex)
                    continue;

                Vector2 otherSite = sites[compareIndex];
                Vector2 normal = otherSite - site;
                if (normal.sqrMagnitude < 0.0001f)
                    continue;

                float maxDistance = (otherSite.sqrMagnitude - site.sqrMagnitude) * 0.5f;
                polygon = ClipPolygonAgainstMaxDistance(polygon, normal, maxDistance);
                if (polygon.Count < 3)
                    break;
            }

            EnsureCounterClockwise(polygon);
            cells.Add(polygon);
        }

        return cells;
    }

    private static List<Vector2> ClipPolygonAgainstMaxDistance(List<Vector2> polygon, Vector2 normal, float maxDistance)
    {
        List<Vector2> clipped = new(polygon.Count + 1);
        if (polygon.Count == 0)
            return clipped;

        Vector2 previousPoint = polygon[polygon.Count - 1];
        float previousDistance = Vector2.Dot(normal, previousPoint) - maxDistance;
        bool previousInside = previousDistance <= 0f;

        foreach (Vector2 currentPoint in polygon)
        {
            float currentDistance = Vector2.Dot(normal, currentPoint) - maxDistance;
            bool currentInside = currentDistance <= 0f;

            if (currentInside != previousInside)
            {
                float denominator = previousDistance - currentDistance;
                float t = Mathf.Abs(denominator) > 0.0001f ? previousDistance / denominator : 0f;
                clipped.Add(Vector2.Lerp(previousPoint, currentPoint, Mathf.Clamp01(t)));
            }

            if (currentInside)
                clipped.Add(currentPoint);

            previousPoint = currentPoint;
            previousDistance = currentDistance;
            previousInside = currentInside;
        }

        return clipped;
    }

    private static List<Vector2> InsetConvexPolygon(List<Vector2> polygon, float inset)
    {
        if (polygon == null || polygon.Count < 3 || inset <= 0f)
            return polygon == null ? new List<Vector2>() : new List<Vector2>(polygon);

        List<Vector2> sourcePolygon = new(polygon);
        EnsureCounterClockwise(sourcePolygon);

        List<Vector2> insetPolygon = new(sourcePolygon);
        for (int index = 0; index < sourcePolygon.Count; index++)
        {
            Vector2 a = sourcePolygon[index];
            Vector2 b = sourcePolygon[(index + 1) % sourcePolygon.Count];
            Vector2 edge = b - a;
            float edgeLength = edge.magnitude;
            if (edgeLength < 0.0001f)
                continue;

            Vector2 inwardNormal = new(-edge.y / edgeLength, edge.x / edgeLength);
            float minDistance = Vector2.Dot(inwardNormal, a) + inset;
            insetPolygon = ClipPolygonAgainstMinDistance(insetPolygon, inwardNormal, minDistance);
            if (insetPolygon.Count < 3)
                break;
        }

        EnsureCounterClockwise(insetPolygon);
        return insetPolygon;
    }

    private static List<Vector2> ClipPolygonAgainstMinDistance(List<Vector2> polygon, Vector2 normal, float minDistance)
    {
        List<Vector2> clipped = new(polygon.Count + 1);
        if (polygon.Count == 0)
            return clipped;

        Vector2 previousPoint = polygon[polygon.Count - 1];
        float previousDistance = Vector2.Dot(normal, previousPoint) - minDistance;
        bool previousInside = previousDistance >= 0f;

        foreach (Vector2 currentPoint in polygon)
        {
            float currentDistance = Vector2.Dot(normal, currentPoint) - minDistance;
            bool currentInside = currentDistance >= 0f;

            if (currentInside != previousInside)
            {
                float denominator = currentDistance - previousDistance;
                float t = Mathf.Abs(denominator) > 0.0001f ? -previousDistance / denominator : 0f;
                clipped.Add(Vector2.Lerp(previousPoint, currentPoint, Mathf.Clamp01(t)));
            }

            if (currentInside)
                clipped.Add(currentPoint);

            previousPoint = currentPoint;
            previousDistance = currentDistance;
            previousInside = currentInside;
        }

        return clipped;
    }

    private static Mesh BuildShardMesh(string meshName, List<Vector2> polygon, float thickness)
    {
        if (polygon == null || polygon.Count < 3)
            return null;

        List<Vector2> localPolygon = new(polygon);
        EnsureCounterClockwise(localPolygon);

        Vector2 centroid = ComputePolygonCentroid(localPolygon);
        for (int index = 0; index < localPolygon.Count; index++)
            localPolygon[index] -= centroid;

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;

        foreach (Vector2 point in localPolygon)
        {
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minY = Mathf.Min(minY, point.y);
            maxY = Mathf.Max(maxY, point.y);
        }

        float width = Mathf.Max(0.0001f, maxX - minX);
        float depth = Mathf.Max(0.0001f, maxY - minY);
        float halfThickness = thickness * 0.5f;
        int count = localPolygon.Count;

        List<Vector3> vertices = new(count * 6);
        List<Vector2> uvs = new(count * 6);
        List<int> triangles = new((count - 2) * 6 + count * 6);

        int topStart = vertices.Count;
        for (int index = 0; index < count; index++)
        {
            Vector2 point = localPolygon[index];
            vertices.Add(new Vector3(point.x, halfThickness, point.y));
            uvs.Add(new Vector2((point.x - minX) / width, (point.y - minY) / depth));
        }

        for (int index = 1; index < count - 1; index++)
        {
            triangles.Add(topStart);
            triangles.Add(topStart + index + 1);
            triangles.Add(topStart + index);
        }

        int bottomStart = vertices.Count;
        for (int index = 0; index < count; index++)
        {
            Vector2 point = localPolygon[index];
            vertices.Add(new Vector3(point.x, -halfThickness, point.y));
            uvs.Add(new Vector2((point.x - minX) / width, (point.y - minY) / depth));
        }

        for (int index = 1; index < count - 1; index++)
        {
            triangles.Add(bottomStart);
            triangles.Add(bottomStart + index);
            triangles.Add(bottomStart + index + 1);
        }

        for (int index = 0; index < count; index++)
        {
            Vector2 a = localPolygon[index];
            Vector2 b = localPolygon[(index + 1) % count];
            float edgeLength = Vector2.Distance(a, b);
            int sideStart = vertices.Count;

            vertices.Add(new Vector3(a.x, halfThickness, a.y));
            vertices.Add(new Vector3(b.x, halfThickness, b.y));
            vertices.Add(new Vector3(b.x, -halfThickness, b.y));
            vertices.Add(new Vector3(a.x, -halfThickness, a.y));

            uvs.Add(new Vector2(0f, 1f));
            uvs.Add(new Vector2(edgeLength, 1f));
            uvs.Add(new Vector2(edgeLength, 0f));
            uvs.Add(new Vector2(0f, 0f));

            triangles.Add(sideStart);
            triangles.Add(sideStart + 1);
            triangles.Add(sideStart + 2);
            triangles.Add(sideStart);
            triangles.Add(sideStart + 2);
            triangles.Add(sideStart + 3);
        }

        Mesh shardMesh = new()
        {
            name = meshName,
        };
        shardMesh.SetVertices(vertices);
        shardMesh.SetTriangles(triangles, 0);
        shardMesh.SetUVs(0, uvs);
        shardMesh.RecalculateNormals();
        shardMesh.RecalculateBounds();
        shardMesh.RecalculateTangents();

        return shardMesh;
    }

    private static float EvaluateBreakRadius(float angle, SelectionSettings settings)
    {
        float phase1 = Hash01(settings.seed, 1) * Mathf.PI * 2f;
        float phase2 = Hash01(settings.seed, 2) * Mathf.PI * 2f;
        float phase3 = Hash01(settings.seed, 3) * Mathf.PI * 2f;
        float phase4 = Hash01(settings.seed, 4) * Mathf.PI * 2f;

        float blendedWave =
            Mathf.Sin(angle * 2.1f + phase1) * 0.52f +
            Mathf.Sin(angle * 4.2f + phase2) * 0.24f +
            Mathf.Sin(angle * 7.4f + phase3) * 0.12f;

        float spikeWaveA = Mathf.Sin(angle * 5.7f + phase4);
        float spikeWaveB = Mathf.Sin(angle * 10.3f + phase2 * 0.75f);
        float spikes =
            Mathf.Sign(spikeWaveA) * Mathf.Pow(Mathf.Abs(spikeWaveA), 7f) * settings.edgeSpikiness +
            Mathf.Sign(spikeWaveB) * Mathf.Pow(Mathf.Abs(spikeWaveB), 9f) * settings.edgeSpikiness * 0.35f;

        float radiusMultiplier = 1f + blendedWave * settings.edgeNoise + spikes;
        float minimumRadius = settings.breakRadius * 0.42f;
        float maximumRadius = settings.breakRadius * 1.18f;
        return Mathf.Clamp(settings.breakRadius * radiusMultiplier, minimumRadius, maximumRadius);
    }

    private static float ComputeInfluenceRadius(List<Vector2> polygon, Vector2 centroid)
    {
        float maxDistance = 0f;
        foreach (Vector2 point in polygon)
            maxDistance = Mathf.Max(maxDistance, Vector2.Distance(point, centroid));

        return maxDistance;
    }

    private static Vector2 RandomPointInCircle(System.Random random, float radius)
    {
        if (radius <= 0f)
            return Vector2.zero;

        float angle = (float)(random.NextDouble() * Mathf.PI * 2f);
        float distance = Mathf.Sqrt((float)random.NextDouble()) * radius;
        return new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);
    }

    private static Vector2 ClampPointToRect(Vector2 point, float halfWidth, float halfDepth, float padding)
    {
        float minX = -halfWidth + padding;
        float maxX = halfWidth - padding;
        float minY = -halfDepth + padding;
        float maxY = halfDepth - padding;

        if (minX > maxX)
        {
            float midpoint = (minX + maxX) * 0.5f;
            minX = midpoint;
            maxX = midpoint;
        }

        if (minY > maxY)
        {
            float midpoint = (minY + maxY) * 0.5f;
            minY = midpoint;
            maxY = midpoint;
        }

        return new Vector2(
            Mathf.Clamp(point.x, minX, maxX),
            Mathf.Clamp(point.y, minY, maxY));
    }

    private static bool IsFarEnoughFromSites(Vector2 candidate, List<Vector2> sites, float minimumDistance)
    {
        float minimumDistanceSquared = minimumDistance * minimumDistance;
        foreach (Vector2 site in sites)
        {
            if ((candidate - site).sqrMagnitude < minimumDistanceSquared)
                return false;
        }

        return true;
    }

    private static void EnsureCounterClockwise(List<Vector2> polygon)
    {
        if (polygon != null && ComputePolygonSignedArea(polygon) < 0f)
            polygon.Reverse();
    }

    private static float ComputePolygonSignedArea(List<Vector2> polygon)
    {
        float area = 0f;
        for (int index = 0; index < polygon.Count; index++)
        {
            Vector2 current = polygon[index];
            Vector2 next = polygon[(index + 1) % polygon.Count];
            area += current.x * next.y - next.x * current.y;
        }

        return area * 0.5f;
    }

    private static Vector2 ComputePolygonCentroid(List<Vector2> polygon)
    {
        float accumulatedArea = 0f;
        Vector2 centroid = Vector2.zero;

        for (int index = 0; index < polygon.Count; index++)
        {
            Vector2 current = polygon[index];
            Vector2 next = polygon[(index + 1) % polygon.Count];
            float cross = current.x * next.y - next.x * current.y;
            accumulatedArea += cross;
            centroid += (current + next) * cross;
        }

        if (Mathf.Abs(accumulatedArea) < 0.0001f)
        {
            Vector2 average = Vector2.zero;
            foreach (Vector2 point in polygon)
                average += point;

            return average / Mathf.Max(1, polygon.Count);
        }

        return centroid / (3f * accumulatedArea);
    }

    private static float Hash01(int seed, int salt)
    {
        float value = Mathf.Sin(seed * 12.9898f + salt * 78.233f) * 43758.5453f;
        return value - Mathf.Floor(value);
    }
}
