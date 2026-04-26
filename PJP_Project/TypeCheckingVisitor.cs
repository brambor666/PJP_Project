using Antlr4.Runtime.Misc;

namespace PJP_Project
{
	public class TypeCheckingVisitor : PLC_exprBaseVisitor<Type>
	{
		SymbolTable symbolTable = new SymbolTable();

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
				symbolTable.Add(identifier.Symbol, type);

			return Type.Error;
		}

		public override Type VisitInt([NotNull] PLC_exprParser.IntContext context)
		{
			return Type.Int;
		}

		public override Type VisitFloat([NotNull] PLC_exprParser.FloatContext context)
		{
			return Type.Float;
		}

		public override Type VisitBoolTrue([NotNull] PLC_exprParser.BoolTrueContext context)
		{
			return Type.Bool;
		}

		public override Type VisitBoolFalse([NotNull] PLC_exprParser.BoolFalseContext context)
		{
			return Type.Bool;
		}

		public override Type VisitString([NotNull] PLC_exprParser.StringContext context)
		{
			return Type.String;
		}

		public override Type VisitId([NotNull] PLC_exprParser.IdContext context)
		{
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

			if (left == Type.Error || right == Type.Error)
				return Type.Error;

			if (left == right)
				return left;

			if (left == Type.Float && right == Type.Int)
				return Type.Float;

			Errors.ReportError(context.IDENTIFIER().Symbol,
				$"Cannot assign {right} to variable '{context.IDENTIFIER().GetText()}' of type {left}.");
			return Type.Error;
		}

		public override Type VisitUnaryMinus([NotNull] PLC_exprParser.UnaryMinusContext context)
		{
			Type type = Visit(context.expr());

			if (type == Type.Int) return Type.Int;
			if (type == Type.Float) return Type.Float;

			if (type != Type.Error)
				Errors.ReportError(context.Start,
					$"Unary minus cannot be applied to type {type}.");
			return Type.Error;
		}

		public override Type VisitNot([NotNull] PLC_exprParser.NotContext context)
		{
			Type type = Visit(context.expr());

			if (type == Type.Bool) return Type.Bool;

			if (type != Type.Error)
				Errors.ReportError(context.Start,
					$"Operator '!' can only be applied to bool, got {type}.");
			return Type.Error;
		}

		public override Type VisitMulDivMod([NotNull] PLC_exprParser.MulDivModContext context)
		{
			Type left = Visit(context.expr()[0]);
			Type right = Visit(context.expr()[1]);

			if (left == Type.Error || right == Type.Error)
				return Type.Error;

			if (context.op.Text == "%")
			{
				if (left == Type.Int && right == Type.Int)
					return Type.Int;
				Errors.ReportError(context.op, $"Operator '%' can only be used with integers.");
				return Type.Error;
			}

			if (left == Type.Int && right == Type.Int) return Type.Int;
			if (left == Type.Float && right == Type.Float) return Type.Float;
			if (left == Type.Int && right == Type.Float) return Type.Float;
			if (left == Type.Float && right == Type.Int) return Type.Float;

			Errors.ReportError(context.op, $"Operator '{context.op.Text}' cannot be applied to types {left} and {right}.");
			return Type.Error;
		}

		public override Type VisitAddSubConcat([NotNull] PLC_exprParser.AddSubConcatContext context)
		{
			Type left = Visit(context.expr()[0]);
			Type right = Visit(context.expr()[1]);

			if (left == Type.Error || right == Type.Error)
				return Type.Error;

			if (context.op.Text == ".")
			{
				if (left == Type.String && right == Type.String)
					return Type.String;
				Errors.ReportError(context.op, $"Operator '.' can only be used with strings.");
				return Type.Error;
			}

			if (left == Type.Int && right == Type.Int) return Type.Int;
			if (left == Type.Float && right == Type.Float) return Type.Float;
			if (left == Type.Int && right == Type.Float) return Type.Float;
			if (left == Type.Float && right == Type.Int) return Type.Float;

			Errors.ReportError(context.op, $"Operator '{context.op.Text}' cannot be applied to types {left} and {right}.");
			return Type.Error;
		}

		public override Type VisitRelational([NotNull] PLC_exprParser.RelationalContext context)
		{
			Type left = Visit(context.expr()[0]);
			Type right = Visit(context.expr()[1]);

			if (left == Type.Error || right == Type.Error)
				return Type.Error;

			if (left == Type.Int && right == Type.Int) return Type.Bool;
			if (left == Type.Float && right == Type.Float) return Type.Bool;
			if (left == Type.Int && right == Type.Float) return Type.Bool;
			if (left == Type.Float && right == Type.Int) return Type.Bool;

			Errors.ReportError(context.op, $"Operator '{context.op.Text}' cannot be applied to types {left} and {right}.");
			return Type.Error;
		}

