using Antlr4.Runtime.Misc;

namespace PJP_Project
{
	public class CodeGenerator : PLC_exprBaseVisitor<Type>
	{
		private SymbolTable symbolTable = new SymbolTable();
		private StreamWriter writer;
		private int labelCounter = 0;

		public CodeGenerator(StreamWriter writer)
		{
			this.writer = writer;
		}

		private int NewLabel() => labelCounter++;

		private void Emit(string instruction)
		{
			writer.WriteLine(instruction);
		}

		private string TypeCode(Type t)
		{
			if (t == Type.Int) return "I";
			if (t == Type.Float) return "F";
			if (t == Type.Bool) return "B";
			return "S";
		}

		private void EmitPromotion(Type left, Type right)
		{
			if (left == Type.Float && right == Type.Int)
			{
				Emit("itof");
			}
			else if (left == Type.Int && right == Type.Float)
			{
				// right is on top of stack, left is buried under it
				// save right temporarily, promote left, restore right
				string temp = $"__temp_{NewLabel()}";
				Emit($"save {temp}");
				Emit("itof");
				Emit($"load {temp}");
			}
		}


		public override Type VisitProgram([NotNull] PLC_exprParser.ProgramContext context)
		{
			foreach (var statement in context.statement())
				Visit(statement);
			return Type.Error;
		}

		public override Type VisitDeclaration([NotNull] PLC_exprParser.DeclarationContext context)
		{
			Type type;
			switch (context.primitiveType().GetText())
			{
				case "int": type = Type.Int; break;
				case "float": type = Type.Float; break;
				case "bool": type = Type.Bool; break;
				case "string": type = Type.String; break;
				default: type = Type.Error; break;
			}

			foreach (var identifier in context.IDENTIFIER())
			{
				symbolTable.Add(identifier.Symbol, type);
				string typeCode = type == Type.Int ? "I" : type == Type.Float ? "F" : type == Type.Bool ? "B" : "S";
				string defaultValue = type == Type.Int ? "0" : type == Type.Float ? "0.0" : type == Type.Bool ? "false" : "\"\"";
				Emit($"push {typeCode} {defaultValue}");
				Emit($"save {identifier.GetText()}");
			}

			return Type.Error;
		}

		public override Type VisitInt([NotNull] PLC_exprParser.IntContext context)
		{
			Emit($"push I {context.GetText()}");
			return Type.Int;
		}

		public override Type VisitFloat([NotNull] PLC_exprParser.FloatContext context)
		{
			Emit($"push F {context.GetText()}");
			return Type.Float;
		}

		public override Type VisitBoolTrue([NotNull] PLC_exprParser.BoolTrueContext context)
		{
			Emit("push B true");
			return Type.Bool;
		}

		public override Type VisitBoolFalse([NotNull] PLC_exprParser.BoolFalseContext context)
		{
			Emit("push B false");
			return Type.Bool;
		}

		public override Type VisitString([NotNull] PLC_exprParser.StringContext context)
		{
			Emit($"push S {context.GetText()}");
			return Type.String;
		}

		public override Type VisitId([NotNull] PLC_exprParser.IdContext context)
		{
			Emit($"load {context.IDENTIFIER().GetText()}");
			return symbolTable[context.IDENTIFIER().Symbol];
		}

		public override Type VisitParens([NotNull] PLC_exprParser.ParensContext context)
		{
			return Visit(context.expr());
		}

		public override Type VisitAssignment([NotNull] PLC_exprParser.AssignmentContext context)
		{
			Type right = Visit(context.expr());
			Type left = symbolTable[context.IDENTIFIER().Symbol];

			if (left == Type.Float && right == Type.Int)
				Emit("itof");

			Emit($"save {context.IDENTIFIER().GetText()}");
			Emit($"load {context.IDENTIFIER().GetText()}");
			return left;
		}

		public override Type VisitUnaryMinus([NotNull] PLC_exprParser.UnaryMinusContext context)
		{
			Type type = Visit(context.expr());
			Emit($"uminus {TypeCode(type)}");
			return type;
		}

		public override Type VisitNot([NotNull] PLC_exprParser.NotContext context)
		{
			Visit(context.expr());
			Emit("not");
			return Type.Bool;
		}

		public override Type VisitMulDivMod([NotNull] PLC_exprParser.MulDivModContext context)
		{
			Type left = Visit(context.expr()[0]);
			Type right = Visit(context.expr()[1]);

			if (context.op.Text == "%")
			{
				Emit("mod");
				return Type.Int;
			}

			EmitPromotion(left, right);
			Type result = (left == Type.Float || right == Type.Float) ? Type.Float : Type.Int;
			string op = context.op.Text == "*" ? "mul" : "div";
			Emit($"{op} {TypeCode(result)}");
			return result;
		}

		public override Type VisitAddSubConcat([NotNull] PLC_exprParser.AddSubConcatContext context)
		{
			Type left = Visit(context.expr()[0]);
			Type right = Visit(context.expr()[1]);

			if (context.op.Text == ".")
			{
				Emit("concat");
				return Type.String;
			}

			EmitPromotion(left, right);
			Type result = (left == Type.Float || right == Type.Float) ? Type.Float : Type.Int;
			string op = context.op.Text == "+" ? "add" : "sub";
			Emit($"{op} {TypeCode(result)}");
			return result;
		}

		public override Type VisitRelational([NotNull] PLC_exprParser.RelationalContext context)
		{
			Type left = Visit(context.expr()[0]);
			Type right = Visit(context.expr()[1]);

			EmitPromotion(left, right);
			Type operandType = (left == Type.Float || right == Type.Float) ? Type.Float : Type.Int;
			string op = context.op.Text == "<" ? "lt" : "gt";
			Emit($"{op} {TypeCode(operandType)}");
			return Type.Bool;
		}

