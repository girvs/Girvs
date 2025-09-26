using Girvs.AntiJump;
using Microsoft.AspNetCore.Mvc;
using Panda.DynamicWebApi.Attributes;

namespace Example.WebApi.Controllers;

/// <summary>
/// 考生端
/// </summary>
[DynamicWebApi]
[AntiJumpActionFilter]
public class TestService : ITestService
{
    // [HttpGet]
    // [AntiJump(GenerateKey = "test1", AntiJumpLogic = AntiJumpLogic.Verify)]
    //
    // public string GetMyClass(string key)
    // {
    //     return key;
    // }


    [HttpGet("{key}")]
    [AntiJump(GenerateKey = "test1", AntiJumpLogic = AntiJumpLogic.Generate)]
    public dynamic GetOne(string key)
    {
        return key;
    }

    [HttpGet("{key}")]
    [AntiJump(GenerateKey = "test1", AntiJumpLogic = AntiJumpLogic.Generate)]
    public dynamic GetOne1(string key,string value)
    {
        return key;
    }


}



public class MyClass
{
    public string Name { get; set; }
    public int Age { get; set; }
}