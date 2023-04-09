using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Metrics;
using System.Linq;

namespace comroid.common;

public static class Units
{
    static Units()
    {
        // constants
        SiPrefixes = typeof(SiPrefix).GetEnumValues()
            .Cast<SiPrefix>()
            .ToImmutableSortedDictionary(si => si.ToString() == "None" ? string.Empty : si.ToString(), si => si);
        EmptyUnit = new UnitInstance(new Unit(UnitCategory.Base, string.Empty), SiPrefix.One);
        EmptyValue = new UnitValue(EmptyUnit, default);

        // predefined units
        Time = new UnitCategory("time");
        Seconds = new Unit(Time, "s") { Name = "Seconds" };
        Minutes = new Unit(Time, "min") { Name = "Minutes", Strategy = { FactorUnit(60, Seconds) } };
        Hours = new Unit(Time, "h") { Name = "Hours", Strategy = { FactorUnit(60, Minutes) } };
        Days = new Unit(Time, "d") { Name = "Days", Strategy = { FactorUnit(24, Hours) } };
        Weeks = new Unit(Time, "w") { Name = "Weeks", Strategy = { FactorUnit(7, Days) } };
        Months = new Unit(Time, "mo") { Name = "Months" };
        Years = new Unit(Time, "y") { Name = "Years", Strategy = { FactorUnit(12, Months) } };

        Programming = new UnitCategory("programming");
        Bytes = new Unit(Programming, "B") { Name = "Byte", Base = 8 };

        Physics = new UnitCategory("physics");
        Hertz = new Unit(Physics, "Hz");
        
        Distance = new UnitCategory("distance", Physics);
        Meter = new Unit(Distance, "m") { Name = "Meter" };
        LightSecond = new Unit(Distance, "ls") { Name = "LightSecond", Strategy = { FactorUnit(2.998e+8, Meter) } };
        LightYear = new Unit(Distance, "ly") { Name = "LightYear", Strategy = { FactorUnit(3.156e+7, LightSecond) } };

        Electrical = new UnitCategory("electrical", Physics);
        Volts = new Unit(Electrical, "V") { Name = "Volt" };
        Ampere = new Unit(Electrical, "A") { Name = "Ampere" };
        Watts = new Unit(Electrical, "W") { Name = "Watt", Strategy = { ResultOf(Volts, UnitOperator.Multiply, Ampere) } };
        Ohm = new Unit(Electrical, "O") { Name = "Ohm", Strategy = { ResultOf(Volts, UnitOperator.Divide, Ampere) } };
        Coulomb = new Unit(Electrical, "C") { Name = "Coulomb" };
        Farad = new Unit(Electrical, "F") { Name = "Farad", Strategy = { ResultOf(Coulomb, UnitOperator.Divide, Volts) } };
        Henry = new Unit(Electrical, "He") { Name = "Henry", Strategy = { ResultOf(Ohm, UnitOperator.Multiply, Seconds) } };
    }

    #region Constants

    public static readonly IDictionary<string, SiPrefix> SiPrefixes;
    public static readonly UnitInstance EmptyUnit;
    public static readonly UnitValue EmptyValue;

    #endregion

    #region Predefined Units
    
    public static readonly UnitCategory Time;
    public static readonly Unit Years;
    public static readonly Unit Months;
    public static readonly Unit Weeks;
    public static readonly Unit Days;
    public static readonly Unit Hours;
    public static readonly Unit Minutes;
    public static readonly Unit Seconds;

    public static readonly UnitCategory Programming;
    public static readonly Unit Bytes;

    public static readonly UnitCategory Physics;
    /* todo: support deliberately many units except imperial units */
    public static readonly Unit Hertz;

    public static readonly UnitCategory Distance;
    public static readonly Unit Meter;
    public static readonly Unit LightSecond;
    public static readonly Unit LightYear;
    
    public static readonly UnitCategory Electrical;
    public static readonly Unit Volts;
    public static readonly Unit Ampere;
    public static readonly Unit Watts;
    public static readonly Unit Ohm;
    public static readonly Unit Coulomb;
    public static readonly Unit Farad;
    public static readonly Unit Henry;

    #endregion

