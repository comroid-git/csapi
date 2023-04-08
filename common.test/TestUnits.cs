﻿using comroid.common;
using NUnit.Framework;

namespace common.test;

public class TestUnits
{
    [Test]
    public void TestConversion()
    {
        var input = Units.Parse("8kV");
        var output = input | SiPrefix.One;

        Assert.That((double)output, Is.EqualTo(8000));
        Assert.That(output.ToString(), Is.EqualTo("8000V"));
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
    public void TestFactorUnit()
    {
        var input = 2 * Units.Days;
        var output = input * 24;

        Assert.That((double)output, Is.EqualTo(2 * 24));
        Assert.That(output.ToString(), Is.EqualTo($"{2 * 24}h"));
    }
}