using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Owns the dynamic instances of the prefab-authored formation slot.
public sealed class BattleFormationView : MonoBehaviour
{
    public const int ColumnCount = BattleEntity.ColumnCount;
    public const int RowCount = BattleEntity.RowCount;
    public const int SlotCount = BattleEntity.SlotCount;

    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform battleGrid;

    private readonly List<GameObject> slots = new List<GameObject>(SlotCount);
    private bool initialized;
    private BattleCombatEffects combatEffects;

    public IReadOnlyList<GameObject> Slots => slots;

    public void ResetCombatEffects()
    {
        if (combatEffects != null) combatEffects.ResetEffects();
    }

    public void PlayActionEffect(BattleActionResult action)
    {
        if (action.ToSlot < 0 || action.ToSlot >= slots.Count) return;
        if (combatEffects == null) combatEffects = gameObject.AddComponent<BattleCombatEffects>();
        combatEffects.Play(action, slots[action.ToSlot].transform, slots);
    }

    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (initialized) return;
        if (slotPrefab == null || battleGrid == null)
        {
            Debug.LogError("[BattleFormation] Assign the slot prefab and battle grid on UIBattle.", this);
            return;
        }
        if (battleGrid.childCount != 0)
        {
            Debug.LogError("[BattleFormation] The battle grid must be empty; slots are generated at runtime.", this);
            return;
        }

        CreateSlots();
        initialized = true;
    }

    private void CreateSlots()
    {
        Color accent = new Color32(83, 200, 222, 255);
        for (int i = 1; i <= SlotCount; i++)
        {
            GameObject slot = Instantiate(slotPrefab, battleGrid, false);
            slot.name = "BattleSlot" + i.ToString("00");
            slot.SetActive(true);
            slots.Add(slot);

            // These are newly instantiated nodes, following the existing UGUI lookup convention.
            Transform indexNode = slot.transform.Find("IndexText");
            if (indexNode != null && indexNode.TryGetComponent(out Text index))
            {
                index.text = i.ToString("00");
                index.color = accent;
            }
            BattleUnitView.ShowEmptySlot(slot);
        }
    }

    public void ShowBattle(BattleEntity state)
    {
        if (state == null) throw new System.ArgumentNullException(nameof(state));
        Initialize();
        if (!initialized) throw new System.InvalidOperationException("Battle grid is not initialized.");
        for (int i = 0; i < slots.Count; i++)
        {
            BattleUnit unit = state.GetUnitAt(i);
            GameObject slot = slots[i];
            BattleUnitView view = slot.GetComponent<BattleUnitView>();
            if (unit == null)
            {
                if (view != null) view.Clear();
                else BattleUnitView.ShowEmptySlot(slot);
                continue;
            }
            if (view == null) view = slot.AddComponent<BattleUnitView>();
            view.Bind(unit);
        }
    }
}
