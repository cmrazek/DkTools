using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.PreprocessorTokens
{
	internal class OperatorToken : Token
	{
		private int _precedence;
		private string _text;

		public OperatorToken(GroupToken parent, string text)
			: base(parent)
		{
			/* From WBDK Platform Help:
			Operators			Type				Associativity	Precedence
			------------------- ------------------- ---------------	-----------
			( ) [ ]									Left to right	100
			-					unary arithmetic	Right to left	91 (odd number means right-to-left)
			* / %				arithmetic			Left to right	80
			+ -					arithmetic			Left to right	70
			< > <= =>			comparison			Left to right	60
			== !=				comparison			Left to right	50
			and					logical				Left to right	40
			or					logical				Left to right	30
			? :					ternary				Left to right	20
			= *= /= %= += -=	assignment			Right to left	11 (odd number means right-to-left)
			*/

			_text = text;

			switch (text)
			{
				case "*":
				case "/":
				case "%":
					_precedence = 80;
					break;
				case "+":
				case "-":
					_precedence = 70;
					break;
				case "<":
				case ">":
				case "<=":
				case ">=":
					_precedence = 60;
					break;
				case "==":
				case "!=":
					_precedence = 80;
					break;
				case "and":
				case "&&":
					_precedence = 40;
					break;
				case "or":
				case "||":
					_precedence = 30;
					break;
				default:
					_precedence = 0;
					break;
			}
		}

		public void Execute()
		{
			_precedence = 0;

			var leftToken = _parent.GetTokenOnLeft(this);
			if (leftToken == null)
			{
				Log.WriteDebug("Operator '{0}' expects token on left.", _text);
				return;
			}
			var left = leftToken.Value;
			if (!left.HasValue)
			{
				Log.WriteDebug("Operator '{0}' expects value on left.", _text);
				return;
			}

			var rightToken = _parent.GetTokenOnRight(this);
			if (rightToken == null)
			{
				Log.WriteDebug("Operator '{0}' expects token on right.", _text);
				return;
			}
			var right = rightToken.Value;
			if (!right.HasValue)
			{
				Log.WriteDebug("Operator '{0}' expects value on right.", _text);
				return;
			}

			switch (_text)
			{
				case "*":
					_parent.ReplaceTokens(new NumberToken(_parent, left.Value * right.Value), leftToken, this, rightToken);
					break;
				case "/":
					if (right.Value == 0)
					{
						Log.WriteDebug("Division by zero.");
						_parent.ReplaceTokens(new NumberToken(_parent, (long?)null), leftToken, this, rightToken);
					}
					else _parent.ReplaceTokens(new NumberToken(_parent, left.Value / right.Value), leftToken, this, rightToken);
					break;
				case "%":
					if (right.Value == 0)
					{
						Log.WriteDebug("Modulus division by zero.");
						_parent.ReplaceTokens(new NumberToken(_parent, (long?)null), leftToken, this, rightToken);
					}
					else _parent.ReplaceTokens(new NumberToken(_parent, left.Value % right.Value), leftToken, this, rightToken);
					break;
				case "+":
					_parent.ReplaceTokens(new NumberToken(_parent, left.Value + right.Value), leftToken, this, rightToken);
					break;
				case "-":
					_parent.ReplaceTokens(new NumberToken(_parent, left.Value - right.Value), leftToken, this, rightToken);
					break;
				case "<":
					_parent.ReplaceTokens(new NumberToken(_parent, left.Value < right.Value ? 1 : 0), leftToken, this, rightToken);
					break;
				case ">":
					_parent.ReplaceTokens(new NumberToken(_parent, left.Value > right.Value ? 1 : 0), leftToken, this, rightToken);
					break;
				case "<=":
					_parent.ReplaceTokens(new NumberToken(_parent, left.Value <= right.Value ? 1 : 0), leftToken, this, rightToken);
					break;
				case ">=":
					_parent.ReplaceTokens(new NumberToken(_parent, left.Value >= right.Value ? 1 : 0), leftToken, this, rightToken);
					break;
				case "==":
					_parent.ReplaceTokens(new NumberToken(_parent, left.Value == right.Value ? 1 : 0), leftToken, this, rightToken);
					break;
				case "!=":
					_parent.ReplaceTokens(new NumberToken(_parent, left.Value != right.Value ? 1 : 0), leftToken, this, rightToken);
					break;
				case "and":
				case "&&":
					_parent.ReplaceTokens(new NumberToken(_parent, (left.Value != 0 && right.Value != 0) ? 1 : 0), leftToken, this, rightToken);
					break;
				case "or":
				case "||":
					_parent.ReplaceTokens(new NumberToken(_parent, (left.Value != 0 || right.Value != 0) ? 1 : 0), leftToken, this, rightToken);
					break;
				default:
					Log.WriteDebug("Unexpected operator '{0}'.", _text);
					break;
			}
		}

		public override long? Value
		{
			// Operators don't have a value of their own, instead they calculate a value.
			get { return null; }
		}

		public int Precedence
		{
			get { return _precedence; }
		}
	}
}
