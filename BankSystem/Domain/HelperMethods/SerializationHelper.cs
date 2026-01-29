using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Domain
{
    public static class SerializationHelper
    {
        public static byte[] Serialize(object obj)
        {
            try
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
            catch
            {
                throw new Exception("Serialization failed");
            }
        }
        public static object Deserialize(byte[] data)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    object obj = bf.Deserialize(ms);
                    return obj;
                }
            }
            catch
            {
                throw new Exception("Serialization failed");
            }
        }
    }
}
