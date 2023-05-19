using comroid.common;
using NUnit.Framework;

namespace common.test;

public class TestWildcardVersion
{
    public static readonly WildcardVersion OtherA = new("2.4.35.3");
    public static readonly WildcardVersion OtherB = new("3.3.50.1");

    [Test]
    public void TestMajor()
    {
        var subj = new WildcardVersion("+");
        Assert.That(subj, Is.GreaterThan(OtherA));
        Assert.That(subj, Is.GreaterThan(OtherB));
    }
    
    [Test]
    public void TestMinor()
    {
        var subj = new WildcardVersion("2.+");
        Assert.That(subj, Is.GreaterThan(OtherA));
        Assert.That(subj, Is.LessThan(OtherB));
    }
    
    [Test]
    public void TestBuild()
    {
        var subj = new WildcardVersion("2.4.*");
        Assert.That(subj, Is.GreaterThan(OtherA));
        Assert.That(subj, Is.LessThan(OtherB));
    }
    
    [Test]
    public void TestRevision()
    {
        var subj = new WildcardVersion("2.4.50.+");
        Assert.That(subj, Is.GreaterThan(OtherA));
        Assert.That(subj, Is.LessThan(OtherB));
    }
}