using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Genera los prefabs de las torres (y sus proyectiles/efectos) a partir de los sprites de
/// Assets/Art/Sprites/Towers. Se ejecuta solo la primera vez, si faltan los prefabs.
/// Menu: Tamagochi > Torres > Reconstruir prefabs  (OJO: sobrescribe los prefabs de Assets/Art/Prefabs/Towers).
/// </summary>
[InitializeOnLoad]
public static class TowerPrefabBuilder {
    private const string SpritesDir = "Assets/Art/Sprites/Towers";
    private const string MaterialsDir = "Assets/Art/Materials";
    private const string PrefabsDir = "Assets/Art/Prefabs/Towers";

    static TowerPrefabBuilder() {
        EditorApplication.delayCall += AutoBuild;
    }

    private static void AutoBuild() {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (File.Exists($"{PrefabsDir}/MachineGunTower.prefab")) return;
        if (!File.Exists($"{SpritesDir}/mg_turret.png")) return;
        Build();
    }

    [MenuItem("Tamagochi/Torres/Reconstruir prefabs")]
    public static void Build() {
        Directory.CreateDirectory(MaterialsDir);
        Directory.CreateDirectory(PrefabsDir);
        AssetDatabase.Refresh();

        // 1) Sprites: importar como Sprite, 128 px = 1 unidad (una casilla)
        foreach (string file in Directory.GetFiles(SpritesDir, "*.png")) {
            SetupSprite(file.Replace('\\', '/'));
        }

        // 2) Materiales de efectos
        Material fxSprite = EnsureMaterial("FX_Sprite", null);
        Material fxSoft = EnsureMaterial("FX_Soft", AssetDatabase.LoadAssetAtPath<Texture2D>($"{SpritesDir}/soft.png"));

        // 3) Efectos y proyectiles
        Explosion explosion = BuildExplosion();
        CannonShell shell = BuildShell(explosion);
        Projectile bullet = BuildBullet();

        // 4) Torres
        var mg = BuildTower<MachineGunTower>("MachineGunTower", "mg", 0.42f, "Metralleta", 80, out GameObject mgRoot);
        SetRef(mg, "bulletPrefab", bullet);
        Save(mgRoot, "MachineGunTower");

        var laser = BuildTower<LaserTower>("LaserTower", "laser", 0.42f, "Laser", 250, out GameObject laserRoot);
        SetRef(laser, "beamMaterial", fxSprite);
        Save(laserRoot, "LaserTower");

        var flame = BuildTower<FlamethrowerTower>("FlamethrowerTower", "flame", 0.4f, "Lanzallamas", 150, out GameObject flameRoot);
        SetRef(flame, "flameMaterial", fxSoft);
        Save(flameRoot, "FlamethrowerTower");

        var cannon = BuildTower<CannonTower>("CannonTower", "cannon", 0.44f, "Cañón explosivo", 200, out GameObject cannonRoot);
        SetRef(cannon, "shellPrefab", shell);
        Save(cannonRoot, "CannonTower");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Torres] Prefabs generados en " + PrefabsDir);
    }

    // ---------------------------------------------------------------- torres

    private static T BuildTower<T>(string objName, string prefix, float muzzleX, string displayName, int cost, out GameObject root)
        where T : Tower {
        root = new GameObject(objName);
        AddSprite(root, LoadSprite($"{prefix}_base"), 5);

        var turret = new GameObject("Turret");
        turret.transform.SetParent(root.transform, false);
        AddSprite(turret, LoadSprite($"{prefix}_turret"), 6);

        var fire = new GameObject("FirePoint");
        fire.transform.SetParent(turret.transform, false);
        fire.transform.localPosition = new Vector3(muzzleX, 0f, 0f);

        var flash = new GameObject("MuzzleFlash");
        flash.transform.SetParent(turret.transform, false);
        flash.transform.localPosition = new Vector3(muzzleX + 0.05f, 0f, 0f);
        AddSprite(flash, LoadSprite("flash"), 7);
        flash.SetActive(false);

        T tower = root.AddComponent<T>();
        tower.displayName = displayName;
        tower.cost = cost;
        SetRef(tower, "turret", turret.transform);
        SetRef(tower, "firePoint", fire.transform);
        SetRef(tower, "muzzleFlash", flash);
        return tower;
    }

    // ---------------------------------------------------- proyectiles / efectos

    private static Projectile BuildBullet() {
        var go = new GameObject("Bullet");
        AddSprite(go, LoadSprite("bullet"), 8);
        go.AddComponent<Projectile>();
        return SavePrefab<Projectile>(go, "Bullet");
    }

    private static CannonShell BuildShell(Explosion explosion) {
        var go = new GameObject("CannonShell");
        AddSprite(go, LoadSprite("shell"), 8);
        var shell = go.AddComponent<CannonShell>();
        SetRef(shell, "explosionPrefab", explosion);
        return SavePrefab<CannonShell>(go, "CannonShell");
    }

    private static Explosion BuildExplosion() {
        var go = new GameObject("Explosion");
        AddSprite(go, LoadSprite("explosion"), 12);
        go.AddComponent<Explosion>();
        return SavePrefab<Explosion>(go, "Explosion");
    }

    // ---------------------------------------------------------------- helpers

    private static void SetupSprite(string path) {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 128f;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.filterMode = FilterMode.Bilinear;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();
    }

    private static Sprite LoadSprite(string fileName) {
        string path = $"{SpritesDir}/{fileName}.png";
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogError("[Torres] No se encuentra el sprite " + path);
        return s;
    }

    private static Material EnsureMaterial(string matName, Texture2D tex) {
        string path = $"{MaterialsDir}/{matName}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) {
            m = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(m, path);
        }
        if (tex != null) m.mainTexture = tex;
        EditorUtility.SetDirty(m);
        return m;
    }

    private static void AddSprite(GameObject go, Sprite sprite, int order) {
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        // Sprites-Default no depende de las luces 2D, asi que se ve igual en cualquier escena.
        sr.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
    }

    private static void SetRef(Object component, string property, Object value) {
        var so = new SerializedObject(component);
        SerializedProperty p = so.FindProperty(property);
        if (p == null) {
            Debug.LogError($"[Torres] {component.GetType().Name} no tiene el campo '{property}'");
            return;
        }
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Save(GameObject root, string prefabName) {
        PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabsDir}/{prefabName}.prefab");
        Object.DestroyImmediate(root);
    }

    private static T SavePrefab<T>(GameObject root, string prefabName) where T : Component {
        GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabsDir}/{prefabName}.prefab");
        Object.DestroyImmediate(root);
        return asset.GetComponent<T>();
    }
}
