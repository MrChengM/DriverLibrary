﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataServer.Points
{
    /// <summary>
    /// 点列表泛型类
      /// </summary>
    /// <typeparam name="T">bool，short，int等数据类型</typeparam>
    public class PointMapping<T> : IPointMapping<T>
    {
        public static PointMapping<T> Instance;
        private static readonly object locker = new object();
        private MappingIndexList _indexList;

        Dictionary<string,IPoint<T>> mapping=new Dictionary<string, IPoint<T>>();
        private ILog _log;


        //public void Init(ILog log)
        //{
        //    mapping = new Dictionary<string, IPoint<T>>();
        //    _log = log;
        //}
        public static PointMapping<T> GetInstance(ILog log)
        {
            if (Instance == null)
            {
                lock (locker)
                {
                    if (Instance == null)
                    {
                        Instance = new PointMapping<T>(log);
                    }
                }
            }
            return Instance;
        }
        private PointMapping(ILog log)
        {
            this._log = log;
            _indexList = MappingIndexList.GetInstance();
        }
        public ILog Log
        {
            get { return _log; }
            set { _log = value; }
        }
        public bool Find(string key)
        {
           return _indexList.Find(key);
        }

        public bool Find(string key, out string type)
        {
            return _indexList.Find(key, out type);
        }

        public IPoint<T> GetPoint(string key)
        {
            if (Find(key))
            {
               return mapping[key];
            }
            else
            {
                return null;
            }
        }

        public T[] GetValue(string key)
        {
            if (Find(key))
            {
                return GetPoint(key).GetValues();
            }
            else
            {
                return null;
            }
        }

        public void Register(string key, IPoint<T> point)
        {
            if(!Find(key))
            {
                
                mapping.Add(key, point);
                _indexList.Add(new MappingIndex(point.Name, point.ValueType));
            }
        }

        public void Remove(string key)
        {
            if (Find(key))
            {
                mapping.Remove(key);
                _indexList.Delete(key);
            }
        }

        public bool SetQuality(string key, QUALITIES quality)
        {
            if (Find(key))
            {
               return mapping[key].SetQuality(quality);
            }
            else
            {
              return  false;
            }
        }

        public int SetValue(string key, T[] value)
        {
            if (Find(key))
            {
              return  mapping[key].SetValue(value)?1:-1;
            }
            else
            {
                return -1;
            }
        }
    }
    public class MappingIndexList
    {
        List<MappingIndex> _indexList;
        #region 单例模式，PointMapping只有一个索引
        public static MappingIndexList Instance;
        private static readonly object locker=new object() ;
        private MappingIndexList()
        {
            _indexList = new List<MappingIndex>();
        }
        public static MappingIndexList GetInstance()
        {

            if (Instance == null)
            {
                lock (locker)
                {
                    if (Instance == null)
                    {
                        Instance = new MappingIndexList ();
                    }
                }
            }
            return Instance;
        }
        #endregion
        #region 增，删，查，改
        public void Add(MappingIndex index)
        {
            if (!Find(index.Name))
                _indexList.Add(index);
        }

        public void Delete(string name )
        {
                _indexList.RemoveAll((s)=>s.Name==name);
        }
        public bool Find(string name)
        {
           return _indexList.Exists((s)=>s.Name==name);
        }
        public bool Find(string name,out string type)
        {
            var index = _indexList.Find((s) => s.Name == name);
            if(index!=null)
            {
                type = index.ValueType;
                return true;
            }
            else
            {
                type = null;
                return false;
                
            }
        }
        #endregion
    }
    public class MappingIndex
    {
        string _name;
        string _valueType;
        public MappingIndex(string name,string valueType)
        {
            _name = name;
            _valueType = valueType;
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }
        public string ValueType
        {
            get { return _valueType; }
            set { _valueType = value; }
        }
    }
}
