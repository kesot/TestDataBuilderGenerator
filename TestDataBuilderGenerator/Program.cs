using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TestDataBuilderGenerator
{
	class Prop
	{
		public string PropertyModificator { get; set; }
		public string PropertyType { get; set; }
		public string PropertyName { get; set; }

		public Prop(string mod, string typ, string name)
		{
			PropertyModificator = mod;
			PropertyType = typ;
			PropertyName = name;
		}

		public static Prop Parse(string str)
		{
			string[] readedParams;
			readedParams = str.Split(new char[] { ' ' });
			return new Prop(readedParams[0], readedParams[1], readedParams[2].TrimEnd(new char[] { ';' }));
		}
	}

	class TrimStreamReader : StreamReader
	{
		public TrimStreamReader(string name) : base(name)
		{
		}

		public override string ReadLine()
		{
			return base.ReadLine().Trim(new char[] {'\t'});
		}
	}
	class Program
	{
		static void Main(string[] args)
		{
			var projectName = args[0];
			var fileNames = new string[args.Length - 1];
			Array.Copy(args, 1, fileNames, 0, args.Length);

			foreach (var filename in fileNames)
			{
				var sr = new TrimStreamReader(filename);
				var classFields = new List<Prop>();

				string tmp;
				do
				{
					tmp = sr.ReadLine();
				} while (!tmp.StartsWith("public class"));
				tmp = tmp.Replace("public class ", "");
				tmp = tmp.Split(' ')[0];
				var className = tmp;
				tmp = sr.ReadLine();
				while (!tmp.StartsWith("private "))
					tmp = sr.ReadLine();

				while (tmp.StartsWith("private ") || String.IsNullOrWhiteSpace(tmp))
				{
					if (tmp.Contains("EntityRef"))
					{
						tmp = tmp.Replace("EntityRef<", "");
						tmp = tmp.Replace(">", "");
					}
					if (!String.IsNullOrWhiteSpace(tmp) 
						&& !tmp.Contains("id") 
						&& !tmp.Contains("Id") 
						&& !tmp.Contains("Entity")
						&& !tmp.Contains("Binary"))
							classFields.Add(Prop.Parse(tmp));

					tmp = sr.ReadLine();
				}

				var text = new StringBuilder();

				text.Append(
					"using Itc.Commons.Tests.DataBuilders;\n" +
					"using Itc.Commons.Tests.Infrastructure;\n" +
					"using Itc." + projectName + ".Model;\n" +
					"using Itc.DirectCrm.Model;\n\n" +
					"namespace Itc." + projectName + ".Tests\n" +
					"{\n" +
					"\tpublic class " + className + "TestDataBuilder : TestDataBuilder<" + className + ", " + className + "TestDataBuilder>\n" +
					"\t{\n"
					);
				foreach (var prop in classFields)
				{
					text.Append("\t\t" + prop.PropertyModificator + " " + prop.PropertyType + " " + prop.PropertyName + ";\n");
				}
				text.Append(
					"\n\n\t\tpublic " + className + "TestDataBuilder(InMemoryDatabase database)\n" +
					"\t\t\t: base(database)\n" +
					"\t\t{\n" +
					"\t\t}\n\n"
					);

				foreach (var prop in classFields)
				{
					var propertyName = new StringBuilder(prop.PropertyName);
					propertyName[0] = Char.ToUpper(propertyName[0]);
					text.Append(
						"\t\tpublic " + className + "TestDataBuilder With" + propertyName + "(" + prop.PropertyType + " " + prop.PropertyName + "Value" + ")\n" +
						"\t\t{\n" +
						"\t\t\tvar result = Clone();\n" +
						"\t\t\tresult." + prop.PropertyName + " = " + prop.PropertyName + "Value;\n" +
						"\t\t\t\treturn result;\n" +
						"\t\t}\n\n"
						);
				}

				text.Append(
					"\t\tprotected override " + className + " DoBuild()\n" +
					"\t\t{\n" +
					"\t\t\tvar result = new " + className + "\n" +
					"\t\t\t{\n"
					);
				foreach (var prop in classFields)
				{
					var propertyName = new StringBuilder(prop.PropertyName);
					propertyName[0] = Char.ToUpper(propertyName[0]);
					text.Append(
						"\t\t\t\t" + propertyName + " = " + prop.PropertyName + ",\n"
						);
				}
				text.Append(
					"\t\t\t};\n\n" +
					"\t\t\tDatabase.GetTable<" + className + ">().Add(result);\n" +
					"\t\t\treturn result;\n" +
					"\t\t}\n" +
					"\t}\n" +
					"}"
					);

				using (var outfile = new StreamWriter(className + "TestDataBuilder.cs", false))
				{
					outfile.Write(text.ToString());
				}
			}
		}
	}
}
