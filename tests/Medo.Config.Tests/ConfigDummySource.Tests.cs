namespace Tests;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Medo;

[TestClass]
public class ConfigDummySource_Tests {

    [TestMethod]
    [DataRow("1", "1")]
    public void ConfigDummySource_String(string input, string expected) {
        var config = new ConfigDummySource();
        config.Write("Value", input);
        Assert.AreEqual(input, config.Read("Value", ""));
    }

    [TestMethod]
    [DataRow(int.MinValue, "-2147483648")]
    [DataRow(int.MaxValue, "2147483647")]
    [DataRow(42, "42")]
    [DataRow(-42, "-42")]
    public void ConfigDummySource_Int32(int input, string expected) {
        var config = new ConfigDummySource();
        config.Write("Value", input);
        Assert.AreEqual(expected, config.Read("Value", ""));
        Assert.AreEqual(input, config.Read("Value", 0));
    }

    [TestMethod]
    [DataRow(long.MinValue, "-9223372036854775808")]
    [DataRow(long.MaxValue, "9223372036854775807")]
    [DataRow(42, "42")]
    [DataRow(-42, "-42")]
    public void ConfigDummySource_Int64(long input, string expected) {
        var config = new ConfigDummySource();
        config.Write("Value", input);
        Assert.AreEqual(expected, config.Read("Value", ""));
        Assert.AreEqual(input, config.Read("Value", 0L));
    }

    [TestMethod]
    [DataRow(float.MinValue, "-3.4028235E+38")]
    [DataRow(float.MaxValue, "3.4028235E+38")]
    [DataRow((float)Math.PI, "3.1415927")]
    [DataRow(42.0f, "42")]
    [DataRow(-42.0f, "-42")]
    public void ConfigDummySource_Float32(float input, string expected) {
        var config = new ConfigDummySource();
        config.Write("Value", input);
        Assert.AreEqual(expected, config.Read("Value", ""));
        Assert.AreEqual(input, config.Read("Value", 0.0f));
    }

    [TestMethod]
    [DataRow(double.MinValue, "-1.7976931348623157E+308")]
    [DataRow(double.MaxValue, "1.7976931348623157E+308")]
    [DataRow(Math.PI, "3.141592653589793")]
    [DataRow(42.0, "42")]
    [DataRow(-42.0, "-42")]
    public void ConfigDummySource_Float64(double input, string expected) {
        var config = new ConfigDummySource();
        config.Write("Value", input);
        Assert.AreEqual(expected, config.Read("Value", ""));
        Assert.AreEqual(input, config.Read("Value", 0.0));
    }

    [TestMethod]
    [DataRow([])]
    [DataRow([""])]
    [DataRow(["A", "B"])]
    public void ConfigDummySource_Strings(string[] input) {
        var config = new ConfigDummySource();
        config.WriteMany("Value", input);
        var actual = config.ReadMany("Value");
        Assert.AreEqual(input.Length, actual.Length);
        for (int i = 0; i < input.Length; i++) {
            Assert.AreEqual(input[i], actual[i]);
        }
    }


    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public void ConfigDummySource_InvalidKey(string key) {
        var config = new ConfigDummySource();
        Assert.Throws<ArgumentException>(() => {
            config.Write(key, 0);
        });
    }

}
