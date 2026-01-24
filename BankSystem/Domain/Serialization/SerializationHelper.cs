using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Domain.Serialization
{
    public static class SerializationHelper
    {
        public static byte[] Serialize<T>(T obj)
        {
            byte[] dataBuffer;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, obj);
                dataBuffer = ms.ToArray();
                return dataBuffer;
            }
        }
        public static T Deserialize<T>(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryFormatter bf = new BinaryFormatter();
                object obj = bf.Deserialize(ms);
                return (T)obj;
            }
        }
    }
}
