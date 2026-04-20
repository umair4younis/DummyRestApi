namespace Puma.MDE.Data
{
   public class UniConfig
   {
      public int Domaintype {get;set;}
      public string Cwd {get;set;} 
      public string Osuser {get; set;}
      public string Hostname {get;set;}
      public string Process {get;set;}
      public string Category {get; set;}
      public string Key {get;set;}
      public string Value {get;set;}
      public string Comm {get;set;}

      public bool Equals(Data.UniConfig other)
      {
         if (ReferenceEquals(null, other)) return false;
         if (ReferenceEquals(this, other)) return true;
         return Equals(other.Category, Category) && Equals(other.Key, Key);
      }

      public override bool Equals(object obj)
      {
         if (ReferenceEquals(null, obj)) return false;
         if (ReferenceEquals(this, obj)) return true;
         if (obj.GetType() != typeof(Data.UniConfig)) return false;
         return Equals((Data.UniConfig)obj);
      }

      public override int GetHashCode()
      {
         unchecked
         {
            int result = ((Category != null ? Category.GetHashCode() : 0) * 397) ^ (Key != null ? Key.GetHashCode() : 0);
            return result;
         }
      }
   }
}
