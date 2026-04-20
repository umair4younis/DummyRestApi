using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Puma.MDE.Common.Configuration
{
    [ComVisible(true)]
    [Guid("073F0AE4-33C7-4b07-8E77-18D42EC51D02")]
    public interface IConfig
    {
        string Get(string key);
        int Count();
        string Item(int i);
        string Key(int i);
        bool IsDefined(string key);
        void Set(string key, string value);
        List<string> GetCollection(string prefix);
    }
}
