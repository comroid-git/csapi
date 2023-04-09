using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace comroid.common;

public static class Units
{
    static Units()
    {
        // constants
        SiPrefixes = typeof(SiPrefix).GetEnumValues()
            .Cast<SiPrefix>()
            .OrderBy(si => (int)si)
            .ToImmutableDictionary(si => si.ToString(), si => si);
        EmptyUnit = new UnitInstance(new Unit(UnitCategory.Base, string.Empty), SiPrefix.One);
        EmptyValue = new UnitValue(EmptyUnit, default);

        // predefined units
        Time = new UnitCategory("time");
        Seconds = new Unit(Time, "s") { Name = "Seconds" };
        Minutes = new Unit(Time, "min") { Name = "Minutes", Strategies = { FactorUnit(60, Seconds) } };
        Hours = new Unit(Time, "h") { Name = "Hours", Strategies = { FactorUnit(60, Minutes) } };
        Days = new Unit(Time, "d") { Name = "Days", Strategies = { FactorUnit(24, Hours) } };
        Weeks = new Unit(Time, "w") { Name = "Weeks", Strategies = { FactorUnit(7, Days) } };
        Months = new Unit(Time, "mo") { Name = "Months" };
        Years = new Unit(Time, "y") { Name = "Years", Strategies = { FactorUnit(12, Months) } };

        Programming = new UnitCategory("programming");
        Bytes = new Unit(Programming, "B") { Name = "Byte", Base = 8 };

        Physics = new UnitCategory("physics");
        Hertz = new Unit(Physics, "Hz");
        
        Distance = new UnitCategory("distance", Physics);
        Meter = new Unit(Distance, "m") { Name = "Meter" };
        LightSecond = new Unit(Distance, "Ls") { Name = "LightSecond", Strategies = { FactorUnit(2.998e+8, Meter) } };
        LightYear = new Unit(Distance, "Ly") { Name = "LightYear", Strategies = { FactorUnit(3.156e+7, LightSecond) } };

        Electrical = new UnitCategory("electrical", Physics);
        Volts = new Unit(Electrical, "V") { Name = "Volt" };
        Ampere = new Unit(Electrical, "A") { Name = "Ampere" };
        Watts = new Unit(Electrical, "W") { Name = "Watt", Strategies = { ResultOf(Volts, UnitOperator.Multiply, Ampere) } };
        Ohm = new Unit(Electrical, "Oh") { Name = "Ohm", Strategies = { ResultOf(Volts, UnitOperator.Divide, Ampere) } };
        Coulomb = new Unit(Electrical, "C") { Name = "Coulomb" };
        Farad = new Unit(Electrical, "F") { Name = "Farad", Strategies = { ResultOf(Coulomb, UnitOperator.Divide, Volts) } };
        Henry = new Unit(Electrical, "He") { Name = "Henry", Strategies = { ResultOf(Ohm, UnitOperator.Multiply, Seconds) } };
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

    public static double FindFactor(Unit from, Unit to)
    {
        foreach (var strategy in UnitCategory.Base.IterateRelatedStrategies(true, from, to))
            if (strategy is FactorUnitStrategy fus)
            {
                if (to == fus.output)
                    return fus.factor;
                if (from == fus.output)
                    return 1 / fus.factor;
                //todo: this is wrong; does not consider multi-stage factors (eg for days->hours->minutes it just does days->hours)
            }
        return 1; // no strategies found
    }

    public static UnitAccumulatorStrategy ResultOf(Unit lhs, UnitOperator op, Unit rhs) => new CombinationUnitStrategy(op, lhs, rhs);
    public static UnitAccumulatorStrategy FactorUnit(double factor, Unit output) => new FactorUnitStrategy(UnitOperator.Multiply, factor, output);

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
        foreach (var strategy in unit.Strategies)
        foreach (var accumulator in strategy.CreateAccumulators(unit))
            yield return accumulator;
    }
    internal IEnumerable<UnitAccumulatorStrategy> IterateRelatedStrategies(bool recursive = true, params Unit[] related)
    {
        foreach (var unit in IterateUnits(recursive))
        foreach (var strategy in unit.Strategies)
            if (related.Any(unit.Equals) || related.Any(strategy.IsUnitRelated))
                yield return strategy;
    }

    public override string ToString() => FullName;
}

#region Strategies

public abstract class UnitAccumulatorStrategy
{
    public abstract bool IsUnitRelated(Unit arg);
    public abstract IEnumerable<UnitAccumulator> CreateAccumulators(Unit unit);
}

public class FactorUnitStrategy : UnitAccumulatorStrategy
{
    private readonly UnitOperator _op;
    internal readonly double factor;
    internal readonly Unit output;
    
    public FactorUnitStrategy(UnitOperator op, double factor, Unit output)
    {
        _op = op;
        this.factor = factor;
        this.output = output;
    }

    public override bool IsUnitRelated(Unit arg) => output == arg;
    public override IEnumerable<UnitAccumulator> CreateAccumulators(Unit input)
    {
        yield return new FactorUnitAccumulator(_op, input, factor, output);
    }
}

public class CombinationUnitStrategy : UnitAccumulatorStrategy
{
    private readonly UnitOperator _op;
    private readonly Unit _lhs;
    private readonly Unit _rhs;
    
    public CombinationUnitStrategy(UnitOperator op, Unit lhs, Unit rhs)
    {
        _op = op;
        _lhs = lhs;
        _rhs = rhs;
    }

