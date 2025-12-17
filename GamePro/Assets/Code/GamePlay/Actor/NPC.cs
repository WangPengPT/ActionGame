using System;
using System.Collections.Generic;
using UnityEngine;
using GamePlay.Data;
using GamePlay.AI;

namespace GamePlay.Actor
{
    /// <summary>
    /// NPC类型
    /// </summary>
    public enum NPCType
    {
        /// <summary>
        /// 普通NPC
        /// </summary>
        Normal = 0,
        
        /// <summary>
        /// 商人
        /// </summary>
        Merchant = 1,
        
        /// <summary>
        /// 任务NPC
        /// </summary>
        QuestGiver = 2,
        
        /// <summary>
        /// 技能训练师
        /// </summary>
        Trainer = 3,
        
        /// <summary>
        /// 铁匠（装备强化）
        /// </summary>
        Blacksmith = 4,
        
        /// <summary>
        /// 传送NPC
        /// </summary>
        Teleporter = 5
    }
    
    /// <summary>
    /// NPC类 - AI控制的友好单位
    /// </summary>
    public class NPC : ActorBase
    {
        #region 属性
        
        /// <summary>
        /// NPC类型
        /// </summary>
        [SerializeField] protected NPCType _npcType = NPCType.Normal;
        public NPCType NPCType => _npcType;
        
        /// <summary>
        /// AI控制器
        /// </summary>
        protected AIController _aiController;
        public AIController AIController => _aiController;
        
        /// <summary>
        /// 对话内容列表
        /// </summary>
        [SerializeField] protected List<string> _dialogues = new List<string>();
        public IReadOnlyList<string> Dialogues => _dialogues;
        
        /// <summary>
        /// 当前对话索引
        /// </summary>
        protected int _currentDialogueIndex;
        
        /// <summary>
        /// 商店物品ID列表（商人专用）
        /// </summary>
        [SerializeField] protected List<int> _shopItemIds = new List<int>();
        public IReadOnlyList<int> ShopItemIds => _shopItemIds;
        
        /// <summary>
        /// 任务ID列表（任务NPC专用）
        /// </summary>
        [SerializeField] protected List<int> _questIds = new List<int>();
        public IReadOnlyList<int> QuestIds => _questIds;
        
        /// <summary>
        /// 交互范围
        /// </summary>
        [SerializeField] protected float _interactionRange = 3f;
        public float InteractionRange => _interactionRange;
        
        /// <summary>
        /// 是否正在与玩家交互
        /// </summary>
        protected bool _isInteracting;
        public bool IsInteracting => _isInteracting;
        
        /// <summary>
        /// 当前交互的玩家
        /// </summary>
        protected Character _interactingPlayer;
        public Character InteractingPlayer => _interactingPlayer;
        
        /// <summary>
        /// 巡逻点
        /// </summary>
        [SerializeField] protected List<Vector3> _patrolPoints = new List<Vector3>();
        
        /// <summary>
        /// 是否固定位置（不巡逻）
        /// </summary>
        [SerializeField] protected bool _isStationary = true;
        public bool IsStationary => _isStationary;
        
        #endregion
        
        #region 事件
        
        /// <summary>
        /// 交互开始事件
        /// </summary>
        public event Action<Character> OnInteractionStart;
        
        /// <summary>
        /// 交互结束事件
        /// </summary>
        public event Action<Character> OnInteractionEnd;
        
        /// <summary>
        /// 对话事件
        /// </summary>
        public event Action<string> OnDialogue;
        
        /// <summary>
        /// 打开商店事件
        /// </summary>
        public event Action<NPC> OnShopOpened;
        
        /// <summary>
        /// 任务接取事件
        /// </summary>
        public event Action<int> OnQuestAccepted;
        
        /// <summary>
        /// 任务提交事件
        /// </summary>
        public event Action<int> OnQuestSubmitted;
        
        #endregion
        
        #region Unity生命周期
        
        protected override void Awake()
        {
            base.Awake();
            _faction = ActorFaction.Friendly;
        }
        
