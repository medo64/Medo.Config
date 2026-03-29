namespace Tests;

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Medo;

[TestClass]
public class RecentFileSource_Tests {

    [TestMethod]
    public void RecentFileSource_Empty() {
        using var tempFile = new TempFile();
        var source = new RecentFileSource(tempFile.FileName);

        var files = source.Files;
        Assert.AreEqual(0, files.Count);

        Assert.IsFalse(File.Exists(tempFile.FileName));  // since there is no write, no file is expected
    }

    [TestMethod]
    public void RecentFileSource_One() {
        using var tempFile = new TempFile();
        var source = new RecentFileSource(tempFile.FileName);
        source.Files.Add(new FileInfo("test.txt"));

        var files = source.Files;
        Assert.AreEqual(1, files.Count);
        Assert.AreEqual("test.txt", files[0].Name);

        var lines = tempFile.GetLines();
        Assert.AreEqual(2, lines.Length);
        Assert.IsTrue(lines[0].EndsWith("test.txt"));
        Assert.AreEqual("", lines[1]);
    }

    [TestMethod]
    public void RecentFileSource_Two() {
        using var tempFile = new TempFile();
        var source = new RecentFileSource(tempFile.FileName);
        source.Files.Add(new FileInfo("test1.txt"));
        source.Files.Add(new FileInfo("test2.txt"));

        var files = source.Files;
        Assert.AreEqual(2, files.Count);
        Assert.AreEqual("test2.txt", files[0].Name);
        Assert.AreEqual("test1.txt", files[1].Name);

        var lines = tempFile.GetLines();
        Assert.AreEqual(3, lines.Length);
        Assert.IsTrue(lines[0].EndsWith("test2.txt"));
        Assert.IsTrue(lines[1].EndsWith("test1.txt"));
        Assert.AreEqual("", lines[2]);
    }

    [TestMethod]
    public void RecentFileSource_TwoLimited() {
        using var tempFile = new TempFile();
        var source = new RecentFileSource(tempFile.FileName) { MaxCount = 2 };
        source.Files.Add(new FileInfo("test1.txt"));
        source.Files.Add(new FileInfo("test2.txt"));
        source.Files.Add(new FileInfo("test3.txt"));

        var files = source.Files;
        Assert.AreEqual(2, files.Count);
        Assert.AreEqual("test3.txt", files[0].Name);
        Assert.AreEqual("test2.txt", files[1].Name);

        var lines = tempFile.GetLines();
        Assert.AreEqual(3, lines.Length);
        Assert.IsTrue(lines[0].EndsWith("test3.txt"));
        Assert.IsTrue(lines[1].EndsWith("test2.txt"));
        Assert.AreEqual("", lines[2]);
    }

    [TestMethod]
    public void RecentFileSource_Remove() {
        using var tempFile = new TempFile();
        var source = new RecentFileSource(tempFile.FileName);
        source.Files.Add(new FileInfo("test1.txt"));
        source.Files.Add(new FileInfo("test2.txt"));
        source.Files.Add(new FileInfo("test3.txt"));
        source.Files.Remove(new FileInfo("test2.txt"));

        var files = source.Files;
        Assert.AreEqual(2, files.Count);
        Assert.AreEqual("test3.txt", files[0].Name);
        Assert.AreEqual("test1.txt", files[1].Name);

        var lines = tempFile.GetLines();
        Assert.AreEqual(3, lines.Length);
        Assert.IsTrue(lines[0].EndsWith("test3.txt"));
        Assert.IsTrue(lines[1].EndsWith("test1.txt"));
        Assert.AreEqual("", lines[2]);
    }

}
