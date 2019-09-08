using Net.Client;
using QF;

/// <summary>
/// 客户端单例类
/// </summary>
/// <typeparam name="T"></typeparam>
public class NetClientMonoSingleton<T> : NetBehaviour, ISingleton where T : NetClientMonoSingleton<T>
{ 
    protected static T mInstance = null;

    public static T Instance
    {
        get
        {
            if (mInstance == null)
            {
                mInstance = MonoSingletonCreator.CreateMonoSingleton<T>();
            }

            return mInstance;
        }
    }

    public virtual void OnSingletonInit()
    {
    }
    public virtual void Dispose()
    {
        if (MonoSingletonCreator.IsUnitTestMode)
        {
            var curTrans = transform;
            do
            {
                var parent = curTrans.parent;
                DestroyImmediate(curTrans.gameObject);
                curTrans = parent;
            } while (curTrans != null);

            mInstance = null;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        mInstance = null;
    }
}
