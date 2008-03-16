#region MIT license
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
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Data.Linq.Mapping;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using DbLinq.Linq.Mapping;
using DbLinq.PostgreSql;
using DbLinq.Util;
using DbLinq.Linq;
using DbLinq.Vendor;
using DbLinq.Linq.Database;

namespace DbLinq.PostgreSql
{
    /// <summary>
    /// PostgreSQL - specific code.
    /// </summary>
    public class PgsqlVendor : Vendor.Implementation.Vendor
    {
        public override string VendorName { get { return "PostgreSql"; } }

        public override IDbDataParameter ProcessPkField(IDbCommand cmd, ProjectionData projData, ColumnAttribute colAtt
                                               , StringBuilder sb, StringBuilder sbValues, StringBuilder sbIdentity, ref int numFieldsAdded)
        {
            ColumnAttribute[] colAttribs = AttribHelper.GetColumnAttribs(projData.type);

            //changing IsPk->IsDbGen after discussion with Andrus:
            //ColumnAttribute idColAttrib = colAttribs.FirstOrDefault(c => c.IsPrimaryKey);
            ColumnAttribute idColAttrib = colAttribs.FirstOrDefault(c => c.IsDbGenerated);

            string idColName = idColAttrib == null ? "ERROR_L93_MissingIdCol" : idColAttrib.Name;
            if (idColAttrib!=null && idColAttrib.Expression != null)
            {
                //sequence name is known, is stored in Expression
                string nextvalExpr = idColAttrib.Expression;                     //eg. "nextval('suppliers_supplierid_seq')"
                string currvalExpr = nextvalExpr.Replace("nextval", "currval");  //eg. "currval('suppliers_supplierid_seq')"
                sbIdentity.Append(";SELECT " + currvalExpr);
            }
            else
            {
                //assume standard format of sequence name
                string sequenceName = projData.tableAttribute.Name + "_" + idColName + "_seq";
                sbIdentity.Append(";SELECT currval('" + sequenceName + "')");
            }

            return null; //we have not created a param object (only Oracle does)
        }

        /// <summary>
        /// Postgres string concatenation, eg 'a||b'
        /// </summary>
        public override string Concat(List<ExpressionAndType> parts)
        {
            //string[] arr = parts.ToArray();
            //return string.Join("||", arr);
            StringBuilder sb = new StringBuilder();
            foreach (ExpressionAndType part in parts)
            {
                if (sb.Length != 0) { sb.Append("||"); }
                if (part.type == typeof(string))
                {
                    sb.Append(part.expression);
                }
                else
                {
                    //integers and friends: must CAST before concatenating
                    sb.Append("CAST(" + part.expression + " AS varchar)");
                }
            }
            return sb.ToString();
        }

        public override string MakeFieldSafeName(string name)
        {
            // --> how is a special field escaped?
            return name;
        }

        protected void SetParameterType(IDbDataParameter parameter, PropertyInfo property, string literal)
        {
            object dbType= Enum.Parse(property.PropertyType, literal);
            property.GetSetMethod().Invoke(parameter, new object[] { dbType });
        }

        protected void SetParameterType(IDbDataParameter parameter, string literal)
        {
            SetParameterType(parameter, parameter.GetType().GetProperty("NpgsqlDbType"), literal);
        }

        public override IDbDataParameter CreateSqlParameter(IDbCommand cmd, string dbTypeName, string paramName)
        {
            IDbDataParameter param = cmd.CreateParameter();
            param.ParameterName = paramName;
            // TODO: remove this hack when a decision is made with bool/bit case
            if (dbTypeName.StartsWith("bit"))
                SetParameterType(param, "Bit");
            return param;
        }

