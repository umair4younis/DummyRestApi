namespace Puma.MDE.Common.Utilities
{
    public static class ObjectUtility
    {
        public static T Clone<T>(T instance)
        {
            T dest;

            using (var stream = new System.IO.MemoryStream())
            {
                var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                formatter.Serialize(stream, instance);
                stream.Position = 0;
                dest = (T)formatter.Deserialize(stream);

            }

            return dest;
        }

    }
}
