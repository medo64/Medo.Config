namespace Tests;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Medo;
using System.IO;

[TestClass]
public class ConfigFileSource_Tests {

    [TestMethod]
    public void ConfigFileSource_Basic() {
        using var tempFile = new TempFile();
        var source = new ConfigFileSource(tempFile.FileName);
        source.Write("Test", 1);
        var lines = tempFile.GetLines();
        Assert.AreEqual(2, lines.Length);
        Assert.AreEqual("Test: 1", lines[0]);
        Assert.AreEqual("", lines[1]);
    }

    [TestMethod]
    public void ConfigFileSource_PreserveComments() {
        using var tempFile = new TempFile();
        File.WriteAllLines(tempFile.FileName, ["# This is a comment", "Test: 1"]);

        var source = new ConfigFileSource(tempFile.FileName);
        source.Write("Test", 2);
        var lines = tempFile.GetLines();
        Assert.AreEqual(3, lines.Length);
        Assert.AreEqual("# This is a comment", lines[0]);
        Assert.AreEqual("Test: 2", lines[1]);
        Assert.AreEqual("", lines[2]);
    }

    [TestMethod]
    public void ConfigFileSource_EscapeNul() {
        using var tempFile = new TempFile();

        var source = new ConfigFileSource(tempFile.FileName);
        source.Write("Test", "A\0B");

        var lines = tempFile.GetLines();
        Assert.AreEqual(2, lines.Length);
        Assert.AreEqual(@"Test: A\0B", lines[0]);
        Assert.AreEqual("", lines[1]);
    }

    [TestMethod]
    public void ConfigFileSource_EscapeCrLf() {
        using var tempFile = new TempFile();
        var source = new ConfigFileSource(tempFile.FileName);
        source.Write("Test", " A\r\nB C ");

        var lines = tempFile.GetLines();
        Assert.AreEqual(2, lines.Length);
        Assert.AreEqual(@"Test: \_A\r\nB C\_", lines[0]);
        Assert.AreEqual("", lines[1]);
    }

    [TestMethod]
    public void ConfigFileSource_NullKey() {
        using var tempFile = new TempFile();
        var source = new ConfigFileSource(tempFile.FileName);

        var ex = Assert.Throws<ArgumentNullException>(() => {
            source.Read(null, "");
        });
    }

    [TestMethod]
    public void ConfigFileSource_EmptyKey() {
        using var tempFile = new TempFile();
        var source = new ConfigFileSource(tempFile.FileName);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => {
            source.Read("", "");
        });
    }

    [TestMethod]
    public void ConfigFileSource_WhitespaceKey() {
        using var tempFile = new TempFile();
        var source = new ConfigFileSource(tempFile.FileName);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => {
            source.Read("   ", "");
        });
    }

    [TestMethod]
    public void ConfigFileSource_EmptySave() {
        using var tempFile = new TempFile();
        var source = new ConfigFileSource(tempFile.FileName);

        Assert.IsFalse(tempFile.Exists());
        source.Save();
        Assert.IsTrue(tempFile.Exists());
        Assert.AreEqual(0, tempFile.Length);
    }

}
