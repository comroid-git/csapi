using comroid.common;
using NUnit.Framework;

namespace common.test;

public class TestUnits
{
    [Test]
    public void TestEmptyParse()
    {
        var x = Units.Parse("8");
        var y = Units.Parse("64");
        var output = x * y;

        Assert.That((double)output, Is.EqualTo(8 * 64));
        Assert.That(output.ToString(), Is.EqualTo((8 * 64).ToString()));
    }
    
    [Test]
    public void TestConversion()
    {
        var input = Units.Parse("8kV");
        var output = input | SiPrefix.One;

        Assert.That((double)output, Is.EqualTo(8000));
        Assert.That(output.ToString(), Is.EqualTo("8000V"));
    }
    
    [Test]
    public void TestInvalidConversion()
    {
        var input = 1 * Units.Bytes;
        var output = input | Units.Seconds;

        Assert.That((double)output, Is.NaN);
    }

    [Test]
    public void TestByteConversion()
    {
        var input = Units.Parse("8kB");
        var output = input | SiPrefix.One;

        Assert.That((double)output, Is.EqualTo(8192));
        Assert.That(output.ToString(), Is.EqualTo("8192B"));
    }

    [Test]
    public void TestNormalization_1()
    {
        var input = 8000 * Units.Volts;
        var output = input.Normalize();

        Assert.That((double)output, Is.EqualTo(8000));
        Assert.That(output.ToString(), Is.EqualTo("8kV"));
    }

    [Test]
    public void TestNormalization_2()
    {
        var input = 30 * Units.Hours;
        var output = input.Normalize();

        Assert.That(output.ToString(), Is.EqualTo("1d6h"));
    }

    [Test]
    public void TestFactorUnit_1()
    {
        var input = 2 * Units.Days;
        var output = input * (double)24;

        Assert.That((double)output, Is.EqualTo(2 * 24));
        Assert.That(output.ToString(), Is.EqualTo($"{2 * 24}h"));
    }

    [Test]
    public void TestFactorUnit_2()
    {
        var input = 1 * Units.LightYear;
        var output = input | Units.LightSecond;

        Assert.That((double)output, Is.EqualTo(31557385.23).Within(1e-3));
        Assert.That(output.ToString(), Is.EqualTo($"{31557385.23:0.###}Ls"));
    }

    [Test]
    public void TestResultUnit()
    {
        var i = 16 * Units.Ampere;
        var u = 230 * Units.Volts;
        var output = i * u;

        Assert.That((double)output, Is.EqualTo(16 * 230));
        Assert.That(output.ToString(), Is.EqualTo("3680W"));
        Assert.That(output.Normalize().ToString(), Is.EqualTo($"{3.68}kW"));
    }

    [Test]
    public void TestCombinationUnit_1()
    {
        var P = 5 * Units.Watts * SiPrefix.k;
        var t = 2 * Units.Hours;
        var output = P * t;

        Assert.That((double)output, Is.EqualTo(5 * 2 * 1000));
        Assert.That((output | SiPrefix.One).ToString(), Is.EqualTo("10000Wh"));
        Assert.That(output.Normalize().ToString(), Is.EqualTo("10kWh"));
    }

    [Test]
    public void TestCombinationUnit_2()
    {
        var input = 5 * (Units.Watts * Units.Hours);
        var output = input / (1 * Units.Hours);

        Assert.That((double)output, Is.EqualTo(5));
        Assert.That((output | SiPrefix.One).ToString(), Is.EqualTo("5W"));
    }
}