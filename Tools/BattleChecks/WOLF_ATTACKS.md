# 血狼人二技能与攻击

二技能（100102）先叠加吸血，然后通过 `BattleBasicAttackComponentSystem.TryAttack` 执行本回合原本的攻击：没有生效的大招时为一次普通攻击；大招已生效时为一轮强化连击，并只消耗一次强化攻击次数。新叠加的吸血对当次攻击立即生效。

技能与攻击属于同一次单位行动，不新增行动机会。技能点只扣二技能预选时的一次费用，技能CD仍在成功释放后开始。我方与敌方共用相同逻辑。

没有范围内目标时，二技能仍可叠加吸血，但不凭空攻击、不消耗强化攻击次数；沉默和已行动限制保持不变。大招自身的释放仍只添加后续三次强化攻击，从下一回合开始生效。一技能仍执行自身攻击及流血逻辑，不额外衔接大招连击。

配置数值、吸血比例、费用和CD以 `Assets/Game/AssetsPacakge/Datas/skill.csv` 当前设置为准。

相关实现：`BattleSkillComponentSystem`、`BattleBasicAttackComponentSystem`、`BattleComboAttackComponentSystem`。验证场景位于 `WolfComboChecks.cs`，覆盖双方普通攻击、强化攻击、新吸血即时生效、单次行动/扣点/强化次数、无目标、沉默和CD。
