﻿namespace Girvs.Infrastructure;

/// <summary>
/// 提供对Girvs引擎的单例实例的访问
/// </summary>
public class EngineContext
{
    /// <summary>
    /// 创建Girvs引擎的静态实例
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public static IEngine Create()
    {
        return Singleton<IEngine>.Instance ?? (Singleton<IEngine>.Instance = new GirvsEngine());
    }

    /// <summary>
    /// 将静态引擎实例设置为提供的引擎。使用此方法提供您自己的引擎实现。
    /// </summary>
    /// <param name="engine">要使用的引擎。</param>
    public static void Replace(IEngine engine)
    {
        Singleton<IEngine>.Instance = engine;
    }
        

    /// <summary>
    /// 获取用于访问服务的单例引擎。
    /// </summary>
    public static IEngine Current
    {
        get
        {
            if (Singleton<IEngine>.Instance == null)
            {
                Create();
            }

            return Singleton<IEngine>.Instance;
        }
    }
}