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
    [DataRow(float.MinValue, "-3.4028235e+38")]
    [DataRow(float.MaxValue, "3.4028235e+38")]
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
    [DataRow(double.MinValue, "-1.7976931348623157e+308")]
    [DataRow(double.MaxValue, "1.7976931348623157e+308")]
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
    public void ConfigDummySource_Decimal() {
        var config = new ConfigDummySource();

        config.Write("MinValue", decimal.MinValue);
        config.Write("MaxValue", decimal.MaxValue);
        config.Write("Value", 42.20M);

        Assert.AreEqual("-79228162514264337593543950335", config.Read("MinValue", ""));
        Assert.AreEqual("79228162514264337593543950335", config.Read("MaxValue", ""));
        Assert.AreEqual("42.20", config.Read("Value", ""));

        Assert.AreEqual(decimal.MinValue, config.Read("MinValue", 0M));
        Assert.AreEqual(decimal.MaxValue, config.Read("MaxValue", 0M));
        Assert.AreEqual(42.20M, config.Read("Value", 0M));
    }

    [TestMethod]
    public void ConfigDummySource_DateTime() {
        var config = new ConfigDummySource();

        config.Write("MinValue", DateTime.MinValue);
        config.Write("MaxValue", DateTime.MaxValue);
        config.Write("UnixEpoch", DateTime.UnixEpoch);

        Assert.AreEqual("0001-01-01T00:00:00.0000000", config.Read("MinValue", ""));
        Assert.AreEqual("9999-12-31T23:59:59.9999999", config.Read("MaxValue", ""));
        Assert.AreEqual("1970-01-01T00:00:00.0000000Z", config.Read("UnixEpoch", ""));

        Assert.AreEqual(DateTime.MinValue, config.Read("MinValue", DateTime.Now));
        Assert.AreEqual(DateTime.MaxValue, config.Read("MaxValue", DateTime.Now));
        Assert.AreEqual(DateTime.UnixEpoch, config.Read("UnixEpoch", DateTime.Now));
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
