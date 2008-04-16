﻿using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.CSharp;
using Microsoft.VisualBasic;

using DbLinq.Schema.Dbml;

namespace DbMetal.Generator
{
	public static class CodeDomGenerator
	{
		/// <summary>
		/// Generates a C# source code wrapper for the database schema objects.
		/// </summary>
		/// <param name="database"></param>
		/// <param name="filename"></param>
		public static void GenerateCSharp(Database database, string filename)
		{
			using (Stream stream = File.Open(filename, FileMode.Create))
			{
				using (StreamWriter writer = new StreamWriter(stream))
				{
					new CSharpCodeProvider().CreateGenerator(writer).GenerateCodeFromNamespace(GenerateCodeDomModel(database), writer, new CodeGeneratorOptions() { BracingStyle = "C" });
				}
			}
		}

		/// <summary>
		/// Generates a Visual Basic source code wrapper for the database schema objects.
		/// </summary>
		/// <param name="database"></param>
		/// <param name="filename"></param>
		public static void GenerateVisualBasic(Database database, string filename)
		{
			using (Stream stream = File.Open(filename, FileMode.Create))
			{
				using (StreamWriter writer = new StreamWriter(stream))
				{
					new VBCodeProvider().CreateGenerator(writer).GenerateCodeFromNamespace(GenerateCodeDomModel(database), writer, new CodeGeneratorOptions() { BracingStyle = "C" });
				}
			}
		}

		static CodeThisReferenceExpression thisReference = new CodeThisReferenceExpression();

		static CodeNamespace GenerateCodeDomModel(Database database)
		{
			CodeNamespace _namespace = new CodeNamespace(database.ContextNamespace);

			_namespace.Imports.Add(new CodeNamespaceImport("System"));
			_namespace.Imports.Add(new CodeNamespaceImport("System.ComponentModel"));
			_namespace.Imports.Add(new CodeNamespaceImport("System.Data"));
			_namespace.Imports.Add(new CodeNamespaceImport("System.Data.Linq.Mapping"));
			_namespace.Imports.Add(new CodeNamespaceImport("System.Diagnostics"));
			_namespace.Imports.Add(new CodeNamespaceImport("DbLinq.Linq"));
			_namespace.Imports.Add(new CodeNamespaceImport("DbLinq.Linq.Mapping"));

			_namespace.Comments.Add(new CodeCommentStatement(GenerateCommentBanner(database)));

			_namespace.Types.Add(GenerateContextClass(database));

			foreach (Table table in database.Tables)
				_namespace.Types.Add(GenerateTableClass(table));
			return _namespace;
		}

        static string GenerateCommentBanner(Database database)
        {
			var result = new StringBuilder();

			// http://www.network-science.de/ascii/
            // http://www.network-science.de/ascii/ascii.php?TEXT=MetalSequel&x=14&y=14&FONT=_all+fonts+with+your+text_&RICH=no&FORM=left&STRE=no&WIDT=80 
            result.Append(
                @"
  ____  _     __  __      _        _ 
 |  _ \| |__ |  \/  | ___| |_ __ _| |
 | | | | '_ \| |\/| |/ _ \ __/ _` | |
 | |_| | |_) | |  | |  __/ || (_| | |
 |____/|_.__/|_|  |_|\___|\__\__,_|_|

");
            result.AppendLine(String.Format(" Auto-generated from {0} on {1}.", database.Name, DateTime.Now));
            result.AppendLine(" Please visit http://linq.to/db for more information.");

			return result.ToString();
        }

		static CodeTypeDeclaration GenerateContextClass(Database database)
		{
			var _class = new CodeTypeDeclaration() { IsClass = true, IsPartial = true, Name = database.Class, TypeAttributes = TypeAttributes.Public };
			
			if (database.BaseType != null)
				_class.BaseTypes.Add(database.BaseType);
			else
				_class.BaseTypes.Add(String.Format("DbLinq.{0}.{0}DataContext", database.Provider));

			// CodeDom does not currently support partial methods.  This will be a problem for VB.  Will probably be fixed in .net 4
			_class.Members.Add(new CodeSnippetTypeMember("\tpartial void OnCreated();"));

			// Implement Constructor
			var constructor = new CodeConstructor() { Attributes = MemberAttributes.Public, Parameters = { new CodeParameterDeclarationExpression("IDbConnection", "connection") }};
			constructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("connection"));
			constructor.Statements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(thisReference, "OnCreated")));
			_class.Members.Add(constructor);

			// todo: override other constructors

			foreach (Table table in database.Tables)
			{
				var tableType = new CodeTypeReference(table.Member);
				var property = new CodeMemberProperty() { Type = new CodeTypeReference("Table", tableType), Name = table.Member, Attributes = MemberAttributes.Public | MemberAttributes.Final };
				property.GetStatements.Add
				(
					new CodeMethodReturnStatement
					(
						new CodeMethodInvokeExpression
						(
							new CodeMethodReferenceExpression(thisReference, "GetTable", tableType)							
						)					
					)
				);
				_class.Members.Add(property);
			}

