namespace Tests;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Medo;

[TestClass]
public class ConfigNoSource_Tests {

    [TestMethod]
    public void ConfigNoSource_String() {
        var config = new ConfigNoSource();
        Assert.Throws<NotSupportedException>(() => {
            config.Write("Value", "1");
        });
        Assert.Throws<NotSupportedException>(() => {
            var _ = config.Read("Value", "");
        });
    }

    [TestMethod]
    public void ConfigNoSource_Int32() {
        var config = new ConfigNoSource();
        Assert.Throws<NotSupportedException>(() => {
            config.Write("Value", 42);
        });
        Assert.Throws<NotSupportedException>(() => {
            var _ = config.Read("Value", 0);
        });
    }

    public void ConfigNoSource_Int64() {
        var config = new ConfigNoSource();
        Assert.Throws<NotSupportedException>(() => {
            config.Write("Value", 42L);
        });
        Assert.Throws<NotSupportedException>(() => {
            var _ = config.Read("Value", 0L);
        });
    }

    [TestMethod]
    public void ConfigNoSource_Float32() {
        var config = new ConfigNoSource();
        Assert.Throws<NotSupportedException>(() => {
            config.Write("Value", 4.2f);
        });
        Assert.Throws<NotSupportedException>(() => {
            var _ = config.Read("Value", 0.0f);
        });
    }

    [TestMethod]
    public void ConfigNoSource_Float64() {
        var config = new ConfigNoSource();
        Assert.Throws<NotSupportedException>(() => {
            config.Write("Value", 4.2);
        });
        Assert.Throws<NotSupportedException>(() => {
            var _ = config.Read("Value", 0.0);
        });
    }

    [TestMethod]
    public void ConfigNoSource_Decimal() {
        var config = new ConfigNoSource();

        Assert.Throws<NotSupportedException>(() => {
            config.Write("Value", 42.20M);
        });
        Assert.Throws<NotSupportedException>(() => {
            var _ = config.Read("Value", 0M);
        });
    }

    [TestMethod]
    public void ConfigNoSource_DateTime() {
        var config = new ConfigNoSource();

        Assert.Throws<NotSupportedException>(() => {
            config.Write("MaxValue", DateTime.Now);
        });
        Assert.Throws<NotSupportedException>(() => {
            var _ = config.Read("MinValue", DateTime.MinValue);
        });
    }

    [TestMethod]
    public void ConfigNoSource_Strings() {
        var config = new ConfigNoSource();
        Assert.Throws<NotSupportedException>(() => {
            config.WriteMany("Value", ["X"]);
        });
        Assert.Throws<NotSupportedException>(() => {
            var _ = config.ReadMany("Value");
        });
    }


    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public void ConfigNoSource_InvalidKey(string key) {
        var config = new ConfigNoSource();
        Assert.Throws<ArgumentException>(() => {
            config.Write(key, 0);
        });
    }

}
