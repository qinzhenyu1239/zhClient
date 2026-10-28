// UIMgr attaches this controller when opening GUI/Prefabs/UIBattle.
public class UIBattle_UICtrl : UICtrl
{
    public BattleFormationView Formation { get; private set; }

    private void Awake()
    {
        Formation = GetComponent<BattleFormationView>();
    }
}