			return _class;
		}

		static CodeTypeDeclaration GenerateTableClass(Table table)
		{
			var _class = new CodeTypeDeclaration() { IsClass = true, IsPartial = true, Name = table.Member, TypeAttributes = TypeAttributes.Public };
			
			_class.CustomAttributes.Add(new CodeAttributeDeclaration("Table", new CodeAttributeArgument("Name", new CodePrimitiveExpression(table.Name))));

			// Implement Constructor
			var constructor = new CodeConstructor() { Attributes = MemberAttributes.Public };
			constructor.Statements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(thisReference, "OnCreated")));
			_class.Members.Add(constructor);

			// todo: implement INotifyPropertyChanging

			// Implement INotifyPropertyChanged
			_class.BaseTypes.Add(typeof(INotifyPropertyChanged));

			var propertyChangedEvent = new CodeMemberEvent() { Type = new CodeTypeReference(typeof(PropertyChangedEventHandler)), Name = "PropertyChanged", Attributes = MemberAttributes.Public };
			_class.Members.Add(propertyChangedEvent);

			var sendPropertyChangedMethod = new CodeMemberMethod() { Attributes = MemberAttributes.Family, Name = "SendPropertyChanged", Parameters = { new CodeParameterDeclarationExpression(typeof(System.String), "propertyName") } };
			sendPropertyChangedMethod.Statements.Add
			(
				new CodeConditionStatement
				(
					new CodeSnippetExpression(propertyChangedEvent.Name + " != null"), // todo: covert this to CodeBinaryOperatorExpression
					new CodeExpressionStatement
					(
						new CodeMethodInvokeExpression
						(
							new CodeMethodReferenceExpression(thisReference, propertyChangedEvent.Name),
							thisReference,
							new CodeObjectCreateExpression(typeof(PropertyChangedEventArgs), new CodeArgumentReferenceExpression("propertyName"))
						)
					)
				)
			);
			_class.Members.Add(sendPropertyChangedMethod);

			// CodeDom does not currently support partial methods.  This will be a problem for VB.  Will probably be fixed in .net 4
			_class.Members.Add(new CodeSnippetTypeMember("\tpartial void OnCreated();"));

			// todo: add these when the actually get called
			//partial void OnLoaded();
			//partial void OnValidate(System.Data.Linq.ChangeAction action);

			// columns
			foreach (Column column in table.Type.Columns)
			{
				var type = new CodeTypeReference(column.Type);
				var columnMember = column.Member ?? column.Name;

				var field = new CodeMemberField(type, "_" + columnMember);
				if (column.IsDbGenerated)
					field.CustomAttributes.Add(new CodeAttributeDeclaration("AutoGenId"));
				_class.Members.Add(field);
				var fieldReference = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), field.Name);

				// CodeDom does not currently support partial methods.  This will be a problem for VB.  Will probably be fixed in .net 4
				string onChangingPartialMethodName = String.Format("On{0}Changing", columnMember);
				_class.Members.Add(new CodeSnippetTypeMember(String.Format("\tpartial void {0}({1} instance);", onChangingPartialMethodName, column.Type)));
				string onChangedPartialMethodName = String.Format("On{0}Changed", columnMember);
				_class.Members.Add(new CodeSnippetTypeMember(String.Format("\tpartial void {0}();", onChangedPartialMethodName)));

				var property = new CodeMemberProperty();
				property.Type = type;
				property.Name = columnMember;
				property.Attributes = MemberAttributes.Public | MemberAttributes.Final;
				property.CustomAttributes.Add
				(
					new CodeAttributeDeclaration
					(
						"Column",
						new CodeAttributeArgument("Storage", new CodePrimitiveExpression(column.Storage)),
						new CodeAttributeArgument("Name", new CodePrimitiveExpression(column.Name)),
						new CodeAttributeArgument("DbType", new CodePrimitiveExpression(column.DbType)),
						new CodeAttributeArgument("CanBeNull", new CodePrimitiveExpression(column.CanBeNull))
					)
				);
				property.CustomAttributes.Add(new CodeAttributeDeclaration("DebuggerNonUserCode"));
				property.GetStatements.Add(new CodeMethodReturnStatement(fieldReference));
				property.SetStatements.Add
				(
					new CodeConditionStatement
					(
						new CodeSnippetExpression(field.Name + " != value"), // todo: covert this to CodeBinaryOperatorExpression
						new CodeExpressionStatement(new CodeMethodInvokeExpression(thisReference, onChangingPartialMethodName, new CodePropertySetValueReferenceExpression())),
						new CodeAssignStatement(fieldReference, new CodePropertySetValueReferenceExpression()),
						new CodeExpressionStatement(new CodeMethodInvokeExpression(thisReference, sendPropertyChangedMethod.Name, new CodePrimitiveExpression(property.Name))),
						new CodeExpressionStatement(new CodeMethodInvokeExpression(thisReference, onChangedPartialMethodName))
					)
				);
				_class.Members.Add(property);
			}

			// TODO: implement associations

			// TODO: implement functions / procedures

			// TODO: Override Equals and GetHashCode

			return _class;
		}
	}
}