		public override Type VisitEquality([NotNull] PLC_exprParser.EqualityContext context)
		{
			Type left = Visit(context.expr()[0]);
			Type right = Visit(context.expr()[1]);

			EmitPromotion(left, right);
			Type operandType = (left == Type.Float || right == Type.Float) ? Type.Float : left;
			Emit($"eq {TypeCode(operandType)}");

			if (context.op.Text == "!=" || context.op.Text == "<>")
				Emit("not");

			return Type.Bool;
		}

		public override Type VisitAnd([NotNull] PLC_exprParser.AndContext context)
		{
			Visit(context.expr()[0]);
			Visit(context.expr()[1]);
			Emit("and");
			return Type.Bool;
		}

		public override Type VisitOr([NotNull] PLC_exprParser.OrContext context)
		{
			Visit(context.expr()[0]);
			Visit(context.expr()[1]);
			Emit("or");
			return Type.Bool;
		}

		public override Type VisitPrintExpr([NotNull] PLC_exprParser.PrintExprContext context)
		{
			Visit(context.expr());
			Emit("pop");
			return Type.Error;
		}

		public override Type VisitWrite([NotNull] PLC_exprParser.WriteContext context)
		{
			var exprs = context.expr();
			foreach (var expr in exprs)
				Visit(expr);
			Emit($"print {exprs.Length}");
			return Type.Error;
		}

		public override Type VisitRead([NotNull] PLC_exprParser.ReadContext context)
		{
			foreach (var identifier in context.IDENTIFIER())
			{
				Type type = symbolTable[identifier.Symbol];
				Emit($"read {TypeCode(type)}");
				Emit($"save {identifier.GetText()}");
			}
			return Type.Error;
		}

		public override Type VisitEmpty([NotNull] PLC_exprParser.EmptyContext context)
		{
			return Type.Error;
		}

		public override Type VisitBlock([NotNull] PLC_exprParser.BlockContext context)
		{
			foreach (var statement in context.statement())
				Visit(statement);
			return Type.Error;
		}

		public override Type VisitIfStmt([NotNull] PLC_exprParser.IfStmtContext context)
		{
			int endLabel = NewLabel();

			Visit(context.expr());

			if (context.statement().Length > 1)
			{
				int elseLabel = NewLabel();
				Emit($"fjmp {elseLabel}");
				Visit(context.statement()[0]);
				Emit($"jmp {endLabel}");
				Emit($"label {elseLabel}");
				Visit(context.statement()[1]);
			}
			else
			{
				Emit($"fjmp {endLabel}");
				Visit(context.statement()[0]);
			}

			Emit($"label {endLabel}");
			return Type.Error;
		}

		public override Type VisitWhileStmt([NotNull] PLC_exprParser.WhileStmtContext context)
		{
			int startLabel = NewLabel();
			int endLabel = NewLabel();

			Emit($"label {startLabel}");
			Visit(context.expr());
			Emit($"fjmp {endLabel}");
			Visit(context.statement());
			Emit($"jmp {startLabel}");
			Emit($"label {endLabel}");

			return Type.Error;
		}


		public override Type VisitDoWhileStmt([NotNull] PLC_exprParser.DoWhileStmtContext context)
		{
			int startLabel = NewLabel();
			int endLabel = NewLabel();

			Emit($"label {startLabel}");
			Visit(context.statement());
			Visit(context.expr());
			Emit($"fjmp {endLabel}");
			Emit($"jmp {startLabel}");
			Emit($"label {endLabel}");

			return Type.Error;
		}

		public override Type VisitRepeatStmt([NotNull] PLC_exprParser.RepeatStmtContext context)
		{
			int startLabel = NewLabel();
			int endLabel = NewLabel();

			Emit($"label {startLabel}");
			Visit(context.statement());
			Visit(context.expr());
			Emit("not");
			Emit($"fjmp {endLabel}");
			Emit($"jmp {startLabel}");
			Emit($"label {endLabel}");

			return Type.Error;
		}

		public override Type VisitForLoop([NotNull] PLC_exprParser.ForLoopContext context)
		{
			Visit(context.expr()[0]);
			Emit("pop");


			int startLabel = NewLabel();
			int endLabel = NewLabel();

			Emit($"label {startLabel}");
			Visit(context.expr()[1]);
			Emit($"fjmp {endLabel}");

			Visit(context.statement());
			Visit(context.expr()[2]);
			Emit("pop");
			Emit($"jmp {startLabel}");
			Emit($"label {endLabel}");

			return Type.Error;
		}


		public override Type VisitPower([NotNull] PLC_exprParser.PowerContext context)
		{
			Type left = Visit(context.expr()[0]);
			Type right = Visit(context.expr()[1]);


			EmitPromotion(left, right);
			Type result = (left == Type.Float || right == Type.Float) ? Type.Float : Type.Int;
			Emit($"pow {TypeCode(result)}");
			return result;
		}

		public override Type VisitIncrement([NotNull] PLC_exprParser.IncrementContext context)
		{
			string name = context.IDENTIFIER().GetText();
			Type type = symbolTable[context.IDENTIFIER().Symbol];

			Emit($"load {name}");    // load original value (this is the return value)
			Emit($"load {name}");    // load it again to increment
			Emit($"push I 1");       // push 1
			Emit($"add I");          // add
			Emit($"save {name}");    // save back to variable

			return type;
		}
	}
}
