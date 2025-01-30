﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ClickHouse.Ado.Impl.ATG.Insert;
using ClickHouse.Ado.Impl.Data;
using Cysharp.Text;

namespace ClickHouse.Ado.Impl.ColumnTypes;

internal class TupleColumnType : ColumnType {
    private readonly MethodInfo _tupleCreator;

    public TupleColumnType(IEnumerable<ColumnType> values) {
        Columns = values.ToArray();
        _tupleCreator = typeof(Tuple).GetMethods(BindingFlags.Static | BindingFlags.Public).Single(x => x.GetParameters().Length == Columns.Length);
        _tupleCreator = _tupleCreator.MakeGenericMethod(Columns.Select(x => x.CLRType).ToArray());
    }

    public ColumnType[] Columns { get; }

    public override int Rows => Columns.First().Rows;
    internal override Type CLRType => Type.GetType("System.Tuple`" + Columns.Length).MakeGenericType(Columns.Select(x => x.CLRType).ToArray());

    internal override async Task Read(ProtocolFormatter formatter, int rows, CancellationToken cToken) {
        foreach (var column in Columns)
            await column.Read(formatter, rows, cToken);
    }

    public override string AsClickHouseType(ClickHouseTypeUsageIntent usageIntent) => $"Tuple({ZString.Join(",", Columns.Select(x => x.AsClickHouseType(usageIntent)))})";

    public override async Task Write(ProtocolFormatter formatter, int rows, CancellationToken cToken) {
        Debug.Assert(Rows == rows, "Row count mismatch!");
        foreach (var column in Columns) await column.Write(formatter, rows, cToken);
    }

    public override void ValueFromConst(Parser.ValueType val) => throw new NotSupportedException();

    public override void ValueFromParam(ClickHouseParameter parameter) => throw new NotSupportedException();

    public override object Value(int currentRow) => _tupleCreator.Invoke(null, Columns.Select(x => x.Value(currentRow)).ToArray());

    public override long IntValue(int currentRow) => throw new InvalidCastException();

    public override void ValuesFromConst(IEnumerable objects) {
        var t = CLRType;
        var accessors = new PropertyInfo[Columns.Length];
        var data = new List<object>[Columns.Length];
        for (var i = 0; i < Columns.Length; i++) {
            accessors[i] = t.GetProperty("Item" + (i + 1));
            data[i] = new List<object>();
        }

        foreach (var row in objects)
            for (var index = 0; index < accessors.Length; index++)
                data[index].Add(accessors[index].GetValue(row));
        for (var index = 0; index < Columns.Length; index++)
            Columns[index].ValuesFromConst(data[index]);
    }
}