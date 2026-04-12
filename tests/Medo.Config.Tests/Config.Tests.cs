namespace Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Medo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Config_Tests {

    [TestMethod]
    public void Config_UserNoAccess() {
        lock (Helpers.SingleTestSync) {
            Config.InitializeFromFiles(null, "", "", "");
            Assert.Throws<NotSupportedException>(() => {
                var _ = Config.User.Read("test", "");
            });
        }
    }

    [TestMethod]
    public void Config_SystemNoAccess() {
        lock (Helpers.SingleTestSync) {
            Config.InitializeFromFiles("", null, "", "");
            Assert.Throws<NotSupportedException>(() => {
                var _ = Config.System.Read("test", "");
            });
        }
    }

    [TestMethod]
    public void Config_StateNoAccess() {
        lock (Helpers.SingleTestSync) {
            Config.InitializeFromFiles("", "", null, "");
            Assert.Throws<NotSupportedException>(() => {
                var _ = Config.State.Read("test", "");
            });
        }
    }

    [TestMethod]
    public void Config_RecentNoAccess() {
        lock (Helpers.SingleTestSync) {
            Config.InitializeFromFiles("", "", "", null);
            Assert.Throws<NotSupportedException>(() => {
                Config.Recent.Files.Add(new FileInfo("test.txt"));
            });
        }
    }


    [TestMethod]
    public void Config_Direct_UserNoAccessSystemNoAccess() {
        lock (Helpers.SingleTestSync) {
            Config.InitializeFromFiles(null, null, null, null);
            Assert.Throws<NotSupportedException>(() => {
                Config.Write("test", "");
            });
            Assert.Throws<NotSupportedException>(() => {
                var _ = Config.Read("test", "");
            });
        }
    }

    [TestMethod]
    public void Config_Direct_UserNoAccessSystemDummy() {
        lock (Helpers.SingleTestSync) {
            Config.InitializeFromFiles(null, "", null, null);
            Assert.Throws<NotSupportedException>(() => {
                Config.Write("test", "X");
            });
            Config.System.Write("test", "Y");
            Assert.AreEqual("Y", Config.Read("test", ""));  // no exception because system is DummySource and user is ignored due to NoSource.
        }
    }

    [TestMethod]
    public void Config_Direct_UserDummySystemNoAccess() {
        lock (Helpers.SingleTestSync) {
            Config.InitializeFromFiles("", null, null, null);
            Config.Write("test", "X");
            Assert.Throws<NotSupportedException>(() => {
                Config.System.Write("test", "Y");
            });
            Assert.AreEqual("X", Config.Read("test", ""));  // no exception because user is DummySource and system is not read when NoSource.
        }
    }

}
