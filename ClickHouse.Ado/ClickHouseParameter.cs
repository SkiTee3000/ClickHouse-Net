﻿using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using ClickHouse.Ado.Impl;
using Cysharp.Text;

namespace ClickHouse.Ado;

/// <summary>
///     Clickhouse specific command parameter.
/// </summary>
public class ClickHouseParameter : DbParameter, IDbDataParameter {
    /// <inheritdoc />
    public override ParameterDirection Direction { get; set; }

    /// <inheritdoc />
    public override bool IsNullable { get; set; }

    /// <inheritdoc />
    public override string SourceColumn { get; set; }

    /// <inheritdoc />
    public override bool SourceColumnNullMapping { get; set; }

    /// <inheritdoc />
    public override int Size { get; set; }

    /// <inheritdoc />
    public override DbType DbType { get; set; }

    /// <inheritdoc />
    public override string ParameterName { get; set; }

    /// <inheritdoc />
    public override object Value { get; set; }

    ParameterDirection IDataParameter.Direction { get; set; }
    bool IDataParameter.IsNullable => false;
    string IDataParameter.SourceColumn { get; set; }
    DataRowVersion IDataParameter.SourceVersion { get; set; }
    byte IDbDataParameter.Precision { get; set; }
    byte IDbDataParameter.Scale { get; set; }
    int IDbDataParameter.Size { get; set; }

    /// <inheritdoc />
    public override void ResetDbType() => DbType = default;

    private string AsSubstitute(object val) {
        if (DbType == DbType.String || DbType == DbType.AnsiString || DbType == DbType.StringFixedLength || DbType == DbType.AnsiStringFixedLength || (DbType == 0 && val is string))
            if (!(val is string) && val is IEnumerable)
                return ZString.Join(",", ((IEnumerable)val).Cast<object>().Select(AsSubstitute));
            else
                return ProtocolFormatter.EscapeStringValue(val.ToString());
        if (DbType == DbType.DateTime || DbType == DbType.DateTime2 || DbType == DbType.DateTime2 || (DbType == 0 && val is DateTime))
            return $"'{(DateTime)val:yyyy-MM-dd HH:mm:ss}'";
        if (DbType == DbType.Date)
            return $"'{(DateTime)val:yyyy-MM-dd}'";
        if (DbType == DbType.Guid)
            return $"'{(Guid)val}'";
        if (DbType != 0 && DbType != DbType.Object && !(val is string) && val is IEnumerable)
            return ZString.Join(",", ((IEnumerable)val).Cast<object>().Select(AsSubstitute));
        if ((DbType == 0 || DbType == DbType.Object) && !(val is string) && val is IEnumerable)
            return "[" + ZString.Join(",", ((IEnumerable)val).Cast<object>().Select(AsSubstitute)) + "]";

        if (val is IFormattable formattable)
            return formattable.ToString(null, CultureInfo.InvariantCulture);
        return val.ToString();
    }

    internal string AsSubstitute() => AsSubstitute(Value);

    /// <inheritdoc />
    public override string ToString() => $"{ParameterName}({DbType}): {Value}";
}