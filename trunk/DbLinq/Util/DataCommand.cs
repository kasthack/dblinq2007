﻿#region MIT license
////////////////////////////////////////////////////////////////////
// MIT license:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//
// Authors:
//        Jiri George Moudry
////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace DbLinq.Util
{
    public static class DataCommand
    {
        public delegate T ReadDelegate<T>(IDataReader reader);

        public static List<T> Find<T>(IDbConnection conn, string sql, string dbParameterName, string db, ReadDelegate<T> readDelegate)
        {
            using (IDbCommand command = conn.CreateCommand())
            {
                command.CommandText = sql;
                if (dbParameterName != null)
                {
                    IDbDataParameter parameter = command.CreateParameter();
                    parameter.ParameterName = dbParameterName;
                    parameter.Value = db;
                    command.Parameters.Add(parameter);
                }
                using (IDataReader rdr = command.ExecuteReader())
                {
                    List<T> list = new List<T>();
                    while (rdr.Read())
                    {
                        list.Add(readDelegate(rdr));
                    }
                    return list;
                }
            }
        }

        public static List<T> Find<T>(IDbConnection conn, string sql, ReadDelegate<T> readDelegate)
        {
            return Find<T>(conn, sql, null, null, readDelegate);
        }
    }
}