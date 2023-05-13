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
        EmptyValue = new UnitValue(EmptyUnit, default) { Name = "NoUnit" };

        // predefined units
        Time = new UnitCategory("time");
        Seconds = new Unit(Time, "s") { Name = "Seconds" };
        Minutes = new Unit(Time, "min") { Name = "Minutes" };
        Hours = new Unit(Time, "h") { Name = "Hours" };
        Days = new Unit(Time, "d") { Name = "Days" };
        Weeks = new Unit(Time, "w") { Name = "Weeks" };
        Months = new Unit(Time, "mo") { Name = "Months" };
        Years = new Unit(Time, "y") { Name = "Years" };
        RegisterFactorUnitChain(
            (Seconds, 1),
            (Minutes, 60),
            (Hours, 60),
            (Days, 24),
            (Weeks, 7));
        RegisterFactorUnitChain(
            (Seconds, 1),
            (Minutes, 60),
            (Hours, 60),
            (Days, 24),
            (Years, 365));
        RegisterFactorUnitChain(
            (Months, 1),
            (Years, 12));

        Programming = new UnitCategory("programming");
        Bytes = new Unit(Programming, "B") { Name = "Byte", Base = 8 };

        Physics = new UnitCategory("physics");
        Hertz = new Unit(Physics, "Hz");
        Joules = new Unit(Physics, "J");

        Distance = new UnitCategory("distance", Physics);
        Meter = new Unit(Distance, "m") { Name = "Meter" };
        AstronomicalUnit = new Unit(Distance, "Au") { Name = "AstronomicalUnit" };
        LightSecond = new Unit(Distance, "Ls") { Name = "LightSecond" };
        LightYear = new Unit(Distance, "Ly") { Name = "LightYear" };
        Parsec = new Unit(Distance, "pc") { Name = "Parsec" };
        RegisterFactorUnitChain(
            (Meter, 1),
            (LightSecond, 299_792_458),
            (AstronomicalUnit, 499.001_996_008),
            (LightYear, 63_241),
            (Parsec, 3.261_56));

        Electrical = new UnitCategory("electrical", Physics);
        Volts = new Unit(Electrical, "V") { Name = "Volt" };
        Ampere = new Unit(Electrical, "A") { Name = "Ampere" };
        Watts = new Unit(Electrical, "W") { Name = "Watt", Strategies = { ResultOf(Volts, UnitOperator.Multiply, Ampere) } };
        Ohm = new Unit(Electrical, "Oh") { Name = "Ohm", Strategies = { ResultOf(Volts, UnitOperator.Divide, Ampere) } };
        Coulomb = new Unit(Electrical, "C") { Name = "Coulomb" };
        Farad = new Unit(Electrical, "F") { Name = "Farad", Strategies = { ResultOf(Coulomb, UnitOperator.Divide, Volts) } };
        Henry = new Unit(Electrical, "He") { Name = "Henry", Strategies = { ResultOf(Ohm, UnitOperator.Multiply, Seconds) } };

        // complex conversion chains
        RegisterFactorUnitChain(
            (Watts * Seconds, 1),
            (Joules, 1));
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
    public static readonly Unit Joules;

    public static readonly UnitCategory Distance;
    public static readonly Unit Meter;
    public static readonly Unit AstronomicalUnit;
    public static readonly Unit LightSecond;
    public static readonly Unit LightYear;
    public static readonly Unit Parsec;

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

    public static void RegisterFactorUnitChain(params (Unit unit, double factorFromPrevious)[] chain)
    {
        var strategy = new FactorUnitChain(chain.ToImmutableList());
        foreach (var (unit, _) in strategy.chain)
            unit.Strategies.Add(strategy);
    }

    public static UnitAccumulatorStrategy ResultOf(Unit lhs, UnitOperator op, Unit rhs) => new CombinationUnitResolver(op, lhs, rhs);

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
        bool UnitUsed(Unit unit) => unit.Identifier == str || unit.Name == str;

        var unit = IterateUnits().FirstOrDefault(UnitUsed);
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

        return new UnitInstance(unit ?? IterateUnits().FirstOrDefault(UnitUsed) ?? Units.EmptyUnit, si?.Value ?? SiPrefix.One);
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
            if (related.Any(unit.Equals) || related.All(strategy.IsUnitRelated))
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

public class FactorUnitChain : UnitAccumulatorStrategy
{
    private readonly Accumulator accumulator;
    internal readonly IList<(Unit unit, double factorFromPrevious)> chain;

    public FactorUnitChain(IList<(Unit unit, double factorFromPrevious)> chain)
    {
        this.chain = chain;
        accumulator = new Accumulator(this);
    }

    public override bool IsUnitRelated(Unit arg) => chain.Any(x => x.unit == arg);
    public override IEnumerable<UnitAccumulator> CreateAccumulators(Unit unit) => new[] { accumulator };

    private class Accumulator : UnitAccumulator
    {
        private readonly FactorUnitChain _chain;

        public Accumulator(FactorUnitChain chain)
        {
            _chain = chain;
        }

        public override bool Accepts(UnitOperator op, UnitValue lhs, UnitValue rhs)
            => _chain.IsUnitRelated(lhs) && (rhs == Units.EmptyUnit || _chain.IsUnitRelated(rhs));
        public override UnitValue Accumulate(UnitOperator op, UnitValue lhs, UnitValue rhs)
        {
            // denormalize values
            lhs |= SiPrefix.One;
            rhs |= SiPrefix.One;

            int IndexOf(Unit unit)
            {
                for (var i = 0; i < _chain.chain.Count; i++)
                    if (_chain.chain[i].unit == unit)
                        return i;
                var me = IndexOf(lhs);
                if (_chain.chain.Count < me + 1)
                    throw new Exception($"Entry for unit {unit} could not be found in chain {string.Join(", ", _chain)}");
                var it = unit as UnitValue;
                (Unit unit, double factorFromPrevious) next;
                var collectiveFactor = 1d;
                var off = 0;
                do
                {
                    next = _chain.chain[me + off];
                    collectiveFactor *= next.factorFromPrevious;
                } while ((double)it < collectiveFactor && me + --off >= 0);
                return me + --off;
            }

            var li = IndexOf(lhs);
            var ri = IndexOf(rhs);
            var l = (double)lhs;
            var r = (double)rhs;
            if (double.IsNaN(r))
            {
                r = 1;
                for (var i = Math.Min(li, ri) + 1; i < Math.Max(li, ri) + 1; i++)
                    r *= _chain.chain[i].factorFromPrevious;
            }
            return _chain.chain[ri].unit * (li > ri ? l * r : l / r);
        }
    }
}

public class CombinationUnitResolver : UnitAccumulatorStrategy
{
    private readonly Unit _lhs;
    private readonly UnitOperator _op;
    private readonly Unit _rhs;

    public CombinationUnitResolver(UnitOperator op, Unit lhs, Unit rhs)
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

public class CombinationUnitAccumulator : UnitAccumulator
{
    private readonly Unit _lhs;
    private readonly UnitOperator _op;
    private readonly Unit _output;
    private readonly Unit _rhs;

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
        => value * new UnitValue(unit, double.NaN);

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