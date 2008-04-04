////////////////////////////////////////////////////////////////////
//Initial author: Jiri George Moudry, 2006.
//License: LGPL. (Visit http://www.gnu.org)
//Commercial code may call into this library, if it's in a different module (DLL)
////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Text;

namespace DBLinq.util
{
    public class FieldUtils
    {
        /// <summary>
        /// assign 'ID' to a field, handles int/uint/long conversions.
        /// </summary>
        /// <param name="rowObj">the object containing the ID field</param>
        /// <param name="finfo">field info</param>
        /// <param name="id">the ID value</param>
        public static void SetObjectIdField(object rowObj, System.Reflection.FieldInfo finfo, object id)
        {
            if(id is long){
                //prevent {"Object of type 'System.Int64' cannot be converted to type 'System.UInt32'."}
                long longID = (long)id;
                bool assignable1 = finfo.FieldType.IsAssignableFrom(typeof(long));
                bool assignable2 = finfo.FieldType.IsAssignableFrom(typeof(int));
                if(finfo.FieldType==typeof(uint))
                {
                    uint uintID = (uint) (long) id;
                    id = uintID;
                }
                else if(finfo.FieldType==typeof(int))
                {
                    int intID = (int) (long) id;
                    id = intID;
                }
            }
            finfo.SetValue(rowObj, id);
        }

        static readonly object[] emptyIndices = new object[0];

        public static object GetValue(System.Reflection.MemberInfo field, object parentObj)
        {
            System.Reflection.PropertyInfo propInfo = field as System.Reflection.PropertyInfo;
            if (propInfo != null)
            {
                object obj = propInfo.GetValue(parentObj,emptyIndices); //retrieve 'myID' from wrapper class
                return obj;
            }
            else
            {
                System.Reflection.FieldInfo fieldInfo = field as System.Reflection.FieldInfo;
                if (fieldInfo == null)
                {
                    string msg = "L55 Member is neither Property nor Field:" + field;
                    Console.WriteLine(msg);
                    throw new Exception(msg);
                }
                object obj = fieldInfo.GetValue(parentObj);
                return obj;
            }
        }
    }
}