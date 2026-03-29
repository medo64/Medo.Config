namespace Tests;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Medo;
using System.IO;

[TestClass]
public class RecentDummySource_Tests {

    [TestMethod]
    public void RecentDummySource_Empty() {
        var source = new RecentDummySource();
        var files = source.Files;
        Assert.AreEqual(0, files.Count);
    }

    [TestMethod]
    public void RecentDummySource_One() {
        var source = new RecentDummySource();
        source.Files.Add(new FileInfo("test.txt"));
        var files = source.Files;
        Assert.AreEqual(1, files.Count);
        Assert.AreEqual("test.txt", files[0].Name);
    }

    [TestMethod]
    public void RecentDummySource_Two() {
        var source = new RecentDummySource();
        source.Files.Add(new FileInfo("test1.txt"));
        source.Files.Add(new FileInfo("test2.txt"));
        var files = source.Files;
        Assert.AreEqual(2, files.Count);
        Assert.AreEqual("test2.txt", files[0].Name);
        Assert.AreEqual("test1.txt", files[1].Name);
    }

    [TestMethod]
    public void RecentDummySource_TwoLimited() {
        var source = new RecentDummySource() { MaxCount = 2 };
        source.Files.Add(new FileInfo("test1.txt"));
        source.Files.Add(new FileInfo("test2.txt"));
        source.Files.Add(new FileInfo("test3.txt"));
        var files = source.Files;
        Assert.AreEqual(2, files.Count);
        Assert.AreEqual("test3.txt", files[0].Name);
        Assert.AreEqual("test2.txt", files[1].Name);
    }

    [TestMethod]
    public void RecentDummySource_Remove() {
        var source = new RecentDummySource();
        source.Files.Add(new FileInfo("test1.txt"));
        source.Files.Add(new FileInfo("test2.txt"));
        source.Files.Add(new FileInfo("test3.txt"));
        source.Files.Remove(new FileInfo("test3.txt"));
        var files = source.Files;
        Assert.AreEqual(2, files.Count);
        Assert.AreEqual("test2.txt", files[0].Name);
        Assert.AreEqual("test1.txt", files[1].Name);
    }

}
