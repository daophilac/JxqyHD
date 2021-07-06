﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Engine.Map;
using IniParser.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine
{
    public static class ObjManager
    {
        private static LinkedList<Obj> _list = new LinkedList<Obj>();
        private static List<Obj> _objInView = new List<Obj>();
        private static string _fileName;

        public static List<Obj> ObjsInView
        {
            get { return _objInView; }
        }

        public static LinkedList<Obj> ObjList
        {
            get { return _list; }
        }

        /// <summary>
        /// Obj file name.
        /// </summary>
        public static string FileName
        {
            get { return _fileName; }
        }

        private static List<Obj> GetObjsInView()
        {
            var viewRegion = Globals.TheCarmera.CarmerRegionInWorld;
            var list = new List<Obj>(_list.Count);
            foreach (var obj in _list)
            {
                if (viewRegion.Intersects(obj.RegionInWorld))
                    list.Add(obj);
            }
            return list;
        }

        public static bool Load(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return false;
            ClearAllObjAndFileName();
            try
            {
                _fileName = fileName;
                var filePath = Utils.GetNpcObjFilePath(fileName);
                var lines = File.ReadAllLines(filePath, Globals.LocalEncoding);
                Load(lines);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        private static bool Load(string[] lines)
        {
            var count = lines.Count();
            for (var i = 0; i < count; )
            {
                var groups = Regex.Match(lines[i++], @"\[OBJ([0-9]+)\]").Groups;
                if (groups[0].Success)
                {
                    var contents = new List<string>();
                    while (i < count && !string.IsNullOrEmpty(lines[i]))
                    {
                        contents.Add(lines[i]);
                        i++;
                    }
                    AddObj(contents.ToArray());
                    i++;
                }
            }
            return true;
        }

        public static void AddObj(string[] lines)
        {
            var obj = new Obj();
            obj.Load(lines);
            AddObj(obj);
        }

        public static void AddObj(Obj obj)
        {
            if (obj != null)
            {
                _list.AddLast(obj);
            }
        }

        public static void AddObj(string fileName, int tileX, int tileY, int direction)
        {
            var path = @"ini\obj\" + fileName;
            var obj = new Obj(path);
            obj.TilePosition = new Vector2(tileX, tileY);
            obj.SetDirection(direction);
            AddObj(obj);
        }

        private static void DeleteObj(LinkedListNode<Obj> node)
        {
            _list.Remove(node);
        }

        public static void ClearAllObjAndFileName()
        {
            _fileName = string.Empty;
            _list.Clear();
        }

        public static void ClearBody()
        {
            for (var node = _list.First; node != null;)
            {
                var next = node.Next;
                if(node.Value.IsBody)
                    DeleteObj(node);
                node = next;
            }
        }

        public static bool IsObstacle(int tileX, int tileY)
        {
            foreach (var obj in _list)
            {
                if (obj.MapX == tileX && obj.MapY == tileY && obj.IsObstacle)
                    return true;
            }
            return false;
        }

        public static bool IsObstacle(Vector2 tilePosition)
        {
            return IsObstacle((int)tilePosition.X, (int)tilePosition.Y);
        }

        //just check objs in view
        public static bool IsObstacleInView(int tileX, int tileY)
        {
            foreach (var obj in ObjsInView)
            {
                if (obj.MapX == tileX && obj.MapY == tileY && obj.IsObstacle)
                    return true;
            }
            return false;
        }

        //just check objs in view
        public static bool IsObstacleInView(Vector2 tilePosition)
        {
            return IsObstacleInView((int)tilePosition.X, (int)tilePosition.Y);
        }

        public static Obj GetObstacle(int tileX, int tileY)
        {
            foreach (var obj in _list)
            {
                if (obj.MapX == tileX && obj.MapY == tileY && obj.IsObstacle)
                    return obj;
            }
            return null;
        }

        public static Obj GetObstacle(Vector2 tilePosition)
        {
            return GetObstacle((int)tilePosition.X, (int)tilePosition.Y);
        }

        public static Obj GetObj(string objName)
        {
            foreach (var obj in _list)
            {
                if (obj.ObjName == objName)
                    return obj;
            }
            return null;
        }

        public static List<Obj> getObj(Vector2 tilePos)
        {
            List<Obj> objs = null;
            foreach (var obj in _list)
            {
                if (obj.TilePosition == tilePos)
                {
                    if(objs == null)
                    {
                        objs = new List<Obj>();
                    }
                    objs.Add(obj);
                }
            }
            return objs;
        }

        public static void DeleteObj(string objName)
        {
            for (var node = _list.First; node != null;)
            {
                var next = node.Next;
                if (node.Value.ObjName == objName)
                {
                    DeleteObj(node);
                }
                node = next;
            }
        }

        public static Obj GetClosestCanInteractObj(Vector2 findBeginTilePosition, int maxTileDistance = int.MaxValue)
        {
            var minDistance = (maxTileDistance == int.MaxValue ? maxTileDistance : maxTileDistance + 1);
            Obj minObj = null;
            foreach (var obj in _list)
            {
                if (!string.IsNullOrEmpty(obj.ScriptFile))
                {
                    var tileDistance = PathFinder.GetViewTileDistance(findBeginTilePosition, obj.TilePosition);
                    if (tileDistance < minDistance)
                    {
                        minDistance = tileDistance;
                        minObj = obj;
                    }
                }
            }
            return minObj;
        }

        public static List<Obj> GetBodyInRaidus(Vector2 startTilePos, int radius, bool isDelete)
        {
            var retList = new List<Obj>();
            for (var node = _list.First; node != null;)
            {
                var next = node.Next;
                if (node.Value.Kind == (int)Obj.ObjKind.Body && PathFinder.GetViewTileDistance(startTilePos, node.Value.TilePosition) <= radius)
                {
                    retList.Add(node.Value);
                    if (isDelete)
                    {
                        DeleteObj(node);
                    }
                }
                node = next;
            }
            return retList;
        }

        public static void Save(string fileName = null)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                if (string.IsNullOrEmpty(_fileName))
                {
                    //Can't save without file name.
                    return;
                }
                fileName = _fileName;
            }
            _fileName = fileName;
            var path = @"save\game\" + fileName;
            try
            {
                var count = _list.Count;
                var data = new IniData();
                data.Sections.AddSection("Head");
                data["Head"].AddKey("Map",
                    MapBase.MapFileName);
                data["Head"].AddKey("Count", count.ToString());

                var node = _list.First;
                for (var i = 0; i < count; i++, node = node.Next)
                {
                    var sectionName = "OBJ" + string.Format("{0:000}", i);
                    data.Sections.AddSection(sectionName);
                    var obj = node.Value;
                    obj.Save(data[sectionName]);
                }
                File.WriteAllText(path, data.ToString(), Globals.LocalEncoding);
            }
            catch (Exception exception)
            {
                Log.LogFileSaveError("Obj", path, exception);
            }
        }

        public static void Update(GameTime gameTime)
        {
            for(var node = _list.First; node != null;)
            {
                var next = node.Next;
                node.Value.Update(gameTime);
                if (node.Value.IsRemoved)
                {
                    DeleteObj(node);
                }
                node = next;
            }
        }

        public static void UpdateObjsInView()
        {
            _objInView = GetObjsInView();
        }

        public static void Draw(SpriteBatch spriteBatch)
        {
            foreach (var obj in _list)
                obj.Draw(spriteBatch);
        }
    }
}
