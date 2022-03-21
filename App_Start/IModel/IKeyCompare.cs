using System.Collections.Generic;

namespace ASM_API.App_Start.TableModel
{
    public interface IKeyCompare
    {
        object GetKey();
        string DisplayNameKey();
    }
    public class IModelCompare<T> : IEqualityComparer<T> where T : IKeyCompare
    {
        public bool Equals(T x, T y)
        {
            return object.Equals(x.GetKey(), y.GetKey());
        }

        public int GetHashCode(T obj)
        {
            return obj.GetKey().GetHashCode();
        }
    }
}