    #region Facade Methods

    public static UnitValue Parse(string str) => UnitCategory.Base.ParseValue(str);
    public static Unit ParseUnit(string str) => UnitCategory.Base.ParseUnit(str);

    public static UnitAccumulatorStrategy ResultOf(Unit lhs, UnitOperator op, Unit rhs)
        => output => new[]
        {
            CreateAccumulator_CombinationUnit(lhs, op, rhs, output),
            CreateAccumulator_CombinationUnit(output, op.Inverse(), lhs, rhs),
            CreateAccumulator_CombinationUnit(output, op.Inverse(), rhs, lhs)
        };
    public static UnitAccumulator CreateAccumulator_CombinationUnit(Unit lhs, UnitOperator _op, Unit rhs, Unit output)
    {
        UnitValue? Accumulate(UnitOperator op, UnitValue l, UnitValue r) => op == _op && lhs == l && rhs == r ? new UnitValue(output, op.Apply(l, r)) : null;
        return _op == UnitOperator.Multiply ? WrapAccumulator_BiDirectional(Accumulate, false) : Accumulate;
    }

    public static UnitAccumulatorStrategy FactorUnit(double factor, Unit output)
        => input => new[] { CreateAccumulator_FactorUnit(UnitOperator.Multiply, input, factor, output) };
    public static UnitAccumulator CreateAccumulator_FactorUnit(UnitOperator _op, Unit lhs, double factor, Unit output)
        => WrapAccumulator_BiDirectional((op, l, r)
            => op == _op && l == lhs && r == factor ? new UnitValue(output, op.Apply(l, factor)) : null);

    public static UnitAccumulator WrapAccumulator_BiDirectional(UnitAccumulator wrap, bool opInverse = true)
        => (op, l, r) => wrap(op, l, r) ?? wrap(opInverse ? op.Inverse() : op, r, l);

    #endregion

    #region Extensions

    public static double ConvertTo(this SiPrefix from, SiPrefix to, double value, int @base)
        => from == to ? value : value * Math.Pow(@base, from - to) * Math.Max(1, /*todo: this is wrong but works with 8 and 10*/10 - @base);
    public static double Apply(this UnitOperator op, params double[] values) => values.Aggregate((l, r) => op switch
    {
        UnitOperator.Multiply => l * r,
        UnitOperator.Divide => l / r,
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unknown operator")
    });
    public static UnitOperator Inverse(this UnitOperator op) => op switch
    {
        UnitOperator.Multiply => UnitOperator.Divide,
        UnitOperator.Divide => UnitOperator.Multiply,
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
    };

    #endregion
}

public sealed class UnitCategory : List<Unit>
{
    public static readonly UnitCategory Base = new("units");
    internal static readonly UnitCategory Combined = new("combined");

    private UnitCategory(string type)
    {
        Name = type;
        Parent = null!;
    }

    public UnitCategory(string name, UnitCategory parent = null!)
    {
        Name = name;
        Parent = parent ?? Base;
        Parent.Children.Add(this);
    }

    public bool Enabled { get; set; }
    public string Name { get; }
    public string FullName
    {
        get => (Parent?.FullName ?? string.Empty) + "/" + Name;
    }
    public UnitCategory Parent { get; }
    public List<UnitCategory> Children { get; } = new();

    public Unit? this[string ident]
    {
        get => this.FirstOrDefault(x => x.Identifier == ident);
    }
    public static Unit operator +(UnitCategory cat, Unit unit)
    {
        if (unit == null)
            throw new ArgumentNullException(nameof(unit), "unit was null");
        cat.Add(unit);
        return unit;
    }

    public Unit ParseUnit(string str)
    {
        var unit = IterateUnits().FirstOrDefault(unit => unit.Identifier == str);
        KeyValuePair<string, SiPrefix>? si = null;
        if (unit == null)
        {
            si = Units.SiPrefixes.FirstOrDefault(prefix => prefix.Key.Length > 0 && str.StartsWith(prefix.Key));
            if (si != null)
            {
                var offset = str.IndexOf(si.Value.Key, StringComparison.Ordinal) + si.Value.Key.Length;
                str = str.Substring(offset, str.Length - offset);
            }
        }

        return new UnitInstance(unit ?? IterateUnits().FirstOrDefault(unit => unit.Identifier == str) ?? Units.EmptyUnit, si?.Value ?? SiPrefix.One);
    }

