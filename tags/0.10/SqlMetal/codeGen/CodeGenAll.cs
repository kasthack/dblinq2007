////////////////////////////////////////////////////////////////////
//Initial author: Jiri George Moudry, 2006.
//License: LGPL. (Visit http://www.gnu.org)
////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Text;
using SqlMetal.schema;
using SqlMetal.util;

namespace SqlMetal.codeGen
{
    /// <summary>
    /// generates a c# class representing database.
    /// calls into CodeGenClass and CodeGenField.
    /// </summary>
    public class CodeGenAll
    {
        CodeGenClass codeGenClass = new CodeGenClass();

        public string generateAll(DlinqSchema.Database dbSchema, string vendorName)
        {
            if(dbSchema==null || dbSchema.Schemas==null || dbSchema.Schemas.Count==0 || dbSchema.Schemas[0].Tables==null)
            {
                Console.WriteLine("CodeGenAll ERROR: incomplete dbSchema, cannot start generating code");
                return null;
            }

            const string prolog = @"//#########################################################################
//generated by MysqlMetal on $date - extracted from $db.
//#########################################################################

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
#if LINQ_PREVIEW_2006
//Visual Studio 2005 with Linq Preview May 2006 - can run on Win2000
using System.Query;
using System.Data.DLinq;
#else
//Visual Studio Orcas - requires WinXP
using System.Linq;
using System.Data.Linq;
#endif

using DBLinq.Linq;
";

            List<string> classBodies = new List<string>();


            foreach(DlinqSchema.Table tbl in dbSchema.Schemas[0].Tables)
            {
                string classBody = codeGenClass.generateClass(dbSchema, tbl);
                classBodies.Add(classBody);
            }

            string opt_namespace = @"
namespace $ns
{
    $classes
}
";
            string prolog1 = prolog.Replace("$date", DateTime.Now.ToString("yyyy-MMM-dd"));
            string source = mmConfig.server!=null ? "server "+mmConfig.server : "file "+mmConfig.schemaXmlFile;
            //prolog1 = prolog1.Replace("$db", mmConfig.server);
            prolog1 = prolog1.Replace("$db", source);
            string classesConcat = string.Join("\n\n", classBodies.ToArray());
            classesConcat = generateDbClass(dbSchema, vendorName) + "\n\n" + classesConcat;
            string fileBody;
            if(mmConfig.@namespace==null || mmConfig.@namespace==""){
                fileBody = prolog1 + classesConcat;
            } else {
                string body1 = opt_namespace;
                body1 = body1.Replace("$ns", mmConfig.@namespace);
                classesConcat = classesConcat.Replace("\n","\n\t"); //move text one tab to the right
                body1 = body1.Replace("$classes", classesConcat);
                fileBody = prolog1 + body1;
            }
            return fileBody;

        }

        string generateDbClass(DlinqSchema.Database dbSchema, string vendorName)
        {
            #region generateDbClass()
            //if (tables.Count==0)
            //    return "//L69 no tables found";
            if (dbSchema.Schemas.Count==0 || dbSchema.Schemas[0].Tables.Count==0)
                return "//L69 no tables found";

            const string dbClassStr = @"
/// <summary>
/// This class represents $vendor database $dbname.
/// </summary>
public partial class $dbname : MContext
{
    public $dbname(string connStr):base(connStr)
    {
        $fieldInit
    }

    //these fields represent tables in database and are
    //ordered - parent tables first, child tables next. Do not change the order.
    $fieldDecl
}
";
            string dbName = dbSchema.Class;
            if (dbName == null)
            {
                dbName = dbSchema.Name;
            }

            List<string> dbFieldDecls = new List<string>();
            List<string> dbFieldInits = new List<string>();
            foreach(DlinqSchema.Table tbl in dbSchema.Schemas[0].Tables)
            {
                string tableClass = tbl.Class;
                if (tableClass == null)
                    tableClass = tbl.Name;

                //string tableNamePlural = tbl.Name.Capitalize().Pluralize();
                string tableNamePlural = Util.TableNamePlural(tbl.Name);

                string fldDecl = string.Format("public readonly MTable<{1}> {0};"
                    , tableNamePlural, tableClass);
                dbFieldDecls.Add(fldDecl);

                string fldInit = string.Format("{0} = new MTable<{1}>(this);"
                    , tableNamePlural, tableClass);
                dbFieldInits.Add(fldInit);
            }
            string dbFieldInitStr = string.Join("\n\t\t", dbFieldInits.ToArray());
            string dbFieldDeclStr = string.Join("\n\t", dbFieldDecls.ToArray());
            string dbs = dbClassStr;
            dbs = dbs.Replace("$vendor", vendorName);
            dbs = dbs.Replace("$dbname",dbName);
            dbs = dbs.Replace("$fieldInit",dbFieldInitStr);
            dbs = dbs.Replace("$fieldDecl",dbFieldDeclStr);
            return dbs;
            #endregion
        }

    }
}