		public override Type VisitEquality([NotNull] PLC_exprParser.EqualityContext context)
		{
			Type left = Visit(context.expr()[0]);
			Type right = Visit(context.expr()[1]);

			if (left == Type.Error || right == Type.Error)
				return Type.Error;

			if (left == right) return Type.Bool;
			if (left == Type.Int && right == Type.Float) return Type.Bool;
			if (left == Type.Float && right == Type.Int) return Type.Bool;

			Errors.ReportError(context.op, $"Operator '{context.op.Text}' cannot be applied to types {left} and {right}.");
			return Type.Error;
		}

		public override Type VisitAnd([NotNull] PLC_exprParser.AndContext context)
		{
			Type left = Visit(context.expr()[0]);
			Type right = Visit(context.expr()[1]);

			if (left == Type.Error || right == Type.Error)
				return Type.Error;

			if (left == Type.Bool && right == Type.Bool)
				return Type.Bool;

			Errors.ReportError(context.Start, $"Operator '&&' can only be applied to bools.");
			return Type.Error;
		}

		public override Type VisitOr([NotNull] PLC_exprParser.OrContext context)
		{
			Type left = Visit(context.expr()[0]);
			Type right = Visit(context.expr()[1]);

			if (left == Type.Error || right == Type.Error)
				return Type.Error;

			if (left == Type.Bool && right == Type.Bool)
				return Type.Bool;

			Errors.ReportError(context.Start, $"Operator '||' can only be applied to bools.");
			return Type.Error;
		}

		public override Type VisitPrintExpr([NotNull] PLC_exprParser.PrintExprContext context)
		{
			Visit(context.expr());
			return Type.Error;
		}

		public override Type VisitWrite([NotNull] PLC_exprParser.WriteContext context)
		{
			foreach (var expr in context.expr())
				Visit(expr);
			return Type.Error;
		}

		public override Type VisitRead([NotNull] PLC_exprParser.ReadContext context)
		{
			foreach (var identifier in context.IDENTIFIER())
			{
				var type = symbolTable[identifier.Symbol];
				if (type == Type.Error)
					Errors.ReportError(identifier.Symbol, $"Variable '{identifier.GetText()}' was not declared.");
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
			Type condition = Visit(context.expr());

			if (condition != Type.Bool && condition != Type.Error)
				Errors.ReportError(context.Start, $"Condition of 'if' must be bool, got {condition}.");

			Visit(context.statement()[0]);

			if (context.statement().Length > 1)
				Visit(context.statement()[1]);

			return Type.Error;
		}

		public override Type VisitWhileStmt([NotNull] PLC_exprParser.WhileStmtContext context)
		{
			Type condition = Visit(context.expr());

			if (condition != Type.Bool && condition != Type.Error)
				Errors.ReportError(context.Start, $"Condition of 'while' must be bool, got {condition}.");

			Visit(context.statement());
			return Type.Error;
		}

		public override Type VisitDoWhileStmt([NotNull] PLC_exprParser.DoWhileStmtContext context)
		{
			Visit(context.statement());
			Type condition = Visit(context.expr());

			if (condition != Type.Bool && condition != Type.Error)
				Errors.ReportError(context.Start, $"Condition of 'while' must be bool, got {condition}.");

			return Type.Error;
		}

		public override Type VisitRepeatStmt([NotNull] PLC_exprParser.RepeatStmtContext context)
		{
			Visit(context.statement());
			Type condition = Visit(context.expr());

			if (condition != Type.Bool && condition != Type.Error)
				Errors.ReportError(context.Start, $"Condition of 'repeat' must be bool, got {condition}.");

			return Type.Error;
		}

		public override Type VisitForLoop([NotNull] PLC_exprParser.ForLoopContext context)
		{

			Type init = Visit(context.expr()[0]);
			Type condition = Visit(context.expr()[1]);
			Type step = Visit(context.expr()[2]);
			Visit(context.statement());

			if (condition != Type.Bool && condition != Type.Error)
				Errors.ReportError(context.Start, $"Condition of 'for' must be bool, got {condition}.");



			return Type.Error;
		}





		public override Type VisitPower([NotNull] PLC_exprParser.PowerContext context)
		{
			Type left = Visit(context.expr()[0]);
			Type right = Visit(context.expr()[1]);

			if (left == Type.Error || right == Type.Error)
				return Type.Error;


			if (left == Type.Int && right == Type.Int) return Type.Int;
			if (left == Type.Float && right == Type.Float) return Type.Float;
			if (left == Type.Int && right == Type.Float) return Type.Float;
			if (left == Type.Float && right == Type.Int) return Type.Float;

			Errors.ReportError(context.Start, $"Operator '**' cannot be applied to types {left} and {right}.");
			return Type.Error;
		}


		public override Type VisitIncrement([NotNull] PLC_exprParser.IncrementContext context)
		{
			Type type = symbolTable[context.IDENTIFIER().Symbol];

			if (type == Type.Error)
				return Type.Error;

			if (type == Type.Int) return Type.Int;


			Errors.ReportError(context.IDENTIFIER().Symbol,
				$"Cannot increment variable '{context.IDENTIFIER().GetText()}' of type {type}.");
			return Type.Error;
		}



	}


}
