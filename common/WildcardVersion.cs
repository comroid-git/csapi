using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace comroid.common;

public class WildcardVersion : IComparable, IComparable<Version?>, IEquatable<Version?>
{
    public const int WildcardValue = int.MaxValue;
    public const int UnsetValue = -1;

    public static readonly Regex Pattern = new("(?<major>[+*]|\\d+)(\\.(?<minor>[+*]|\\d+))?(\\.(?<build>[+*]|\\d+))?(\\.(?<rev>[+*]|\\d+))?");

    public WildcardVersion(string version)
    {
        var invalidStringException =
            new ArgumentOutOfRangeException(nameof(version), version, "Invalid Version String");
        if (Pattern.Match(version) is not { Success: true } match)
            throw invalidStringException;
        Major = FromWildcard(match, "major") ?? throw invalidStringException;
        Minor = FromWildcard(match, "minor") ?? WildcardValue;
        Build = FromWildcard(match, "build") ?? WildcardValue;
        Revision = FromWildcard(match, "rev") ?? WildcardValue;
    }

    public WildcardVersion(int major, int minor = -1, int build = -1, int revision = -1)
    {
        Major = major;
        Minor = minor;
        Build = build;
        Revision = revision;
    }

    public WildcardVersion(int major, int minor = -1, int build = -1, short majorRevision = -1,
        short minorRevision = -1)
    {
        Major = major;
        Minor = minor;
        Build = build;
        Revision = majorRevision << 16 | (ushort)minorRevision;
    }

    public int Major { get; }
    public int Minor { get; }
    public int Build { get; }
    public int Revision { get; }
    public short MajorRevision
    {
        get => (short)(Revision >> 16);
    }
    public short MinorRevision
    {
        get => (short)(Revision & 0xFFFF);
    }

    public override string ToString()
    {
        var str = IntoWildcard(Major, out var wc);
        if (!wc) foreach (var field in new[] { Minor, Build, Revision })
        {
            if (field == UnsetValue)
                break;
            else str += '.' + IntoWildcard(field, out wc);
            if (wc) break;
        }
        return str;
    }

    #region Utility Methods

    private int? FromWildcard(Match match, string group) => !match.Groups.ContainsKey(group)
        ? null
        : match.Groups[group].Value is "*" or "+"
          || string.IsNullOrEmpty(match.Groups[group].Value)
            ? WildcardValue
            : int.Parse(match.Groups[group].Value);

    private string IntoWildcard(int value, out bool wc) => (wc = value == WildcardValue) ? "*" : value.ToString();

    public static implicit operator Version?(WildcardVersion? ver) => ver == null ? null : new(ver.Major, ver.Minor, ver.Build, ver.Revision);

    public static implicit operator WildcardVersion?(Version? ver) => ver == null ? null : new(ver.Major, ver.Minor, ver.Build, ver.Revision);

    #endregion

    #region Comparison

    public override bool Equals(object? obj) => (obj as Version)?.Equals(this) ?? false;

    public bool Equals(Version? other) => Major == other?.Major && Minor == other.Minor && Build == other.Build && Revision == other.Revision;

    public override int GetHashCode() => ToString().GetHashCode();

    public int CompareTo(object? obj) => -1 * (obj as Version)?.CompareTo(this) ?? 1;

    public int CompareTo(Version? other) => this.CompareTo((WildcardVersion?)other);

    public int CompareTo(WildcardVersion? other)
    {
        if (other == null) return 1;
        if (Major != other?.Major) return Major - other!.Major;
        if (Minor != other.Minor) return Minor - other.Minor;
        if (Build != other.Build) return Build - other.Build;
        if (Revision != other.Revision) return Revision - other.Revision;
        return 0;
    }

    public static bool operator ==(WildcardVersion? l, Version? r) => l?.Equals(r) ?? Equals(r, null);

    public static bool operator !=(WildcardVersion? l, Version? r) => !(l == r);

    public static bool operator >(WildcardVersion? l, Version? r) => (l?.CompareTo(r) ?? -1) > 0;

    public static bool operator <(WildcardVersion? l, Version? r) => r > l;

    public static bool operator >=(WildcardVersion? l, Version? r) => l?.Major == r?.Major && l > r;

    public static bool operator <=(WildcardVersion? l, Version? r) => r >= l;

    #endregion
}