    public UnitValue ParseValue(string str)
    {
        var val = string.Empty;
        foreach (var c in str)
            if (char.IsDigit(c) || c == '.')
                val += c;
            else break;
        return new UnitValue(ParseUnit(str.Substring(val.Length)), double.Parse(val));
    }

    private IEnumerable<Unit> IterateUnits(bool recursive = true)
        => this.Concat(Children.SelectMany(x => recursive ? x.IterateUnits(recursive) : x));
    internal IEnumerable<UnitAccumulator> IterateAccumulators(bool recursive = true)
    {
        foreach (var unit in IterateUnits(recursive))
        foreach (var strategy in unit.Strategy)
        foreach (var accumulator in strategy(unit))
            yield return accumulator;
    }

    public override string ToString() => FullName;
}

public delegate IEnumerable<UnitAccumulator> UnitAccumulatorStrategy(Unit unit);

public delegate UnitValue? UnitAccumulator(UnitOperator op, UnitValue lhs, UnitValue rhs);

public enum UnitOperator
{
    Multiply,
    Divide
}

public class Unit
{
    private string? _name;

    public Unit(UnitCategory category, string identifier)
    {
        Category = category;
        Identifier = identifier;

        Category.Add(this);
    }

    public UnitCategory Category { get; }
    public virtual SiPrefix SiPrefix
    {
        get => SiPrefix.One;
    }
    public virtual string Identifier { get; }

    public virtual string Name
    {
        get => _name ?? Identifier;
        set => _name = value;
    }
    public string FullName
    {
        get => (Category?.FullName ?? string.Empty) + "/" + Name;
    }

    public int Base { get; init; } = 10;
    public List<UnitAccumulatorStrategy> Strategy { get; init; } = new();

    public static UnitValue operator *(Unit left, double right) => new(left, right);
    public static UnitValue operator *(double right, Unit left) => new(left, right);
    public static UnitValue operator *(SiPrefix right, Unit left) => left * right;
    public static UnitValue operator *(Unit left, SiPrefix right) => new(left, (left is UnitValue v ? (double)v : 1) * Math.Pow(left.Base, (int)right));
    public static Unit operator *(Unit left, Unit right) => new CombinationUnit(left, right);
    public static Unit operator /(Unit left, Unit right) => left is CombinationUnit cu ? cu / right : throw new ArgumentException("Cannot remove unit from " + left.GetType().Name);
    public static bool operator ==(Unit? left, Unit? right) => Equals(null, left) && Equals(null, right) || left?.Name == right?.Name;
    public static bool operator !=(Unit? left, Unit? right) => !(left == right);

    public static UnitInstance operator |(Unit unit, SiPrefix prefix) => new(unit, prefix);

    public override bool Equals(object? obj) => obj is Unit other && other == this;
    public override int GetHashCode() => Name.GetHashCode();

    public override string ToString() => FullName;
}

internal class CombinationUnit : Unit
{
    private readonly Unit[] _parts;

    internal CombinationUnit(params Unit[] parts) : base(UnitCategory.Combined, null!)
    {
        _parts = parts;
    }

    public override string Identifier
    {
        get => _parts.Aggregate(string.Empty, (s, u) => s + u.Identifier);
    }
    public override string Name
    {
        get => _parts.Aggregate(string.Empty, (s, u) => s + u.Name);
        set => throw new NotSupportedException("Cannot change name of CombinationUnit");
    }

    public static Unit operator /(CombinationUnit left, Unit right)
    {
        var leftoversByIM = left._parts.Where(x => x != right).ToArray();
        if (leftoversByIM.Length > 1)
            return new CombinationUnit(leftoversByIM);
        return leftoversByIM.Length == 1 ? leftoversByIM[0] : left;
    }
}

public class UnitInstance : Unit
{
    private readonly Unit _unit;
    public UnitInstance(Unit unit, SiPrefix siPrefix) : base(unit.Category, unit.Identifier)
    {
        _unit = unit;
        Base = unit.Base;
        SiPrefix = siPrefix;
    }

