﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Assets.Scripts.Network;
using Assets.Scripts.PlayerControl;
using Assets.Scripts.Utility;
using JetBrains.Annotations;
using RebuildSharedData.ClientTypes;
using RebuildSharedData.Enum;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.U2D;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Sprites
{
    public class MapViewpoint
    {
        public string MapName;
        public int ZoomMin;
        public int ZoomDist;
        public int ZoomIn;
        public int SpinMin;
        public int SpinMax;
        public int SpinIn;
        public int HeightMin;
        public int HeightMax;
        public int HeightIn;
    }

    public class ClientDataLoader : MonoBehaviour
    {
        public static ClientDataLoader Instance;

        public TextAsset MonsterClassData;
        public TextAsset PlayerClassData;
        public TextAsset PlayerHeadData;
        public TextAsset PlayerWeaponData;
        public TextAsset WeaponClassData;
        public TextAsset SkillData;
        public TextAsset SkillTreeData;
        public TextAsset MapViewpointData;
        public TextAsset UniqueAttackActionData;
        public TextAsset ItemData;
        public TextAsset MapData;
        public TextAsset EquipmentSpriteData;
        public SpriteAtlas ItemIconAtlas;

        private readonly Dictionary<int, MonsterClassData> monsterClassLookup = new();
        private readonly Dictionary<int, PlayerHeadData> playerHeadLookup = new();
        private readonly Dictionary<int, PlayerClassData> playerClassLookup = new();
        private readonly Dictionary<int, Dictionary<int, PlayerWeaponData>> playerWeaponLookup = new();
        private readonly Dictionary<int, WeaponClassData> weaponClassData = new();
        private readonly Dictionary<CharacterSkill, SkillData> skillData = new();
        private readonly Dictionary<string, MapViewpoint> mapViewpoints = new();
        private readonly Dictionary<string, Dictionary<CharacterSkill, UniqueAttackAction>> uniqueSpriteActions = new();
        private readonly Dictionary<int, ClientSkillTree> jobSkillTrees = new();
        private readonly Dictionary<string, int> jobNameToIdTable = new();
        private readonly Dictionary<int, ItemData> itemIdLookup = new();
        private readonly Dictionary<string, ItemData> itemNameLookup = new();
        private readonly Dictionary<string, ClientMapEntry> mapDataLookup = new();
        private readonly Dictionary<string, string> displaySpriteList = new();

        private readonly List<string> validMonsterClasses = new();
        private readonly List<string> validMonsterCodes = new();

        private static int EffectClassId = 3999;

        private bool isInitialized;

        public bool IsValidMonsterName(string name) => validMonsterClasses.Contains(name);
        public bool IsValidMonsterCode(string name) => validMonsterCodes.Contains(name);

        public int GetJobIdForName(string name) => jobNameToIdTable.GetValueOrDefault(name, -1);
        public string GetFullNameForMap(string mapName) => mapDataLookup.TryGetValue(mapName, out var map) ? map.Name : "Unknown Map";
        public string GetJobNameForId(int id) => playerClassLookup.TryGetValue(id, out var job) ? job.Name : "-";
        public string GetSkillName(CharacterSkill skill) => skillData.TryGetValue(skill, out var skOut) ? skOut.Name : "";
        public SkillData GetSkillData(CharacterSkill skill) => skillData[skill];
        public SkillTarget GetSkillTarget(CharacterSkill skill) => skillData.TryGetValue(skill, out var target) ? target.Target : SkillTarget.Any;
        public Dictionary<CharacterSkill, SkillData> GetAllSkills() => skillData;
        public ClientSkillTree GetSkillTree(int jobId) => jobSkillTrees.GetValueOrDefault(jobId);

        public MapViewpoint GetMapViewpoint(string mapName) => mapViewpoints.GetValueOrDefault(mapName);
        public MonsterClassData GetMonsterData(int classId) => monsterClassLookup.GetValueOrDefault(classId);
        public ItemData GetItemById(int id) => itemIdLookup.TryGetValue(id, out var item) ? item : itemIdLookup[-1];
        public ItemData GetItemByName(string name) => itemNameLookup[name];
        public bool TryGetItemByName(string name, out ItemData item) => itemNameLookup.TryGetValue(name, out item);
        public bool TryGetItemById(int id, out ItemData item) => itemIdLookup.TryGetValue(id, out item);

        public string GetHitSoundForWeapon(int weaponId)
        {
            if (!weaponClassData.TryGetValue(weaponId, out var weapon))
                weapon = weaponClassData[0];

            var hitSoundsCount = weapon.HitSounds.Count;
            if (hitSoundsCount <= 1)
                return weapon.HitSounds[0];

            return weapon.HitSounds[Random.Range(0, hitSoundsCount)];
        }

        public bool GetUniqueAction(string spriteName, CharacterSkill skill, out UniqueAttackAction actOut)
        {
            actOut = null;
            if (uniqueSpriteActions.TryGetValue(spriteName, out var list))
                if (list.TryGetValue(skill, out var action))
                    actOut = action;
            return actOut != null;
        }

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            Instance = this;
            var entityData = JsonUtility.FromJson<Wrapper<MonsterClassData>>(MonsterClassData.text);
            foreach (var m in entityData.Items)
            {
                monsterClassLookup.Add(m.Id, m);
                validMonsterClasses.Add(m.Name);
                validMonsterCodes.Add(m.Code);
            }

            var headData = JsonUtility.FromJson<Wrapper<PlayerHeadData>>(PlayerHeadData.text);
            foreach (var h in headData.Items)
            {
                //playerHeadLookup.Add(h.Id, h);

                for (var i = 0; i < Mathf.Min(h.FemaleIds.Length, h.MaleIds.Length); i++)
                {
                    var colorHead = h.Id + (i << 8);

                    var mPath = h.SpriteMale.Replace("<id>", h.MaleIds[i]);
                    var fPath = h.SpriteFemale.Replace("<id>", h.FemaleIds[i]);

                    var handle = Addressables.LoadResourceLocationsAsync(mPath);
                    handle.WaitForCompletion();
                    var handle2 = Addressables.LoadResourceLocationsAsync(fPath);
                    handle2.WaitForCompletion();
                    if (handle.Result.Count > 0 && handle2.Result.Count > 0)
                    {
                        playerHeadLookup.Add(colorHead, new PlayerHeadData()
                        {
                            Id = colorHead,
                            SpriteMale = mPath,
                            SpriteFemale = fPath
                        });
                    }
                    else
                        Debug.LogWarning($"Failed to find expected head variation {h.MaleIds[i]} and {h.FemaleIds[i]}");
                }
            }


            var playerData = JsonUtility.FromJson<Wrapper<PlayerClassData>>(PlayerClassData.text);
            foreach (var p in playerData.Items)
            {
                playerClassLookup.Add(p.Id, p);
                jobNameToIdTable.Add(p.Name, p.Id);
            }

            //split weapon entries into a tree of base job -> weapon class -> sprite list
            var weaponData = JsonUtility.FromJson<Wrapper<PlayerWeaponData>>(PlayerWeaponData.text);
            foreach (var weapon in weaponData.Items)
            {
                if (!playerWeaponLookup.ContainsKey(weapon.Job))
                    playerWeaponLookup.Add(weapon.Job, new Dictionary<int, PlayerWeaponData>());

                var jList = playerWeaponLookup[weapon.Job];
                if (!jList.ContainsKey(weapon.Class))
                    jList.Add(weapon.Class, weapon);
            }

            var weaponClass = JsonUtility.FromJson<Wrapper<WeaponClassData>>(WeaponClassData.text);
            foreach (var weapon in weaponClass.Items)
                weaponClassData.TryAdd(weapon.Id, weapon);


            var items = JsonUtility.FromJson<Wrapper<ItemData>>(ItemData.text);

            var itemIcons = new Dictionary<string, string>();
            foreach (var item in items.Items)
            {
                itemIdLookup.Add(item.Id, item);
                itemNameLookup.Add(item.Code, item);
                if (!itemIcons.ContainsKey(item.Sprite))
                    itemIcons.Add(item.Sprite, item.Code);
                item.Sprite = itemIcons[item.Sprite];
            }

            itemIdLookup.Add(-1, new ItemData()
            {
                Code = "UNKNOWN_ITEM",
                Id = -1,
                IsUnique = true,
                ItemClass = ItemClass.Etc,
                Name = "Unknown Item",
                Sprite = "Apple"
            });

            foreach (var l in Regex.Split(EquipmentSpriteData.text, "\n|\r|\r\n"))
            {
                if (string.IsNullOrWhiteSpace(l))
                    continue;
                // Debug.Log(l);
                var l2 = l.Trim().Split('\t');
                displaySpriteList.Add(l2[0], l2[1]);
            }

            var uniqueAttacks = JsonUtility.FromJson<Wrapper<UniqueAttackAction>>(UniqueAttackActionData.text);
            foreach (var action in uniqueAttacks.Items)
            {
                if (!uniqueSpriteActions.TryGetValue(action.Sprite, out var list))
                {
                    list = new Dictionary<CharacterSkill, UniqueAttackAction>();
                    uniqueSpriteActions.Add(action.Sprite, list);
                }

                if (Enum.TryParse(action.Action, out CharacterSkill skill))
                    list.Add(skill, action);
                else
                    Debug.LogWarning($"Could not convert {action.Action} to a skill type when parsing unique skill actions");
            }

            var skills = JsonUtility.FromJson<Wrapper<SkillData>>(SkillData.text);
            foreach (var skill in skills.Items)
            {
                skill.Icon = "skill_" + skill.Icon;
                skill.Description = skill.Description.Replace("\r\n", "\n");
                skill.Description = skill.Description.Replace("<br>\n", "<br>"); //should do this elsewhere honestly...
                skill.Description = skill.Description.Replace("\n", "<line-height=25>\n</line-height>");
                skillData.Add(skill.SkillId, skill);
            }

            var trees = JsonUtility.FromJson<Wrapper<ClientSkillTree>>(SkillTreeData.text);
            foreach (var tree in trees.Items)
                jobSkillTrees.Add(tree.ClassId, tree);


            foreach (var mapDef in MapViewpointData.text.Split("\r\n"))
            {
                var s = mapDef.Split(',');
                if (s.Length < 9 || s[0] == "map")
                    continue;
                mapViewpoints.Add(s[0], new MapViewpoint()
                {
                    MapName = s[0],
                    ZoomMin = int.Parse(s[1]),
                    ZoomDist = int.Parse(s[2]),
                    ZoomIn = int.Parse(s[3]),
                    SpinMin = int.Parse(s[4]),
                    SpinMax = int.Parse(s[5]),
                    SpinIn = int.Parse(s[6]),
                    HeightMin = int.Parse(s[7]),
                    HeightMax = int.Parse(s[8]),
                    HeightIn = int.Parse(s[9]),
                });
            }


            var mapClass = JsonUtility.FromJson<Wrapper<ClientMapEntry>>(MapData.text);
            foreach (var map in mapClass.Items)
                mapDataLookup.TryAdd(map.Code, map);

            isInitialized = true;
        }

        private List<RoSpriteAnimator> tempList;

        public void ChangePlayerClass(ServerControllable player, ref PlayerSpawnParameters param)
        {
            if (!isInitialized)
                throw new Exception($"We shouldn't be changing the player class while not initialized!");

            var pData = playerClassLookup[0]; //novice
            if (playerClassLookup.TryGetValue(param.ClassId, out var lookupData))
                pData = lookupData;
            else
                Debug.LogWarning("Failed to find player with id of " + param.ClassId);

            var hData = playerHeadLookup[0]; //default;
            if (playerHeadLookup.TryGetValue(param.HeadId, out var lookupData2))
                hData = lookupData2;
            else
                Debug.LogWarning("Failed to find player head with id of " + param.ClassId);

            var bodySprite = player.SpriteAnimator;

            //find the head child, discard everything else
            if (tempList == null)
                tempList = new List<RoSpriteAnimator>();
            else
                tempList.Clear();

            RoSpriteAnimator headSprite = null;

            for (var i = 0; i < bodySprite.ChildrenSprites.Count; i++)
            {
                var type = bodySprite.ChildrenSprites[i].Type;
                switch (type)
                {
                    case SpriteType.Head:
                        headSprite = bodySprite.ChildrenSprites[i];
                        tempList.Add(headSprite);
                        break;
                    case SpriteType.Headgear:
                        tempList.Add(bodySprite.ChildrenSprites[i]);
                        break;
                    default:
                        Destroy(bodySprite.ChildrenSprites[i].gameObject);
                        break;
                }
            }

            if (headSprite == null)
                throw new Exception($"Existing player has no head!");

            bodySprite.ChildrenSprites.Clear();
            for (var i = 0; i < tempList.Count; i++)
                bodySprite.ChildrenSprites.Add(tempList[i]);
        }

        public string GetPlayerBodySpriteName(int jobId, bool isMale)
        {
            var pData = playerClassLookup[0]; //novice
            if (playerClassLookup.TryGetValue(jobId, out var lookupData))
                pData = lookupData;
            else
                Debug.LogWarning("Failed to find player with id of " + jobId);

            return isMale ? pData.SpriteMale : pData.SpriteFemale;
        }

        public string GetPlayerHeadSpriteName(int headId, int color, bool isMale)
        {
            var hData = playerHeadLookup[0]; //default;
            if (playerHeadLookup.TryGetValue(headId + (color << 8), out var lookupData2)) //see if we have a head with the right palette
                hData = lookupData2;
            else if (playerHeadLookup.TryGetValue(headId, out lookupData2)) //fallback to default color
                hData = lookupData2;

            return isMale ? hData.SpriteMale : hData.SpriteFemale;
        }

        public string GetHeadgearSpriteName(int itemId, bool isMale)
        {
            if (!itemIdLookup.TryGetValue(itemId, out var item))
                return null;

            return $"Assets/Sprites/Headgear/{(isMale ? "Male" : "Female")}/{item.Sprite}.spr";
        }

        public static bool DoesAddressableExist<T>(string key)
        {
            foreach (var local in Addressables.ResourceLocators)
            {
                local.Locate(key, typeof(T), out IList<IResourceLocation> resourceLocations);
                if (resourceLocations != null)
                {
                    if (resourceLocations.Count >= 2)
                        Debug.LogWarning($"key = {key} type = {typeof(T).Name} was found in {resourceLocations.Count} locations.");
                    //location = resourceLocations[0];
                    return true;
                }
            }

            Debug.LogWarning($"Addressable {key} not found.");

            return false;
        }


        public ServerControllable InstantiatePlayer(ref PlayerSpawnParameters param)
        {
            if (!isInitialized)
                Initialize();

            var pData = playerClassLookup[0]; //novice
            if (playerClassLookup.TryGetValue(param.ClassId, out var lookupData))
                pData = lookupData;
            else
                Debug.LogWarning("Failed to find player with id of " + param.ClassId);

            var go = new GameObject(pData.Name);
            go.layer = LayerMask.NameToLayer("Characters");
            go.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            var control = go.AddComponent<ServerControllable>();
            var billboard = go.AddComponent<BillboardObject>();
            billboard.Style = BillboardStyle.Character;

            var body = new GameObject("Sprite");
            body.layer = LayerMask.NameToLayer("Characters");
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.AddComponent<SortingGroup>();

            var head = new GameObject("Head");
            head.layer = LayerMask.NameToLayer("Characters");
            head.transform.SetParent(body.transform, false);
            head.transform.localPosition = Vector3.zero;

            var bodySprite = body.AddComponent<RoSpriteAnimator>();
            var headSprite = head.AddComponent<RoSpriteAnimator>();

            control.ClassId = param.ClassId;
            control.SpriteAnimator = bodySprite;
            control.CharacterType = CharacterType.Player;
            control.SpriteMode = ClientSpriteType.Sprite;
            control.IsAlly = true;
            control.IsMainCharacter = param.IsMainCharacter;
            control.IsMale = param.IsMale;
            control.Level = param.Level;
            control.WeaponClass = param.WeaponClass;

            bodySprite.Controllable = control;
            if (param.State == CharacterState.Moving)
                bodySprite.ChangeMotion(SpriteMotion.Walk);
            bodySprite.ChildrenSprites.Add(headSprite);
            //bodySprite.SpriteOffset = 0.5f;
            bodySprite.HeadFacing = param.HeadFacing;

            if (param.State == CharacterState.Sitting)
                bodySprite.State = SpriteState.Sit;
            if (param.State == CharacterState.Moving)
                bodySprite.State = SpriteState.Walking;
            if (param.State == CharacterState.Dead)
            {
                bodySprite.State = SpriteState.Dead;
                bodySprite.ChangeMotion(SpriteMotion.Dead, true);
            }

            headSprite.Parent = bodySprite;
            headSprite.SpriteOrder = 1;

            control.ShadowSize = 0.5f;
            control.WeaponClass = param.WeaponClass;

            var bodySpriteName = GetPlayerBodySpriteName(param.ClassId, param.IsMale);
            var headSpriteName = GetPlayerHeadSpriteName(param.HeadId, param.HairDyeId, param.IsMale);

            // bodySpriteName = bodySpriteName.Replace(".spr", "_4.spr");

            Debug.Log($"Instantiate player sprite with job {param.ClassId} weapon {param.WeaponClass}");

            LoadAndAttachWeapon(control, param.Weapon);
            
            LoadAndAttachEquipmentSprite(control, param.Headgear1, EquipPosition.HeadUpper, 4);
            LoadAndAttachEquipmentSprite(control, param.Headgear2, EquipPosition.HeadMid, 3);
            LoadAndAttachEquipmentSprite(control, param.Headgear3, EquipPosition.HeadLower, 2);
            
            control.ConfigureEntity(param.ServerId, param.Position, param.Facing);
            control.Name = param.Name;
            control.Hp = param.Hp;
            control.MaxHp = param.MaxHp;
            // control.EnsureFloatingDisplayCreated().SetUp(param.Name, param.MaxHp, 0);

            AddressableUtility.LoadRoSpriteData(go, bodySpriteName, bodySprite.OnSpriteDataLoad);
            AddressableUtility.LoadRoSpriteData(go, headSpriteName, headSprite.OnSpriteDataLoad);
            AddressableUtility.LoadSprite(go, "shadow", control.AttachShadow);

            if (control.IsMainCharacter)
            {
                CameraFollower.Instance.CharacterJob.text = pData.Name;
                CameraFollower.Instance.CharacterName.text = $"Lv. {control.Level} {control.Name}";
            }

            control.Init();

            if (param.CharacterStatusEffects != null)
                foreach (var s in param.CharacterStatusEffects)
                    StatusEffectState.AddStatusToTarget(control, s);

            return control;
        }

        public void LoadAndAttachWeapon(ServerControllable ctrl, int item)
        {
            var weaponSpriteFile = "";
            var isEffect = item == int.MaxValue;
            
            var attachPosition = isEffect ? EquipPosition.Accessory : EquipPosition.Weapon; //it's not an accessory but too lazy to make a new option
            if (ctrl.AttachedComponents.TryGetValue(attachPosition, out var existing))
            {
                ctrl.SpriteAnimator.ChildrenSprites.Remove(existing.GetComponent<RoSpriteAnimator>());
                Destroy(existing);
            }
            
            var data = GetItemById(item);
            if (data.Id > 0)
            {
                var spr = $"Assets/Sprites/Weapon/{GetJobNameForId(ctrl.ClassId)}/{(ctrl.IsMale ? "Male/남_" : "Female/여_")}{data.Sprite}.spr";
                if (DoesAddressableExist<RoSpriteData>(spr))
                {
                    weaponSpriteFile = spr;
                }
                else
                    Debug.Log($"Weapon sprite {data.Sprite} could not be loaded for {ctrl.Name}");
            }

            if (!playerWeaponLookup.TryGetValue(ctrl.ClassId, out var weaponsByJob))
                return;
            
            if (!weaponsByJob.TryGetValue(ctrl.WeaponClass, out var weapon))
            {
                Debug.Log($"Could not load default weapon sprite for weapon class {ctrl.WeaponClass} for job {ctrl.ClassId}");
                return;
            }

            if (string.IsNullOrWhiteSpace(weaponSpriteFile))
            {
                weaponSpriteFile = ctrl.IsMale ? weapon.SpriteMale : weapon.SpriteFemale;
                if (isEffect)
                    weaponSpriteFile = ctrl.IsMale ? weapon.EffectMale : weapon.EffectFemale;
            }

            var bodyTransform = ctrl.SpriteAnimator.transform;

            var weaponObj = new GameObject("Weapon");
            weaponObj.layer = LayerMask.NameToLayer("Characters");
            weaponObj.transform.SetParent(bodyTransform, false);
            weaponObj.transform.localPosition = Vector3.zero;

            var weaponSprite = weaponObj.AddComponent<RoSpriteAnimator>();

            weaponSprite.Parent = ctrl.SpriteAnimator;
            weaponSprite.SpriteOrder = 5;
            if (isEffect)
                weaponSprite.SpriteOrder = 20;

            ctrl.SpriteAnimator.PreferredAttackMotion = ctrl.IsMale ? weapon.AttackMale : weapon.AttackFemale;
            ctrl.SpriteAnimator.ChildrenSprites.Add(weaponSprite);

            ctrl.AttachedComponents[attachPosition] = weaponObj;

            AddressableUtility.LoadRoSpriteData(ctrl.gameObject, weaponSpriteFile, weaponSprite.OnSpriteDataLoad);
            if(!isEffect) LoadAndAttachWeapon(ctrl, int.MaxValue); //attach effect sprite too
        }


        public void LoadAndAttachEquipmentSprite(ServerControllable ctrl, int itemId, EquipPosition position, int priority)
        {
            if (ctrl.AttachedComponents.TryGetValue(position, out var existing))
            {
                ctrl.SpriteAnimator.ChildrenSprites.Remove(existing.GetComponent<RoSpriteAnimator>());
                Destroy(existing);
            }

            if (!itemIdLookup.TryGetValue(itemId, out var hat) || !displaySpriteList.TryGetValue(hat.Code, out var hatSprite))
                return;

            var headgearObj = new GameObject(position.ToString());
            headgearObj.layer = LayerMask.NameToLayer("Characters");
            headgearObj.transform.SetParent(ctrl.SpriteAnimator.transform, false);
            headgearObj.transform.localPosition = new Vector3(0f, 0f, -0.05f);

            var headgearSprite = headgearObj.AddComponent<RoSpriteAnimator>();
            
            headgearSprite.Parent = ctrl.SpriteAnimator;
            headgearSprite.SpriteOrder = priority; //weapon is 5 so we should be below that
            ctrl.SpriteAnimator.ChildrenSprites.Add(headgearSprite);

            var spriteName = $"Assets/Sprites/{(position != EquipPosition.Shield ? "Headgear" : "Shield")}/{(ctrl.IsMale ? "Male/남_" : "Female/여_")}{hatSprite}.spr";

            ctrl.AttachedComponents[position] = headgearObj;

            if (!DoesAddressableExist<RoSpriteData>(spriteName))
                Debug.LogWarning($"Could not load equipment sprite at path: {spriteName}");
            else
                AddressableUtility.LoadRoSpriteData(ctrl.gameObject, spriteName, headgearSprite.OnSpriteDataLoad);
        }

        public void AttachEmote(GameObject target, int emoteId)
        {
            if (emoteId >= 34)
                emoteId--; //34 is the chat prohibited marker, but it isn't actually an emote within emotes.spr

            var go = new GameObject("Emote");
            //go.layer = LayerMask.NameToLayer("Characters");
            var billboard = go.AddComponent<BillboardObject>();
            billboard.Style = BillboardStyle.Character;

            var emote = go.AddComponent<EmoteController>();
            emote.AnimationId = emoteId;
            emote.Target = target;

            var child = new GameObject("Sprite");
            //child.layer = LayerMask.NameToLayer("Characters");
            child.transform.SetParent(go.transform, false);
            child.transform.localPosition = Vector3.zero + new Vector3(0, 0, -0.01f);
            child.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

            var sr = child.AddComponent<RoSpriteRendererStandard>();
            sr.SecondPassForWater = false;
            sr.UpdateAngleWithCamera = false;

            var sprite = child.AddComponent<RoSpriteAnimator>();
            emote.RoSpriteAnimator = sprite;
            sprite.Type = SpriteType.Npc;
            sprite.State = SpriteState.Dead;
            //sprite.LockAngle = true;
            sprite.SpriteRenderer = sr;

            AddressableUtility.LoadRoSpriteData(go, "Assets/Sprites/Misc/emotion.spr", emote.OnFinishLoad);
        }

        public ServerControllable InstantiateEffect(ref MonsterSpawnParameters param, NpcEffectType type)
        {
            if (type == NpcEffectType.None)
            {
                Debug.LogError($"Attempting to instantiate effect with type None!");
                return null;
            }

            // Debug.Log($"Instantiating effect {type}");

            var obj = new GameObject(type.ToString());
            obj.layer = LayerMask.NameToLayer("Characters");
            obj.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

            var control = obj.AddComponent<ServerControllable>();
            control.ClassId = param.ClassId;
            control.CharacterType = CharacterType.NPC;
            control.SpriteMode = ClientSpriteType.Prefab;
            control.EntityObject = obj;
            control.Level = param.Level;
            control.Name = param.Name;
            control.IsAlly = true;
            control.IsInteractable = false;

            control.ConfigureEntity(param.ServerId, param.Position, param.Facing);
            obj.AddComponent<BillboardObject>();

            switch (type)
            {
                case NpcEffectType.Firewall:
                    CameraFollower.Instance.AttachEffectToEntity("FirewallEffect", obj);
                    break;
            }

            return control;
        }

        private ServerControllable PrefabMonster(MonsterClassData mData, ref MonsterSpawnParameters param)
        {
            var prefabName = mData.SpriteName;

            var obj = new GameObject(prefabName);

            var control = obj.AddComponent<ServerControllable>();
            control.ClassId = param.ClassId;
            control.CharacterType = CharacterType.NPC;
            control.SpriteMode = ClientSpriteType.Prefab;
            control.EntityObject = obj;
            control.Level = param.Level;
            control.Name = param.Name;
            control.IsAlly = true;
            control.IsInteractable = false;

            control.ConfigureEntity(param.ServerId, param.Position, param.Facing);
            control.EnsureFloatingDisplayCreated().SetUp(control, param.Name, param.MaxHp, 0, false, false);

            var loader = Addressables.LoadAssetAsync<GameObject>(prefabName);
            loader.Completed += ah =>
            {
                if (obj != null && obj.activeInHierarchy)
                {
                    var obj2 = GameObject.Instantiate(ah.Result, obj.transform, false);
                    obj2.transform.localPosition = Vector3.zero;

                    var sprite = obj2.GetComponent<RoSpriteAnimator>();
                    if (sprite != null)
                        sprite.Controllable = control;
                }
            };

            control.Init();

            return control;
        }

        public ServerControllable InstantiateMonster(ref MonsterSpawnParameters param, CharacterType entityType)
        {
            if (!isInitialized)
                Initialize();

            var mData = monsterClassLookup[4000]; //poring
            if (monsterClassLookup.TryGetValue(param.ClassId, out var lookupData))
                mData = lookupData;
            else
                Debug.LogWarning("Failed to find monster with id of " + param.ClassId);

            if (mData.SpriteName.Contains(".prefab"))
                return PrefabMonster(mData, ref param);

            var go = new GameObject(mData.Name);
            go.layer = LayerMask.NameToLayer("Characters");
            go.transform.localScale = new Vector3(1.5f * mData.Size, 1.5f * mData.Size, 1.5f * mData.Size);
            var control = go.AddComponent<ServerControllable>();
            // if (param.ClassId < 4000)
            //     control.CharacterType = CharacterType.NPC;
            // else
            //     control.CharacterType = CharacterType.Monster;
            control.ClassId = param.ClassId;
            control.CharacterType = entityType;
            control.SpriteMode = ClientSpriteType.Sprite;
            control.IsInteractable = param.Interactable;
            var billboard = go.AddComponent<BillboardObject>();
            billboard.Style = BillboardStyle.Character;

            var child = new GameObject("Sprite");
            child.layer = LayerMask.NameToLayer("Characters");
            child.transform.SetParent(go.transform, false);
            child.transform.localPosition = Vector3.zero;

            var sprite = child.AddComponent<RoSpriteAnimator>();
            sprite.Controllable = control;

            control.SpriteAnimator = sprite;
            //control.SpriteAnimator.SpriteOffset = mData.Offset;
            control.ShadowSize = mData.ShadowSize;
            control.IsAlly = false;
            control.Level = param.Level;
            control.Name = param.Name;
            if (string.IsNullOrEmpty(param.Name))
                control.Name = mData.Name;
            control.WeaponClass = 0;
            control.Hp = param.Hp;
            control.MaxHp = param.MaxHp;
            if (ColorUtility.TryParseHtmlString(mData.Color, out var color))
                sprite.BaseColor = color;

            control.ConfigureEntity(param.ServerId, param.Position, param.Facing);
            // control.EnsureFloatingDisplayCreated().SetUp(param.Name, param.MaxHp, 0);

            var basePath = "Assets/Sprites/Monsters/";
            if (param.ClassId < 4000)
                basePath = "Assets/Sprites/Npcs/";


            AddressableUtility.LoadRoSpriteData(go, basePath + mData.SpriteName, control.SpriteAnimator.OnSpriteDataLoad);
            if (mData.ShadowSize > 0)
                AddressableUtility.LoadSprite(go, "shadow", control.AttachShadow);

            control.Init();

            if (param.CharacterStatusEffects != null)
                foreach (var s in param.CharacterStatusEffects)
                    StatusEffectState.AddStatusToTarget(control, s);

            return control;
        }
    }
}