        /*
                public override IDbDataParameter CreateSqlParameter(IDbCommand cmd, string dbTypeName, string paramName)
                {
                    //System.Data.SqlDbType dbType = DbLinq.util.SqlTypeConversions.ParseType(dbTypeName);
                    //SqlParameter param = new SqlParameter(paramName, dbType);
                    NpgsqlTypes.NpgsqlDbType dbType = PgsqlTypeConversions.ParseType(dbTypeName);
                    NpgsqlParameter param = new NpgsqlParameter(paramName, dbType);
                    return param;
                }
        */
        /// <summary>
        /// call mysql stored proc or stored function, 
        /// optionally return DataSet, and collect return params.
        /// </summary>
        public override System.Data.Linq.IExecuteResult ExecuteMethodCall(DbLinq.Linq.DataContext context, MethodInfo method
                                                                 , params object[] inputValues)
        {
            if (method == null)
                throw new ArgumentNullException("L56 Null 'method' parameter");

            //check to make sure there is exactly one [FunctionEx]? that's below.
            FunctionAttribute functionAttrib = GetFunctionAttribute(method);

            ParameterInfo[] paramInfos = method.GetParameters();
            //int numRequiredParams = paramInfos.Count(p => p.IsIn || p.IsRetval);
            //if (numRequiredParams != inputValues.Length)
            //    throw new ArgumentException("L161 Argument count mismatch");

            string sp_name = functionAttrib.Name;

            using (IDbCommand command = context.DatabaseContext.CreateCommand(sp_name))
            {
                //MySqlCommand command = new MySqlCommand("select hello0()");
                int currInputIndex = 0;

                List<string> paramNames = new List<string>();
                for (int i = 0; i < paramInfos.Length; i++)
                {
                    ParameterInfo paramInfo = paramInfos[i];

                    //TODO: check to make sure there is exactly one [Parameter]?
                    ParameterAttribute paramAttrib = paramInfo.GetCustomAttributes(false).OfType<ParameterAttribute>().Single();

                    //string paramName = "?" + paramAttrib.Name; //eg. '?param1' MYSQL
                    string paramName = ":" + paramAttrib.Name; //eg. '?param1' PostgreSQL
                    paramNames.Add(paramName);

                    System.Data.ParameterDirection direction = GetDirection(paramInfo, paramAttrib);
                    //MySqlDbType dbType = MySqlTypeConversions.ParseType(paramAttrib.DbType);
                    IDbDataParameter cmdParam = command.CreateParameter();
                    cmdParam.ParameterName = paramName;
                    //cmdParam.Direction = System.Data.ParameterDirection.Input;
                    if (direction == ParameterDirection.Input || direction == ParameterDirection.InputOutput)
                    {
                        object inputValue = inputValues[currInputIndex++];
                        cmdParam.Value = inputValue;
                    }
                    else
                    {
                        cmdParam.Value = null;
                    }
                    cmdParam.Direction = direction;
                    command.Parameters.Add(cmdParam);
                }

                if (!functionAttrib.IsComposable)
                {
                    //procedures: under the hood, this seems to prepend 'CALL '
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                }
                else
                {
                    //functions: 'SELECT myFunction()' or 'SELECT hello(?s)'
                    string cmdText = "SELECT " + command.CommandText + "($args)";
                    cmdText = cmdText.Replace("$args", string.Join(",", paramNames.ToArray()));
                    command.CommandText = cmdText;
                }

                if (method.ReturnType == typeof(DataSet))
                {
                    //unknown shape of resultset:
                    System.Data.DataSet dataSet = new DataSet();
                    IDbDataAdapter adapter = context.DatabaseContext.CreateDataAdapter();
                    adapter.SelectCommand = command;
                    adapter.Fill(dataSet);
                    List<object> outParamValues = CopyOutParams(paramInfos, command.Parameters);
                    return new ProcResult(dataSet, outParamValues.ToArray());
                }
                else
                {
                    object obj = command.ExecuteScalar();
                    List<object> outParamValues = CopyOutParams(paramInfos, command.Parameters);
                    return new ProcResult(obj, outParamValues.ToArray());
                }
            }
        }

        static ParameterDirection GetDirection(ParameterInfo paramInfo, ParameterAttribute paramAttrib)
        {
            //strange hack to determine what's a ref, out parameter:
            //http://lists.ximian.com/pipermain/mono-list/2003-March/012751.html
            bool hasAmpersand = paramInfo.ParameterType.FullName.Contains('&');
            if (paramInfo.IsOut)
                return ParameterDirection.Output;
            if (hasAmpersand)
                return ParameterDirection.InputOutput;
            return ParameterDirection.Input;
        }

        /// <summary>
        /// Collect all Out or InOut param values, casting them to the correct .net type.
        /// </summary>
        static List<object> CopyOutParams(ParameterInfo[] paramInfos, IDataParameterCollection paramSet)
        {
            List<object> outParamValues = new List<object>();
            //Type type_t = typeof(T);
            int i = -1;
            foreach (IDbDataParameter param in paramSet)
            {
                i++;
                if (param.Direction == ParameterDirection.Input)
                {
                    outParamValues.Add("unused");
                    continue;
                }

                object val = param.Value;
                Type desired_type = paramInfos[i].ParameterType;

                if (desired_type.Name.EndsWith("&"))
                {
                    //for ref and out parameters, we need to tweak ref types, e.g.
                    // "System.Int32&, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
                    string fullName1 = desired_type.AssemblyQualifiedName;
                    string fullName2 = fullName1.Replace("&", "");
                    desired_type = Type.GetType(fullName2);
                }
                try
                {
                    //fi.SetValue(t, val); //fails with 'System.Decimal cannot be converted to Int32'
                    //DbLinq.util.FieldUtils.SetObjectIdField(t, fi, val);
                    object val2 = DbLinq.Util.FieldUtils.CastValue(val, desired_type);
                    outParamValues.Add(val2);
                }
                catch (Exception ex)
                {
                    //fails with 'System.Decimal cannot be converted to Int32'
                    Console.WriteLine("CopyOutParams ERROR L245: failed on CastValue(): " + ex.Message);
                }
            }
            return outParamValues;
        }
/*
        public override IDataReader2 CreateDataReader(IDataReader dataReader)
        {
            return new PgsqlDataReader2(dataReader);
        }*/
    }
}