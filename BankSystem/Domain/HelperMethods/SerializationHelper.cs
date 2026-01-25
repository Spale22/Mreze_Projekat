using Domain.Enumerations;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Domain.HelperMethods
{
    public static class SerializationHelper
    {
        public static byte[] Serialize<T>(PackageType pkgType , T obj)
        {
            byte[] dataBuffer;
            using (MemoryStream ms = new MemoryStream())
            {
                ms.WriteByte((byte)pkgType);
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, obj);
                dataBuffer = ms.ToArray();
                return dataBuffer;
            }
        }
        public static (PackageType, T) Deserialize<T>(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                var pkgType = (PackageType)ms.ReadByte();
                BinaryFormatter bf = new BinaryFormatter();
                object obj = bf.Deserialize(ms);
                return (pkgType, (T)obj);
            }
        }
    }
}