    public override bool IsUnitRelated(Unit arg) => _lhs == arg || _rhs == arg;
    public override IEnumerable<UnitAccumulator> CreateAccumulators(Unit unit)
    {
        yield return new CombinationUnitAccumulator(_op, _lhs, _rhs, unit) { OpInverse = false };
        yield return new CombinationUnitAccumulator(_op.Inverse(), _lhs, unit, _rhs) { OpInverse = false };
        yield return new CombinationUnitAccumulator(_op.Inverse(), _rhs, unit, _lhs) { OpInverse = false };
    }
}

public abstract class UnitAccumulator
{
    public bool BiDirectional { get; init; } = true;
    public bool OpInverse { get; init; } = true;
    
    public IEnumerable<UnitValue> Apply(UnitOperator op, UnitValue lhs, UnitValue rhs)
    {
        if (Accepts(op, lhs, rhs))
            yield return Accumulate(op, lhs, rhs);
        if (BiDirectional && Accepts(OpInverse ? op.Inverse() : op, rhs, lhs))
            yield return Accumulate(OpInverse ? op.Inverse() : op, rhs, lhs);
    }
    
    public abstract bool Accepts(UnitOperator op, UnitValue lhs, UnitValue rhs);
    public abstract UnitValue Accumulate(UnitOperator op, UnitValue lhs, UnitValue rhs);
}

public class FactorUnitAccumulator : UnitAccumulator
{
    private readonly UnitOperator _op;
    private readonly Unit _lhs;
    private readonly double _factor;
    private readonly Unit _output;
    
    public FactorUnitAccumulator(UnitOperator op, Unit lhs, double factor, Unit output)
    {
        _op = op;
        _lhs = lhs;
        _factor = factor;
        _output = output;
    }

    public override bool Accepts(UnitOperator op, UnitValue lhs, UnitValue rhs) => op == _op && lhs == _lhs && rhs == _factor;
    public override UnitValue Accumulate(UnitOperator op, UnitValue lhs, UnitValue rhs) => new(_output, op.Apply(lhs, _factor));
}

public class CombinationUnitAccumulator : UnitAccumulator
{
    private readonly UnitOperator _op;
    private readonly Unit _lhs;
    private readonly Unit _rhs;
    private readonly Unit _output;
    
    public CombinationUnitAccumulator(UnitOperator op, Unit lhs, Unit rhs, Unit output)
    {
        _op = op;
        _lhs = lhs;
        _rhs = rhs;
        _output = output;
    }
    
    public override bool Accepts(UnitOperator op, UnitValue lhs, UnitValue rhs) => op == _op && lhs == _lhs && rhs == _rhs;
    public override UnitValue Accumulate(UnitOperator op, UnitValue lhs, UnitValue rhs) => new(_output, op.Apply(lhs, rhs));
}

#endregion

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
    public List<UnitAccumulatorStrategy> Strategies { get; init; } = new();

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
    
    public static UnitValue? Strip(Unit @base, Unit strip, double value)
    {
        @base |= SiPrefix.One;
        strip |= SiPrefix.One;
        if (!@base.Identifier.Contains(strip.Identifier))
            return null;
        return Units.ParseUnit(@base.Identifier.Replace(strip.Identifier, string.Empty)) * value;
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
    public static UnitValue operator *(UnitValue l, UnitValue r)
        => RunAccumulators(UnitOperator.Multiply, l, r) ?? new UnitValue(new CombinationUnit(l, r), (double)l * (double)r);
    public static UnitValue operator /(double right, UnitValue l) => l / right;
    public static UnitValue operator /(UnitValue l, double right) => l / (Units.EmptyUnit * right);
    public static UnitValue operator /(UnitValue l, UnitValue r)
        => RunAccumulators(UnitOperator.Divide, l, r) ?? CombinationUnit.Strip(l, r, (double)l / (double)r) ?? Units.EmptyValue;
    private static UnitValue? RunAccumulators(UnitOperator op, UnitValue l, UnitValue r) => UnitCategory.Base.IterateAccumulators()
        .SelectMany(acc => acc.Apply(op, l, r))
        .CastOrSkip<UnitValue>()
        .FirstOrDefault(); 

    public static UnitValue operator |(UnitValue value, SiPrefix prefix)
        => new(value as Unit | prefix, value.SiPrefix.ConvertTo(prefix, value.Value, value.Base));
    public static UnitValue operator |(UnitValue value, Unit unit)
        => new(unit, (double)value * Units.FindFactor(value, unit));

    public static bool operator ==(UnitValue? left, UnitValue? right) => Equals(null, left) && Equals(null, right) || left as Unit == right && left!.Value == right!.Value;
    public static bool operator !=(UnitValue? left, UnitValue? right) => !(left == right);

    public static implicit operator double(UnitValue? value) => value?.SiPrefix.ConvertTo(SiPrefix.One, value.Value, value.Base) ?? default;
    public static implicit operator UnitValue(double? value) => value != null ? Units.EmptyUnit * value.Value : Units.EmptyValue;

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

    public override bool Equals(object? obj) => obj is Unit u && u == this;
    public override int GetHashCode() => ToString().GetHashCode();

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
    //c = -2, // Centi
    //d = -1, // Deci
    One = 0, // None
    //da = 1, // Deca
    //h = 2, // Hecto
    k = 3, // Kilo
    M = 6, // Mega
    G = 9, // Giga
    T = 12, // Tera
    P = 15, // Peta
    E = 18, // Exa
    Z = 21, // Zetta
    Y = 24 // Yotta
}