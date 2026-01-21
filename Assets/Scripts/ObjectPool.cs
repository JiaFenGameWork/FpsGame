using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用对象池，用于管理可复用的游戏对象
/// </summary>
/// <typeparam name="T">组件类型，必须继承自 Component</typeparam>
public class ObjectPool<T> where T : Component
{
    private readonly T _prefab;
    private readonly Queue<T> _pool;
    private readonly Transform _parent;
    
    // 获取和归还时的回调
    private readonly Action<T> _onGet;
    private readonly Action<T> _onReturn;
    
    /// <summary>
    /// 当前池中可用对象数量
    /// </summary>
    public int CountInactive => _pool.Count;
    
    /// <summary>
    /// 创建对象池
    /// </summary>
    /// <param name="prefab">预制体</param>
    /// <param name="initialSize">初始池大小</param>
    /// <param name="parent">对象的父级Transform（可选）</param>
    /// <param name="onGet">获取对象时的回调（可选）</param>
    /// <param name="onReturn">归还对象时的回调（可选）</param>
    public ObjectPool(T prefab, int initialSize = 10, Transform parent = null, 
        Action<T> onGet = null, Action<T> onReturn = null)
    {
        _prefab = prefab;
        _parent = parent;
        _pool = new Queue<T>(initialSize);
        _onGet = onGet;
        _onReturn = onReturn;
        
        // 预热：预先创建对象
        Prewarm(initialSize);
    }
    
    /// <summary>
    /// 预热对象池，创建指定数量的对象
    /// </summary>
    public void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            T obj = CreateNewObject();
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
        }
    }
    
    /// <summary>
    /// 从对象池获取一个对象
    /// </summary>
    public T Get()
    {
        T obj = null;
        
        // 从池中取出对象，跳过已被销毁的对象
        while (_pool.Count > 0)
        {
            obj = _pool.Dequeue();
            // Unity重载了==操作符，已销毁对象比较时会返回true
            if (obj != null)
                break;
            obj = null;
        }
        
        // 池为空或全部对象都已销毁时，创建新对象
        if (obj == null)
        {
            obj = CreateNewObject();
        }
        
        obj.gameObject.SetActive(true);
        _onGet?.Invoke(obj);
        
        return obj;
    }
    
    /// <summary>
    /// 从对象池获取对象，并设置位置和旋转
    /// </summary>
    public T Get(Vector3 position, Quaternion rotation)
    {
        T obj = Get();
        obj.transform.SetPositionAndRotation(position, rotation);
        return obj;
    }
    
    /// <summary>
    /// 将对象归还到对象池
    /// </summary>
    public void Return(T obj)
    {
        if (obj == null) return;
        
        _onReturn?.Invoke(obj);
        obj.gameObject.SetActive(false);
        _pool.Enqueue(obj);
    }
    
    /// <summary>
    /// 清空对象池并销毁所有对象
    /// </summary>
    public void Clear()
    {
        while (_pool.Count > 0)
        {
            T obj = _pool.Dequeue();
            if (obj != null)
            {
                UnityEngine.Object.Destroy(obj.gameObject);
            }
        }
    }
    
    private T CreateNewObject()
    {
        T obj = UnityEngine.Object.Instantiate(_prefab, _parent);
        return obj;
    }
}

/// <summary>
/// GameObject 专用对象池（不需要指定组件类型时使用）
/// </summary>
public class GameObjectPool
{
    private readonly GameObject _prefab;
    private readonly Queue<GameObject> _pool;
    private readonly Transform _parent;
    
    private readonly Action<GameObject> _onGet;
    private readonly Action<GameObject> _onReturn;
    
    public int CountInactive => _pool.Count;
    
    public GameObjectPool(GameObject prefab, int initialSize = 10, Transform parent = null,
        Action<GameObject> onGet = null, Action<GameObject> onReturn = null)
    {
        _prefab = prefab;
        _parent = parent;
        _pool = new Queue<GameObject>(initialSize);
        _onGet = onGet;
        _onReturn = onReturn;
        
        Prewarm(initialSize);
    }
    
    public void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject obj = UnityEngine.Object.Instantiate(_prefab, _parent);
            obj.SetActive(false);
            _pool.Enqueue(obj);
        }
    }
    
    public GameObject Get()
    {
        GameObject obj = null;
        
        // 从池中取出对象，跳过已被销毁的对象
        while (_pool.Count > 0)
        {
            obj = _pool.Dequeue();
            // Unity重载了==操作符，已销毁对象比较时会返回true
            if (obj != null)
                break;
            obj = null;
        }
        
        // 池为空或全部对象都已销毁时，创建新对象
        if (obj == null)
        {
            obj = UnityEngine.Object.Instantiate(_prefab, _parent);
        }
        
        obj.SetActive(true);
        _onGet?.Invoke(obj);
        
        return obj;
    }
    
    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject obj = Get();
        obj.transform.SetPositionAndRotation(position, rotation);
        return obj;
    }
    
    public void Return(GameObject obj)
    {
        if (obj == null) return;
        
        _onReturn?.Invoke(obj);
        obj.SetActive(false);
        _pool.Enqueue(obj);
    }
    
    public void Clear()
    {
        while (_pool.Count > 0)
        {
            GameObject obj = _pool.Dequeue();
            if (obj != null)
            {
                UnityEngine.Object.Destroy(obj);
            }
        }
    }
}
