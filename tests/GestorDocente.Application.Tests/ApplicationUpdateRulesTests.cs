using GestorDocente.Application;
using Xunit;

namespace GestorDocente.Application.Tests;

public class ApplicationUpdateRulesTests
{
    [Theory]
    [InlineData("1.5.0","1.6.0",true)]
    [InlineData("1.6.0","1.6.0",false)]
    [InlineData("1.6.0","1.5.9",false)]
    [InlineData("1.6.0","1.7.0-beta",false)]
    [InlineData("1.6.0","unknown",false)]
    [InlineData("1.6.0","2.0.0",true)]
    public void Only_newer_stable_versions_are_accepted(string current,string candidate,bool expected)=>
        Assert.Equal(expected,ApplicationUpdateRules.IsNewStableVersion(current,candidate));

    [Theory]
    [InlineData("win","x64","win-x64")]
    [InlineData("osx","x64","osx-x64")]
    [InlineData("osx","arm64","osx-arm64")]
    [InlineData("linux","x64","linux-x64")]
    [InlineData("linux","arm64","linux-arm64")]
    public void Release_channels_keep_platforms_and_architectures_separate(string os,string architecture,string expected)=>
        Assert.Equal(expected,ApplicationUpdateRules.Channel(os,architecture));

    [Fact]
    public void Unsupported_architecture_cannot_fall_back_to_another_platform()
    {
        Assert.Throws<NotSupportedException>(()=>ApplicationUpdateRules.Channel("linux","x86"));
        Assert.Throws<NotSupportedException>(()=>ApplicationUpdateRules.Channel("win","arm64"));
        Assert.Equal("https://github.com/danielpose1996-stack/SIGNA",ApplicationUpdateRules.Repository);
    }
}
