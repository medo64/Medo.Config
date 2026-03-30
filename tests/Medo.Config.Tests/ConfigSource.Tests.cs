namespace Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Medo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ConfigSource_Tests {

    [TestMethod]
    public void ConfigSource_EmptyLinesCRLF() {
        lock (SingleTestSync) {
            using var loader = new ConfigLoader("EmptyLinesCRLF.conf.raw");

            Config.User.Save();

            Assert.AreEqual(BitConverter.ToString(loader.Bytes), BitConverter.ToString(File.ReadAllBytes(loader.FileName)));
        }
    }

    [TestMethod]
    public void ConfigSource_EmptyLinesLF() {
        lock (SingleTestSync) {
            using var loader = new ConfigLoader("EmptyLinesLF.conf.raw");

            Config.User.Save();

            Assert.AreEqual(BitConverter.ToString(loader.Bytes), BitConverter.ToString(File.ReadAllBytes(loader.FileName)));
        }
    }

    [TestMethod]
    public void ConfigSource_EmptyLinesCR() {
        lock (SingleTestSync) {
            using var loader = new ConfigLoader("EmptyLinesCR.conf.raw");

            Config.User.Save();

            Assert.AreEqual(BitConverter.ToString(loader.Bytes), BitConverter.ToString(File.ReadAllBytes(loader.FileName)));
        }
    }

    [TestMethod]
    public void ConfigSource_EmptyLinesMixed() {
        lock (SingleTestSync) {
            using var loader = new ConfigLoader("EmptyLinesMixed.conf.raw", "EmptyLinesMixed.Good.conf.raw");

            Config.User.Save();

            Assert.AreEqual(loader.GoodText, File.ReadAllText(loader.FileName));
        }
    }

    [TestMethod]
    public void ConfigSource_CommentsOnly() {
        lock (SingleTestSync) {
            using var loader = new ConfigLoader("CommentsOnly.conf.raw", "CommentsOnly.Good.conf.raw");

            Config.User.Save();

            Assert.AreEqual(BitConverter.ToString(loader.GoodBytes), BitConverter.ToString(File.ReadAllBytes(loader.FileName)));
        }
    }

    [TestMethod]
    public void ConfigSource_CommentsWithValues() {
        lock (SingleTestSync) {
            using var loader = new ConfigLoader("CommentsWithValues.conf.raw");

            Config.User.Save();

            Assert.AreEqual(Encoding.UTF8.GetString(loader.Bytes), Encoding.UTF8.GetString(File.ReadAllBytes(loader.FileName)));
            Assert.AreEqual(BitConverter.ToString(loader.Bytes), BitConverter.ToString(File.ReadAllBytes(loader.FileName)));
        }
    }

    [TestMethod]
    public void ConfigSource_SpacingEscape() {
        lock (SingleTestSync) {
            using var loader = new ConfigLoader("SpacingEscape.conf.raw", "SpacingEscape.Good.conf.raw");
            Assert.AreEqual(" Value 1", Config.Read("Key1", ""));
            Assert.AreEqual("Value 2 ", Config.Read("Key2", ""));
            Assert.AreEqual(" Value 3 ", Config.Read("Key3", ""));
            Assert.AreEqual("  Value 4  ", Config.Read("Key4", ""));
            Assert.AreEqual("\tValue 5\t", Config.Read("Key5", ""));
            Assert.AreEqual("\tValue 6", Config.Read("Key6", ""));
            Assert.AreEqual("\0", Config.Read("Null", ""));

            Config.Write("Null", "\0Null\0");

            Assert.AreEqual(loader.GoodText, File.ReadAllText(loader.FileName));
        }
    }

    [TestMethod]
    public void ConfigSource_WriteBasic() {
        lock (SingleTestSync) {
            using var loader = new ConfigLoader("Empty.conf.raw", "WriteBasic.Good.conf.raw");
            Config.Write("Key1", "Value 1");
            Config.Write("Key2", "Value 2");

            Assert.AreEqual(loader.GoodText.ReplaceLineEndings(), File.ReadAllText(loader.FileName).ReplaceLineEndings());
        }
    }

    [TestMethod]
    public void ConfigSource_WriteNoEmptyLine() {
        lock (SingleTestSync) {
            using var loader = new ConfigLoader("WriteNoEmptyLine.conf.raw", "WriteNoEmptyLine.Good.conf.raw");
            Config.Write("Key1", "Value 1");
            Config.Write("Key2", "Value 2");

            Assert.AreEqual(loader.GoodText.ReplaceLineEndings(), File.ReadAllText(loader.FileName).ReplaceLineEndings());
        }
    }

    [TestMethod]
    public void ConfigSource_WriteSameSeparatorEquals() {
        lock (SingleTestSync) {
            using var loader = new ConfigLoader("WriteSameSeparatorEquals.conf.raw", "WriteSameSeparatorEquals.Good.conf.raw");
            Config.Write("Key1", "Value 1");
            Config.Write("Key2", "Value 2");

            Assert.AreEqual(loader.GoodText, File.ReadAllText(loader.FileName));
        }
    }

    [TestMethod]
    public void ConfigSource_WriteSameSeparatorSpace() {
        lock (SingleTestSync) {
            using var loader = new ConfigLoader("WriteSameSeparatorSpace.conf.raw", "WriteSameSeparatorSpace.Good.conf.raw");
            Config.Write("Key1", "Value 1");
            Config.Write("Key2", "Value 2");

            Assert.AreEqual(loader.GoodText, File.ReadAllText(loader.FileName));
        }
    }

    [TestMethod]
    public void ConfigSource_Replace() {
        lock (SingleTestSync) {
            using var loader = new ConfigLoader("Replace.conf.raw", "Replace.Good.conf.raw");
            Config.Write("Key1", "Value 1a");
            Config.Write("Key2", "Value 2a");

            Assert.AreEqual("Value 1a", Config.Read("Key1", ""));
            Assert.AreEqual("Value 2a", Config.Read("Key2", ""));

            Assert.AreEqual(loader.GoodText, File.ReadAllText(loader.FileName));
        }
    }

    [TestMethod]
    public void ConfigSource_SpacingPreservedOnAdd() {
        lock (SingleTestSync) {
            using var loader = new ConfigLoader("SpacingPreservedOnAdd.conf.raw", "SpacingPreservedOnAdd.Good.conf.raw");
            Config.Write("One", "Value 1a");
            Config.User.WriteMany("Two", new string[] { "Value 2a", "Value 2b" });
            Config.Write("Three", "Value 3a");
            Config.Write("Four", "Value 4a");
            Config.User.WriteMany("Five", new string[] { "Value 5a", "Value 5b", "Value 5c" });
            Config.Write("FourtyTwo", 42);

            Assert.AreEqual(loader.GoodText, File.ReadAllText(loader.FileName));
        }
    }

    [TestMethod]
    public void ConfigSource_WriteToEmpty() {
        lock (SingleTestSync) {
            using var loader = new ConfigLoader(null, "Replace.Good.conf.raw");
            Config.Write("Key1", "Value 1a");
            Config.Write("Key2", "Value 2a");

            Assert.AreEqual(loader.GoodText.ReplaceLineEndings(), File.ReadAllText(loader.FileName).ReplaceLineEndings());
        }
    }

    [TestMethod]
    public void ConfigSource_SaveInNonexistingDirectory1() {
        lock (SingleTestSync) {
            var propertiesFile = Path.Combine(Path.GetTempPath(), "ConfigDirectory", "Test.conf.raw");
            try {
                Directory.Delete(Path.Combine(Path.GetTempPath(), "ConfigDirectory"), true);
            } catch (IOException) { }

            Config.Initialize(propertiesFile, "", "", "");

            Config.User.Save();
            Assert.IsTrue(File.Exists(propertiesFile));

            Directory.Delete(Path.Combine(Path.GetTempPath(), "ConfigDirectory"), true);
        }
    }

    [TestMethod]
    public void ConfigSource_SaveInNonexistingDirectory2() {
        lock (SingleTestSync) {
            var propertiesFile = Path.Combine(Path.GetTempPath(), "ConfigDirectoryOuter", "ConfigDirectoryInner", "Test.conf.raw");
            try {
                Directory.Delete(Path.Combine(Path.GetTempPath(), "ConfigDirectoryOuter"), true);
            } catch (IOException) { }

            Config.Initialize(propertiesFile, "", "", "");

            Config.User.Save();
            Assert.IsTrue(File.Exists(propertiesFile));

            Directory.Delete(Path.Combine(Path.GetTempPath(), "ConfigDirectoryOuter"), true);
        }
    }

    [TestMethod]
    public void ConfigSource_SaveInNonexistingDirectory3() {
        lock (SingleTestSync) {
            var propertiesFile = Path.Combine(Path.GetTempPath(), "ConfigDirectoryOuter", "ConfigDirectoryMiddle", "ConfigDirectoryInner", "Test.conf.raw");
            try {
                Directory.Delete(Path.Combine(Path.GetTempPath(), "ConfigDirectoryOuter"), true);
            } catch (IOException) { }

            Config.Initialize(propertiesFile, "", "", "");

            Config.User.Save();
            Assert.IsTrue(File.Exists(propertiesFile));

            Directory.Delete(Path.Combine(Path.GetTempPath(), "ConfigDirectoryOuter"), true);
        }
    }

    [TestMethod]
    public void ConfigSource_ReadMulti() {
        lock (SingleTestSync) {
            using var loader = new ConfigLoader("RemoveMulti.conf.raw");
            var list = new List<string>(Config.User.ReadMany("Key2"));
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("Value 2a", list[0]);
            Assert.AreEqual("Value 2b", list[1]);
            Assert.AreEqual("Value 2c", list[2]);
        }
    }

    [TestMethod]
    public void ConfigSource_MultiWrite() {
        lock (SingleTestSync) {
            using var loader = new ConfigLoader(null, resourceFileNameGood: "WriteMulti.Good.conf.raw");
            Config.Write("Key1", "Value 1");
            Config.User.WriteMany("Key2", new string[] { "Value 2a", "Value 2b", "Value 2c" });
            Config.Write("Key3", "Value 3");

            Assert.AreEqual(loader.GoodText.ReplaceLineEndings(), File.ReadAllText(loader.FileName).ReplaceLineEndings());

            Assert.AreEqual("Value 1", Config.Read("Key1", ""));
            Assert.AreEqual("Value 3", Config.Read("Key3", ""));

            var list = new List<string>(Config.User.ReadMany("Key2"));
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("Value 2a", list[0]);
            Assert.AreEqual("Value 2b", list[1]);
            Assert.AreEqual("Value 2c", list[2]);
        }
    }

    [TestMethod]
    public void ConfigSource_MultiReplace() {
        lock (SingleTestSync) {
            using var loader = new ConfigLoader("WriteMulti.conf.raw", resourceFileNameGood: "WriteMulti.Good.conf.raw");
            Config.User.WriteMany("Key2", new string[] { "Value 2a", "Value 2b", "Value 2c" });
            Config.User.Save();
            Assert.AreEqual(loader.GoodText, File.ReadAllText(loader.FileName));

            Assert.AreEqual("Value 1", Config.User.Read("Key1"));
            Assert.AreEqual("Value 3", Config.User.Read("Key3"));

            var list = new List<string>(Config.User.ReadMany("Key2"));
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("Value 2a", list[0]);
            Assert.AreEqual("Value 2b", list[1]);
            Assert.AreEqual("Value 2c", list[2]);
        }
    }

    [TestMethod]
    public void ConfigSource_TestConversion() {
        lock (SingleTestSync) {
            using var loader = new ConfigLoader(null, resourceFileNameGood: "WriteConverted.Good.conf.raw");
            Config.Write("Integer", 42);
            Config.Write("Integer Min", int.MinValue);
            Config.Write("Integer Max", int.MaxValue);
            Config.Write("Long", 42L);
            Config.Write("Long Min", long.MinValue);
            Config.Write("Long Max", long.MaxValue);
            Config.Write("Boolean", true);
            Config.Write("Double", 42.42);
            Config.Write("Double Pi", System.Math.PI);
            Config.Write("Double Third", 1.0 / 3);
            Config.Write("Double Seventh", 1.0 / 7);
            Config.Write("Double Min", double.MinValue);
            Config.Write("Double Max", double.MaxValue);
            Config.Write("Double NaN", double.NaN);
            Config.Write("Double Infinity+", double.PositiveInfinity);
            Config.Write("Double Infinity-", double.NegativeInfinity);

            Assert.AreEqual(loader.GoodText.ReplaceLineEndings(), File.ReadAllText(loader.FileName).ReplaceLineEndings());

            using var loader2 = new ConfigLoader(loader.FileName, resourceFileNameGood: "WriteConverted.Good.conf.raw");
            Assert.AreEqual(42, Config.Read("Integer", 0));
            Assert.AreEqual(int.MinValue, Config.Read("Integer Min", 0));
            Assert.AreEqual(int.MaxValue, Config.Read("Integer Max", 0));
            Assert.AreEqual(42, Config.Read("Long", 0L));
            Assert.AreEqual(long.MinValue, Config.Read("Long Min", 0L));
            Assert.AreEqual(long.MaxValue, Config.Read("Long Max", 0L));
            Assert.IsTrue(Config.Read("Boolean", false));
            Assert.AreEqual(42.42, Config.Read("Double", 0.0));
            Assert.AreEqual(System.Math.PI, Config.Read("Double Pi", 0.0));
            Assert.AreEqual(1.0 / 3, Config.Read("Double Third", 0.0));
            Assert.AreEqual(1.0 / 7, Config.Read("Double Seventh", 0.0));
            Assert.AreEqual(double.MinValue, Config.Read("Double Min", 0.0));
            Assert.AreEqual(double.MaxValue, Config.Read("Double Max", 0.0));
            Assert.AreEqual(double.NaN, Config.Read("Double NaN", 0.0));
            Assert.AreEqual(double.PositiveInfinity, Config.Read("Double Infinity+", 0.0));
            Assert.AreEqual(double.NegativeInfinity, Config.Read("Double Infinity-", 0.0));
        }
    }

    [TestMethod]
    public void ConfigSource_KeyWhitespace() {
        lock (SingleTestSync) {
            using var loader = new ConfigLoader("KeyWhitespace.conf.raw", "KeyWhitespace.Good.conf.raw");
            Config.User.Save();

            Assert.AreEqual(loader.GoodText, File.ReadAllText(loader.FileName));

            Assert.AreEqual("Value 1", Config.Read("Key 1"));
            Assert.AreEqual("Value 3", Config.Read("Key 3"));

            var list = new List<string>(Config.ReadMany("Key 2"));
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("Value 2a", list[0]);
            Assert.AreEqual("Value 2b", list[1]);
            Assert.AreEqual("Value 2c", list[2]);
        }
    }


    [TestMethod]
    public void ConfigSource_DeleteAll() {
        lock (SingleTestSync) {
            using var loader = new ConfigLoader("WriteBasic.Good.conf.raw", "Empty.conf.raw");
            Assert.AreEqual("Value 1", Config.Read("Key1"));
            Assert.AreEqual("Value 2", Config.Read("Key2"));
            Config.User.Clear();

            Assert.IsNull(Config.Read("Key1"));
            Assert.IsNull(Config.Read("Key2"));
        }
    }


    #region Utils

    private readonly Lock SingleTestSync = new();

    private class ConfigLoader : IDisposable {

        public string FileName { get; }
        public byte[] Bytes { get; }
        public byte[] GoodBytes { get; }

        public ConfigLoader(string resourceFileName, string resourceFileNameGood = null) {
            if (File.Exists(resourceFileName)) {
                Bytes = File.ReadAllBytes(resourceFileName);
            } else {
                Bytes = (resourceFileName != null) ? Helper.GetResourceBytes("Config." + resourceFileName) : null;
            }
            GoodBytes = (resourceFileNameGood != null) ? Helper.GetResourceBytes("Config." + resourceFileNameGood) : null;

            FileName = Path.GetTempFileName();
            if (resourceFileName == null) {
                File.Delete(FileName); //to start fresh
            } else {
                File.WriteAllBytes(FileName, Bytes);
            }

            Config.Initialize(FileName, null, null, null);
        }

        private readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        public string Text { get => Utf8.GetString(Bytes); }
        public string GoodText { get => Utf8.GetString(GoodBytes ?? Array.Empty<byte>()); }

        #region IDisposable Support

        ~ConfigLoader() {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing) {
            try {
                File.Delete(FileName);
            } catch (IOException) { }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }

    #endregion

}
