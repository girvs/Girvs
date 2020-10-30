using Panda.DynamicWebApi;
using Panda.DynamicWebApi.Attributes;

namespace Test.DynamicWebApi
{
    [DynamicWebApi]
    public class TestService : IDynamicWebApi
    {
        public string GetByName()
        {
            return "hello world!";
        }
    }
}