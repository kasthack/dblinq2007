#region HEADER
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Data.Linq;
using Test_NUnit.Linq_101_Samples;
#endregion

namespace Test_NUnit
{
#region HEADER
    /// <summary>
    /// when a problem crops up in NUnit, you can convert the project from DLL into EXE, 
    /// and debug into the offending method.
    /// </summary>
#endregion
    class Program2
    {
        static void Main()
        {
            //new ReadTest_GroupBy().G01_SimpleGroup_Count();
            //new ReadTest_GroupBy().G08_OrderSumByCustomerID();

            //new ReadTest().C1_SelectProducts();
            //new StoredProcTest().SP3_GetOrderCount_SelField();
            //new ReadTest().D08_Products_Take5();
            //new ReadTest_GroupBy().G01_SimpleGroup_Count();
            //new ReadTest_AllTypes().AT5_SelectEnum_();
            //new ReadTest_Operands().H1_SelectConcat();
            //new ReadTest_GroupBy().G05_Group_Into();
            new ReadTest_Complex().F7_ExplicitJoin();
            //rc.F11_ConcatString();
            //new WriteTest().E2_UpdateEnum();
            //new WriteTest().E2_UpdateEnum();
            //new WriteTest_BulkInsert().BI01_InsertProducts();
            //new NullTest().NullableT_Value();
            //new Count_Sum_Min_Max_Avg().LiqnToSqlCount02();
            //new Top_Bottom().LinqToSqlTop03_Ex_Andrus();
            //new Join().LinqToSqlJoin01();
        }
    }

}
