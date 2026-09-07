using System.IO;
using System.Text;
using RedlineLegends.Vehicles;
using UnityEditor;
using UnityEngine;

namespace RedlineLegends.Editor
{
    /// <summary>
    /// Headless audit of every vehicle definition's visual prefab against the
    /// <see cref="VehicleVisualUtility"/> contract: wheel transforms, anchors, paint renderers,
    /// LOD levels with triangle counts, bounds, materials and shaders. Writes a CSV plus a text
    /// report into REDLINE_CAPTURE_DIR (or Builds/Captures).
    ///   Unity.exe -batchmode -nographics -quit -executeMethod RedlineLegends.Editor.VehiclePrefabAudit.RunBatch
    /// </summary>
    public static class VehiclePrefabAudit
    {
        public static void RunBatch()
        {
            try
            {
                Run();
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        [MenuItem("Redline Legends/Capture/Audit vehicle prefabs", priority = 61)]
        public static void Run()
        {
            string dir = System.Environment.GetEnvironmentVariable("REDLINE_CAPTURE_DIR");
            if (string.IsNullOrEmpty(dir)) dir = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Captures");
            Directory.CreateDirectory(dir);
            var csv = new StringBuilder("id,class,prefab,lods,tris_lod0,tris_lod1,tris_lod2,tris_total,renderers,materials,wheels_found,anchors_found,paint_renderers,size_x,size_y,size_z,bottom_y,warnings\n");
            var report = new StringBuilder();
            foreach (var guid in AssetDatabase.FindAssets("t:VehicleDefinition", new[] { EditorPaths.Content + "/Vehicles" }))
            {
                var def = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (def == null) continue;
                var prefab = def.VisualPrefab;
                if (prefab == null)
                {
                    csv.Append(def.Id).Append(',').Append(def.VehicleClass).Append(",MISSING\n");
                    report.Append(def.Id).Append(": no visual prefab\n");
                    continue;
                }
                var warnings = new StringBuilder();
                string path = AssetDatabase.GetAssetPath(prefab);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                try
                {
                    // Wheels and anchors
                    int wheels = 0;
                    foreach (var name in new[] { VehicleVisualUtility.WheelFL, VehicleVisualUtility.WheelFR, VehicleVisualUtility.WheelRL, VehicleVisualUtility.WheelRR })
                    {
                        var t = VehicleVisualUtility.FindDeep(instance.transform, name);
                        if (t != null) wheels++; else warnings.Append("missing ").Append(name).Append("; ");
                    }
                    int anchors = 0;
                    foreach (var name in new[] { VehicleVisualUtility.CockpitCameraAnchor, VehicleVisualUtility.ExhaustAnchor })
                    {
                        if (VehicleVisualUtility.FindDeep(instance.transform, name) != null) anchors++; else warnings.Append("missing ").Append(name).Append("; ");
                    }
                    // Wheel radius vs stats
                    var fl = VehicleVisualUtility.FindDeep(instance.transform, VehicleVisualUtility.WheelFL);
                    if (fl != null)
                    {
                        float statR = def.BaseStats.Tires.WheelRadiusM;
                        if (Mathf.Abs(fl.position.y - statR) > statR * 0.08f)
                            warnings.Append("hub height ").Append(fl.position.y.ToString("0.000")).Append(" vs stat radius ").Append(statR.ToString("0.000")).Append("; ");
                        var wr = fl.GetComponentInChildren<Renderer>();
                        if (wr != null && Mathf.Abs(wr.bounds.extents.y - statR) > statR * 0.08f)
                            warnings.Append("wheel visual radius ").Append(wr.bounds.extents.y.ToString("0.000")).Append("; ");
                    }
                    // LODs and triangles
                    var lodGroup = instance.GetComponentInChildren<LODGroup>();
                    int lods = lodGroup != null ? lodGroup.lodCount : 0;
                    var lodTris = new int[3];
                    int total = 0;
                    var renderers = instance.GetComponentsInChildren<Renderer>(true);
                    var mats = new System.Collections.Generic.HashSet<string>();
                    var shaders = new System.Collections.Generic.HashSet<string>();
                    int paintRenderers = 0;
                    var bounds = new Bounds(instance.transform.position, Vector3.zero);
                    bool any = false;
                    foreach (var r in renderers)
                    {
                        int tris = 0;
                        var mf = r.GetComponent<MeshFilter>();
                        if (mf != null && mf.sharedMesh != null)
                        {
                            for (int s = 0; s < mf.sharedMesh.subMeshCount; s++) tris += (int)mf.sharedMesh.GetIndexCount(s) / 3;
                        }
                        total += tris;
                        int lodIndex = LodIndexOf(lodGroup, r);
                        if (lodIndex >= 0 && lodIndex < 3) lodTris[lodIndex] += tris;
                        else if (lodIndex < 0) lodTris[0] += tris; // always-visible parts count against LOD0
                        bool paint = false;
                        foreach (var m in r.sharedMaterials)
                        {
                            if (m == null) { warnings.Append("null material on ").Append(r.name).Append("; "); continue; }
                            mats.Add(m.name);
                            shaders.Add(m.shader.name);
                            if (m.name.Contains(VehicleVisualUtility.PaintMaterialKeyword)) paint = true;
                            if (!m.shader.name.Contains("Universal Render Pipeline")) warnings.Append("non-URP shader ").Append(m.shader.name).Append(" on ").Append(m.name).Append("; ");
                        }
                        if (paint) paintRenderers++;
                        if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds);
                    }
                    if (paintRenderers == 0) warnings.Append("no Paint renderer; ");
                    if (lods == 0) warnings.Append("no LODGroup; ");
                    if (lodTris[0] > 30000) warnings.Append("LOD0 over 30k tris; ");
                    if (bounds.min.y < -0.02f) warnings.Append("geometry below ground ").Append(bounds.min.y.ToString("0.00")).Append("; ");
                    if (bounds.size.z < 3.6f || bounds.size.z > 5.2f) warnings.Append("length ").Append(bounds.size.z.ToString("0.00")).Append(" out of range; ");
                    if (instance.GetComponentInChildren<Collider>() != null) warnings.Append("has collider; ");
                    if (instance.GetComponentInChildren<MonoBehaviour>() != null) warnings.Append("has MonoBehaviour; ");

                    csv.Append(def.Id).Append(',').Append(def.VehicleClass).Append(',').Append(path).Append(',')
                        .Append(lods).Append(',').Append(lodTris[0]).Append(',').Append(lodTris[1]).Append(',').Append(lodTris[2]).Append(',').Append(total).Append(',')
                        .Append(renderers.Length).Append(',').Append(mats.Count).Append(',').Append(wheels).Append(',').Append(anchors).Append(',').Append(paintRenderers).Append(',')
                        .Append(bounds.size.x.ToString("0.00")).Append(',').Append(bounds.size.y.ToString("0.00")).Append(',').Append(bounds.size.z.ToString("0.00")).Append(',')
                        .Append(bounds.min.y.ToString("0.00")).Append(",\"").Append(warnings.ToString().Trim()).Append("\"\n");
                    report.Append(def.Id).Append(" (").Append(def.VehicleClass).Append(")\n  prefab: ").Append(path)
                        .Append("\n  LODs: ").Append(lods).Append("  tris LOD0/1/2: ").Append(lodTris[0]).Append('/').Append(lodTris[1]).Append('/').Append(lodTris[2]).Append("  total: ").Append(total)
                        .Append("\n  renderers: ").Append(renderers.Length).Append("  materials: ").Append(string.Join(", ", mats))
                        .Append("\n  shaders: ").Append(string.Join(", ", shaders))
                        .Append("\n  size (w,h,l): ").Append(bounds.size.ToString("0.00")).Append("  bottom y: ").Append(bounds.min.y.ToString("0.00"))
                        .Append("\n  wheels: ").Append(wheels).Append("/4  anchors: ").Append(anchors).Append("/2  paint renderers: ").Append(paintRenderers)
                        .Append("\n  warnings: ").Append(warnings.Length == 0 ? "none" : warnings.ToString()).Append("\n\n");
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }
            File.WriteAllText(Path.Combine(dir, "vehicle_audit.csv"), csv.ToString());
            File.WriteAllText(Path.Combine(dir, "vehicle_audit.txt"), report.ToString());
            Debug.Log("[Audit] wrote vehicle_audit.csv/.txt to " + dir + "\n" + report);
        }

        private static int LodIndexOf(LODGroup group, Renderer r)
        {
            if (group == null) return -1;
            var lods = group.GetLODs();
            for (int i = 0; i < lods.Length; i++)
                foreach (var lr in lods[i].renderers)
                    if (lr == r) return i;
            return -1;
        }
    }
}
