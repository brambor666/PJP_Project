using System.Globalization;

namespace PJP_Interpreter
{
	public class Program
	{
		static Stack<object> stack = new Stack<object>();
		static Dictionary<string, object> variables = new Dictionary<string, object>();
		static List<string> instructions = new List<string>();
		static int currentLine = 0;
		static Dictionary<int, int> labelMap = new Dictionary<int, int>();

		public static void Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
			instructions = File.ReadAllLines(@"..\..\..\..\PJP_Project\bin\Debug\net8.0\output.txt").ToList();

			for (int i = 0; i < instructions.Count; i++)
			{
				string line = instructions[i].Trim();
				if (line.StartsWith("label "))
				{
					int labelNum = int.Parse(line.Split(' ')[1]);
					labelMap[labelNum] = i;
				}
			}

			while (currentLine < instructions.Count)
			{
				ExecuteInstruction(instructions[currentLine]);
				currentLine++;
			}
		}

		static string FormatValue(object value)
		{
			if (value is double d)
				return d.ToString("0.0##############");
			if (value is bool b)
				return b.ToString().ToLower();
			return value.ToString();
		}
		static void ExecuteInstruction(string line)
		{
			line = line.Trim();
			if (string.IsNullOrEmpty(line)) return;

			string[] parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
			string instruction = parts[0];
			string argument = parts.Length > 1 ? parts[1].Trim() : "";

			switch (instruction)
			{
				case "push":
					{
						string[] pushParts = argument.Split(' ', 2);
						string pushType = pushParts[0];
						string pushValue = pushParts[1];
						switch (pushType)
						{
							case "I": stack.Push(int.Parse(pushValue)); break;
							case "F": stack.Push(double.Parse(pushValue)); break;
							case "B": stack.Push(bool.Parse(pushValue)); break;
							case "S": stack.Push(pushValue.Substring(1, pushValue.Length - 2)); break;
						}
						break;
					}
				case "pop":
					stack.Pop();
					break;

				case "load":
					stack.Push(variables[argument]);
					break;

				case "save":
					variables[argument] = stack.Pop();
					break;

				case "add":
					if (argument == "I") { int b = (int)stack.Pop(); int a = (int)stack.Pop(); stack.Push(a + b); }
					else { double b = (double)stack.Pop(); double a = (double)stack.Pop(); stack.Push(a + b); }
					break;

				case "sub":
					if (argument == "I") { int b = (int)stack.Pop(); int a = (int)stack.Pop(); stack.Push(a - b); }
					else { double b = (double)stack.Pop(); double a = (double)stack.Pop(); stack.Push(a - b); }
					break;

				case "mul":
					if (argument == "I") { int b = (int)stack.Pop(); int a = (int)stack.Pop(); stack.Push(a * b); }
					else { double b = (double)stack.Pop(); double a = (double)stack.Pop(); stack.Push(a * b); }
					break;

				case "div":
					if (argument == "I") { int b = (int)stack.Pop(); int a = (int)stack.Pop(); stack.Push(a / b); }
					else { double b = (double)stack.Pop(); double a = (double)stack.Pop(); stack.Push(a / b); }
					break;

				case "mod":
					{
						int b = (int)stack.Pop(); int a = (int)stack.Pop();
						stack.Push(a % b);
						break;
					}
				case "uminus":
					if (argument == "I") stack.Push(-(int)stack.Pop());
					else stack.Push(-(double)stack.Pop());
					break;

				case "concat":
					{
						string b = (string)stack.Pop(); string a = (string)stack.Pop();
						stack.Push(a + b);
						break;
					}
				case "and":
					{
						bool b = (bool)stack.Pop(); bool a = (bool)stack.Pop();
						stack.Push(a && b);
						break;
					}
				case "or":
					{
						bool b = (bool)stack.Pop(); bool a = (bool)stack.Pop();
						stack.Push(a || b);
						break;
					}
				case "not":
					stack.Push(!(bool)stack.Pop());
					break;

				case "gt":
					if (argument == "I") { int b = (int)stack.Pop(); int a = (int)stack.Pop(); stack.Push(a > b); }
					else { double b = (double)stack.Pop(); double a = (double)stack.Pop(); stack.Push(a > b); }
					break;

				case "lt":
					if (argument == "I") { int b = (int)stack.Pop(); int a = (int)stack.Pop(); stack.Push(a < b); }
					else { double b = (double)stack.Pop(); double a = (double)stack.Pop(); stack.Push(a < b); }
					break;

				case "eq":
					stack.Push(stack.Pop().Equals(stack.Pop()));
					break;

				case "itof":
					stack.Push((double)(int)stack.Pop());
					break;

				case "label":
					break; // already processed

				case "jmp":
					currentLine = labelMap[int.Parse(argument)] - 1;
					break;

				case "fjmp":
					if (!(bool)stack.Pop())
						currentLine = labelMap[int.Parse(argument)] - 1;
					break;

				case "print":
					{
						int n = int.Parse(argument);
						object[] values = new object[n];
						for (int i = n - 1; i >= 0; i--)
							values[i] = stack.Pop();
						foreach (var v in values)
							Console.Write(FormatValue(v));
						Console.WriteLine();
						break;
					}
				case "read":
					{
						string readLine = Console.ReadLine();
						switch (argument)
						{
							case "I": stack.Push(int.Parse(readLine)); break;
							case "F": stack.Push(double.Parse(readLine)); break;
							case "B": stack.Push(bool.Parse(readLine)); break;
							case "S": stack.Push(readLine); break;
						}
						break;
					}
				default:
					Console.WriteLine($"Unknown instruction: {instruction}");
					break;
			}
		}
	}
}