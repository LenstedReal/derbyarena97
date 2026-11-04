using System;
using System.Collections.Generic;
using DerbyArena.Core;
using UnityEngine;

namespace DerbyArena.World
{
    /// <summary>
    /// Desert Ruins arena (4.0): a destroyed desert town built in code. Filled in by the arena stage; this
    /// skeleton provides the layout contract (spawns, cells, gates, pickups) and a safe floor.
    /// </summary>
    public static class RuinsArena
    {
        public static ArenaInfo Info()
        {
            var info = new ArenaInfo();
            info.Kind = ArenaKind.Ruins;
            info.Center = Vector3.zero;
            info.Radius = 34f;
            info.Bounds = new Rect(-38f, -38f, 76f, 76f);
            info.PlayerSpawn = new Vector3(0f, 0.6f, -30f);
            info.PlayerYaw = 0f;
            info.EnemySpawns = new[]
            {
                new Vector3(-26f, 0.6f, 12f), new Vector3(26f, 0.6f, 12f), new Vector3(-16f, 0.6f, 30f), new Vector3(16f, 0.6f, 30f),
                new Vector3(0f, 0.6f, 24f), new Vector3(-30f, 0.6f, -10f), new Vector3(30f, 0.6f, -10f), new Vector3(-6f, 0.6f, 32f)
            };
            info.CellPoints = new[] { new Vector3(-24f, 0.9f, -20f), new Vector3(24f, 0.9f, -20f), new Vector3(-31f, 0.9f, 16f), new Vector3(31f, 0.9f, 16f), new Vector3(-8f, 0.9f, 33f) };
            info.Extraction = new Vector3(0f, 0.05f, 33f);
            info.PickupPoints = new[]
            {
                new Vector3(-24f, 0.6f, -28f), new Vector3(24f, 0.6f, -28f), new Vector3(-28f, 0.6f, 26f), new Vector3(28f, 0.6f, 26f),
                new Vector3(0f, 0.6f, 0f), new Vector3(-32f, 0.6f, -2f), new Vector3(32f, 0.6f, -2f)
            };
            info.BossSpawn = new Vector3(6f, 0.8f, 26f);
            var gates = new List<Vector3>();
            for (int i = 0; i < 8; i++)
            {
                float a = (180f + i * 45f) * Mathf.Deg2Rad;
                gates.Add(new Vector3(Mathf.Sin(a) * 26f, 0f, Mathf.Cos(a) * 26f));
            }
            info.GatePoints = gates.ToArray();
            info.AcidPoints = new[] { new Vector3(-22f, 0f, 4f), new Vector3(22f, 0f, -4f), new Vector3(-4f, 0f, -23f) };
            info.ZoneStart = 32f;
            info.ZoneEnd = 9f;
            return info;
        }

        public static string Build(Transform root, Atmosphere.Theme theme, int quality)
        {
            MeshFactory.SolidBox("Floor", root, new Vector3(0f, -0.5f, 0f), new Vector3(96f, 1f, 96f), Quaternion.identity, Mats.Lit(new Color(0.62f, 0.52f, 0.36f), 0.95f, 0f, Color.black, 0f));
            MeshFactory.ColliderBox("BoundN", root, new Vector3(0f, 6f, 39f), new Vector3(96f, 12f, 1f), Quaternion.identity);
            MeshFactory.ColliderBox("BoundS", root, new Vector3(0f, 6f, -39f), new Vector3(96f, 12f, 1f), Quaternion.identity);
            MeshFactory.ColliderBox("BoundE", root, new Vector3(39f, 6f, 0f), new Vector3(1f, 12f, 96f), Quaternion.identity);
            MeshFactory.ColliderBox("BoundW", root, new Vector3(-39f, 6f, 0f), new Vector3(1f, 12f, 96f), Quaternion.identity);
            return Loc.T("Çöl Harabeleri", "Desert Ruins");
        }
    }
}