        protected override void Start()
        {
            base.Start();
            
            // 初始化AI（如果不是固定位置的NPC）
            if (!_isStationary)
            {
                _aiController = GetComponent<AIController>();
                if (_aiController == null)
                {
                    _aiController = gameObject.AddComponent<NPCAIController>();
                }
                _aiController.Initialize(this);
            }
        }
        
        protected override void Update()
        {
            base.Update();
            
            if (!_attribute.IsAlive) return;
            
            // 非固定NPC的AI更新
            if (!_isStationary && _aiController != null && !_isInteracting)
            {
                _aiController.UpdateAI();
            }
            
            // 面向交互玩家
            if (_isInteracting && _interactingPlayer != null)
            {
                Vector3 lookDir = _interactingPlayer.transform.position - transform.position;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation, 
                        Quaternion.LookRotation(lookDir), 
                        Time.deltaTime * 5f);
                }
            }
        }
        
        #endregion
        
        #region 初始化
        
        /// <summary>
        /// 初始化NPC
        /// </summary>
        public void InitializeNPC(int actorId, int configId, string name, NPCType type)
        {
            Initialize(actorId, configId, name, ActorFaction.Friendly);
            _npcType = type;
            _selectable = true;
        }
        
        /// <summary>
        /// 设置对话内容
        /// </summary>
        public void SetDialogues(List<string> dialogues)
        {
            _dialogues = dialogues ?? new List<string>();
            _currentDialogueIndex = 0;
        }
        
        /// <summary>
        /// 设置商店物品
        /// </summary>
        public void SetShopItems(List<int> itemIds)
        {
            _shopItemIds = itemIds ?? new List<int>();
        }
        
        /// <summary>
        /// 设置任务列表
        /// </summary>
        public void SetQuests(List<int> questIds)
        {
            _questIds = questIds ?? new List<int>();
        }
        
        #endregion
        
        #region 交互
        
        /// <summary>
        /// 与玩家交互
        /// </summary>
        public override void Interact(ActorBase interactor)
        {
            if (!(interactor is Character character)) return;
            
            float distance = Vector3.Distance(transform.position, character.transform.position);
            if (distance > _interactionRange)
            {
                Debug.LogWarning("距离太远，无法交互");
                return;
            }
            
            StartInteraction(character);
        }
        
        /// <summary>
        /// 开始交互
        /// </summary>
        protected virtual void StartInteraction(Character player)
        {
            _isInteracting = true;
            _interactingPlayer = player;
            CurrentState = ActorState.Interacting;
            
            OnInteractionStart?.Invoke(player);
            
            // 显示对话或打开功能界面
            switch (_npcType)
            {
                case NPCType.Normal:
                    ShowDialogue();
                    break;
                    
                case NPCType.Merchant:
                    OpenShop();
                    break;
                    
                case NPCType.QuestGiver:
                    ShowQuestMenu();
                    break;
                    
                case NPCType.Trainer:
                    ShowTrainingMenu();
                    break;
                    
                case NPCType.Blacksmith:
                    ShowUpgradeMenu();
                    break;
                    
                case NPCType.Teleporter:
                    ShowTeleportMenu();
                    break;
            }
        }
        
        /// <summary>
        /// 结束交互
        /// </summary>
        public virtual void EndInteraction()
        {
            if (!_isInteracting) return;
            
            var player = _interactingPlayer;
            _isInteracting = false;
            _interactingPlayer = null;
            CurrentState = ActorState.Idle;
            _currentDialogueIndex = 0;
            
            OnInteractionEnd?.Invoke(player);
        }
        
        #endregion
        
        #region 对话
        
        /// <summary>
        /// 显示对话
        /// </summary>
        public virtual void ShowDialogue()
        {
            if (_dialogues.Count == 0)
            {
                Debug.Log($"{_actorName}: ...");
                return;
            }
            
            string dialogue = _dialogues[_currentDialogueIndex];
            Debug.Log($"{_actorName}: {dialogue}");
            OnDialogue?.Invoke(dialogue);
        }
        
        /// <summary>
        /// 下一条对话
        /// </summary>
        public virtual void NextDialogue()
        {
            _currentDialogueIndex++;
            if (_currentDialogueIndex >= _dialogues.Count)
            {
                EndInteraction();
                return;
            }
            
            ShowDialogue();
        }
        
        #endregion
        
        #region 商店功能
        
        /// <summary>
        /// 打开商店
        /// </summary>
        public virtual void OpenShop()
        {
            if (_npcType != NPCType.Merchant)
            {
                Debug.LogWarning("该NPC不是商人");
                return;
            }
            
            Debug.Log($"{_actorName} 的商店已打开");
            OnShopOpened?.Invoke(this);
        }
        
        /// <summary>
        /// 购买物品
        /// </summary>
        public virtual bool BuyItem(Character buyer, int itemConfigId, int count = 1)
        {
            if (!_shopItemIds.Contains(itemConfigId))
            {
                Debug.LogWarning("商店没有该物品");
                return false;
            }
            
            // TODO: 通过配置获取价格并扣除金币
            // int price = GetItemPrice(itemConfigId) * count;
            // if (!buyer.SpendGold(price)) return false;
            
            Debug.Log($"{buyer.ActorName} 购买了物品 {itemConfigId} x{count}");
            return true;
        }
        
        /// <summary>
        /// 出售物品
        /// </summary>
        public virtual bool SellItem(Character seller, int itemId)
        {
            // TODO: 实现出售逻辑
            Debug.Log($"{seller.ActorName} 出售了物品");
            return true;
        }
        
        #endregion
        
        #region 任务功能
        
        /// <summary>
        /// 显示任务菜单
        /// </summary>
        public virtual void ShowQuestMenu()
        {
            if (_npcType != NPCType.QuestGiver)
            {
                Debug.LogWarning("该NPC不是任务NPC");
                return;
            }
            
            Debug.Log($"{_actorName} 的任务列表:");
            foreach (var questId in _questIds)
            {
                Debug.Log($"  - 任务ID: {questId}");
            }
        }
        
        /// <summary>
        /// 接取任务
        /// </summary>
        public virtual bool AcceptQuest(Character player, int questId)
        {
            if (!_questIds.Contains(questId))
            {
                Debug.LogWarning("该NPC没有此任务");
                return false;
            }
            
            Debug.Log($"{player.ActorName} 接取了任务 {questId}");
            OnQuestAccepted?.Invoke(questId);
            return true;
        }
        
        /// <summary>
        /// 提交任务
        /// </summary>
        public virtual bool SubmitQuest(Character player, int questId)
        {
            // TODO: 验证任务完成条件
            Debug.Log($"{player.ActorName} 提交了任务 {questId}");
            OnQuestSubmitted?.Invoke(questId);
            return true;
        }
        
        #endregion
        
        #region 其他功能
        
        /// <summary>
        /// 显示训练菜单（技能训练师）
        /// </summary>
        protected virtual void ShowTrainingMenu()
        {
            Debug.Log($"{_actorName}: 想要学习什么技能？");
        }
        
        /// <summary>
        /// 显示强化菜单（铁匠）
        /// </summary>
        protected virtual void ShowUpgradeMenu()
        {
            Debug.Log($"{_actorName}: 要强化装备吗？");
        }
        
        /// <summary>
        /// 显示传送菜单
        /// </summary>
        protected virtual void ShowTeleportMenu()
        {
            Debug.Log($"{_actorName}: 想传送到哪里？");
        }
        
        /// <summary>
        /// 传送玩家
        /// </summary>
        public virtual void TeleportPlayer(Character player, Vector3 destination)
        {
            if (_npcType != NPCType.Teleporter) return;
            
            player.transform.position = destination;
            EndInteraction();
            Debug.Log($"{player.ActorName} 被传送到 {destination}");
        }
        
        #endregion
    }
}
