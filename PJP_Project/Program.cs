using System.Globalization;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace PJP_Project
{
	public class Program
	{
		public static void Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

			var fileName = "PLC_t1.txt";
			Console.WriteLine("Parsing: " + fileName);

			var inputFile = new StreamReader(fileName);
			AntlrInputStream input = new AntlrInputStream(inputFile);
			PLC_exprLexer lexer = new PLC_exprLexer(input);
			CommonTokenStream tokens = new CommonTokenStream(lexer);
			PLC_exprParser parser = new PLC_exprParser(tokens);

			parser.AddErrorListener(new VerboseErrorListener());

			IParseTree tree = parser.program();

			if (parser.NumberOfSyntaxErrors == 0)
			{
				var typeChecker = new TypeCheckingVisitor();
				typeChecker.Visit(tree);

				if (Errors.NumberOfErrors == 0)
				{
					using var outputFile = new StreamWriter("output.txt");
					var generator = new CodeGenerator(outputFile);
					generator.Visit(tree);
				}
				else
				{
					Errors.PrintAndClearErrors();
				}
			}
		}
	}
}
