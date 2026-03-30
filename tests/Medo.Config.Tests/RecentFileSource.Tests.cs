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



    [TestMethod]
    public void RecentFileSource_MaximumCount() {
        using var tempFile = new TempFile();
        var source = new RecentFileSource(tempFile.FileName);

        Assert.Throws<ArgumentOutOfRangeException>(() => {
            source.MaxCount = 0;
        });
        Assert.Throws<ArgumentOutOfRangeException>(() => {
            source.MaxCount = 101;
        });
    }

    [TestMethod]
    public void RecentFileSource_AddNull() {
        using var tempFile = new TempFile();
        var source = new RecentFileSource(tempFile.FileName);

        Assert.Throws<ArgumentNullException>(() => {
            source.Files.Add(null);
        });
    }

    [TestMethod]
    public void RecentFileSource_RemoveNull() {
        using var tempFile = new TempFile();
        var source = new RecentFileSource(tempFile.FileName);
        Assert.Throws<ArgumentNullException>(() => {
            source.Files.Remove(null);
        });
    }


    [TestMethod]
    public void RecentFileSource_Limit() {
        var file1 = new FileInfo("test1.txt");
        var file2 = new FileInfo("test2.txt");
        var file3 = new FileInfo("test3.txt");
        var file4 = new FileInfo("test4.txt");
        var file5 = new FileInfo("test5.txt");

        using var tempFile = new TempFile();
        var source = new RecentFileSource(tempFile.FileName);

        source.MaxCount = 3;
        source.Files.Add(file1);
        source.Files.Add(file2);
        source.Files.Add(file3);
        source.Files.Add(file4);
        source.Files.Add(file5);

        var files = source.Files;
        Assert.AreEqual(3, files.Count);
        Assert.AreEqual(file5.FullName, files[0].FullName);
        Assert.AreEqual(file4.FullName, files[1].FullName);
        Assert.AreEqual(file3.FullName, files[2].FullName);
    }

    [TestMethod]
    public void RecentFileSource_IgnoreSame() {
        var file1 = new FileInfo("test1.txt");
        var file2 = new FileInfo("test2.txt");
        var file3 = new FileInfo("test3.txt");

        using var tempFile = new TempFile();
        var source = new RecentFileSource(tempFile.FileName);

        source.MaxCount = 3;
        source.Files.Add(file1);
        source.Files.Add(file2);
        source.Files.Add(file3);
        source.Files.Add(file2);
        source.Files.Add(file2);

        var files = source.Files;
        Assert.AreEqual(3, files.Count);
        Assert.AreEqual(file2.FullName, files[0].FullName);
        Assert.AreEqual(file3.FullName, files[1].FullName);
        Assert.AreEqual(file1.FullName, files[2].FullName);
    }

    [TestMethod]
    public void RecentFileSource_Clear() {
        var file1 = new FileInfo("test1.txt");
        var file2 = new FileInfo("test2.txt");
        var file3 = new FileInfo("test3.txt");

        using var tempFile = new TempFile();
        var source = new RecentFileSource(tempFile.FileName);

        source.MaxCount = 3;
        source.Files.Add(file1);
        source.Files.Add(file2);
        source.Files.Add(file3);
        source.Files.Clear();

        var files = source.Files;
        Assert.AreEqual(0, files.Count);
    }

    [TestMethod]
    public void RecentFileSource_Remove2() {
        var file1 = new FileInfo("test1.txt");
        var file2 = new FileInfo("test2.txt");
        var file3 = new FileInfo("test3.txt");

        using var tempFile = new TempFile();
        var source = new RecentFileSource(tempFile.FileName);

        source.MaxCount = 3;
        source.Files.Add(file1);
        source.Files.Add(file2);
        source.Files.Add(file3);
        source.Files.Remove(file2);

        var files = source.Files;
        Assert.AreEqual(2, files.Count);
        Assert.AreEqual(file3.FullName, files[0].FullName);
        Assert.AreEqual(file1.FullName, files[1].FullName);
    }

    [TestMethod]
    public void RecentFileSource_EscapeControl1() {
        var file1 = new FileInfo("test1\b.txt");
        var file2 = new FileInfo("test2\t.txt");
        var file3 = new FileInfo("test3\r.txt");
        var file4 = new FileInfo("test4\n.txt");

        using var tempFile = new TempFile();
        var source = new RecentFileSource(tempFile.FileName);

        source.MaxCount = 4;
        source.Files.Add(file4);
        source.Files.Add(file3);
        source.Files.Add(file2);
        source.Files.Add(file1);

        var files = source.Files;
        Assert.AreEqual(4, files.Count);
        Assert.AreEqual(file1.FullName, files[0].FullName);
        Assert.AreEqual(file2.FullName, files[1].FullName);
        Assert.AreEqual(file3.FullName, files[2].FullName);
        Assert.AreEqual(file4.FullName, files[3].FullName);
    }

    [TestMethod]
    public void RecentFileSource_EscapeControl2() {
        var file1 = new FileInfo("test1\u0005.txt");

        using var tempFile = new TempFile();
        var source = new RecentFileSource(tempFile.FileName);

        source.Files.Add(file1);

        var files = source.Files;
        Assert.AreEqual(1, files.Count);
        Assert.AreEqual(file1.FullName, files[0].FullName);
    }

}
