﻿using System;
using System.Collections.Generic;
using IniParser.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Engine
{
    public class Npc : Character
    {
        private List<Vector2> _actionPathTilePositionList;
        private int _idledFrame;
        private Character _keepDistanceCharacterWhenFriendDeath;
        private float _blindMilliseconds;

        private List<Vector2> ActionPathTilePositionList
        {
            get
            {
                if (_actionPathTilePositionList == null)
                {
                    _actionPathTilePositionList = GetRandTilePath(8, 
                        Kind == (int) CharacterKind.Flyer);
                }
                return _actionPathTilePositionList;
            }
            set { _actionPathTilePositionList = value; }
        }

        #region public properties
        public override PathFinder.PathType PathType
        {
            get
            {
                if (Kind == (int)CharacterKind.Flyer)
                {
                    return Engine.PathFinder.PathType.PathStraightLine;
                }
                else if (base.PathFinder == 1 || IsPartner)
                {
                    return Engine.PathFinder.PathType.PerfectMaxNpcTry;
                }
                else if (Kind == 0 || Kind == 5)
                {
                    // Normal npc
                    return Engine.PathFinder.PathType.PerfectMaxPlayerTry;
                }
                else if (base.PathFinder == 0 || IsInLoopWalk || IsEnemy)
                {
                    return Engine.PathFinder.PathType.PathOneStep;
                }
                else
                {
                    return Engine.PathFinder.PathType.PerfectMaxNpcTry;
                }
            }
        }

        public float BlindMilliseconds
        {
            get { return _blindMilliseconds; }
            set { _blindMilliseconds = value; }
        }


        public static bool IsAIDisabled { protected set; get; }
        #endregion

        #region Ctor
        public Npc() { }

        public Npc(string filePath)
            : base(filePath)
        {
            if (LevelIni == null)
            {
                LevelIni = Utils.GetLevelLists(@"ini\level\level-npc.ini");
            }
            Initlize();
        }

        public Npc(KeyDataCollection keyDataCollection)
            : base(keyDataCollection)
        {
            if (LevelIni == null)
            {
                LevelIni = Utils.GetLevelLists(@"ini\level\level-npc.ini");
            }

            Initlize();
        }

        private void Initlize()
        {
            if (_level < 0 && Globals.ThePlayer != null)
            {
                SetPropToLevel(Globals.ThePlayer.Level + Level);
            }
        }
        #endregion Ctor

        #region Private method

        private void MoveToPlayer()
        {
            if (!Globals.PlayerKindCharacter.IsStanding())
            {
                PartnerMoveTo(Globals.PlayerTilePosition);
            }
        }

        #endregion Private method

        public override bool HasObstacle(Vector2 tilePosition)
        {
            if (Kind == (int)CharacterKind.Flyer) return false;

            return (NpcManager.IsObstacle(tilePosition) ||
                    ObjManager.IsObstacle(tilePosition) ||
                    MagicManager.IsObstacle(tilePosition) ||
                    Globals.ThePlayer.TilePosition == tilePosition);
        }

        protected override void PlaySoundEffect(SoundEffect soundEffect)
        {
            SoundManager.Play3DSoundOnece(soundEffect,
                PositionInWorld - Globals.ListenerPosition);
        }

        protected override void FollowTargetFound(bool attackCanReach)
        {
            if (IsAIDisabled || _blindMilliseconds > 0)
            {
                CancleAttackTarget();
            }
            else
            {
                //Because character won't update new destination before step move end, 
                //so set MoveTargetChanged to true make sure character get new destination.
                MoveTargetChanged = true;

                if (attackCanReach)
                {
                    if (_idledFrame >= Idle)
                    {
                        _idledFrame = 0;
                        Attacking(FollowTarget.TilePosition);
                    }
                }
                else
                {
                    WalkTo(FollowTarget.TilePosition);
                }
            }
        }

        protected override void FollowTargetLost()
        {
            //just walk to last find position
            CancleAttackTarget();
            if (IsPartner)
            {
                MoveToPlayer();
            }
        }

        public static void DisableAI()
        {
            IsAIDisabled = true;
            //Cancle attack
            NpcManager.CancleFighterAttacking();
        }

        public static void EnableAI()
        {
            IsAIDisabled = false;
        }

        public bool KeepDistanceWhenLifeLow()
        {
            if (FollowTarget != null &&
                KeepRadiusWhenLifeLow > 0 &&
                LifeMax > 0 &&
                Life / (float)LifeMax <= LifeLowPercent / 100.0f)
            {
                //Life is low, keep distance with target
                var distance = Engine.PathFinder.GetViewTileDistance(TilePosition, FollowTarget.TilePosition);
                if (distance < KeepRadiusWhenLifeLow)
                {
                    if (MoveAwayTarget(FollowTarget.PositionInWorld, KeepRadiusWhenLifeLow - distance, false))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool CheckKeepDistanceWhenFriendDeath()
        {
            if (KeepRadiusWhenFriendDeath > 0 && 
                Kind != 3) // Follower has no effect
            {
                var target = _keepDistanceCharacterWhenFriendDeath;
                if (target == null || target.IsDeathInvoked)
                {
                    target = _keepDistanceCharacterWhenFriendDeath = null;

                    var dead = NpcManager.FindFriendDeadKilledByLiveCharacter(this, VisionRadius);
                    if (dead != null)
                    {
                        target = _keepDistanceCharacterWhenFriendDeath = dead.LastAttackerMagicSprite.BelongCharacter;
                    }
                }

                if (target != null)
                {
                    var distance = Engine.PathFinder.GetViewTileDistance(TilePosition, target.TilePosition);
                    if (distance < KeepRadiusWhenFriendDeath)
                    {
                        if (MoveAwayTarget(target.TilePosition, KeepRadiusWhenFriendDeath - distance, false))
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }

        public override void Update(GameTime gameTime)
        {
            if (_controledMagicSprite != null)
            {
                base.Update(gameTime);
                return;
            }

            if(_blindMilliseconds > 0)
            {
                _blindMilliseconds -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            }

            //Find follow target
            if (!IsFollowTargetFound || // Not find target.
                FollowTarget == null || // Follow target not assign.
                FollowTarget.IsDeathInvoked || //Follow target is death.
                !FollowTarget.IsVisible ||
                (IsEnemy && FollowTarget.IsEnemy && FollowTarget.Group == Group) || // Follow target relation changed
                (IsFighterFriend && (FollowTarget.IsFighterFriend || FollowTarget.IsPlayer)) || // Follow target relation changed
                IsAIDisabled || //Npc AI is disabled.
                _blindMilliseconds > 0) 
            {
                if (IsEnemy)
                {
                    //AIType - 1 - rand move, rand attack
                    if ((StopFindingTarget == 0 && AIType != 1) || 
                        (AIType == 1 && IsStanding() && Globals.TheRandom.Next(100) > 70))
                    {
                        FollowTarget = (IsAIDisabled || _blindMilliseconds > 0) ? null : NpcManager.GetLiveClosestOtherGropEnemy(Group, PositionInWorld);
                        if (NoAutoAttackPlayer == 0 && FollowTarget == null)
                        {
                            FollowTarget = (IsAIDisabled || _blindMilliseconds > 0) ? null : NpcManager.GetLiveClosestPlayerOrFighterFriend(PositionInWorld, true, false);
                        }
                    }
                    else if(FollowTarget != null && FollowTarget.IsDeathInvoked)
                    {
                        FollowTarget = null;
                    }
                }
                else if (IsFighterFriend)
                {
                    if (StopFindingTarget == 0)
                    {
                        FollowTarget = (IsAIDisabled || _blindMilliseconds > 0) ? null : NpcManager.GetClosestEnemyTypeCharacter(PositionInWorld, true, false);
                    }
                    else if (FollowTarget != null && FollowTarget.IsDeathInvoked)
                    {
                        FollowTarget = null;
                    }
                    //Fighter friend may be parter
                    if (FollowTarget == null && IsPartner)
                    {
                        //Can't find enemy, move to player
                        MoveToPlayer();
                    }
                }
                else if (IsNoneFighter)
                {
                    if (StopFindingTarget == 0)
                    {
                        FollowTarget = (IsAIDisabled || _blindMilliseconds > 0) ? null : NpcManager.GetLiveClosestNonneturalFighter(PositionInWorld);
                    }
                    else if (FollowTarget != null && FollowTarget.IsDeathInvoked)
                    {
                        FollowTarget = null;
                    }
                }
                else if (IsPartner)
                {
                    MoveToPlayer();
                }
            }

            if (!CheckKeepDistanceWhenFriendDeath() && !KeepDistanceWhenLifeLow() && MagicToUseWhenLifeLow != null && LifeMax > 0 && Life/(float)LifeMax <= LifeLowPercent/100.0f)
            {
                PerformeAttack(PositionInWorld+Utils.GetDirection8(CurrentDirection), MagicToUseWhenLifeLow);
            }
            else
            {
                //Follow target
                PerformeFollow();
            }

            //Attack interval
            if (_idledFrame < Idle)
            {
                _idledFrame++;
            }

            if (IsFollowTargetFound)
            {
                //Character found follow target and move to follow target.
                //This may cause character move far away from the initial base tile position.
                //Assigning ActionPathTilePositionList value to null,
                //when get next time, new path is generated use new base tile position.
                ActionPathTilePositionList = null;
            }
            else
            {
                if (AIType == 1 && IsStanding())
                {
                    var poses = GetRandTilePath(2, false, 10);
                    if (poses.Count == 2)
                    {
                        WalkTo(poses[1]);
                    }
                }
            }

            ////Follow target not found, do something else.
            if ((FollowTarget == null || !IsFollowTargetFound) &&
                !(IsFighterKind && IsAIDisabled)) //Fighter can't move when AI disabled
            {
                var isFlyer = Kind == (int)CharacterKind.Flyer;
                const int randWalkPosibility = 400;
                const int flyerRandWalkPosibility = 20;

                if (Action == (int)ActionType.LoopWalk &&
                    FixedPathTilePositionList != null)
                {
                    //Loop walk along FixedPos
                    LoopWalk(FixedPathTilePositionList,
                        isFlyer ? flyerRandWalkPosibility : randWalkPosibility,
                        ref _currentFixedPosIndex,
                        isFlyer);
                }
                else
                {
                    switch ((CharacterKind)Kind)
                    {
                        case CharacterKind.Normal:
                        case CharacterKind.Fighter:
                        case CharacterKind.GroundAnimal:
                        case CharacterKind.Eventer:
                        case CharacterKind.Flyer:
                            {
                                switch ((ActionType)Action)
                                {
                                    case ActionType.RandWalk:
                                        RandWalk(ActionPathTilePositionList,
                                            isFlyer ? flyerRandWalkPosibility : randWalkPosibility,
                                            isFlyer);
                                        break;
                                }
                            }
                            break;
                        case CharacterKind.AfraidPlayerAnimal:
                            {
                                KeepMinTileDistance(Globals.PlayerTilePosition, VisionRadius);
                            }
                            break;
                    }
                }
            }

            base.Update(gameTime);
        }
    }
}
