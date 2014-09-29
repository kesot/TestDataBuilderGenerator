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
		public string Mod { get; set; }
		public string Typ { get; set; }
		public string Name { get; set; }

		public Prop(string mod, string typ, string name)
		{
			Mod = mod;
			Typ = typ;
			Name = name;
		}

		public static Prop Parse(string str)
		{
			string[] readedParams;
			readedParams = str.Split(new char[] { ' ' });
			return new Prop(readedParams[0], readedParams[1], readedParams[2].TrimEnd(new char[] { ';' }));
		}
	}

	class TrimStreamReader: StreamReader
	{
		public TrimStreamReader(string name):base(name)
		{
		}
		public override string ReadLine()
		{
			return base.ReadLine().Trim(new char[] {'\t'});
		}
	}
	class Program
	{
		private static string Name;
		static void Main(string[] args)
		{
			foreach (var filename in args)
			{

				TrimStreamReader sr = new TrimStreamReader(filename);
				List<Prop> classFields = new List<Prop>();
				string tmp;
				do
				{
					tmp = sr.ReadLine();
				} while (!tmp.StartsWith("public class"));
				tmp = tmp.Replace("public class ", "");
				tmp = tmp.Split(' ')[0];
				Name = tmp;
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
				StringBuilder text = new StringBuilder();

				text.Append(
					"using Itc.Commons.Tests.DataBuilders;\n" +
					"using Itc.Commons.Tests.Infrastructure;\n" +
					"using Itc.JtiCpa.Model;\n" +
					"using Itc.DirectCrm.Model;\n\n" +
					"namespace Itc.JtiCpa.Tests\n" +
					"{\n" +
					"\tpublic class " + Name + "TestDataBuilder : TestDataBuilder<" + Name + ", " + Name + "TestDataBuilder>\n" +
					"\t{\n"
					);
				foreach (var prop in classFields)
				{
					text.Append("\t\t" + prop.Mod + " " + prop.Typ + " " + prop.Name + ";\n");
				}
				text.Append(
					"\n\n\t\tpublic " + Name + "TestDataBuilder(InMemoryDatabase database)\n" +
					"\t\t\t: base(database)\n" +
					"\t\t{\n" +
					"\t\t}\n\n"
					);
				foreach (var prop in classFields)
				{
					StringBuilder name = new StringBuilder(prop.Name);
					name[0] = Char.ToUpper(name[0]);
					text.Append(
						"\t\tpublic " + Name + "TestDataBuilder With" + name + "(" + prop.Typ + " " + prop.Name + "Value" + ")\n" +
						"\t\t{\n" +
						"\t\t\tvar result = Clone();\n" +
						"\t\t\tresult." + prop.Name + " = " + prop.Name + "Value;\n" +
						"\t\t\t\treturn result;\n" +
						"\t\t}\n\n"
						);
				}
				text.Append(
					"\t\tprotected override " + Name + " DoBuild()\n" +
					"\t\t{\n" +
					"\t\t\tvar result = new " + Name + "\n" +
					"\t\t\t{\n"
					);
				foreach (var prop in classFields)
				{
					StringBuilder name = new StringBuilder(prop.Name);
					name[0] = Char.ToUpper(name[0]);
					text.Append(
						"\t\t\t\t" + name + " = " + prop.Name + ",\n"
						);
				}
				text.Append(
					"\t\t\t};\n\n" +
					"\t\t\tDatabase.GetTable<" + Name + ">().Add(result);\n" +
					"\t\t\treturn result;\n" +
					"\t\t}\n" +
					"\t}\n" +
					"}"
					);

				using (StreamWriter outfile = new StreamWriter(Name + "TestDataBuilder.cs", false))
				{
					outfile.Write(text.ToString());
				}
			}
		}
	}
}
