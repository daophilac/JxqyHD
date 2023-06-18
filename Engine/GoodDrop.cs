﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Engine
{
    public static class GoodDrop
    {
        static private Obj GetObj(GoodType type, Character character)
        {
            if (character == null) return null;
            var fileName = string.Empty;
            switch (type)
            {
                case GoodType.Weapon:
                    fileName = "可捡武器.ini";
                    break;
                case GoodType.Armor:
                    fileName = "可捡防具.ini";
                    break;
                case GoodType.Money:
                    fileName = "可捡钱.ini";
                    break;
                case GoodType.Drug:
                    fileName = "可捡药品.ini";
                    break;
                default:
                    return null;
            }
            var obj = new Obj(@"ini\obj\" + fileName);
            obj.TilePosition = character.TilePosition;
            var level = character.Level;
            if (character.ExpBonus > 0)
            {
                var rand = Globals.TheRandom.Next(100);
                if (rand < 10)
                {
                    level += 0;
                }
                else if(rand < 60)
                {
                    level += 12;
                }
                else
                {
                    level += 24;
                }
            }
            obj.ScriptFile = GetScriptFileName(type, level);
            return obj;
        }

        static private string GetScriptFileName(GoodType type, int characterLevel)
        {
            var fileName = string.Empty;
            switch (type)
            {
                case GoodType.Weapon:
                case GoodType.Armor:
                case GoodType.Money:
                {
                    var level = characterLevel/12 + 1;
                    if (level > 7) level = 7;
                    switch (type)
                    {
                        case GoodType.Weapon:
                            fileName = level + "级武器.txt";
                            break;
                        case GoodType.Armor:
                            fileName = level + "级防具.txt";
                            break;
                        case GoodType.Money:
                            fileName = level + "级钱.txt";
                            break;
                    }
                }
                    break;
                case GoodType.Drug:
                    if (characterLevel <= 10)
                    {
                        fileName = "低级药品.txt";
                    }
                    else if (characterLevel <= 30)
                    {
                        fileName = "中级药品.txt";
                    }
                    else if (characterLevel <= 60)
                    {
                        fileName = "高级药品.txt";
                    }
                    else
                    {
                        fileName = "特级药品.txt";
                    }
                    break;
            }

            return fileName;
        }

        /// <summary>
        /// Get drap obj.If character not drop obj return null.
        /// </summary>
        /// <param name="characterLevel">Character level</param>
        /// <returns>Droped obj.If not drop return null.</returns>
        static public Obj GetDropObj(Character character)
        {
            //Just enemy can drop
            if (Globals.IsDropGoodWhenDefeatEnemyDisabled || !character.IsEnemy || character.NoDropWhenDie > 0) return null;

            if (!string.IsNullOrEmpty(character.DropIni))
            {
                var ini = character.DropIni;
                if (ini.EndsWith("]"))
                {
                    var badFormat = false;
                    var startIdx = ini.LastIndexOf("[", StringComparison.Ordinal);
                    if (startIdx != -1)
                    {
                        var rand = 0;
                        if (int.TryParse(ini.Substring(startIdx + 1, ini.Length - startIdx - 2), out rand))
                        {
                            if (Globals.TheRandom.Next(100) > rand)
                            {
                                return null;
                            }
                        }
                        else
                        {
                            badFormat = true;
                        }
                    }
                    else
                    {
                        badFormat = true;
                    }

                    if (badFormat)
                    {
                        MessageBox.Show("DropIni格式错误，无法解析，角色名=" + character.Name + " DropIni=" + character.DropIni);
                    }
                    else
                    {
                        ini = ini.Substring(0, startIdx);
                    }
                }
                var obj = new Obj(@"ini\obj\" + ini);
                obj.TilePosition = character.TilePosition;
                return obj;
            }

            if (character.ExpBonus > 0)
            {
                //Boss
                return GetObj(Globals.TheRandom.Next(0, 2) == 0 ? GoodType.Weapon : GoodType.Armor,
                    character);
            }

            var goodType = (GoodType) Globals.TheRandom.Next(0, (int) GoodType.MaxType);
            var maxRandValue = 2;
            switch (goodType)
            {
                case GoodType.Weapon:
                case GoodType.Armor:
                    maxRandValue = 10;
                    break;
            }

            if (Globals.TheRandom.Next(maxRandValue) == 0)
            {
                return GetObj(goodType, character);
            }
            return null;
        }

        public enum GoodType
        {
            Weapon,
            Armor,
            Money,
            Drug,
            MaxType
        }
    }
}