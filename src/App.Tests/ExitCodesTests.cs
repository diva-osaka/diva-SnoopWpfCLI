using System.Collections.Generic;
using SnoopWpfCLI;
using Xunit;

namespace SnoopWpfCLI.Tests;

public class ExitCodesTests
{
    [Fact]
    public void Success_Is0()
    {
        Assert.Equal(0, ExitCodes.Success);
    }

    [Fact]
    public void GeneralError_Is1()
    {
        Assert.Equal(1, ExitCodes.GeneralError);
    }

    [Fact]
    public void ProcessNotFound_Is2()
    {
        Assert.Equal(2, ExitCodes.ProcessNotFound);
    }

    [Fact]
    public void InjectionFailed_Is3()
    {
        Assert.Equal(3, ExitCodes.InjectionFailed);
    }

    [Fact]
    public void Timeout_Is4()
    {
        Assert.Equal(4, ExitCodes.Timeout);
    }

    [Fact]
    public void AllCodesAreUnique()
    {
        var codes = new[]
        {
            ExitCodes.Success,
            ExitCodes.GeneralError,
            ExitCodes.ProcessNotFound,
            ExitCodes.InjectionFailed,
            ExitCodes.Timeout
        };

        Assert.Equal(codes.Length, new HashSet<int>(codes).Count);
    }
}
