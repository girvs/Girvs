using Microsoft.AspNetCore.Mvc;
using Panda.DynamicWebApi.Attributes;

namespace Example.WebApi.Controllers;

/// <summary>
/// 考生端
/// </summary>
[DynamicWebApi]
public class TestService : ITestService
{
    [HttpGet]
    public MyClass GetMyClass(MyClass key)
    {
        return key;
    }


    [HttpGet]
    public dynamic GetOne(string key)
    {
        return key;
    }

    [HttpPut]
    public dynamic PutOne(string key)
    {
        return key;
    }


    [HttpDelete]
    public MyClass DeleteMyClass(MyClass key)
    {
        return key;
    }



}



public class MyClass
{
    public string Name { get; set; }
    public int Age { get; set; }
}