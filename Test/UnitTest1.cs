using Microsoft.VisualStudio.TestTools.UnitTesting;
using Medoz.Docker;

namespace Medoz.Docker.Test;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void TestMethod1()
    {
        var t = Process.HasDocker();
        bool b = t.Result;
        Assert.IsTrue(b);
    }
}