using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Owns the dynamic instances of the prefab-authored formation slot.
public sealed class BattleFormationView : MonoBehaviour
{
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform allyGrid;
    [SerializeField] private Transform enemyGrid;

    private readonly List<GameObject> allySlots = new List<GameObject>(9);
    private readonly List<GameObject> enemySlots = new List<GameObject>(9);
    private bool initialized;

    public IReadOnlyList<GameObject> AllySlots => allySlots;
    public IReadOnlyList<GameObject> EnemySlots => enemySlots;

    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (initialized) return;
        if (slotPrefab == null || allyGrid == null || enemyGrid == null)
        {
            Debug.LogError("[BattleFormation] Assign the slot prefab and both grids on UIBattle.", this);
            return;
        }
        if (allyGrid.childCount != 0 || enemyGrid.childCount != 0)
        {
            Debug.LogError("[BattleFormation] Both grids must be empty; slots are generated at runtime.", this);
            return;
        }

        CreateSlots(allyGrid, "Ally", new Color32(83, 200, 222, 255), allySlots);
        CreateSlots(enemyGrid, "Enemy", new Color32(240, 120, 120, 255), enemySlots);
        initialized = true;
    }

    private void CreateSlots(Transform parent, string side, Color accent, List<GameObject> slots)
    {
        for (int i = 1; i <= 9; i++)
        {
            GameObject slot = Instantiate(slotPrefab, parent, false);
            slot.name = side + "Slot" + i.ToString("00");
            slot.SetActive(true);
            slots.Add(slot);

            // These are newly instantiated nodes, following the existing UGUI lookup convention.
            Transform indexNode = slot.transform.Find("IndexText");
            if (indexNode != null && indexNode.TryGetComponent(out Text index))
            {
                index.text = i.ToString("00");
                index.color = accent;
            }
            SetAccent(slot.transform, "Selected", accent);
            SetAccent(slot.transform, "HealthBar/Fill", accent);
            Transform portraitNode = slot.transform.Find("Portrait");
            Transform emptyLabel = slot.transform.Find("EmptyLabel");
            if (portraitNode != null && emptyLabel != null &&
                portraitNode.TryGetComponent(out Image portrait) && portrait.sprite != null && portrait.color.a > 0)
                emptyLabel.gameObject.SetActive(false);
        }
    }

    private static void SetAccent(Transform slot, string path, Color color)
    {
        Transform node = slot.Find(path);
        if (node != null && node.TryGetComponent(out Image image)) image.color = color;
    }
}