    public override SiPrefix SiPrefix { get; }
    public override string Name
    {
        get => _unit.Name;
        set => _unit.Name = value;
    }
}

public class UnitValue : UnitInstance
{
    public UnitValue(Unit unit, double value) : base(unit, unit.SiPrefix)
    {
        Base = unit.Base;
        Value = value;
    }

    public double Value { get; }

    public static UnitValue operator +(double right, UnitValue left) => left + right;
    public static UnitValue operator +(UnitValue left, double right) => left + (left as Unit) * right;
    public static UnitValue operator +(UnitValue left, UnitValue right) => (left as Unit) * (left.Value + right.Value);
    public static UnitValue operator -(double right, UnitValue left) => left - right;
    public static UnitValue operator -(UnitValue left, double right) => left - (left as Unit) * right;
    public static UnitValue operator -(UnitValue left, UnitValue right) => (left as Unit) * (left.Value - right.Value);
    public static UnitValue operator *(double right, UnitValue l) => l * right;
    public static UnitValue operator *(UnitValue l, double right) => l * (Units.EmptyUnit * right);
    public static UnitValue operator *(UnitValue l, UnitValue r) => UnitCategory.Base.IterateAccumulators()
        .Select(acc => acc(UnitOperator.Multiply, l, r))
        .CastOrSkip<UnitValue>()
        .FirstOrDefault() ?? new UnitValue(new CombinationUnit(l, r), (double)l * (double)r);
    public static UnitValue operator /(double right, UnitValue l) => l / right;
    public static UnitValue operator /(UnitValue l, double right) => l / (Units.EmptyUnit * right);
    public static UnitValue operator /(UnitValue l, UnitValue r) => UnitCategory.Base.IterateAccumulators()
        .Select(acc => acc(UnitOperator.Divide, l, r))
        .CastOrSkip<UnitValue>()
        .FirstOrDefault() ?? new UnitValue(new CombinationUnit(l, r), (double)l * (double)r);

    public static UnitValue operator |(UnitValue value, SiPrefix prefix)
        => new(value as Unit | prefix, value.SiPrefix.ConvertTo(prefix, value.Value, value.Base));

    public static bool operator ==(UnitValue? left, UnitValue? right) => Equals(null, left) && Equals(null, right) || left as Unit == right && left!.Value == right!.Value;
    public static bool operator !=(UnitValue? left, UnitValue? right) => !(left == right);

    public static implicit operator double(UnitValue value) => value.SiPrefix.ConvertTo(SiPrefix.One, value.Value, value.Base);

    public UnitValue Normalize()
    {
        var value = Value;
        var prefixes = Units.SiPrefixes.Values.OrderBy(si => (int)si).ToArray();
        for (var i = 0; i < prefixes.Length; i++)
        {
            var si = prefixes[i];
            var next = i + 1 < prefixes.Length ? prefixes[i + 1] : (SiPrefix?)null;

            if (next != null && value >= Math.Pow(Base, (int)si) && value < Math.Pow(Base, (int)next))
                return new UnitValue(new UnitInstance(this, si), SiPrefix.ConvertTo(si, value, Base));
        }

        return new UnitValue(new UnitInstance(this, SiPrefix.One), value);
    }

    public override string ToString()
        => $"{Value:0.###}{(SiPrefix == SiPrefix.One ? string.Empty : SiPrefix.ToString())}{Identifier}";
}

public enum SiPrefix
{
    z = -21, // Zepto
    y = -24, // Yocto
    a = -18, // Atto
    f = -15, // Femto
    p = -12, // Pico
    n = -9, // Nano
    \u00B5 = -6, // Micro
    m = -3, // Milli
    c = -2, // Centi
    d = -1, // Deci
    One = 0, // None
    da = 1, // Deca
    h = 2, // Hecto
    k = 3, // Kilo
    M = 6, // Mega
    G = 9, // Giga
    T = 12, // Tera
    P = 15, // Peta
    E = 18, // Exa
    Z = 21, // Zetta
    Y = 24 // Yotta
}