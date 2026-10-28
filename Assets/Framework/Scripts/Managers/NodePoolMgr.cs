using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodePoolMgr : UnitySingleton<NodePoolMgr>
{
    private Transform nodePoolRoot = null;
    private Dictionary<string, Transform> nodePoolMaps = null;
    private Transform tempItemRoot = null;

    public void Init() {
        this.nodePoolMaps = new Dictionary<string, Transform>();
        this.nodePoolRoot = this.transform.Find("NodePoolRoot");
        if (this.nodePoolRoot == null) {
            this.nodePoolRoot = new GameObject().transform;
            this.nodePoolRoot.SetParent(this.transform, false);
            this.nodePoolRoot.localPosition = Vector3.zero;
            this.nodePoolRoot.gameObject.name = "NodePoolRoot";
        }

        this.nodePoolRoot.gameObject.SetActive(false);

        this.tempItemRoot = this.transform.Find("TempItemRoot");
        if (this.tempItemRoot == null) {
            this.tempItemRoot = new GameObject().transform;
            this.tempItemRoot.SetParent(this.nodePoolRoot, false);
            this.tempItemRoot.localPosition = Vector3.zero;
            this.tempItemRoot.gameObject.name = "TempItemRoot";
        }
        this.tempItemRoot.gameObject.SetActive(false);

    }

    public void AddNodePool(string assetPrefabPath, int count = 0) {
        if (this.nodePoolMaps.ContainsKey(assetPrefabPath)) {
            return;
        }

        Transform typeNodeRoot = new GameObject().transform;
        typeNodeRoot.SetParent(this.nodePoolRoot, false);
        typeNodeRoot.gameObject.SetActive(false);
        typeNodeRoot.name = assetPrefabPath;
        typeNodeRoot.gameObject.SetActive(false);

        this.nodePoolMaps.Add(assetPrefabPath, typeNodeRoot);

        if (count <= 0) {
            return;
        }

        GameObject nodePrefab = ResMgr.Instance.LoadAssetSync<GameObject>(assetPrefabPath);
        for (int i = 0; i < count; i++) {
            GameObject item = GameObject.Instantiate(nodePrefab);
            item.transform.SetParent(typeNodeRoot, false);
            item.transform.localPosition = Vector3.zero;
        }

        return;
    }

    public void ClearNodePool(string assetPrefabPath, int residueCout = 0) {
        if (!this.nodePoolMaps.ContainsKey(assetPrefabPath)) {
            return;
        }

        Transform typeNodeRoot = this.nodePoolMaps[assetPrefabPath];
        if (typeNodeRoot == null) {
            return;
        }

        residueCout = (residueCout < 0) ? 0 : residueCout;
        if (typeNodeRoot.childCount <= residueCout) {
            return;
        }

        int count = typeNodeRoot.childCount - residueCout;
        for (int i = 0; i < count; i++) {
            GameObject.DestroyImmediate(typeNodeRoot.GetChild(0));
        }
    }

    public GameObject Get(string assetPrefabPath) {
        if (!this.nodePoolMaps.ContainsKey(assetPrefabPath)) {
            return null;
        }
        Transform typeNodeRoot = this.nodePoolMaps[assetPrefabPath];
        if (typeNodeRoot == null) {
            return null;
        }

        GameObject item = null;

        if (typeNodeRoot.childCount <= 0) { // 分配一个，返回
            GameObject nodePrefab = ResMgr.Instance.LoadAssetSync<GameObject>(assetPrefabPath);
            item = GameObject.Instantiate(nodePrefab);

            item.transform.SetParent(this.tempItemRoot, false);
            item.transform.localPosition = Vector3.zero;
            return item;
        }

        item = typeNodeRoot.GetChild(0).gameObject;
        item.transform.SetParent(this.tempItemRoot, false);
        item.transform.localPosition = Vector3.zero;

        return item;
    }

    public void Recycle(string assetPrefabPath, GameObject obj) {
        if (!this.nodePoolMaps.ContainsKey(assetPrefabPath)) {
            Debug.LogWarning($"obj not in {assetPrefabPath} Pool");
            return;
        }

        Transform typeNodeRoot = this.nodePoolMaps[assetPrefabPath];
        obj.transform.SetParent(typeNodeRoot, false);
        obj.transform.localPosition = Vector3.zero;

    }
}
