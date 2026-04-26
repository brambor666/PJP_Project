using Antlr4.Runtime;

namespace PJP_Project
{
	public class VerboseErrorListener : BaseErrorListener
	{
		public override void SyntaxError(IRecognizer recognizer,
			IToken offendingSymbol, int line, int charPositionInLine,
			string msg, RecognitionException e)
		{
			Console.WriteLine($"Syntax error at {line}:{charPositionInLine} - {msg}");
		}
	}
}
