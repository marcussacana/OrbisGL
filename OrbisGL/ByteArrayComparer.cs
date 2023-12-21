using System;
using System.Collections.Generic;
using System.Linq;

namespace OrbisGL
{
    public class ByteArrayComparer : EqualityComparer<byte[]>
    {
        public override bool Equals(byte[] first, byte[] second)
        {
            if (first == null || second == null)
            {
                return first == second;
            }
            if (ReferenceEquals(first, second))
            {
                return true;
            }
            if (first.Length != second.Length)
            {
                return false;
            }

            return first.SequenceEqual(second);
        }
        public unsafe override int GetHashCode(byte[] obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            var len = obj.Length;

            if (len >= 4)
            {
                fixed (void* pObj = obj)
                    return unchecked(obj.Length ^ ((int*)pObj)[0]);
            }
            return len;
        }
    }
}
